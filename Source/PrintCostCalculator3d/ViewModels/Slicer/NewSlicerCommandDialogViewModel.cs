﻿using log4net;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using PrintCostCalculator3d.Models.Settings;
using PrintCostCalculator3d.Models.Slicer;
using PrintCostCalculator3d.Resources.Localization;
using PrintCostCalculator3d.Utilities;

namespace PrintCostCalculator3d.ViewModels.Slicer
{
    class NewSlicerCommandDialogViewModel : ViewModelBase
    {
        #region Variables
        private readonly IDialogCoordinator _dialogCoordinator;
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly bool _isLoading;
        #endregion

        #region Properties
        private bool _isEdit;
        public bool IsEdit
        {
            get => _isEdit;
            set
            {
                if (value == _isEdit)
                    return;

                _isEdit = value;
                OnPropertyChanged();
            }
        }

        private Models.Slicer.Slicer _slicerName;
        public Models.Slicer.Slicer SlicerName
        {
            get => _slicerName;
            set
            {
                if (_slicerName == value) return;

                if (!_isLoading)
                    SettingsManager.Current.Slicer_LastUsed = value;

                _slicerName = value;
                OnPropertyChanged();


            }
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _autoAddFilePath = true;
        public bool AutoAddFilePath
        {
            get => _autoAddFilePath;
            set
            {
                if (_autoAddFilePath == value) return;
                
                _autoAddFilePath = value;
                OnPropertyChanged();
                
            }
        }
        private string _slicerCommand = string.Empty;
        public string SlicerCommand
        {
            get => _slicerCommand;
            set
            {
                if (_slicerCommand != value)
                {
                    _slicerCommand = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _outputFileFormat = "([filename])+([A-Za-z0-9-_.])+(.gcode|.gco|.gc)$";
        public string OutputFileFormat
        {
            get => _outputFileFormat;
            set
            {
                if (_outputFileFormat == value) return;

                _outputFileFormat = value;
                OnPropertyChanged();

            }
        }
        #endregion

        #region Slicers
        private ObservableCollection<Models.Slicer.Slicer> _slicers = new ObservableCollection<Models.Slicer.Slicer>();
        public ObservableCollection<Models.Slicer.Slicer> Slicers
        {
            get => _slicers;
            set
            {
                if (_slicers == value) return;
                _slicers = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Constructor
        public NewSlicerCommandDialogViewModel(
            Action<NewSlicerCommandDialogViewModel> saveCommand, 
            Action<NewSlicerCommandDialogViewModel> cancelHandler,
            SlicerCommand cmd = null
            )
        {

            SaveCommand = new RelayCommand(p => saveCommand(this));
            CancelCommand = new RelayCommand(p => cancelHandler(this));
            try
            {
                _isLoading = true;
                LoadSettings();
                _isLoading = false;

                IsEdit = cmd != null;
                try
                {
                    var tempCMD = cmd ?? new SlicerCommand();
                    if (tempCMD != null)
                    {
                        Name = tempCMD.Name;
                        SlicerName = tempCMD.Slicer;
                        SlicerCommand = tempCMD.Command;
                        AutoAddFilePath = tempCMD.AutoAddFilePath;
                        OutputFileFormat = tempCMD.OutputFilePatternString;
                    }

                }
                catch (Exception exc)
                {
                    logger.Error(string.Format(Strings.EventExceptionOccurredFormated, exc.TargetSite, exc.Message));
                }

                logger.Info(string.Format(Strings.EventViewInitFormated, this.GetType().Name));
            }
            catch (Exception exc)
            {
                logger.Error(string.Format(Strings.EventExceptionOccurredFormated, exc.TargetSite, exc.Message));
            }

        }

        public NewSlicerCommandDialogViewModel(
            Action<NewSlicerCommandDialogViewModel> saveCommand, 
            Action<NewSlicerCommandDialogViewModel> cancelHandler, 
            IDialogCoordinator dialogCoordinator,
            SlicerCommand cmd = null
            )
        {
            this._dialogCoordinator = dialogCoordinator;

            SaveCommand = new RelayCommand(p => saveCommand(this));
            CancelCommand = new RelayCommand(p => cancelHandler(this));
            try
            {
                _isLoading = true;
                LoadSettings();
                _isLoading = false;

                IsEdit = cmd != null;
                try
                {
                    var tempCMD = cmd ?? new SlicerCommand();
                    if (tempCMD != null)
                    {
                        Name = tempCMD.Name;
                        SlicerName = tempCMD.Slicer;
                        SlicerCommand = tempCMD.Command;
                        AutoAddFilePath = tempCMD.AutoAddFilePath;
                        OutputFileFormat = tempCMD.OutputFilePatternString;
                    }

                }
                catch (Exception exc)
                {
                    logger.Error(string.Format(Strings.EventExceptionOccurredFormated, exc.TargetSite, exc.Message));
                }

                logger.Info(string.Format(Strings.EventViewInitFormated, this.GetType().Name));
            }
            catch (Exception exc)
            {
                logger.Error(string.Format(Strings.EventExceptionOccurredFormated, exc.TargetSite, exc.Message));
            }
        }


        private void LoadSettings()
        {
            Slicers = SettingsManager.Current.Slicers;
            if (Slicers.Count > 0)
            {
                if (SettingsManager.Current.Slicer_LastUsed != null)
                {
                    SlicerName = Slicers.FirstOrDefault(slicer => slicer.Equals(SettingsManager.Current.Slicer_LastUsed));
                }
                if (SlicerName == null)
                {
                    try
                    {
                        if (Slicers.Count > 0)
                        {
                            SlicerName = Slicers[0];
                            SettingsManager.Current.Slicer_LastUsed = SlicerName;
                        }
                    }
                    catch (Exception exc)
                    {
                        logger.ErrorFormat(Strings.EventExceptionOccurredFormated, exc.Message, exc.TargetSite);
                    }
                }
            }
        }

        #endregion

        #region Methods

        #endregion

        #region Events

        #endregion

        #region iCommands & Actions
        public ICommand SaveCommand { get; }

        public ICommand CancelCommand { get; }
        #endregion
    }
}