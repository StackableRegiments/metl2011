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

namespace SandRibbon.Components.Submissions
{
    public class ScreenshotDetails
    {
        public string message;
        public long time;
        public bool showPrivate;
    }
    public partial class ScreenshotSubmission : UserControl
    {
        public List<TargettedSubmission> submissionList = new List<TargettedSubmission>();
        public ScreenshotSubmission()
        {
            InitializeComponent();
            Commands.ReceiveScreenshotSubmission.RegisterCommand(new DelegateCommand<TargettedSubmission>(receiveSubmission));
            Commands.UpdateConversationDetails.RegisterCommandToDispatcher(new DelegateCommand<ConversationDetails>(detailsChanged));
            Commands.JoinConversation.RegisterCommand(new DelegateCommand<object>(conversationChanged));
            Commands.PreParserAvailable.RegisterCommand(new DelegateCommand<PreParser>(PreParserAvailable));
            Commands.ViewSubmissions.RegisterCommand(new DelegateCommand<object>(viewSubmissions, canViewSubmissions));
            conversationChanged(null);
        }
        private void viewSubmissions(object _obj)
        {
            var view = new ViewSubmissions(submissionList);
            view.Owner = Window.GetWindow(this);
            view.Show();
        }
        private bool canViewSubmissions(object _e)
        {
           return submissionList.Count > 0;
        }
        private void PreParserAvailable(PreParser parser)
        {
            foreach(var submission in parser.submissions)
                receiveSubmission(submission);
        }
        private void detailsChanged(ConversationDetails details)
        {
            if (ConversationDetails.Empty.Equals(details)) return;
            try
            {
                if (Globals.conversationDetails.Author == Globals.me)
                    amTeacher();
                else
                    amStudent();
            }
            catch(NotSetException)
            {
            }
      
        }
        private void conversationChanged(object details)
        {
            Dispatcher.adoptAsync( delegate
                                                {
                                                    try
                                                    {
                                                        submissionList = new List<TargettedSubmission>();
                                                        if (Globals.conversationDetails.Author == Globals.me)
                                                            amTeacher();
                                                        else
                                                            amStudent();
                                                    }
                                                    catch(NotSetException)
                                                    {
                                                    }
      
                                                });
        }
        private void amTeacher()
        {
            submitSubmission.Visibility = Visibility.Collapsed;
            viewSubmission.Visibility = Visibility.Visible;

        }
        private void amStudent()
        {
            submitSubmission.Visibility = Visibility.Visible;
            viewSubmission.Visibility = Visibility.Collapsed;
        }
        private void receiveSubmission(MeTLLib.DataTypes.TargettedSubmission submission)
        {
            if (!IHaveThisSubmission(submission))
            {
                submissionList.Add(submission);
                Commands.RequerySuggested(Commands.ViewSubmissions);
            }
        }
        private bool IHaveThisSubmission(MeTLLib.DataTypes.TargettedSubmission submission)
        {
            if (submissionList.Where(s => s.time == submission.time && s.author == submission.author && s.url == submission.url).ToList().Count > 0)
                return true;
            return false;
        }
        private void generateScreenshot(object sender, RoutedEventArgs e)
        {
            Trace.TraceInformation("SubmittedScreenshot");
            var time = SandRibbonObjects.DateTimeFactory.Now().Ticks;
            DelegateCommand<string> sendScreenshot = null;
            sendScreenshot = new DelegateCommand<string>(hostedFileName =>
                             {
                                 Commands.ScreenshotGenerated.UnregisterCommand(sendScreenshot);
                                 MeTLLib.ClientFactory.Connection().uploadAndSendSubmission(new MeTLStanzas.LocalSubmissionInformation
                                 (MeTLLib.ClientFactory.Connection().location.currentSlide,Globals.me,"submission","public",hostedFileName));
                                 MeTLMessage.Information("Submission sent to " + Globals.conversationDetails.Author);
                             });
            Commands.ScreenshotGenerated.RegisterCommand(sendScreenshot);
            Commands.GenerateScreenshot.ExecuteAsync(new ScreenshotDetails
                                                    {
                                                        time = time,
                                                        message = string.Format("Submission at {0}", new DateTime(time)),
                                                        showPrivate = true
                                                    });
        }
     
    }
}
