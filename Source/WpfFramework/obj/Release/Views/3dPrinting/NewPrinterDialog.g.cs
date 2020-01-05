﻿#pragma checksum "..\..\..\..\Views\3dPrinting\NewPrinterDialog.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "4AC5D534969EB85B7D15422B49923EEA7F44F28BCF9F88233005CEED600C854B"
//------------------------------------------------------------------------------
// <auto-generated>
//     Dieser Code wurde von einem Tool generiert.
//     Laufzeitversion:4.0.30319.42000
//
//     Änderungen an dieser Datei können falsches Verhalten verursachen und gehen verloren, wenn
//     der Code erneut generiert wird.
// </auto-generated>
//------------------------------------------------------------------------------

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using MahApps.Metro.IconPacks.Converter;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using WpfFramework.Converters;
using WpfFramework.Models._3dprinting;
using WpfFramework.Resources.Localization;
using WpfFramework.Validators;
using WpfFramework.ViewModels._3dPrinting;


namespace WpfFramework.Views._3dPrinting {
    
    
    /// <summary>
    /// NewPrinterDialog
    /// </summary>
    public partial class NewPrinterDialog : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 71 "..\..\..\..\Views\3dPrinting\NewPrinterDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox cbProcessTechnology;
        
        #line default
        #line hidden
        
        
        #line 103 "..\..\..\..\Views\3dPrinting\NewPrinterDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox tbModel;
        
        #line default
        #line hidden
        
        
        #line 123 "..\..\..\..\Views\3dPrinting\NewPrinterDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal MahApps.Metro.Controls.NumericUpDown nudPowerConsumption;
        
        #line default
        #line hidden
        
        
        #line 143 "..\..\..\..\Views\3dPrinting\NewPrinterDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal MahApps.Metro.Controls.NumericUpDown nudNozzleTemp;
        
        #line default
        #line hidden
        
        
        #line 176 "..\..\..\..\Views\3dPrinting\NewPrinterDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox ckbHeatbed;
        
        #line default
        #line hidden
        
        
        #line 192 "..\..\..\..\Views\3dPrinting\NewPrinterDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal MahApps.Metro.Controls.NumericUpDown nudHeatbedTemp;
        
        #line default
        #line hidden
        
        
        #line 230 "..\..\..\..\Views\3dPrinting\NewPrinterDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal MahApps.Metro.Controls.NumericUpDown nudX;
        
        #line default
        #line hidden
        
        
        #line 242 "..\..\..\..\Views\3dPrinting\NewPrinterDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal MahApps.Metro.Controls.NumericUpDown nudY;
        
        #line default
        #line hidden
        
        
        #line 254 "..\..\..\..\Views\3dPrinting\NewPrinterDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal MahApps.Metro.Controls.NumericUpDown nudZ;
        
        #line default
        #line hidden
        
        
        #line 277 "..\..\..\..\Views\3dPrinting\NewPrinterDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal MahApps.Metro.Controls.NumericUpDown nudPrice;
        
        #line default
        #line hidden
        
        
        #line 314 "..\..\..\..\Views\3dPrinting\NewPrinterDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal MahApps.Metro.Controls.NumericUpDown nudMHR;
        
        #line default
        #line hidden
        
        
        #line 402 "..\..\..\..\Views\3dPrinting\NewPrinterDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox tbReorderUri;
        
        #line default
        #line hidden
        
        
        #line 416 "..\..\..\..\Views\3dPrinting\NewPrinterDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button btnSave;
        
        #line default
        #line hidden
        
        
        #line 456 "..\..\..\..\Views\3dPrinting\NewPrinterDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button btnCancel;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/3dPrintCostCalculator2;component/views/3dprinting/newprinterdialog.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\Views\3dPrinting\NewPrinterDialog.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal System.Delegate _CreateDelegate(System.Type delegateType, string handler) {
            return System.Delegate.CreateDelegate(delegateType, this, handler);
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.cbProcessTechnology = ((System.Windows.Controls.ComboBox)(target));
            return;
            case 2:
            this.tbModel = ((System.Windows.Controls.TextBox)(target));
            return;
            case 3:
            this.nudPowerConsumption = ((MahApps.Metro.Controls.NumericUpDown)(target));
            return;
            case 4:
            this.nudNozzleTemp = ((MahApps.Metro.Controls.NumericUpDown)(target));
            return;
            case 5:
            this.ckbHeatbed = ((System.Windows.Controls.CheckBox)(target));
            return;
            case 6:
            this.nudHeatbedTemp = ((MahApps.Metro.Controls.NumericUpDown)(target));
            return;
            case 7:
            this.nudX = ((MahApps.Metro.Controls.NumericUpDown)(target));
            return;
            case 8:
            this.nudY = ((MahApps.Metro.Controls.NumericUpDown)(target));
            return;
            case 9:
            this.nudZ = ((MahApps.Metro.Controls.NumericUpDown)(target));
            return;
            case 10:
            this.nudPrice = ((MahApps.Metro.Controls.NumericUpDown)(target));
            return;
            case 11:
            this.nudMHR = ((MahApps.Metro.Controls.NumericUpDown)(target));
            return;
            case 12:
            this.tbReorderUri = ((System.Windows.Controls.TextBox)(target));
            return;
            case 13:
            this.btnSave = ((System.Windows.Controls.Button)(target));
            return;
            case 14:
            this.btnCancel = ((System.Windows.Controls.Button)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}
