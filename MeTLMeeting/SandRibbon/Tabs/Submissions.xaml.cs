﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Microsoft.Practices.Composite.Presentation.Commands;
using SandRibbon.Components.Submissions;
using SandRibbon.Quizzing;
using SandRibbon.Utils.Connection;
using SandRibbonInterop;
using SandRibbon.Providers;
using SandRibbonInterop.MeTLStanzas;

namespace SandRibbon.Tabs
{
    public partial class Submissions : Divelements.SandRibbon.RibbonTab
    {
        private List<FileInfo> files; 
        public Submissions()
        {
            InitializeComponent();
            files = new List<FileInfo>();
            Commands.ReceiveFileResource.RegisterCommand(new DelegateCommand<TargettedFile>(receiveFile));
        }

        private void receiveFile(TargettedFile file)
        {
           Dispatcher.adoptAsync(() => files.Add(new FileInfo
                                                     {
                                                         fileType = FileUploads.getFileType(file.url),
                                                         filename = FileUploads.getFileName(file.url),
                                                         url = file.url,
                                                         author = file.author,
                                                         fileImage = FileUploads.getFileImage(file.url)
                                                     }));
        }

        private void openUploads(object sender, RoutedEventArgs e)
        {
            new FileUploads(files).Show();
        }
    }
}