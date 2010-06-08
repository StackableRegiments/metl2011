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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace SandRibbon.Components
{
    public partial class SlideNavigationControls : UserControl
    {
        public SlideNavigationControls()
        {
            InitializeComponent();
        }
        private void toggleSync(object sender, RoutedEventArgs e)
        {
            Commands.SetSync.Execute(null);
            BitmapImage source;
            var synced = new Uri(Directory.GetCurrentDirectory() + "\\Resources\\SyncRed.png");
            var deSynced = new Uri(Directory.GetCurrentDirectory() + "\\Resources\\SyncGreen.png");
            if(syncButton.Icon.ToString().Contains("SyncGreen"))
                source = new BitmapImage(synced);
            else
                source = new BitmapImage(deSynced);
            syncButton.Icon = source;
        }
    }
}
