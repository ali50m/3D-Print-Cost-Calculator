﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfFramework.Views
{
    /// <summary>
    /// Interaktionslogik für CredentialsMasterPasswordDialog.xaml
    /// </summary>
    public partial class CredentialsMasterPasswordDialog : UserControl
    {
        public CredentialsMasterPasswordDialog()
        {
            InitializeComponent();
        }
        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // Need to be in loaded event, focusmanger won't work...
            PasswordBoxPassword.Focus();
        }
    }
}
