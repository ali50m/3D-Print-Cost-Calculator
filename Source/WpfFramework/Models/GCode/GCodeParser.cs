﻿using HelixToolkit.Wpf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using WpfFramework.Models.GCode.Helper;

namespace WpfFramework.Models.GCode
{
    public class GCodeParser : INotifyPropertyChanged
    {
        #region Events
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Properties
        public GCodeModel Model { get; private set; }

        private int _progresss = 0;
        public int Progress
        {
            get => _progresss;
            set
            {
                if (value == _progresss)
                    return;
                _progresss = value;
                OnPropertyChanged();
            }
        }

        private bool _layerModelGenerated = false;
        public bool LayerModelGenerated
        {
            get => _layerModelGenerated;
            set
            {
                if (value == _layerModelGenerated)
                    return;
                _layerModelGenerated = value;
                OnPropertyChanged();
            }
        }


        private ObservableCollection<ModelVisual3D> _models = new ObservableCollection<ModelVisual3D>();
        public ObservableCollection<ModelVisual3D> _3dModels
        {
            get => _models;
            set
            {
                if (_models != value)
                {
                    _models = value;
                    OnPropertyChanged();
                }
            }
        }
        
        private ObservableCollection<LinesVisual3D> _modelLayers = new ObservableCollection<LinesVisual3D>();
        public ObservableCollection<LinesVisual3D> ModelLayers
        {
            get => _modelLayers;
            private set
            {
                if (_modelLayers != value)
                {
                    _modelLayers = value;
                    OnPropertyChanged();
                }
            }
        }
        private ModelVisual3D _Visual3dModel;
        public ModelVisual3D Visual3dModel
        {
            get => _Visual3dModel;
            private set
            {
                if (_Visual3dModel == value)
                    return;
                _Visual3dModel = value;
                OnPropertyChanged();
            }
        }
        public virtual Dispatcher DispatcherObject
        {
            get;
            protected set;
        }

        #endregion

        #region Variables
        private string Filename;
        private List<string> _lines = new List<string>();
        public List<string> Lines
        {
            get => _lines;
            private set
            {
                if (_lines == value)
                    return;
                _lines = value;
                OnPropertyChanged();
            }
        }

        private List<string> Gcode;

        private LinesVisual3D normalmoves = new LinesVisual3D();
        private LinesVisual3D rapidmoves = new LinesVisual3D();
        private LinesVisual3D wirebox = new LinesVisual3D();
        #endregion

        #region Constructor
        public GCodeParser(string filename)
        {
            DispatcherObject = Dispatcher.CurrentDispatcher;
            _3dModels.CollectionChanged += _3dModels_CollectionChanged;
            ModelLayers.CollectionChanged += ModelLayers_CollectionChanged;

            var lights = new ModelVisual3D();
            lights.Children.Add(new SunLight());
            _3dModels.Add(lights);

            Model = new GCodeModel();
            Filename = filename;

            ReadFile();
        }

