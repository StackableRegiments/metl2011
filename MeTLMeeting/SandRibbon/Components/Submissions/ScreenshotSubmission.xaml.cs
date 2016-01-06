﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Practices.Composite.Presentation.Commands;
using SandRibbon.Providers;
using MeTLLib.DataTypes;
using MeTLLib.Providers.Connection;
using System.Diagnostics;
using SandRibbon.Components.Utility;
using System.Windows.Media;

namespace SandRibbon.Components.Submissions
{
    public class ScreenshotDetails
    {
        public string author;
        public string message;
        public long time;
        public bool showPrivate;
        public Size dimensions;
        public Action<byte[]> onGeneration;
    }
    public partial class ScreenshotSubmission : UserControl
    {
        public List<TargettedSubmission> submissionList = new List<TargettedSubmission>();
        public ScreenshotSubmission()
        {
            InitializeComponent();
            Commands.ReceiveScreenshotSubmission.RegisterCommand(new DelegateCommand<TargettedSubmission>(receiveSubmission));
            Commands.UpdateConversationDetails.RegisterCommand(new DelegateCommand<ConversationDetails>(detailsChanged));
            Commands.JoinConversation.RegisterCommand(new DelegateCommand<object>(conversationChanged));
            Commands.PreParserAvailable.RegisterCommand(new DelegateCommand<PreParser>(PreParserAvailable));
            Commands.ViewSubmissions.RegisterCommand(new DelegateCommand<object>(viewSubmissions, canViewSubmissions));
            conversationChanged(null);
        }
        private void viewSubmissions(object _obj)
        {
            var view = new ViewSubmissions();
            view.Owner = Window.GetWindow(this);
            view.Show();
        }
        private bool canViewSubmissions(object _e)
        {
            return submissionList.Count > 0;
        }
        private void PreParserAvailable(PreParser parser)
        {
            foreach (var submission in parser.submissions)
                receiveSubmission(submission);
        }
        private void detailsChanged(ConversationDetails details)
        {
            if (ConversationDetails.Empty.Equals(details)) return;
            Dispatcher.adopt(delegate
            {
                try
                {
                    if (Globals.conversationDetails.Author == Globals.me)
                        amTeacher();
                    else
                        amStudent();
                }
                catch (NotSetException)
                {
                }

            });
        }
        private void conversationChanged(object details)
        {
            Dispatcher.adoptAsync(delegate
                                               {
                                                   try
                                                   {
                                                       submissionList = new List<TargettedSubmission>();
                                                       if (Globals.conversationDetails.Author == Globals.me)
                                                           amTeacher();
                                                       else
                                                           amStudent();
                                                   }
                                                   catch (NotSetException)
                                                   {
                                                   }

                                               });
        }
        private void amTeacher()
        {
            viewSubmission.Visibility = Visibility.Visible;

        }
        private void amStudent()
        {
            viewSubmission.Visibility = Visibility.Collapsed;
        }
        private void receiveSubmission(MeTLLib.DataTypes.TargettedSubmission submission)
        {
            if (!String.IsNullOrEmpty(submission.target) && submission.target != "submission")
                return;

            if (!IHaveThisSubmission(submission))
            {
                submissionList.Add(submission);
                Commands.RequerySuggested(Commands.ViewSubmissions);
            }
        }
        private bool IHaveThisSubmission(TargettedSubmission submission)
        {
            if (submissionList.Where(s => s.time == submission.time && s.author == submission.author && s.url == submission.url).ToList().Count > 0)
                return true;
            return false;
        }
        private void generateScreenshot(object sender, RoutedEventArgs e)
        {
            var time = SandRibbonObjects.DateTimeFactory.Now().Ticks;
            Commands.GenerateScreenshot.ExecuteAsync(new ScreenshotDetails
            {
                time = time,
                message = string.Format("Submission by {1} at {0}", new DateTime(time), Globals.me),
                showPrivate = true,
                author = Globals.me,
                onGeneration = bytes =>
                {
                    App.controller.client.UploadAndSendSubmission(
                        new MeTLStanzas.LocalSubmissionInformation(
                            App.controller.client.location.currentSlide.id, 
                            Globals.me, 
                            "submission",
                            Privacy.Public, 
                            -1L, 
                            bytes, 
                            Globals.conversationDetails.Title, 
                            new Dictionary<string, Color>(), 
                            Globals.generateId()));
                    MeTLMessage.Information("Submission sent to " + Globals.conversationDetails.Author);
                }
            });
        }
    }
}
