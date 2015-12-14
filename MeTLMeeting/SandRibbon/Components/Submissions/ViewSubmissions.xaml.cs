﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Practices.Composite.Presentation.Commands;
using MeTLLib.DataTypes;
using MeTLLib.Providers.Connection;

namespace SandRibbon.Components.Submissions
{
    public partial class ViewSubmissions : Window
    {
        public ObservableCollection<TargettedSubmission> submissions { get; set; }
        public ConvertMeTLIdentityStringToImageSource ConvertMeTLIdentityStringToImageSource
        {
            get; protected set;
        }
        public NetworkController controller { get; protected set; }
        public ViewSubmissions(NetworkController _controller, List<TargettedSubmission> userSubmissions)
        {
            InitializeComponent();
            controller = _controller;
            ConvertMeTLIdentityStringToImageSource = new ConvertMeTLIdentityStringToImageSource(controller);
            submissions = new ObservableCollection<TargettedSubmission>(userSubmissions);
            Submissions.ItemsSource = submissions;
            Commands.ReceiveScreenshotSubmission.RegisterCommand(new DelegateCommand<TargettedSubmission>(recieveSubmission));
            Commands.JoiningConversation.RegisterCommand(new DelegateCommand<string>(close));
            Commands.ShowConversationSearchBox.RegisterCommand(new DelegateCommand<object>(close));
        }

        private void close(object obj)
        {
            Dispatcher.adopt(delegate
            {

                Close();
            });
        }
        private void recieveSubmission(TargettedSubmission submission)
        {
            if (!String.IsNullOrEmpty(submission.target) && submission.target != "submission")
                return;
            Dispatcher.adopt(delegate
            {
                submissions.Add(submission);
            });
        }

        private void importAllSubmissionsInBucket(object sender, ExecutedRoutedEventArgs e)
        {
            var items = Submissions.SelectedItems;
            var imagesToDrop = new List<ImageDrop>();
            var height = 0;
            foreach (var elem in items)
            {
                var image = (TargettedSubmission)elem;
                imagesToDrop.Add(new ImageDrop
                {
                    Filename = image.url.ToString(),
                    Target = "presentationSpace",
                    Point = new Point(0, height),
                    Position = 1,
                    OverridePoint = true
                });
                height += 540;
            }
            Commands.ImagesDropped.Execute(imagesToDrop);
        }
        private void canImportSubmission(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Submissions.SelectedItems.Count > 0;
        }
    }
}