        private void ModelLayers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(ModelLayers));
        }

        private void _3dModels_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(_3dModels));
        }
        #endregion

        #region Public Methods
        public void Parse()
        {
            DoParse();
            Analyze();
        }
        public async Task<bool> ParseAsync(IProgress<int> prog = null)
        {
            try
            {
                var read = await ReadFileAsync();
                var progress = prog;
                if (read)
                {
                    var parse = await DoParseAsync();
                    if (parse)
                        await Task.Run(() => AnalyzeAsync(progress));
                }
                return true;
            }
            catch (Exception exc)
            {
                return false;
            }
        }
        /*
        public async Task<bool> Create3dGcodeModelAsync(IProgress<int> prog = null)
        {
            try
            {
                var viewport = new ModelVisual3D();
                bool inch = false;
                GCodeCommand lastPosition = new GCodeCommand() {
                    X = 0,
                    Y = 0,
                    Z = 0,
                };

                foreach (List<GCodeCommand> commands in Model.Commands)
                {
                    if (commands.Count == 0)
                    {
                        continue;
                    }

                    foreach (GCodeCommand command in commands)
                    {
                        if (command.NoMove)
                            continue;

                        double factor = (inch ? 25.4 : 1);

                        double x_pos_old = Convert.ToDouble(lastPosition.X);
                        double y_pos_old = Convert.ToDouble(lastPosition.Y);
                        double z_pos_old = Convert.ToDouble(lastPosition.Z);
                        double x_pos = Convert.ToDouble(((!float.IsInfinity(command.X) && (!float.IsNaN(command.X)) ? command.X * factor : lastPosition.X)));
                        double y_pos = Convert.ToDouble(((!float.IsInfinity(command.Y) && (!float.IsNaN(command.Y)) ? command.Y * factor : lastPosition.Y)));
                        double z_pos = Convert.ToDouble(((!float.IsInfinity(command.Z) && (!float.IsNaN(command.Z)) ? command.Z * factor : lastPosition.Z)));
                        
                        //double i_pos = Convert.ToDouble((positions.i_pos.HasValue ? positions.i_pos * factor : Double.NaN));
                        //double j_pos = Convert.ToDouble((positions.j_pos.HasValue ? positions.j_pos * factor : Double.NaN));

                        // G0
                        if(command.Command == "g0")
                        {
                            DrawLine(rapidmoves, x_pos_old, y_pos_old, z_pos_old, x_pos, y_pos, z_pos);
                        }
                        // G1
                        if(command.Command == "g1")
                        {
                            DrawLine(normalmoves, x_pos_old, y_pos_old, z_pos_old, x_pos, y_pos, z_pos);
                        }
                        // G2 || G3
                        if (command.Command == "g2" || command.Command == "g3") // G2 or G3 > draw an arc
                        {
                            bool clockwise = false;
                            // G2
                            if (command.Command == "g2")
                                clockwise = true;

                            DrawArc(x_pos_old, y_pos_old, z_pos_old, x_pos, y_pos, z_pos, 0, 0, false, clockwise);
                        }

                        if (!float.IsInfinity(command.X) && !float.IsNaN(command.X))
                            lastPosition.X = command.X;
                        if (!float.IsInfinity(command.Y) && !float.IsNaN(command.Y))
                            lastPosition.Y = command.Y;
                        if (!float.IsInfinity(command.Z) && !float.IsNaN(command.Z))
                            lastPosition.Z = command.Z;
                    }

                }
                rapidmoves.Thickness = 1;
                rapidmoves.Color = Colors.Blue;
                viewport.Children.Add(rapidmoves);
                normalmoves.Thickness = 1;
                normalmoves.Color = Colors.Black;
                viewport.Children.Add(normalmoves);

                // Rectancle bottom
                DrawLine(wirebox, 0, 0, 0, Model.Width, 0, 0);
                DrawLine(wirebox, 0, 0, 0, 0, Model.Depth, 0);
                DrawLine(wirebox, Model.Width, 0, 0, Model.Width, Model.Depth, 0);
                DrawLine(wirebox, 0, Model.Depth, 0, Model.Width, Model.Depth, 0);

                // Rectancle top
                DrawLine(wirebox, 0, 0, Model.Height, Model.Width, 0, Model.Height);
                DrawLine(wirebox, 0, 0, Model.Height, 0, Model.Depth, Model.Height);
                DrawLine(wirebox, Model.Width, 0, Model.Height, Model.Width, Model.Depth, Model.Height);
                DrawLine(wirebox, 0, Model.Depth, Model.Height, Model.Width, Model.Depth, Model.Height);

                // Rectancle stege
                DrawLine(wirebox, 0, 0, 0, 0, 0, Model.Height);
                DrawLine(wirebox, Model.Width, 0, 0, Model.Width, 0, Model.Height);
                DrawLine(wirebox, Model.Width, Model.Depth, 0, Model.Width, Model.Depth, Model.Height);
                DrawLine(wirebox, 0, Model.Depth, 0, 0, Model.Depth, Model.Height);

                wirebox.Thickness = 1;
                wirebox.Color = Colors.LightGray;
                viewport.Children.Add(wirebox);

                return true;
            }
            catch (Exception exc)
            {
                return false;
            }
        }
       */
        public async Task<bool> Create3dGcodeModelParallelAsync(IProgress<int> prog = null)
        {
            try
            {
                var viewport = new ModelVisual3D();
                bool inch = false;
                GCodeCommand lastPosition = new GCodeCommand() {
                    X = 0,
                    Y = 0,
                    Z = 0,
                };
                int i = 0;
                Parallel.ForEach(Model.Commands, commands =>
                {
                    Parallel.ForEach(commands, command =>
                    {
                        if (command.NoMove)
                            return;

                        double factor = (inch ? 25.4 : 1);

                        double x_pos_old = Convert.ToDouble(lastPosition.X);
                        double y_pos_old = Convert.ToDouble(lastPosition.Y);
                        double z_pos_old = Convert.ToDouble(lastPosition.Z);
                        double x_pos = Convert.ToDouble(((!float.IsInfinity(command.X) && (!float.IsNaN(command.X)) ? command.X * factor : lastPosition.X)));
                        double y_pos = Convert.ToDouble(((!float.IsInfinity(command.Y) && (!float.IsNaN(command.Y)) ? command.Y * factor : lastPosition.Y)));
                        double z_pos = Convert.ToDouble(((!float.IsInfinity(command.Z) && (!float.IsNaN(command.Z)) ? command.Z * factor : lastPosition.Z)));

                        //double i_pos = Convert.ToDouble((positions.i_pos.HasValue ? positions.i_pos * factor : Double.NaN));
                        //double j_pos = Convert.ToDouble((positions.j_pos.HasValue ? positions.j_pos * factor : Double.NaN));

                        // G0
                        if (command.Command == "g0")
                        {
                            DispatcherObject.Invoke(new Action(() => DrawLine(rapidmoves, x_pos_old, y_pos_old, z_pos_old, x_pos, y_pos, z_pos)));
                        }
                        // G1
                        if (command.Command == "g1")
                        {
                            DispatcherObject.Invoke(new Action(() => DrawLine(normalmoves, x_pos_old, y_pos_old, z_pos_old, x_pos, y_pos, z_pos)));
                        }
                        // G2 || G3
                        if (command.Command == "g2" || command.Command == "g3") // G2 or G3 > draw an arc
                        {
                            bool clockwise = false;
                            // G2
                            if (command.Command == "g2")
                                clockwise = true;
                            DispatcherObject.Invoke(new Action(() => DrawArc(x_pos_old, y_pos_old, z_pos_old, x_pos, y_pos, z_pos, 0, 0, false, clockwise)));
                            
                        }

                        if (!float.IsInfinity(command.X) && !float.IsNaN(command.X))
                            lastPosition.X = command.X;
                        if (!float.IsInfinity(command.Y) && !float.IsNaN(command.Y))
                            lastPosition.Y = command.Y;
                        if (!float.IsInfinity(command.Z) && !float.IsNaN(command.Z))
                            lastPosition.Z = command.Z;
                    });
                    i++;
                });

                var query = from cmdId in Enumerable.Range(0, Model.Commands.Count)
                            from command in Model.Commands[cmdId]
                            select command
                            ;
                var result = query.AsParallel().Select(command =>
                {
                    if (command.NoMove)
                        return false;

                    double factor = (inch ? 25.4 : 1);

                    double x_pos_old = Convert.ToDouble(lastPosition.X);
                    double y_pos_old = Convert.ToDouble(lastPosition.Y);
                    double z_pos_old = Convert.ToDouble(lastPosition.Z);
                    double x_pos = Convert.ToDouble(((!float.IsInfinity(command.X) && (!float.IsNaN(command.X)) ? command.X * factor : lastPosition.X)));
                    double y_pos = Convert.ToDouble(((!float.IsInfinity(command.Y) && (!float.IsNaN(command.Y)) ? command.Y * factor : lastPosition.Y)));
                    double z_pos = Convert.ToDouble(((!float.IsInfinity(command.Z) && (!float.IsNaN(command.Z)) ? command.Z * factor : lastPosition.Z)));

                    //double i_pos = Convert.ToDouble((positions.i_pos.HasValue ? positions.i_pos * factor : Double.NaN));
                    //double j_pos = Convert.ToDouble((positions.j_pos.HasValue ? positions.j_pos * factor : Double.NaN));

                    // G0
                    if (command.Command == "g0")
                    {
                        DrawLine(rapidmoves, x_pos_old, y_pos_old, z_pos_old, x_pos, y_pos, z_pos);
                    }
                    // G1
                    if (command.Command == "g1")
                    {
                        DrawLine(normalmoves, x_pos_old, y_pos_old, z_pos_old, x_pos, y_pos, z_pos);
                    }
                    // G2 || G3
                    if (command.Command == "g2" || command.Command == "g3") // G2 or G3 > draw an arc
                    {
                        bool clockwise = false;
                        // G2
                        if (command.Command == "g2")
                            clockwise = true;

                        DrawArc(x_pos_old, y_pos_old, z_pos_old, x_pos, y_pos, z_pos, 0, 0, false, clockwise);
                    }

                    if (!float.IsInfinity(command.X) && !float.IsNaN(command.X))
                        lastPosition.X = command.X;
                    if (!float.IsInfinity(command.Y) && !float.IsNaN(command.Y))
                        lastPosition.Y = command.Y;
                    if (!float.IsInfinity(command.Z) && !float.IsNaN(command.Z))
                        lastPosition.Z = command.Z;
                    return true;
                });


                foreach (List<GCodeCommand> commands in Model.Commands)
                {
                    if (commands.Count == 0)
                    {
                        continue;
                    }

                    foreach (GCodeCommand command in commands)
                    {
                        if (command.NoMove)
                            continue;

                        double factor = (inch ? 25.4 : 1);

                        double x_pos_old = Convert.ToDouble(lastPosition.X);
                        double y_pos_old = Convert.ToDouble(lastPosition.Y);
                        double z_pos_old = Convert.ToDouble(lastPosition.Z);
                        double x_pos = Convert.ToDouble(((!float.IsInfinity(command.X) && (!float.IsNaN(command.X)) ? command.X * factor : lastPosition.X)));
                        double y_pos = Convert.ToDouble(((!float.IsInfinity(command.Y) && (!float.IsNaN(command.Y)) ? command.Y * factor : lastPosition.Y)));
                        double z_pos = Convert.ToDouble(((!float.IsInfinity(command.Z) && (!float.IsNaN(command.Z)) ? command.Z * factor : lastPosition.Z)));
                        
                        //double i_pos = Convert.ToDouble((positions.i_pos.HasValue ? positions.i_pos * factor : Double.NaN));
                        //double j_pos = Convert.ToDouble((positions.j_pos.HasValue ? positions.j_pos * factor : Double.NaN));

                        // G0
                        if(command.Command == "g0")
                        {
                            DrawLine(rapidmoves, x_pos_old, y_pos_old, z_pos_old, x_pos, y_pos, z_pos);
                        }
                        // G1
                        if(command.Command == "g1")
                        {
                            DrawLine(normalmoves, x_pos_old, y_pos_old, z_pos_old, x_pos, y_pos, z_pos);
                        }
                        // G2 || G3
                        if (command.Command == "g2" || command.Command == "g3") // G2 or G3 > draw an arc
                        {
                            bool clockwise = false;
                            // G2
                            if (command.Command == "g2")
                                clockwise = true;

                            DrawArc(x_pos_old, y_pos_old, z_pos_old, x_pos, y_pos, z_pos, 0, 0, false, clockwise);
                        }

                        if (!float.IsInfinity(command.X) && !float.IsNaN(command.X))
                            lastPosition.X = command.X;
                        if (!float.IsInfinity(command.Y) && !float.IsNaN(command.Y))
                            lastPosition.Y = command.Y;
                        if (!float.IsInfinity(command.Z) && !float.IsNaN(command.Z))
                            lastPosition.Z = command.Z;
                    }

                }
                rapidmoves.Thickness = 1;
                rapidmoves.Color = Colors.Blue;
                viewport.Children.Add(rapidmoves);
                normalmoves.Thickness = 1;
                normalmoves.Color = Colors.Black;
                viewport.Children.Add(normalmoves);

                // Rectancle bottom
                DrawLine(wirebox, 0, 0, 0, Model.Width, 0, 0);
                DrawLine(wirebox, 0, 0, 0, 0, Model.Depth, 0);
                DrawLine(wirebox, Model.Width, 0, 0, Model.Width, Model.Depth, 0);
                DrawLine(wirebox, 0, Model.Depth, 0, Model.Width, Model.Depth, 0);

                // Rectancle top
                DrawLine(wirebox, 0, 0, Model.Height, Model.Width, 0, Model.Height);
                DrawLine(wirebox, 0, 0, Model.Height, 0, Model.Depth, Model.Height);
                DrawLine(wirebox, Model.Width, 0, Model.Height, Model.Width, Model.Depth, Model.Height);
                DrawLine(wirebox, 0, Model.Depth, Model.Height, Model.Width, Model.Depth, Model.Height);

                // Rectancle stege
                DrawLine(wirebox, 0, 0, 0, 0, 0, Model.Height);
                DrawLine(wirebox, Model.Width, 0, 0, Model.Width, 0, Model.Height);
                DrawLine(wirebox, Model.Width, Model.Depth, 0, Model.Width, Model.Depth, Model.Height);
                DrawLine(wirebox, 0, Model.Depth, 0, 0, Model.Depth, Model.Height);

                wirebox.Thickness = 1;
                wirebox.Color = Colors.LightGray;
                viewport.Children.Add(wirebox);

                return true;
            }
            catch (Exception exc)
            {
                return false;
            }
        }

        public async Task<List<LinesVisual3D>> Create2dGcodeLayerModelListAsync(IProgress<int> prog = null)
        {
            var list = new List<LinesVisual3D>();
            var line = new LinesVisual3D();
            try
            {
                await Task.Factory.StartNew(delegate () 
                {
                    var temp = new List<LinesVisual3D>();
                    int i = 0;
                    foreach (List<GCodeCommand> commands in Model.Commands)
                    {

                        var pointsPerLayer = getLayerPointsCollection(i, 0, Model.Commands[i].Count, false);
                        if (DispatcherObject.Thread != Thread.CurrentThread)
                        {
                            DispatcherObject.Invoke(new Action(() =>
                            {
                                line = new LinesVisual3D() { Points = new Point3DCollection(pointsPerLayer)};
                                list.Add(line);
                            }));
                        }

                        if (prog != null)
                        {
                            float test = (((float)i / Model.Commands.Count) * 100f);
                            if (i < Model.Commands.Count - 1)
                                prog.Report(Convert.ToInt32(test));
                            else
                                prog.Report(100);
                        }
                        i++;
                    }
                });
                LayerModelGenerated = true;
                return list;
               
            }
            catch (Exception exc)
            {
                return list;
            }
        }
        public async Task<List<LinesVisual3D>> Create3dGcodeLayerModelListAsync(IProgress<int> prog = null)
        {
            var list = new List<LinesVisual3D>();
            buildModelIteratively();
            var line = new LinesVisual3D();
            try
            {
                await Task.Factory.StartNew(delegate () 
                {
                    var temp = new List<LinesVisual3D>();
                    List<Point3D> points = get3dModel();

                    DispatcherObject.Invoke(new Action(() =>
                    {
                        line = new LinesVisual3D() { Points = new Point3DCollection(points) };
                        list.Add(line);
                    }));
                });
                LayerModelGenerated = true;
                return list;
               
            }
            catch (Exception exc)
            {
                return list;
            }
        }
        /*
        public async Task<LinesVisual3D> Create2dGcodeLayerAsync(IProgress<int> prog = null)
        {
            var temp = new LinesVisual3D();
            try
            {
                
                await Task.Factory.StartNew(delegate ()
                {
                    if (DispatcherObject.Thread != Thread.CurrentThread)
                        DispatcherObject.BeginInvoke(new Action(() => temp = drawLayer(3, 0, Model.Commands[3].Count, false)));
                    //temp = drawLayer(3, 0, Model.Commands[3].Count, false);
                });
                LayerModelGenerated = true;
                return temp;

            }
            catch (Exception exc)
            {
                return temp;
            }
        }
        */
        /*
        public async Task<LinesVisual3D> Get2dGcodeLayerAsync(int LayerNumber)
        {
            var lines = new LinesVisual3D();
            try
            {
                if (LayerModelGenerated)
                {
                    if (LayerNumber < ModelLayers.Count)
                    {
                        lines = ModelLayers[LayerNumber];
                    }
                    else if (LayerNumber < Model.Commands.Count)
                    {
                        lines = await drawLayerAsync(LayerNumber, 0, Model.Commands[LayerNumber].Count, false);
                    }
                }
                else if (LayerNumber < Model.Commands.Count)
                {
                    lines = await drawLayerAsync(LayerNumber, 0, Model.Commands[LayerNumber].Count, false);
                }
            }
            catch(Exception exc)
            {

            }
            return lines;
        }
        */
        #endregion

        #region Private Methods
        private void ReadFile()
        {
            Lines = new List<string>();
            Gcode = new List<string>();

            using (StreamReader reader = new StreamReader(Filename))
            {
                Lines = reader.ReadToEnd().Split(new string[] { "\n" }, StringSplitOptions.None).ToList();
                string firstLine = Lines[0];
                if (firstLine.Contains("Slic3r"))
                {
                    Model.Slicer = "Slic3r";
                }
                else if (firstLine.Contains("KISSlicer"))
                {
                    Model.Slicer = "KISSlicer";
                }
                else if (firstLine.Contains("skeinforge"))
                {
                    Model.Slicer = "skeinforge";
                }
                else if (firstLine.Contains("CURA_PROFILE_STRING"))
                {
                    Model.Slicer = "Cura";
                }
                else if (firstLine.Contains("Miracle"))
                {
                    Model.Slicer = "Makerbot";
                }
                foreach (string line in Lines)
                {
                    if (Regex.IsMatch(line, @"/^(G0|G1|G90|G91|G92|M82|M83|G28)/i"))
                    {
                        Gcode.Add(line);
                    }
                }
            }
        }
        private async Task<bool> ReadFileAsync()
        {
            try
            {
                Lines = new List<string>();
                Gcode = new List<string>();

                using (StreamReader reader = new StreamReader(Filename))
                {
                    string lines = await reader.ReadToEndAsync();
                    Lines = lines.Split(new string[] { "\n" }, StringSplitOptions.None).ToList();
                    string firstLine = Lines[0];
                    if (firstLine.Contains("Slic3r"))
                    {
                        Model.Slicer = "Slic3r";
                    }
                    else if (firstLine.Contains("KISSlicer"))
                    {
                        Model.Slicer = "KISSlicer";
                    }
                    else if (firstLine.Contains("skeinforge"))
                    {
                        Model.Slicer = "skeinforge";
                    }
                    else if (firstLine.Contains("CURA_PROFILE_STRING"))
                    {
                        Model.Slicer = "Cura";
                    }
                    else if (firstLine.Contains("Miracle"))
                    {
                        Model.Slicer = "Makerbot";
                    }
                    else if (firstLine.Contains("ideaMaker"))
                    {
                        Model.Slicer = "ideaMaker";
                    }
                    foreach (string line in Lines)
                    {
                        if (Regex.IsMatch(line, @"/^(G0|G1|G90|G91|G92|M82|M83|G28)/i"))
                        {
                            Gcode.Add(line);
                        }
                    }
                    return true;
                }
            }
            catch (Exception exc)
            {
                return false;
            }
        }

        private void DoParse()
        {
            int layer = 0;
            float X = 0;
            float Y = 0;
            float Z = 0;
            float previousX = float.NegativeInfinity;
            float previousY = float.NegativeInfinity;
            float previousZ = float.NegativeInfinity;
            float numSlice = 0;

            bool previousG90Found = false;
            bool extruding = false;
            bool extrudeRelative = false;
            char extruder;

            string sExtruder = "";
            int retract = 0;

            float extrusion;
            Dictionary<string, float> previousExtrusion = new Dictionary<string, float>()
            {
                {"a", 0 },
                {"b", 0 },
                {"c", 0 },
                {"e", 0 },
                {"abs", 0 },
            };
            float retraction;
            Dictionary<string, float> previousRetraction = new Dictionary<string, float>()
            {
                {"a", 0 },
                {"b", 0 },
                {"c", 0 },
                {"e", 0 },
            };
            // "Last" represents the last time the print head speed was requested to be changed
            float lastSpeed = 4000;

            bool dcExtrude = false;
            bool assumeNonDC = false;

            float volumePerMM = 0;

            bool shouldSaveCommand = false;
            // 78964
            for (int i = 0; i < Lines.Count; i++)
            {
                if (i == 28)
                {

                }
                string gcode = Lines[i];
                var test = Regex.Split(gcode, @"\;");
                gcode = Regex.Split(gcode, @"\;")[0].TrimEnd();
                //string[] args = gcode.ToLower().TrimEnd().Split(' ');
                string[] args = gcode.ToLower().TrimEnd().Split(' ');

                X = float.NegativeInfinity;
                Y = float.NegativeInfinity;
                Z = float.NegativeInfinity;
                volumePerMM = 0;
                retract = 0;

                extruding = false;
                sExtruder = "";
                previousExtrusion["abs"] = 0;

                shouldSaveCommand = false;

                if (args[0] == "g0" || args[0] == "g1")
                {
                    for (int j = 1; j < args.Length; j++)
                    {
                        string arg = args[j];
                        if (string.IsNullOrEmpty(arg))
                            continue;
                        switch (arg[0])
                        {
                            case 'x':
                                X = float.Parse(arg.TrimStart(arg[0]), CultureInfo.InvariantCulture);
                                break;
                            case 'y':
                                Y = float.Parse(arg.TrimStart(arg[0]), CultureInfo.InvariantCulture);
                                break;
                            case 'z':
                                Z = float.Parse(arg.TrimStart(arg[0]), CultureInfo.InvariantCulture);
                                if (Z == previousZ)
                                {
                                    continue;
                                }
                                if (Model.zHeights.ContainsKey(Z))
                                {
                                    layer = Model.zHeights[Z];
                                }
                                else
                                {
                                    layer = Model.Commands.Count;
                                    Model.zHeights[Z] = layer;
                                }
                                previousZ = Z;
                                break;
                            case 'e':
                            case 'a':
                            case 'b':
                            case 'c':
                                assumeNonDC = true;
                                // These 4 cases appear to map to different extruders
                                extruder = arg[0];
                                sExtruder = arg[0].ToString();
                                var t = arg.TrimStart(arg[0]);
                                extrusion = float.Parse(arg.TrimStart(arg[0]), CultureInfo.InvariantCulture);
                                if (!extrudeRelative)
                                {
                                    // Absolute positioning
                                    previousExtrusion["abs"] = extrusion - previousExtrusion[sExtruder];
                                }
                                else
                                {
                                    previousExtrusion["abs"] = extrusion;
                                }

                                extruding = previousExtrusion["abs"] > 0;

                                if (previousExtrusion["abs"] < 0)
                                {
                                    //We're retracting...
                                    previousRetraction[sExtruder] = -1;
                                    retract = -1;
                                }
                                else if (previousExtrusion["abs"] == 0)
                                {
                                    retract = 0;
                                }
                                else if (previousExtrusion["abs"] > 0 && previousRetraction[sExtruder] < 0)
                                {
                                    previousRetraction[sExtruder] = 0;
                                    retract = 1;
                                }
                                else
                                {
                                    retract = 0;
                                }

                                //previousExtrusion[sExtruder] = extrusion;
                                previousExtrusion[sExtruder] = extrusion;
                                break;
                            case 'f':
                                extrusion = float.Parse(arg.TrimStart(arg[0]), CultureInfo.InvariantCulture);
                                lastSpeed = extrusion;
                                break;
                            default:
                                break;
                        }

                    }

                    if (dcExtrude && !assumeNonDC)
                    {
                        extruding = true;
                        previousExtrusion["abs"] = (float)Math.Sqrt((previousX - X) * (previousX - X) + (previousY - Y) * (previousY - Y));
                    }

                    if (extruding && retract == 0)
                    {

                        // 
                        volumePerMM = previousExtrusion["abs"] / (float)Math.Sqrt(
                            ((double)(previousX - X)) * ((double)(previousX - X)) +
                            ((double)(previousY - Y)) * ((double)(previousY - Y))

                            );
                    }

                    if (Model.Commands.Count == 0 || Model.Commands.Count - 1 < layer || Model.Commands[layer] == null)
                    {
                        Model.Commands.Add(new List<GCodeCommand>());
                    }

                    // {x: Number(x), y: Number(y), z: Number(z), extrude: extrude, retract: Number(retract), noMove: false, 
                    // extrusion: (extrude||retract)?Number(prev_extrude["abs"]):0, extruder: extruder, prevX: Number(prevX), 
                    // prevY: Number(prevY), prevZ: Number(prevZ), speed: Number(lastF), gcodeLine: Number(i), 
                    // volPerMM: typeof(volPerMM)==='undefined'?-1:volPerMM};
                    GCodeCommand command = new GCodeCommand()
                    {
                        X = X,
                        Y = Y,
                        Z = Z,
                        Speed = lastSpeed,
                        Extrude = extruding,
                        Retract = retract,
                        NoMove = false,
                        Extrusion = (extruding || retract != 0) ? previousExtrusion["abs"] : 0,
                        Extruder = sExtruder,
                        PreviousX = previousX,
                        PreviousY = previousY,
                        PreviousZ = previousZ,
                        VolumePerMM = float.IsNaN(volumePerMM) || float.IsInfinity(volumePerMM) ? -1 : volumePerMM,
                        GCodeLine = i,
                    };
                    Model.Commands[layer].Add(command);

                    if (!float.IsNegativeInfinity(X)) previousX = X;
                    if (!float.IsNegativeInfinity(Y)) previousY = Y;
                    //previousZ = Z;
                }
                else if (args[0] == "m82")
                {
                    extrudeRelative = false;
                }
                else if (args[0] == "g91")
                {
                    extrudeRelative = true;
                }
                else if (args[0] == "g90")
                {
                    previousG90Found = true;
                    extrudeRelative = false;
                }
                else if (args[0] == "m83")
                {
                    extrudeRelative = true;
                    /*
                    if (!previousG90Found)
                        extrudeRelative = true;
                    else
                    {
                        extrudeRelative = false;
                        previousG90Found = false;
                    }
                    */
                }
                else if (args[0] == "m101")
                {
                    dcExtrude = true;
                    //throw new NotImplementedException();
                }
                else if (args[0] == "M103")
                {
                    dcExtrude = false;
                    //throw new NotImplementedException();
                }

                else if (args[0] == "g92")
                {
                    for (int j = 1; j < args.Length; j++)
                    {
                        string arg = args[j];
                        if (string.IsNullOrEmpty(arg))
                            continue;
                        switch (arg[0])
                        {
                            case 'x':
                                X = float.Parse(arg.TrimStart(arg[0]), CultureInfo.InvariantCulture);
                                break;
                            case 'y':
                                Y = float.Parse(arg.TrimStart(arg[0]), CultureInfo.InvariantCulture);
                                break;
                            case 'z':
                                Z = float.Parse(arg.TrimStart(arg[0]), CultureInfo.InvariantCulture);
                                previousZ = Z;
                                break;
                            case 'e':
                            case 'a':
                            case 'b':
                            case 'c':
                                // These 4 cases appear to map to different extruders
                                extruder = arg[0];
                                sExtruder = arg[0].ToString();
                                var t = arg.TrimStart(arg[0]);
                                extrusion = float.Parse(arg.TrimStart(arg[0]), CultureInfo.InvariantCulture);
                                if (!extrudeRelative)
                                {
                                    // Absolute positioning
                                    previousExtrusion[sExtruder] = 0;
                                }
                                else
                                {
                                    previousExtrusion[sExtruder] = extrusion;
                                }
                                break;
                            default:
                                break;
                        }
                        // if(typeof(x) !== 'undefined' || typeof(y) !== 'undefined' ||typeof(z) !== 'undefined')
                        if (!float.IsNegativeInfinity(X) || !float.IsNegativeInfinity(Y) || !float.IsNegativeInfinity(Z))
                        {
                            if (Model.Commands.Count == 0 || Model.Commands.Count - 1 < layer || Model.Commands[layer] == null)
                            {
                                Model.Commands.Add(new List<GCodeCommand>());
                            }
                            // {x: parseFloat(x), y: parseFloat(y), z: parseFloat(z), extrude: extrude, retract: parseFloat(retract), noMove: true, 
                            // extrusion: 0, extruder: extruder, prevX: parseFloat(prevX), prevY: parseFloat(prevY), prevZ: parseFloat(prevZ), speed: parseFloat(lastF), 
                            // gcodeLine: parseFloat(i)};
                            GCodeCommand command = new GCodeCommand()
                            {
                                X = X,
                                Y = Y,
                                Z = Z,
                                Speed = lastSpeed,
                                Extrude = extruding,
                                Retract = retract,
                                NoMove = true,
                                Extrusion = 0,
                                Extruder = sExtruder,
                                PreviousX = previousX,
                                PreviousY = previousY,
                                PreviousZ = previousZ,
                                //VolumePerMM = float.IsNaN(volumePerMM) || float.IsInfinity(volumePerMM) ? -1 : volumePerMM,
                                GCodeLine = i,
                            };
                            Model.Commands[layer].Add(command);
                        }
                    }
                }
                else if (args[0] == "g28")
                {
                    for (int j = 1; j < args.Length; j++)
                    {
                        string arg = args[j];
                        if (string.IsNullOrEmpty(arg))
                            continue;
                        switch (arg[0])
                        {
                            case 'x':
                                //X = float.Parse(arg.TrimStart('x'));
                                X = float.Parse(arg.TrimStart(arg[0]), CultureInfo.InvariantCulture);
                                break;
                            case 'y':
                                Y = float.Parse(arg.TrimStart(arg[0]), CultureInfo.InvariantCulture);
                                break;
                            case 'z':
                                Z = float.Parse(arg.TrimStart(arg[0]), CultureInfo.InvariantCulture);
                                if (Z == previousZ)
                                {
                                    continue;
                                }
                                else
                                {
                                    layer = Model.Commands.Count;
                                }
                                previousZ = Z;
                                break;
                            case 'e':
                            case 'a':
                            case 'b':
                            case 'c':

                                break;
                            default:
                                break;
                        }
                    }
                    // G28 with no arguments
                    if (args.Length == 1)
                    {
                        //need to init values to default here
                    }
                    // if it's the first layer and G28 was without
                    if (layer == 0 && float.IsNegativeInfinity(Z))
                    {
                        Z = 0;
                        if (Model.zHeights.ContainsKey(Z))
                        {
                            layer = Model.zHeights[Z];
                        }
                        else
                        {
                            layer = Model.Commands.Count;
                            Model.zHeights[Z] = layer;
                        }
                        previousZ = Z;
                    }
                    if (Model.Commands.Count == 0 || Model.Commands.Count - 1 < layer || Model.Commands[layer] == null)
                    {
                        Model.Commands.Add(new List<GCodeCommand>());
                    }
                    // model[layer][model[layer].length] = {x: Number(x), y: Number(y), z: Number(z), extrude: extrude, retract: Number(retract), noMove: false, 
                    // extrusion: (extrude||retract)?Number(prev_extrude["abs"]):0, 
                    // extruder: extruder, prevX: Number(prevX), prevY: Number(prevY), prevZ: Number(prevZ), speed: Number(lastF), gcodeLine: Number(i)};
                    GCodeCommand command = new GCodeCommand()
                    {
                        X = X,
                        Y = Y,
                        Z = Z,
                        Speed = lastSpeed,
                        Extrude = extruding,
                        Retract = retract,
                        NoMove = false,
                        Extrusion = (extruding || retract != 0) ? previousExtrusion["abs"] : 0,
                        Extruder = sExtruder,
                        PreviousX = previousX,
                        PreviousY = previousY,
                        PreviousZ = previousZ,
                        //VolumePerMM = float.IsNaN(volumePerMM) || float.IsInfinity(volumePerMM) ? -1 : volumePerMM,
                        GCodeLine = i,
                    };
                    Model.Commands[layer].Add(command);
                }

                if (shouldSaveCommand)
                {
                    /*
                    if (Model.Commands.Count == 0 || Model.Commands.Count - 1 < layer || Model.Commands[layer] == null)
                    {
                        Model.Commands.Add(new List<GCodeCommand>());
                    }

                    GCodeCommand command = new GCodeCommand() { 
                        X = X, Y = Y, Z = Z, 
                        Speed = lastSpeed, 
                        Extrude = extruding, 
                        Retract = retract, 
                        NoMove = true,
                        Extrusion = (extruding || retract != 0) ? previousExtrusion["abs"] : 0,
                        Extruder = sExtruder,
                        PreviousX = previousX,
                        PreviousY = previousY,
                        PreviousZ = previousZ,
                        VolumePerMM = volumePerMM,
                        GCodeLine = i,
                    };
                    Model.Commands[layer].Add(command);
                    /*
                    command.X = X;
                    command.Y = Y;
                    command.Z = Z;
                    command.Speed = lastSpeed;

                    command.Extrude = extruding;
                    command.Retract = retract;
                    command.NoMove = false;
                    command.Extrusion = (extruding || retract != 0) ? previousExtrusion["abs"] : 0;
                    command.Extruder = sExtruder;

                    command.PreviousX = previousX;
                    command.PreviousY = previousY;
                    command.PreviousZ = previousZ;

                    command.VolumePerMM = volumePerMM;

                    command.GCodeLine = i;
                    */

                }
            }

            Model.Layers = layer;
        }
        private async Task<bool> DoParseAsync()
        {
            try
            {
                int layer = 0;
                float X = 0;
                float Y = 0;
                float Z = 0;
                float previousX = float.NegativeInfinity;
                float previousY = float.NegativeInfinity;
                float previousZ = float.NegativeInfinity;

                bool extruding = false;
                bool extrudeRelative = false;
                char extruder;

                string sExtruder = "";
                int retract = 0;

                float extrusion;
                Dictionary<string, float> previousExtrusion = new Dictionary<string, float>()
            {
                {"a", 0 },
                {"b", 0 },
                {"c", 0 },
                {"e", 0 },
                {"abs", 0 },
            };
                Dictionary<string, float> previousRetraction = new Dictionary<string, float>()
            {
                {"a", 0 },
                {"b", 0 },
                {"c", 0 },
                {"e", 0 },
            };
                // "Last" represents the last time the print head speed was requested to be changed
                float lastSpeed = 4000;

                bool dcExtrude = false;
                bool assumeNonDC = false;

                float volumePerMM = 0;

                for (int i = 0; i < Lines.Count; i++)
                {
                    string gcode = Lines[i];
                    var test = Regex.Split(gcode, @"\;");
                    gcode = Regex.Split(gcode, @"\;")[0].TrimEnd();
                    //string[] args = gcode.ToLower().TrimEnd().Split(' ');
                    string[] args = gcode.ToLower().TrimEnd().Split(' ');

                    X = float.NegativeInfinity;
                    Y = float.NegativeInfinity;
                    Z = float.NegativeInfinity;
                    volumePerMM = 0;
                    retract = 0;

                    extruding = false;
                    sExtruder = "";
                    previousExtrusion["abs"] = 0;


                    if (args[0] == "g0" || args[0] == "g1")
                    {
                        for (int j = 1; j < args.Length; j++)
                        {
                            string arg = args[j];
                            if (string.IsNullOrEmpty(arg))
                                continue;
                            switch (arg[0])
                            {
                                case 'x':
                                    X = float.Parse(arg.TrimStart(arg[0]), CultureInfo.InvariantCulture);
                                    break;
                                case 'y':
                                    Y = float.Parse(arg.TrimStart(arg[0]), CultureInfo.InvariantCulture);
                                    break;
                                case 'z':
                                    Z = float.Parse(arg.TrimStart(arg[0]), CultureInfo.InvariantCulture);
                                    if (Z == previousZ)
                                    {
                                        continue;
                                    }
                                    if (Model.zHeights.ContainsKey(Z))
                                    {
                                        layer = Model.zHeights[Z];
                                    }
                                    else
                                    {
                                        layer = Model.Commands.Count;
                                        Model.zHeights[Z] = layer;
                                    }
                                    previousZ = Z;
                                    break;
                                case 'e':
                                case 'a':
                                case 'b':
                                case 'c':
                                    assumeNonDC = true;
                                    // These 4 cases appear to map to different extruders
                                    //extruder = arg[0];
                                    sExtruder = arg[0].ToString();
                                    var t = arg.TrimStart(arg[0]);
                                    extrusion = float.Parse(arg.TrimStart(arg[0]), CultureInfo.InvariantCulture);
                                    if (!extrudeRelative)
                                    {
                                        // Absolute positioning
                                        previousExtrusion["abs"] = extrusion - previousExtrusion[sExtruder];
                                    }
                                    else
                                    {
                                        previousExtrusion["abs"] = extrusion;
                                    }

                                    extruding = previousExtrusion["abs"] > 0;

                                    if (previousExtrusion["abs"] < 0)
                                    {
                                        //We're retracting...
                                        previousRetraction[sExtruder] = -1;
                                        retract = -1;
                                    }
                                    else if (previousExtrusion["abs"] == 0)
                                    {
                                        retract = 0;
                                    }
                                    else if (previousExtrusion["abs"] > 0 && previousRetraction[sExtruder] < 0)
                                    {
                                        previousRetraction[sExtruder] = 0;
                                        retract = 1;
                                    }
                                    else
                                    {
                                        retract = 0;
                                    }

                                    //previousExtrusion[sExtruder] = extrusion;
                                    previousExtrusion[sExtruder] = extrusion;
                                    break;
                                case 'f':
                                    extrusion = float.Parse(arg.TrimStart(arg[0]), CultureInfo.InvariantCulture);
                                    lastSpeed = extrusion;
                                    break;
                                default:
                                    break;
                            }

                        }

                        if (dcExtrude && !assumeNonDC)
                        {
                            extruding = true;
                            previousExtrusion["abs"] = (float)Math.Sqrt((previousX - X) * (previousX - X) + (previousY - Y) * (previousY - Y));
                        }

                        if (extruding && retract == 0)
                        {

                            // 
                            volumePerMM = previousExtrusion["abs"] / (float)Math.Sqrt(
                                ((double)(previousX - X)) * ((double)(previousX - X)) +
                                ((double)(previousY - Y)) * ((double)(previousY - Y))

                                );
                        }

                        if (Model.Commands.Count == 0 || Model.Commands.Count - 1 < layer || Model.Commands[layer] == null)
                        {
                            Model.Commands.Add(new List<GCodeCommand>());
                        }

                        // {x: Number(x), y: Number(y), z: Number(z), extrude: extrude, retract: Number(retract), noMove: false, 
                        // extrusion: (extrude||retract)?Number(prev_extrude["abs"]):0, extruder: extruder, prevX: Number(prevX), 
                        // prevY: Number(prevY), prevZ: Number(prevZ), speed: Number(lastF), gcodeLine: Number(i), 
                        // volPerMM: typeof(volPerMM)==='undefined'?-1:volPerMM};
                        GCodeCommand command = new GCodeCommand()
                        {
                            OriginalLine = gcode,
                            Command = args[0],
                            X = X,
                            Y = Y,
                            Z = Z,
                            Speed = lastSpeed,
                            Extrude = extruding,
                            Retract = retract,
                            NoMove = false,
                            Extrusion = (extruding || retract != 0) ? previousExtrusion["abs"] : 0,
                            Extruder = sExtruder,
                            PreviousX = previousX,
                            PreviousY = previousY,
                            PreviousZ = previousZ,
                            VolumePerMM = float.IsNaN(volumePerMM) || float.IsInfinity(volumePerMM) ? -1 : volumePerMM,
                            GCodeLine = i,
                        };
                        Model.Commands[layer].Add(command);

                        if (!float.IsNegativeInfinity(X)) previousX = X;
                        if (!float.IsNegativeInfinity(Y)) previousY = Y;
                        //previousZ = Z;
                    }
                    else if (args[0] == "m82")
                    {
                        extrudeRelative = false;
                    }
                    else if (args[0] == "g91")
                    {
                        extrudeRelative = true;
                    }
                    else if (args[0] == "g90")
                    {
                        extrudeRelative = false;
                    }
                    else if (args[0] == "m83")
                    {
                        extrudeRelative = true;
                        /*
                        if (!previousG90Found)
                            extrudeRelative = true;
                        else
                        {
                            extrudeRelative = false;
                            previousG90Found = false;
                        }
                        */
                    }
                    else if (args[0] == "m101")
                    {
                        dcExtrude = true;
                        //throw new NotImplementedException();
                    }
                    else if (args[0] == "M103")
                    {
                        dcExtrude = false;
                        //throw new NotImplementedException();
                    }

                    else if (args[0] == "g92")
                    {
                        for (int j = 1; j < args.Length; j++)
                        {
                            string arg = args[j];
                            if (string.IsNullOrEmpty(arg))
                                continue;
                            switch (arg[0])
                            {
                                case 'x':
                                    X = float.Parse(arg.TrimStart(arg[0]), CultureInfo.InvariantCulture);
                                    break;
                                case 'y':
                                    Y = float.Parse(arg.TrimStart(arg[0]), CultureInfo.InvariantCulture);
                                    break;
                                case 'z':
                                    Z = float.Parse(arg.TrimStart(arg[0]), CultureInfo.InvariantCulture);
                                    previousZ = Z;
                                    break;
                                case 'e':
                                case 'a':
                                case 'b':
                                case 'c':
                                    // These 4 cases appear to map to different extruders
                                    extruder = arg[0];
                                    sExtruder = arg[0].ToString();
                                    var t = arg.TrimStart(arg[0]);
                                    extrusion = float.Parse(arg.TrimStart(arg[0]), CultureInfo.InvariantCulture);
                                    if (!extrudeRelative)
                                    {
                                        // Absolute positioning
                                        previousExtrusion[sExtruder] = 0;
                                    }
                                    else
                                    {
                                        previousExtrusion[sExtruder] = extrusion;
                                    }
                                    break;
                                default:
                                    break;
                            }
                            // if(typeof(x) !== 'undefined' || typeof(y) !== 'undefined' ||typeof(z) !== 'undefined')
                            if (!float.IsNegativeInfinity(X) || !float.IsNegativeInfinity(Y) || !float.IsNegativeInfinity(Z))
                            {
                                if (Model.Commands.Count == 0 || Model.Commands.Count - 1 < layer || Model.Commands[layer] == null)
                                {
                                    Model.Commands.Add(new List<GCodeCommand>());
                                }
                                // {x: parseFloat(x), y: parseFloat(y), z: parseFloat(z), extrude: extrude, retract: parseFloat(retract), noMove: true, 
                                // extrusion: 0, extruder: extruder, prevX: parseFloat(prevX), prevY: parseFloat(prevY), prevZ: parseFloat(prevZ), speed: parseFloat(lastF), 
                                // gcodeLine: parseFloat(i)};
                                GCodeCommand command = new GCodeCommand()
                                {
                                    Command = args[0],
                                    OriginalLine = gcode,
                                    X = X,
                                    Y = Y,
                                    Z = Z,
                                    Speed = lastSpeed,
                                    Extrude = extruding,
                                    Retract = retract,
                                    NoMove = true,
                                    Extrusion = 0,
                                    Extruder = sExtruder,
                                    PreviousX = previousX,
                                    PreviousY = previousY,
                                    PreviousZ = previousZ,
                                    //VolumePerMM = float.IsNaN(volumePerMM) || float.IsInfinity(volumePerMM) ? -1 : volumePerMM,
                                    GCodeLine = i,
                                };
                                Model.Commands[layer].Add(command);
                            }
                        }
                    }
                    else if (args[0] == "g28")
                    {
                        for (int j = 1; j < args.Length; j++)
                        {
                            string arg = args[j];
                            if (string.IsNullOrEmpty(arg))
                                continue;
                            switch (arg[0])
                            {
                                case 'x':
                                    //X = float.Parse(arg.TrimStart('x'));
                                    X = float.Parse(arg.TrimStart(arg[0]), CultureInfo.InvariantCulture);
                                    break;
                                case 'y':
                                    Y = float.Parse(arg.TrimStart(arg[0]), CultureInfo.InvariantCulture);
                                    break;
                                case 'z':
                                    Z = float.Parse(arg.TrimStart(arg[0]), CultureInfo.InvariantCulture);
                                    if (Z == previousZ)
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        layer = Model.Commands.Count;
                                    }
                                    previousZ = Z;
                                    break;
                                case 'e':
                                case 'a':
                                case 'b':
                                case 'c':

                                    break;
                                default:
                                    break;
                            }
                        }
                        // G28 with no arguments
                        if (args.Length == 1)
                        {
                            //need to init values to default here
                        }
                        // if it's the first layer and G28 was without
                        if (layer == 0 && float.IsNegativeInfinity(Z))
                        {
                            Z = 0;
                            if (Model.zHeights.ContainsKey(Z))
                            {
                                layer = Model.zHeights[Z];
                            }
                            else
                            {
                                layer = Model.Commands.Count;
                                Model.zHeights[Z] = layer;
                            }
                            previousZ = Z;
                        }
                        if (Model.Commands.Count == 0 || Model.Commands.Count - 1 < layer || Model.Commands[layer] == null)
                        {
                            Model.Commands.Add(new List<GCodeCommand>());
                        }
                        // model[layer][model[layer].length] = {x: Number(x), y: Number(y), z: Number(z), extrude: extrude, retract: Number(retract), noMove: false, 
                        // extrusion: (extrude||retract)?Number(prev_extrude["abs"]):0, 
                        // extruder: extruder, prevX: Number(prevX), prevY: Number(prevY), prevZ: Number(prevZ), speed: Number(lastF), gcodeLine: Number(i)};
                        GCodeCommand command = new GCodeCommand()
                        {
                            Command = args[0],
                            OriginalLine = gcode,
                            X = X,
                            Y = Y,
                            Z = Z,
                            Speed = lastSpeed,
                            Extrude = extruding,
                            Retract = retract,
                            NoMove = false,
                            Extrusion = (extruding || retract != 0) ? previousExtrusion["abs"] : 0,
                            Extruder = sExtruder,
                            PreviousX = previousX,
                            PreviousY = previousY,
                            PreviousZ = previousZ,
                            //VolumePerMM = float.IsNaN(volumePerMM) || float.IsInfinity(volumePerMM) ? -1 : volumePerMM,
                            GCodeLine = i,
                        };
                        Model.Commands[layer].Add(command);
                    }

                }

                Model.Layers = layer;
                return true;
            }
            catch (Exception exc)
            {
                return false;
            }
        }

        private void Analyze()
        {
            bool validX = false;
            bool validY = false;

            GCodeObjectSize min = new GCodeObjectSize() { X = float.NegativeInfinity, Y = float.NegativeInfinity, Z = float.NegativeInfinity };
            GCodeObjectSize max = new GCodeObjectSize() { X = float.NegativeInfinity, Y = float.NegativeInfinity, Z = float.NegativeInfinity };

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            float minZ = float.MaxValue;
            float maxZ = float.MinValue;

            float tmp1 = 0;
            float tmp2 = 0;
            float printTimeAddition = 0;

            foreach (List<GCodeCommand> commands in Model.Commands)
            {
                if (commands.Count == 0)
                {
                    continue;
                }

                foreach (GCodeCommand command in commands)
                {
                    validX = false;
                    validY = false;
                    // if(typeof(cmds[j].x) !== 'undefined'&&typeof(cmds[j].prevX) !== 'undefined'&&typeof(cmds[j].extrude) !== 'undefined'&&cmds[j].extrude&&!isNaN(cmds[j].x))
                    if (!float.IsInfinity(command.X) && !float.IsInfinity(command.PreviousX) && command.Extrude && !float.IsNaN(command.X))
                    {
                        maxX = (maxX > command.X) ? maxX : command.X;
                        maxX = (maxX > command.PreviousX) ? maxX : command.PreviousX;
                        minX = (minX < command.X) ? minX : command.X;
                        minX = (minX < command.PreviousX) ? minX : command.PreviousX;
                        validX = true;
                    }

                    if (!float.IsInfinity(command.Y) && !float.IsInfinity(command.PreviousY) && command.Extrude && !float.IsNaN(command.Y))
                    {
                        maxY = (maxY > command.Y) ? maxY : command.Y;
                        maxY = (maxY > command.PreviousY) ? maxY : command.PreviousY;
                        minY = (minY < command.Y) ? minY : command.Y;
                        minY = (minY < command.PreviousY) ? minY : command.PreviousY;
                        validY = true;
                    }

                    if (!float.IsInfinity(command.PreviousZ) && command.Extrude && !float.IsNaN(command.PreviousZ))
                    {
                        maxZ = (maxZ > command.PreviousZ) ? maxZ : command.PreviousZ;
                        minZ = (minZ < command.PreviousZ) ? minZ : command.PreviousZ;
                    }

                    if (command.Extrude || command.Retract != 0)
                    {
                        Model.TotalFilament += command.Extrusion;
                    }
                    if (validX && validY)
                    {
                        //  printTimeAdd = Math.sqrt(Math.pow(parseFloat(cmds[j].x)-parseFloat(cmds[j].prevX),2)+Math.pow(parseFloat(cmds[j].y)-parseFloat(cmds[j].prevY),2))/(cmds[j].speed/60);
                        var test = (float)Math.Sqrt(Math.Pow((command.X) - (command.PreviousX), 2) + Math.Pow((command.Y) - (command.PreviousY), 2)) / (command.Speed / 60);
                        if (float.IsNaN(test) || float.IsInfinity(test))
                        {

                        }
                        else
                            printTimeAddition = (float)Math.Sqrt(Math.Pow((command.X) - (command.PreviousX), 2) + Math.Pow((command.Y) - (command.PreviousY), 2)) / (command.Speed / 60);
                    }
                    else if (command.Retract == 0 && command.Extrusion != 0)
                    {
                        // tmp1 = Math.sqrt(Math.pow(parseFloat(cmds[j].x)-parseFloat(cmds[j].prevX),2)+Math.pow(parseFloat(cmds[j].y)-parseFloat(cmds[j].prevY),2))/(cmds[j].speed/60);
                        tmp1 = (float)Math.Sqrt(Math.Pow((command.X) - (command.PreviousX), 2) + Math.Pow((command.Y) - (command.PreviousY), 2)) / (command.Speed / 60);
                        tmp2 = (float)Math.Abs((command.Extrusion) / (command.Speed / 60));
                        if (float.IsNaN(tmp1) || float.IsInfinity(tmp1) || float.IsNaN(tmp2) || float.IsInfinity(tmp2))
                        {

                        }
                        else
                            printTimeAddition = tmp1 >= tmp2 ? tmp1 : tmp2;
                    }
                    else if (command.Retract != 0)
                    {
                        // printTimeAdd = Math.abs(parseFloat(cmds[j].extrusion)/(cmds[j].speed/60));
                        var test = Math.Abs(command.Extrusion / (command.Speed / 60));
                        if (float.IsNaN(test) || float.IsInfinity(test))
                        {

                        }
                        else
                            printTimeAddition = Math.Abs(command.Extrusion / (command.Speed / 60));
                    }

                    Model.PrintTime += printTimeAddition;

                    if (command.Extrude && command.Retract == 0 && validX && validY)
                    {
                        // we are extruding
                        var volPerMM = command.VolumePerMM;

                        //volPerMM = float.Parse(volPerMM);

                        var volIndex = Model.volSpeeds.IndexOf(volPerMM);
                        if (volIndex == -1)
                        {
                            Model.volSpeeds.Add(volPerMM);
                            volIndex = Model.volSpeeds.IndexOf(volPerMM);
                        }
                        //if (!Model.volSpeedsByLayer.ContainsKey(command.PreviousZ) )
                        //var t = Model.volSpeedsByLayer[command.PreviousZ];
                        if (!Model.volSpeedsByLayer.ContainsKey(command.PreviousZ) || Model.volSpeedsByLayer[command.PreviousZ] == null)
                        {
                            Model.volSpeedsByLayer.Add(command.PreviousZ, new List<float>() { -1 });
                        }
                        if (Model.volSpeedsByLayer[command.PreviousZ].IndexOf(volPerMM) == -1)
                        {
                            //Model.volSpeedsByLayer[command.PreviousZ][volIndex] = volPerMM;
                            Model.volSpeedsByLayer[command.PreviousZ].Add(volPerMM);
                        }

                        var extrusionSpeed = command.VolumePerMM * (command.Speed / 60);

                        volIndex = Model.extrusionSpeeds.IndexOf(extrusionSpeed);
                        if (volIndex == -1)
                        {
                            Model.extrusionSpeeds.Add(extrusionSpeed);
                            volIndex = Model.extrusionSpeeds.IndexOf(extrusionSpeed);
                        }
                        if (!Model.extrusionSpeedsByLayer.ContainsKey(command.PreviousZ) || Model.extrusionSpeedsByLayer[command.PreviousZ] == null)
                        {
                            Model.extrusionSpeedsByLayer.Add(command.PreviousZ, new List<float>() { -1 });
                        }
                        if (Model.extrusionSpeedsByLayer[command.PreviousZ].IndexOf(extrusionSpeed) == -1)
                        {
                            //Model.extrusionSpeedsByLayer[command.PreviousZ][volIndex] = extrusionSpeed;
                            Model.extrusionSpeedsByLayer[command.PreviousZ].Add(extrusionSpeed);
                        }
                    }
                }

                Model.Width = Math.Abs(maxX - minX);
                Model.Depth = Math.Abs(maxY - minY);
                Model.Height = Math.Abs(maxZ - minZ);
                Model.LayerHeight = (maxZ - minZ) / (Model.Layers - 1);
            }
        }
        private async Task<bool> AnalyzeAsync(IProgress<int> progress)
        {
            try
            {
                bool validX = false;
                bool validY = false;

                GCodeObjectSize min = new GCodeObjectSize() { X = float.NegativeInfinity, Y = float.NegativeInfinity, Z = float.NegativeInfinity };
                GCodeObjectSize max = new GCodeObjectSize() { X = float.NegativeInfinity, Y = float.NegativeInfinity, Z = float.NegativeInfinity };

                float minX = float.MaxValue;
                float maxX = float.MinValue;
                float minY = float.MaxValue;
                float maxY = float.MinValue;
                float minZ = float.MaxValue;
                float maxZ = float.MinValue;

                float tmp1 = 0;
                float tmp2 = 0;
                float printTimeAddition = 0;

                int i = 0;

                foreach (List<GCodeCommand> commands in Model.Commands)
                {
                    if (commands.Count == 0)
                    {
                        continue;
                    }

                    foreach (GCodeCommand command in commands)
                    {
                        validX = false;
                        validY = false;
                        // if(typeof(cmds[j].x) !== 'undefined'&&typeof(cmds[j].prevX) !== 'undefined'&&typeof(cmds[j].extrude) !== 'undefined'&&cmds[j].extrude&&!isNaN(cmds[j].x))
                        if (!float.IsInfinity(command.X) && !float.IsInfinity(command.PreviousX) && command.Extrude && !float.IsNaN(command.X))
                        {
                            maxX = (maxX > command.X) ? maxX : command.X;
                            maxX = (maxX > command.PreviousX) ? maxX : command.PreviousX;
                            minX = (minX < command.X) ? minX : command.X;
                            minX = (minX < command.PreviousX) ? minX : command.PreviousX;
                            validX = true;
                        }

                        if (!float.IsInfinity(command.Y) && !float.IsInfinity(command.PreviousY) && command.Extrude && !float.IsNaN(command.Y))
                        {
                            maxY = (maxY > command.Y) ? maxY : command.Y;
                            maxY = (maxY > command.PreviousY) ? maxY : command.PreviousY;
                            minY = (minY < command.Y) ? minY : command.Y;
                            minY = (minY < command.PreviousY) ? minY : command.PreviousY;
                            validY = true;
                        }

                        if (!float.IsInfinity(command.PreviousZ) && command.Extrude && !float.IsNaN(command.PreviousZ))
                        {
                            maxZ = (maxZ > command.PreviousZ) ? maxZ : command.PreviousZ;
                            minZ = (minZ < command.PreviousZ) ? minZ : command.PreviousZ;
                        }

                        if (command.Extrude || command.Retract != 0)
                        {
                            Model.TotalFilament += command.Extrusion;
                        }
                        if (validX && validY)
                        {
                            //  printTimeAdd = Math.sqrt(Math.pow(parseFloat(cmds[j].x)-parseFloat(cmds[j].prevX),2)+Math.pow(parseFloat(cmds[j].y)-parseFloat(cmds[j].prevY),2))/(cmds[j].speed/60);
                            var test = (float)Math.Sqrt(Math.Pow((command.X) - (command.PreviousX), 2) + Math.Pow((command.Y) - (command.PreviousY), 2)) / (command.Speed / 60);
                            if (float.IsNaN(test) || float.IsInfinity(test))
                            {

                            }
                            else
                                printTimeAddition = (float)Math.Sqrt(Math.Pow((command.X) - (command.PreviousX), 2) + Math.Pow((command.Y) - (command.PreviousY), 2)) / (command.Speed / 60);
                        }
                        else if (command.Retract == 0 && command.Extrusion != 0)
                        {
                            // tmp1 = Math.sqrt(Math.pow(parseFloat(cmds[j].x)-parseFloat(cmds[j].prevX),2)+Math.pow(parseFloat(cmds[j].y)-parseFloat(cmds[j].prevY),2))/(cmds[j].speed/60);
                            tmp1 = (float)Math.Sqrt(Math.Pow((command.X) - (command.PreviousX), 2) + Math.Pow((command.Y) - (command.PreviousY), 2)) / (command.Speed / 60);
                            tmp2 = (float)Math.Abs((command.Extrusion) / (command.Speed / 60));
                            if (float.IsNaN(tmp1) || float.IsInfinity(tmp1) || float.IsNaN(tmp2) || float.IsInfinity(tmp2))
                            {

                            }
                            else
                                printTimeAddition = tmp1 >= tmp2 ? tmp1 : tmp2;
                        }
                        else if (command.Retract != 0)
                        {
                            // printTimeAdd = Math.abs(parseFloat(cmds[j].extrusion)/(cmds[j].speed/60));
                            var test = Math.Abs(command.Extrusion / (command.Speed / 60));
                            if (float.IsNaN(test) || float.IsInfinity(test))
                            {

                            }
                            else
                                printTimeAddition = Math.Abs(command.Extrusion / (command.Speed / 60));
                        }

                        Model.PrintTime += printTimeAddition;

                        if (command.Extrude && command.Retract == 0 && validX && validY)
                        {
                            // we are extruding
                            var volPerMM = command.VolumePerMM;

                            //volPerMM = float.Parse(volPerMM);

                            var volIndex = Model.volSpeeds.IndexOf(volPerMM);
                            if (volIndex == -1)
                            {
                                Model.volSpeeds.Add(volPerMM);
                                volIndex = Model.volSpeeds.IndexOf(volPerMM);
                            }
                            //if (!Model.volSpeedsByLayer.ContainsKey(command.PreviousZ) )
                            //var t = Model.volSpeedsByLayer[command.PreviousZ];
                            if (!Model.volSpeedsByLayer.ContainsKey(command.PreviousZ) || Model.volSpeedsByLayer[command.PreviousZ] == null)
                            {
                                Model.volSpeedsByLayer.Add(command.PreviousZ, new List<float>() { -1 });
                            }
                            if (Model.volSpeedsByLayer[command.PreviousZ].IndexOf(volPerMM) == -1)
                            {
                                //Model.volSpeedsByLayer[command.PreviousZ][volIndex] = volPerMM;
                                Model.volSpeedsByLayer[command.PreviousZ].Add(volPerMM);
                            }

                            var extrusionSpeed = command.VolumePerMM * (command.Speed / 60);

                            volIndex = Model.extrusionSpeeds.IndexOf(extrusionSpeed);
                            if (volIndex == -1)
                            {
                                Model.extrusionSpeeds.Add(extrusionSpeed);
                                volIndex = Model.extrusionSpeeds.IndexOf(extrusionSpeed);
                            }
                            if (!Model.extrusionSpeedsByLayer.ContainsKey(command.PreviousZ) || Model.extrusionSpeedsByLayer[command.PreviousZ] == null)
                            {
                                Model.extrusionSpeedsByLayer.Add(command.PreviousZ, new List<float>() { -1 });
                            }
                            if (Model.extrusionSpeedsByLayer[command.PreviousZ].IndexOf(extrusionSpeed) == -1)
                            {
                                //Model.extrusionSpeedsByLayer[command.PreviousZ][volIndex] = extrusionSpeed;
                                Model.extrusionSpeedsByLayer[command.PreviousZ].Add(extrusionSpeed);
                            }
                        }


                    }
                    if (progress != null)
                    {
                        float test = (((float)i / Model.Commands.Count) * 100f);
                        if(i < Model.Commands.Count -1)
                            progress.Report(Convert.ToInt32(test));
                        else
                            progress.Report(100);
                    }
                    i++;
                }
                Model.Width = Math.Abs(maxX - minX);
                Model.Depth = Math.Abs(maxY - minY);
                Model.Height = Math.Abs(maxZ - minZ);
                Model.LayerHeight = (maxZ - minZ) / (Model.Layers - 1);

                return true;
            }
            catch (Exception exc)
            {
                return false;
            }
        }

        private async Task<bool> CreateLayerModel(IProgress<int> progress)
        {
            try
            {
                var temp = new List<LinesVisual3D>();
                int i = 0;
                foreach (List<GCodeCommand> commands in Model.Commands)
                {

                    var line = await drawLayerAsync(i, 0, Model.Commands[i].Count, false);
                    ModelLayers.Add(line);

                    if (progress != null)
                    {
                        float test = (((float)i / Model.Commands.Count) * 100f);
                        if (i < Model.Commands.Count - 1)
                            progress.Report(Convert.ToInt32(test));
                        else
                            progress.Report(100);
                    }
                    i++;
                }
                return true;
            }
            catch (Exception exc)
            {
                return false;
            }
        
        }

        private LinesVisual3D drawLayer(int layerNumber, int fromProgress, int toProgress, bool isNextLayer)
        {
            // https://github.com/hudbrog/gCodeViewer/blob/master/js/renderer.js
            var model = Model.Commands;
            LinesVisual3D line = new LinesVisual3D();
            try
            {
                int i = 0;
                int speedIndex = 0;
                float prevX = 0;
                float prevY = 0;
                float prevZ = 0;

                if (!isNextLayer)
                {
                    // Store current layer
                    // layerNumStore=layerNum;
                    // progressStore = {from: fromProgress, to: toProgress};
                }
                if (model == null || model.Count == 0 || layerNumber > model.Count)
                    return line;

                var cmd = model[layerNumber];
                float x = float.NegativeInfinity;
                float y = float.NegativeInfinity;

                if (fromProgress > 0)
                {
                    prevX = cmd[fromProgress - 1].X;
                    prevY = -cmd[fromProgress - 1].Y;
                }
                else if (fromProgress == 0 && layerNumber == 0)
                {
                    if (!float.IsInfinity(model[0][0].X) && !float.IsInfinity(model[0][0].Y))
                    {
                        prevX = model[0][0].X;
                        prevY = -model[0][0].Y;
                    }
                    else
                    {
                        prevX = 0;
                        prevY = 0;
                    }
                }
                else
                {
                    if (model[layerNumber - 1] != null)
                    {
                        prevX = float.NegativeInfinity;
                        prevY = float.NegativeInfinity;
                        for (i = model[layerNumber - 1].Count - 1; i >= 0; i--)
                        {
                            if (float.IsInfinity(prevX) && !float.IsInfinity(model[layerNumber - 1][i].X))
                                prevX = model[layerNumber - 1][i].X;
                            if (float.IsInfinity(prevY) && !float.IsInfinity(model[layerNumber - 1][i].Y))
                                prevY = model[layerNumber - 1][i].Y;
                        }
                        if (float.IsInfinity(prevX))
                            prevX = 0;
                        if (float.IsInfinity(prevY))
                            prevY = 0;
                    }
                    else
                    {
                        prevX = 0;
                        prevY = 0;
                    }

                }

                prevZ = Model.zHeights.Keys.ElementAt(layerNumber);

                for (i = fromProgress; i <= toProgress; i++)
                {
                    // ctx.lineWidth = 1;
                    try
                    {
                        var test = cmd[i];
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    if (!float.IsInfinity(cmd[i].PreviousX) && !float.IsInfinity(cmd[i].PreviousY))
                    {
                        prevX = cmd[i].PreviousX;
                        prevY = cmd[i].PreviousY;
                    }

                    if (float.IsInfinity(cmd[i].X) || float.IsNaN(cmd[i].X))
                        x = prevX;
                    else
                        x = cmd[i].X;

                    if (float.IsInfinity(cmd[i].Y) || float.IsNaN(cmd[i].Y))
                        y = prevY;
                    else
                        y = cmd[i].Y;


                    // if(renderOptions["differentiateColors"]&&!renderOptions['showNextLayer']&&!renderOptions['renderAnalysis']) { }

                    if (!cmd[i].Extrude && !cmd[i].NoMove)
                    {
                        if (cmd[i].Retract == -1)
                        {
                            // show retracts
                            if (true)
                            {
                                // void ctx.arc(x, y, radius, startAngle, endAngle [, anticlockwise]);
                                // (prevX, prevY, renderOptions["sizeRetractSpot"], 0, Math.PI*2, true);
                                //var m = GetCircleModel(2, new Vector3D(0, 0, 0), new Point3D(x, y, prevZ), 20);
                                //lines.Add(m);
                                //ctx.strokeStyle = renderOptions["colorRetract"];
                                //ctx.fillStyle = renderOptions["colorRetract"];
                                //ctx.beginPath();
                                //ctx.arc(prevX, prevY, renderOptions["sizeRetractSpot"], 0, Math.PI * 2, true);
                                //ctx.stroke();
                                //ctx.fill()
                            }
                            // show moves
                            if (true)
                            {
                                line.Points.Add(new Point3D(prevX, prevY, prevZ));
                                line.Points.Add(new Point3D(x, y, prevZ));

                                //ctx.strokeStyle = renderOptions["colorMove"];
                                //ctx.beginPath();
                                //ctx.moveTo(prevX, prevY);
                                //ctx.lineTo(x * zoomFactor, y * zoomFactor);
                                //ctx.stroke();
                            }
                        }
                    }
                    else if (cmd[i].Extrude)
                    {
                        if (cmd[i].Retract >= 0)
                        {
                            //ctx.strokeStyle = renderOptions["colorLine"][speedIndex];
                        }
                        else if (cmd[i].Retract == -1)
                        {

                        }

                        line.Points.Add(new Point3D(prevX, prevY, prevZ));
                        line.Points.Add(new Point3D(x, y, prevZ));
                        //ctx.lineWidth = renderOptions['extrusionWidth'];
                        //ctx.beginPath();
                        //ctx.moveTo(prevX, prevY);
                        //ctx.lineTo(x * zoomFactor, y * zoomFactor);
                        //ctx.stroke();

                    }
                    else
                    {
                        // show retracts
                        if (true)
                        {
                            //ctx.strokeStyle = renderOptions["colorRestart"];
                            //ctx.fillStyle = renderOptions["colorRestart"];
                            //ctx.beginPath();
                            //ctx.arc(prevX, prevY, renderOptions["sizeRetractSpot"], 0, Math.PI * 2, true);
                            //ctx.stroke();
                            //ctx.fill();

                        }
                    }
                    prevX = x;
                    prevY = y;
                }
                return line;
            }
            catch (Exception exc)
            {
                return line;
            }
        }
        private async Task<LinesVisual3D> drawLayerAsync(int layerNumber, int fromProgress, int toProgress, bool isNextLayer)
        {
            // https://github.com/hudbrog/gCodeViewer/blob/master/js/renderer.js
            var model = Model.Commands;
            LinesVisual3D line = new LinesVisual3D();
            try
            {
                int i = 0;
                int speedIndex = 0;
                float prevX = 0;
                float prevY = 0;
                float prevZ = 0;

                if (!isNextLayer)
                {
                    // Store current layer
                    // layerNumStore=layerNum;
                    // progressStore = {from: fromProgress, to: toProgress};
                }
                if (model == null || model.Count == 0 || layerNumber > model.Count)
                    return line;

                var cmd = model[layerNumber];
                float x = float.NegativeInfinity;
                float y = float.NegativeInfinity;

                if (fromProgress > 0)
                {
                    prevX = cmd[fromProgress - 1].X;
                    prevY = -cmd[fromProgress - 1].Y;
                }
                else if (fromProgress == 0 && layerNumber == 0)
                {
                    if (!float.IsInfinity(model[0][0].X) && !float.IsInfinity(model[0][0].Y))
                    {
                        prevX = model[0][0].X;
                        prevY = -model[0][0].Y;
                    }
                    else
                    {
                        prevX = 0;
                        prevY = 0;
                    }
                }
                else
                {
                    if (model[layerNumber - 1] != null)
                    {
                        prevX = float.NegativeInfinity;
                        prevY = float.NegativeInfinity;
                        for (i = model[layerNumber - 1].Count - 1; i >= 0; i--)
                        {
                            if (float.IsInfinity(prevX) && !float.IsInfinity(model[layerNumber - 1][i].X))
                                prevX = model[layerNumber - 1][i].X;
                            if (float.IsInfinity(prevY) && !float.IsInfinity(model[layerNumber - 1][i].Y))
                                prevY = model[layerNumber - 1][i].Y;
                        }
                        if (float.IsInfinity(prevX))
                            prevX = 0;
                        if (float.IsInfinity(prevY))
                            prevY = 0;
                    }
                    else
                    {
                        prevX = 0;
                        prevY = 0;
                    }

                }

                prevZ = Model.zHeights.Keys.ElementAt(layerNumber);

                for (i = fromProgress; i <= toProgress; i++)
                {
                    // ctx.lineWidth = 1;
                    try
                    {
                        var test = cmd[i];
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    if (!float.IsInfinity(cmd[i].PreviousX) && !float.IsInfinity(cmd[i].PreviousY))
                    {
                        prevX = cmd[i].PreviousX;
                        prevY = cmd[i].PreviousY;
                    }

                    if (float.IsInfinity(cmd[i].X) || float.IsNaN(cmd[i].X))
                        x = prevX;
                    else
                        x = cmd[i].X;

                    if (float.IsInfinity(cmd[i].Y) || float.IsNaN(cmd[i].Y))
                        y = prevY;
                    else
                        y = cmd[i].Y;


                    // if(renderOptions["differentiateColors"]&&!renderOptions['showNextLayer']&&!renderOptions['renderAnalysis']) { }

                    if (!cmd[i].Extrude && !cmd[i].NoMove)
                    {
                        if (cmd[i].Retract == -1)
                        {
                            // show retracts
                            if (true)
                            {
                                // void ctx.arc(x, y, radius, startAngle, endAngle [, anticlockwise]);
                                // (prevX, prevY, renderOptions["sizeRetractSpot"], 0, Math.PI*2, true);
                                //var m = GetCircleModel(2, new Vector3D(0, 0, 0), new Point3D(x, y, prevZ), 20);
                                //lines.Add(m);
                                //ctx.strokeStyle = renderOptions["colorRetract"];
                                //ctx.fillStyle = renderOptions["colorRetract"];
                                //ctx.beginPath();
                                //ctx.arc(prevX, prevY, renderOptions["sizeRetractSpot"], 0, Math.PI * 2, true);
                                //ctx.stroke();
                                //ctx.fill()
                            }
                            // show moves
                            if (true)
                            {
                                line.Points.Add(new Point3D(prevX, prevY, prevZ));
                                line.Points.Add(new Point3D(x, y, prevZ));

                                //ctx.strokeStyle = renderOptions["colorMove"];
                                //ctx.beginPath();
                                //ctx.moveTo(prevX, prevY);
                                //ctx.lineTo(x * zoomFactor, y * zoomFactor);
                                //ctx.stroke();
                            }
                        }
                    }
                    else if (cmd[i].Extrude)
                    {
                        if (cmd[i].Retract >= 0)
                        {
                            //ctx.strokeStyle = renderOptions["colorLine"][speedIndex];
                        }
                        else if (cmd[i].Retract == -1)
                        {

                        }

                        line.Points.Add(new Point3D(prevX, prevY, prevZ));
                        line.Points.Add(new Point3D(x, y, prevZ));
                        //ctx.lineWidth = renderOptions['extrusionWidth'];
                        //ctx.beginPath();
                        //ctx.moveTo(prevX, prevY);
                        //ctx.lineTo(x * zoomFactor, y * zoomFactor);
                        //ctx.stroke();

                    }
                    else
                    {
                        // show retracts
                        if (true)
                        {
                            //ctx.strokeStyle = renderOptions["colorRestart"];
                            //ctx.fillStyle = renderOptions["colorRestart"];
                            //ctx.beginPath();
                            //ctx.arc(prevX, prevY, renderOptions["sizeRetractSpot"], 0, Math.PI * 2, true);
                            //ctx.stroke();
                            //ctx.fill();

                        }
                    }
                    prevX = x;
                    prevY = y;
                }
                return line;
            }
            catch (Exception exc)
            {
                return line;
            }
        }
        
        private List<Point3D> getLayerPointsCollection(int layerNumber, int fromProgress, int toProgress, bool isNextLayer)
        {
            // https://github.com/hudbrog/gCodeViewer/blob/master/js/renderer.js
            var model = Model.Commands;
            List<Point3D> Points = new List<Point3D>();
            try
            {
                int i = 0;
                int speedIndex = 0;
                float prevX = 0;
                float prevY = 0;
                float prevZ = 0;

                if (!isNextLayer)
                {
                    // Store current layer
                    // layerNumStore=layerNum;
                    // progressStore = {from: fromProgress, to: toProgress};
                }
                if (model == null || model.Count == 0 || layerNumber > model.Count)
                    return Points;

                var cmd = model[layerNumber];
                float x = float.NegativeInfinity;
                float y = float.NegativeInfinity;

                if (fromProgress > 0)
                {
                    prevX = cmd[fromProgress - 1].X;
                    prevY = -cmd[fromProgress - 1].Y;
                }
                else if (fromProgress == 0 && layerNumber == 0)
                {
                    if (!float.IsInfinity(model[0][0].X) && !float.IsInfinity(model[0][0].Y))
                    {
                        prevX = model[0][0].X;
                        prevY = -model[0][0].Y;
                    }
                    else
                    {
                        prevX = 0;
                        prevY = 0;
                    }
                }
                else
                {
                    if (model[layerNumber - 1] != null)
                    {
                        prevX = float.NegativeInfinity;
                        prevY = float.NegativeInfinity;
                        for (i = model[layerNumber - 1].Count - 1; i >= 0; i--)
                        {
                            if (float.IsInfinity(prevX) && !float.IsInfinity(model[layerNumber - 1][i].X))
                                prevX = model[layerNumber - 1][i].X;
                            if (float.IsInfinity(prevY) && !float.IsInfinity(model[layerNumber - 1][i].Y))
                                prevY = model[layerNumber - 1][i].Y;
                        }
                        if (float.IsInfinity(prevX))
                            prevX = 0;
                        if (float.IsInfinity(prevY))
                            prevY = 0;
                    }
                    else
                    {
                        prevX = 0;
                        prevY = 0;
                    }

                }

                prevZ = Model.zHeights.Keys.ElementAt(layerNumber);

                for (i = fromProgress; i <= toProgress; i++)
                {
                    // ctx.lineWidth = 1;
                    try
                    {
                        var test = cmd[i];
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    
                    if (!float.IsInfinity(cmd[i].PreviousX) && !float.IsInfinity(cmd[i].PreviousY))
                    {
                        prevX = cmd[i].PreviousX;
                        prevY = cmd[i].PreviousY;
                    }

                    if (float.IsInfinity(cmd[i].X) || float.IsNaN(cmd[i].X))
                        x = prevX;
                    else
                        x = cmd[i].X;

                    if (float.IsInfinity(cmd[i].Y) || float.IsNaN(cmd[i].Y))
                        y = prevY;
                    else
                        y = cmd[i].Y;


                    // if(renderOptions["differentiateColors"]&&!renderOptions['showNextLayer']&&!renderOptions['renderAnalysis']) { }

                    if (!cmd[i].Extrude && !cmd[i].NoMove)
                    {
                        if (cmd[i].Retract == -1)
                        {
                            // show retracts
                            if (true)
                            {
                                // void ctx.arc(x, y, radius, startAngle, endAngle [, anticlockwise]);
                                // (prevX, prevY, renderOptions["sizeRetractSpot"], 0, Math.PI*2, true);
                                //var m = GetCircleModel(2, new Vector3D(0, 0, 0), new Point3D(x, y, prevZ), 20);
                                //lines.Add(m);
                                //ctx.strokeStyle = renderOptions["colorRetract"];
                                //ctx.fillStyle = renderOptions["colorRetract"];
                                //ctx.beginPath();
                                //ctx.arc(prevX, prevY, renderOptions["sizeRetractSpot"], 0, Math.PI * 2, true);
                                //ctx.stroke();
                                //ctx.fill()
                            }
                            // show moves
                            if (false)
                            {
                                Points.Add(new Point3D(prevX, prevY, prevZ));
                                Points.Add(new Point3D(x, y, prevZ));

                                //ctx.strokeStyle = renderOptions["colorMove"];
                                //ctx.beginPath();
                                //ctx.moveTo(prevX, prevY);
                                //ctx.lineTo(x * zoomFactor, y * zoomFactor);
                                //ctx.stroke();
                            }
                        }
                    }
                    else if (cmd[i].Extrude)
                    {
                        if (cmd[i].Retract >= 0)
                        {
                            //ctx.strokeStyle = renderOptions["colorLine"][speedIndex];
                        }
                        else if (cmd[i].Retract == -1)
                        {

                        }

                        Points.Add(new Point3D(prevX, prevY, prevZ));
                        Points.Add(new Point3D(x, y, prevZ));
                        //ctx.lineWidth = renderOptions['extrusionWidth'];
                        //ctx.beginPath();
                        //ctx.moveTo(prevX, prevY);
                        //ctx.lineTo(x * zoomFactor, y * zoomFactor);
                        //ctx.stroke();

                    }
                    else
                    {
                        // show retracts
                        if (true)
                        {
                            //ctx.strokeStyle = renderOptions["colorRestart"];
                            //ctx.fillStyle = renderOptions["colorRestart"];
                            //ctx.beginPath();
                            //ctx.arc(prevX, prevY, renderOptions["sizeRetractSpot"], 0, Math.PI * 2, true);
                            //ctx.stroke();
                            //ctx.fill();

                        }
                    }
                    prevX = x;
                    prevY = y;
                }
                return Points;
            }
            catch (Exception exc)
            {
                return Points;
            }
        }
        private async Task<List<Point3D>> getLayerPointsCollectionAsync(int layerNumber, int fromProgress, int toProgress, bool isNextLayer)
        {
            // https://github.com/hudbrog/gCodeViewer/blob/master/js/renderer.js
            var model = Model.Commands;
            List<Point3D> Points = new List<Point3D>();
            try
            {
                int i = 0;
                int speedIndex = 0;
                float prevX = 0;
                float prevY = 0;
                float prevZ = 0;

                if (!isNextLayer)
                {
                    // Store current layer
                    // layerNumStore=layerNum;
                    // progressStore = {from: fromProgress, to: toProgress};
                }
                if (model == null || model.Count == 0 || layerNumber > model.Count)
                    return Points;

                var cmd = model[layerNumber];
                float x = float.NegativeInfinity;
                float y = float.NegativeInfinity;

                if (fromProgress > 0)
                {
                    prevX = cmd[fromProgress - 1].X;
                    prevY = -cmd[fromProgress - 1].Y;
                }
                else if (fromProgress == 0 && layerNumber == 0)
                {
                    if (!float.IsInfinity(model[0][0].X) && !float.IsInfinity(model[0][0].Y))
                    {
                        prevX = model[0][0].X;
                        prevY = -model[0][0].Y;
                    }
                    else
                    {
                        prevX = 0;
                        prevY = 0;
                    }
                }
                else
                {
                    if (model[layerNumber - 1] != null)
                    {
                        prevX = float.NegativeInfinity;
                        prevY = float.NegativeInfinity;
                        for (i = model[layerNumber - 1].Count - 1; i >= 0; i--)
                        {
                            if (float.IsInfinity(prevX) && !float.IsInfinity(model[layerNumber - 1][i].X))
                                prevX = model[layerNumber - 1][i].X;
                            if (float.IsInfinity(prevY) && !float.IsInfinity(model[layerNumber - 1][i].Y))
                                prevY = model[layerNumber - 1][i].Y;
                        }
                        if (float.IsInfinity(prevX))
                            prevX = 0;
                        if (float.IsInfinity(prevY))
                            prevY = 0;
                    }
                    else
                    {
                        prevX = 0;
                        prevY = 0;
                    }

                }

                prevZ = Model.zHeights.Keys.ElementAt(layerNumber);

                for (i = fromProgress; i <= toProgress; i++)
                {
                    // ctx.lineWidth = 1;
                    try
                    {
                        var test = cmd[i];
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    if (!float.IsInfinity(cmd[i].PreviousX) && !float.IsInfinity(cmd[i].PreviousY))
                    {
                        prevX = cmd[i].PreviousX;
                        prevY = cmd[i].PreviousY;
                    }

                    if (float.IsInfinity(cmd[i].X) || float.IsNaN(cmd[i].X))
                        x = prevX;
                    else
                        x = cmd[i].X;

                    if (float.IsInfinity(cmd[i].Y) || float.IsNaN(cmd[i].Y))
                        y = prevY;
                    else
                        y = cmd[i].Y;


                    // if(renderOptions["differentiateColors"]&&!renderOptions['showNextLayer']&&!renderOptions['renderAnalysis']) { }

                    if (!cmd[i].Extrude && !cmd[i].NoMove)
                    {
                        if (cmd[i].Retract == -1)
                        {
                            // show retracts
                            if (true)
                            {
                                // void ctx.arc(x, y, radius, startAngle, endAngle [, anticlockwise]);
                                // (prevX, prevY, renderOptions["sizeRetractSpot"], 0, Math.PI*2, true);
                                //var m = GetCircleModel(2, new Vector3D(0, 0, 0), new Point3D(x, y, prevZ), 20);
                                //lines.Add(m);
                                //ctx.strokeStyle = renderOptions["colorRetract"];
                                //ctx.fillStyle = renderOptions["colorRetract"];
                                //ctx.beginPath();
                                //ctx.arc(prevX, prevY, renderOptions["sizeRetractSpot"], 0, Math.PI * 2, true);
                                //ctx.stroke();
                                //ctx.fill()
                            }
                            // show moves
                            if (true)
                            {
                                
                                Points.Add(new Point3D(prevX, prevY, prevZ));
                                Points.Add(new Point3D(x, y, prevZ));

                                //ctx.strokeStyle = renderOptions["colorMove"];
                                //ctx.beginPath();
                                //ctx.moveTo(prevX, prevY);
                                //ctx.lineTo(x * zoomFactor, y * zoomFactor);
                                //ctx.stroke();
                            }
                        }
                    }
                    else if (cmd[i].Extrude)
                    {
                        if (cmd[i].Retract >= 0)
                        {
                            //ctx.strokeStyle = renderOptions["colorLine"][speedIndex];
                        }
                        else if (cmd[i].Retract == -1)
                        {

                        }

                        Points.Add(new Point3D(prevX, prevY, prevZ));
                        Points.Add(new Point3D(x, y, prevZ));
                        //ctx.lineWidth = renderOptions['extrusionWidth'];
                        //ctx.beginPath();
                        //ctx.moveTo(prevX, prevY);
                        //ctx.lineTo(x * zoomFactor, y * zoomFactor);
                        //ctx.stroke();

                    }
                    else
                    {
                        // show retracts
                        if (true)
                        {
                            //ctx.strokeStyle = renderOptions["colorRestart"];
                            //ctx.fillStyle = renderOptions["colorRestart"];
                            //ctx.beginPath();
                            //ctx.arc(prevX, prevY, renderOptions["sizeRetractSpot"], 0, Math.PI * 2, true);
                            //ctx.stroke();
                            //ctx.fill();

                        }
                    }
                    prevX = x;
                    prevY = y;
                }
                return Points;
            }
            catch (Exception exc)
            {
                return Points;
            }
        }
        
        private List<Point3D> get3dModel()
        {
            List<Point3D> points = new List<Point3D>();
            float prevX = 0;
            float prevY = 0;
            float prevZ = 0;
            int i = 0;
            int j = 0;
            List<GCodeCommand> cmds;
            for(i = 0; i < Model.Commands.Count; i++)
            {
                cmds = Model.Commands[i];
                for(j = 0; j <cmds.Count; j++)
                {
                    GCodeCommand cmd = cmds[j];
                    //if (cmd == null) continue;
                    if (float.IsInfinity(cmd.X))
                        cmd.X = prevX;
                    if (float.IsInfinity(cmd.Y))
                        cmd.Y = prevY;
                    if (float.IsInfinity(cmd.Z))
                        cmd.Z = prevZ;
                    if(!cmd.Extrude)
                    {

                    }
                    else
                    {
                        points.Add(new Point3D(prevX, prevY, prevZ));
                        points.Add(new Point3D(cmd.X, cmd.Y, cmd.Z));
                    }
                    prevX = cmd.X;
                    prevY = cmd.Y;
                    prevZ = cmd.Z;
                }
            }
            return points;
        }

        private void buildModelIteratively()
        {
            var listOfVectors = new List<Vector3D>();
            int i = 0;
            float prevX = 0;
            float prevY = 0;
            float prevZ = 0;
            for(i = 0; i < Model.Commands.Count; i++)
            {
                var vertices = buildModelIteration(i, ref prevX, ref prevY, ref prevZ);
                listOfVectors.AddRange(vertices);
            }
            ModelVisual3D mod = new ModelVisual3D();
            GeometryModel3D geometry = new GeometryModel3D();
            MeshGeometry3D meshGeometry = new MeshGeometry3D();

            foreach (Vector3D vect in listOfVectors)
            {
                meshGeometry.Positions.Add(new Point3D(vect.X, vect.Y, vect.Z));
                
                //meshGeometry.TriangleIndices.Add(cnt)
            }
            Point3DCollection col = new Point3DCollection(listOfVectors.Select(vect => 
                { 
                    return new Point3D(vect.X, vect.Y, vect.Z); 
                }));
            meshGeometry.CalculateNormals();
            
            meshGeometry.Validate();
            geometry.Geometry = meshGeometry;
            geometry.Transform = new TranslateTransform3D(2,0,-1);
            geometry.Material = new DiffuseMaterial(Brushes.White);
            mod.Content = geometry;

            this.Visual3dModel = mod;
        }

        // http://csharphelper.com/blog/2014/10/draw-a-smooth-3d-surface-with-wpf-xaml-and-c/
        private void AddTriangle(MeshGeometry3D mesh,
            Point3D point1, Point3D point2, Point3D point3)
        {
            // Get the points' indices.
            int index1 = AddPoint(mesh.Positions, point1);
            int index2 = AddPoint(mesh.Positions, point2);
            int index3 = AddPoint(mesh.Positions, point3);

            // Create the triangle.
            mesh.TriangleIndices.Add(index1);
            mesh.TriangleIndices.Add(index2);
            mesh.TriangleIndices.Add(index3);
        }

        // If the point already exists, return its index.
        // Otherwise create the point and return its new index.
        private int AddPoint(Point3DCollection points, Point3D point)
        {
            // See if the point exists.
            for (int i = 0; i < points.Count; i++)
            {
                if ((point.X == points[i].X) &&
                    (point.Y == points[i].Y) &&
                    (point.Z == points[i].Z))
                    return i;
            }

            // We didn't find the point. Create it.
            points.Add(point);
            return points.Count - 1;
        }
        private List<Vector3D> buildModelIteration(int layerNumber, ref float prevX, ref float prevY, ref float prevZ)
        {
            int j = 0;
            var vertices = new List<Vector3D>();
            try
            {
                List<GCodeCommand> cmds = Model.Commands[layerNumber];
                for(j = 0; j < cmds.Count; j++)
                {
                    GCodeCommand cmd = cmds[j];
                    //if (!cmds[j]) continue;
                    if (float.IsNaN(cmd.X) || float.IsInfinity(cmd.X))
                        cmd.X = prevX;
                    if (float.IsNaN(cmd.Y) || float.IsInfinity(cmd.Y))
                        cmd.Y = prevY;
                    if (float.IsNaN(cmd.Z) || float.IsInfinity(cmd.Z))
                        cmd.Z = prevZ;
                    if(!cmd.Extrude)
                    {

                    }
                    else
                    {
                        vertices.Add(new Vector3D(prevX, prevY, prevZ));
                        vertices.Add(new Vector3D(cmd.X, cmd.Y, cmd.Z));
                    }
                    prevX = cmd.X;
                    prevY = cmd.Y;
                    prevZ = cmd.Z;
                }
            }
            catch(Exception exc)
            {

            }
            return vertices;
        }
        #region GViewer
        // Source: https://github.com/xpix/XGView
        private void DrawLine(LinesVisual3D lines, double x_start, double y_start, double z_start, double x_stop, double y_stop, double z_stop)
        {
            lines.Points.Add(new Point3D(x_start, y_start, z_start));
            lines.Points.Add(new Point3D(x_stop, y_stop, z_stop));
        }
        private void DrawLine(LinesVisual3D lines, Point3D start, Point3D end)
        {
            lines.Points.Add(start);
            lines.Points.Add(end);
        }

        private void DrawArc(double x_start, double y_start, double z_start,
                             double x_stop, double y_stop, double z_stop,
                             double j_pos, double i_pos,
                             bool absoluteIJKMode, bool clockwise)
        {
            Point3D initial = new Point3D(x_start, y_start, z_start);
            Point3D nextpoint = new Point3D(x_stop, y_stop, z_stop);
            double k = Double.NaN;
            double radius = Double.NaN;

            Point3D center = updateCenterWithCommand(initial, nextpoint, j_pos, i_pos, k, radius, absoluteIJKMode, clockwise);
            List<Point3D> kreispunkte = generatePointsAlongArcBDring(initial, nextpoint, center, clockwise, 0, 10); // Dynamic resolution

            Point3D old_point = new Point3D();
            foreach (Point3D point in kreispunkte)
            {
                if (old_point.X != 0)
                {
                    DrawLine(normalmoves, old_point, point);
                }
                old_point = point;
            }
        }

        private static List<Point3D> generatePointsAlongArcBDring(Point3D p1, Point3D p2, Point3D center, bool isCw, double R, int arcResolution)
        {
            double radius = R;
            double sweep;

            // Calculate radius if necessary.
            if (radius == 0)
            {
                radius = Math.Sqrt(Math.Pow(p1.X - center.X, 2.0) + Math.Pow(p1.Y - center.Y, 2.0));
            }

            // Calculate angles from center.
            double startAngle = getAngle(center, p1);
            double endAngle = getAngle(center, p2);

            // Fix semantics, if the angle ends at 0 it really should end at 360.
            if (endAngle == 0)
            {
                endAngle = Math.PI * 2;
            }

            // Calculate distance along arc.
            if (!isCw && endAngle < startAngle)
            {
                sweep = ((Math.PI * 2 - startAngle) + endAngle);
            }
            else if (isCw && endAngle > startAngle)
            {
                sweep = ((Math.PI * 2 - endAngle) + startAngle);
            }
            else
            {
                sweep = Math.Abs(endAngle - startAngle);
            }

            return generatePointsAlongArcBDring(p1, p2, center, isCw, radius, startAngle, endAngle, sweep, arcResolution);
        }

        private static List<Point3D> generatePointsAlongArcBDring(Point3D p1,
                Point3D p2, Point3D center, bool isCw, double radius,
                double startAngle, double endAngle, double sweep, int numPoints)
        {

            Point3D lineEnd = p2;
            List<Point3D> segments = new List<Point3D>();
            double angle;

            double zIncrement = (p2.Z - p1.Z) / numPoints;
            for (int i = 0; i < numPoints; i++)
            {
                if (isCw)
                {
                    angle = (startAngle - i * sweep / numPoints);
                }
                else
                {
                    angle = (startAngle + i * sweep / numPoints);
                }

                if (angle >= Math.PI * 2)
                {
                    angle = angle - Math.PI * 2;
                }

                lineEnd.X = Math.Cos(angle) * radius + center.X;
                lineEnd.Y = Math.Sin(angle) * radius + center.Y;
                lineEnd.Z += zIncrement;

                segments.Add(lineEnd);
            }

            segments.Add(p2);

            return segments;
        }


        private static double getAngle(Point3D start, Point3D end)
        {
            double deltaX = end.X - start.X;
            double deltaY = end.Y - start.Y;

            double angle = 0.0;

            if (deltaX != 0)
            { // prevent div by 0
                // it helps to know what quadrant you are in
                if (deltaX > 0 && deltaY >= 0)
                {  // 0 - 90
                    angle = Math.Atan(deltaY / deltaX);
                }
                else if (deltaX < 0 && deltaY >= 0)
                { // 90 to 180
                    angle = Math.PI - Math.Abs(Math.Atan(deltaY / deltaX));
                }
                else if (deltaX < 0 && deltaY < 0)
                { // 180 - 270
                    angle = Math.PI + Math.Abs(Math.Atan(deltaY / deltaX));
                }
                else if (deltaX > 0 && deltaY < 0)
                { // 270 - 360
                    angle = Math.PI * 2 - Math.Abs(Math.Atan(deltaY / deltaX));
                }
            }
            else
            {
                // 90 deg
                if (deltaY > 0)
                {
                    angle = Math.PI / 2.0;
                }
                // 270 deg
                else
                {
                    angle = Math.PI * 3.0 / 2.0;
                }
            }

            return angle;
        }


        private static Point3D updateCenterWithCommand(Point3D initial, Point3D nextPoint,
                        double j, double i, double k, double radius,
                        bool absoluteIJKMode, bool clockwise)
        {
            if (Double.IsNaN(i) && Double.IsNaN(j) && Double.IsNaN(k))
            {
                return convertRToCenter(initial, nextPoint, radius, absoluteIJKMode, clockwise);
            }

            Point3D newPoint = new Point3D();

            if (absoluteIJKMode)
            {
                if (!Double.IsNaN(i))
                {
                    newPoint.X = i;
                }
                if (!Double.IsNaN(j))
                {
                    newPoint.Y = j;
                }
                if (!Double.IsNaN(k))
                {
                    newPoint.Z = k;
                }
            }
            else
            {
                if (!Double.IsNaN(i))
                {
                    newPoint.X = initial.X + i;
                }
                if (!Double.IsNaN(j))
                {
                    newPoint.Y = initial.Y + j;
                }
                if (!Double.IsNaN(k))
                {
                    newPoint.Z = initial.Z + k;
                }
            }

            return newPoint;
        }

        private static Point3D convertRToCenter(Point3D start, Point3D end, double radius, bool absoluteIJK, bool clockwise)
        {
            double R = radius;
            Point3D center = new Point3D();

            // This math is copied from GRBL in gcode.c
            double x = end.X - start.X;
            double y = end.Y - start.Y;

            double h_x2_div_d = 4 * R * R - x * x - y * y;
            if (h_x2_div_d < 0) { Console.Write("Error computing arc radius."); }
            h_x2_div_d = (-Math.Sqrt(h_x2_div_d)) / Hypotenuse(x, y);

            if (clockwise == false)
            {
                h_x2_div_d = -h_x2_div_d;
            }

            // Special message from gcoder to software for which radius
            // should be used.
            if (R < 0)
            {
                h_x2_div_d = -h_x2_div_d;
                // TODO: Places that use this need to run ABS on radius.
                radius = -radius;
            }

            double offsetX = 0.5 * (x - (y * h_x2_div_d));
            double offsetY = 0.5 * (y + (x * h_x2_div_d));

            if (!absoluteIJK)
            {
                center.X = start.X + offsetX;
                center.Y = start.Y + offsetY;
            }
            else
            {
                center.X = offsetX;
                center.Y = offsetY;
            }

            return center;
        }

        private static double Hypotenuse(double a, double b)
        {
            return Math.Sqrt(Math.Pow(a, 2) + Math.Pow(b, 2));
        }


        #endregion

        #endregion
    }
}