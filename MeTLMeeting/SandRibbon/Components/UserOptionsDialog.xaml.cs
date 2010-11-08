﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using SandRibbon.Providers;
using MeTLLib.DataTypes;

namespace SandRibbon.Components
{
    public partial class UserOptionsDialog : Window
    {
        public UserOptionsDialog()
        {
            InitializeComponent();
            DataContext = Globals.UserOptions;
        }
        private void Apply(object sender, RoutedEventArgs e)
        {
            Commands.SetUserOptions.Execute(DataContext);
            Close();
        }
        private void Cancel(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void Reset(object sender, RoutedEventArgs e)
        {
            Commands.SetUserOptions.Execute(UserOptions.DEFAULT);
            Close();
        }
    }
}
