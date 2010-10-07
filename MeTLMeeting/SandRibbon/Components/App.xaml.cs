﻿using System;
using System.Collections.Specialized;
using System.Deployment.Application;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using SandRibbon.Utils;
using System.Security.Permissions;
using SandRibbon.Providers;
using SandRibbon.Utils.Connection;
using SandRibbon.Quizzing;
using Microsoft.Practices.Composite.Presentation.Commands;

[assembly:UIPermission(SecurityAction.RequestMinimum)]
namespace SandRibbon
{
    public partial class App : Application
    {
        private bool loggingOut = false;

        public static string Now(string title){
            var now = SandRibbonObjects.DateTimeFactory.Now();
            var s = string.Format("{2} {0}:{1}", now, now.Millisecond, title);
            Logger.Log(s);
            Console.WriteLine(s);
            return s;
        }
        static App() {
            Now("Static App start");
        }
        private void LogOut(object _Unused)
        {
            loggingOut = true;
            WorkspaceStateProvider.ClearSettings();
            ThumbnailProvider.ClearThumbnails();
            Application.Current.Shutdown();
        }
        
        protected override void OnStartup(StartupEventArgs e)
        {
            //This is to ensure that all the static constructors are called.
            base.OnStartup(e);
            new Worm();
            new Printer();
            new CommandParameterProvider();
            Commands.LogOut.RegisterCommand(new DelegateCommand<object>(LogOut));
            new SandRibbonInterop.MeTLStanzas.MeTLStanzas.Ink();
            new SandRibbonInterop.MeTLStanzas.MeTLStanzas.Quiz();
            new SandRibbonInterop.MeTLStanzas.MeTLStanzas.Image();
            new SandRibbonInterop.MeTLStanzas.MeTLStanzas.Video();
            new SandRibbonInterop.MeTLStanzas.MeTLStanzas.Bubble();
            new SandRibbonInterop.MeTLStanzas.MeTLStanzas.TextBox();
            new SandRibbonInterop.MeTLStanzas.MeTLStanzas.DirtyInk();
            new SandRibbonInterop.MeTLStanzas.MeTLStanzas.DirtyText();
            new SandRibbonInterop.MeTLStanzas.MeTLStanzas.AutoShape();
            new SandRibbonInterop.MeTLStanzas.MeTLStanzas.DirtyImage();
            new SandRibbonInterop.MeTLStanzas.MeTLStanzas.LiveWindow();
            new SandRibbonInterop.MeTLStanzas.MeTLStanzas.QuizOption();
            new SandRibbonInterop.MeTLStanzas.MeTLStanzas.FileResource();
            new SandRibbonInterop.MeTLStanzas.MeTLStanzas.QuizResponse();
            new SandRibbonInterop.MeTLStanzas.MeTLStanzas.DirtyElement();
            new SandRibbonInterop.MeTLStanzas.MeTLStanzas.DirtyAutoshape();
            new SandRibbonInterop.MeTLStanzas.MeTLStanzas.DirtyLiveWindow();
            App.Now("Finished static constructor");
            try
            {
                new Worm();
                new Printer();
                new CommandParameterProvider();
                new SandRibbonInterop.MeTLStanzas.MeTLStanzas.Ink();
                new SandRibbonInterop.MeTLStanzas.MeTLStanzas.Quiz();
                new SandRibbonInterop.MeTLStanzas.MeTLStanzas.QuizResponse();
                new SandRibbonInterop.MeTLStanzas.MeTLStanzas.QuizOption();
                new SandRibbonInterop.MeTLStanzas.MeTLStanzas.Image();
                new SandRibbonInterop.MeTLStanzas.MeTLStanzas.Video();
                new SandRibbonInterop.MeTLStanzas.MeTLStanzas.Bubble();
                new SandRibbonInterop.MeTLStanzas.MeTLStanzas.TextBox();
                new SandRibbonInterop.MeTLStanzas.MeTLStanzas.DirtyInk();
                new SandRibbonInterop.MeTLStanzas.MeTLStanzas.DirtyText();
                new SandRibbonInterop.MeTLStanzas.MeTLStanzas.AutoShape();
                new SandRibbonInterop.MeTLStanzas.MeTLStanzas.DirtyImage();
                new SandRibbonInterop.MeTLStanzas.MeTLStanzas.LiveWindow();
                new SandRibbonInterop.MeTLStanzas.MeTLStanzas.DirtyElement();
                new SandRibbonInterop.MeTLStanzas.MeTLStanzas.DirtyVideo();
                new SandRibbonInterop.MeTLStanzas.MeTLStanzas.DirtyAutoshape();
                new SandRibbonInterop.MeTLStanzas.MeTLStanzas.DirtyLiveWindow();
                new SandRibbonInterop.MeTLStanzas.MeTLStanzas.ScreenshotSubmission();
                Console.WriteLine("End ", SandRibbonObjects.DateTimeFactory.Now().ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Instantiation error in metlstanzas");
            }
            DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
        }
        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.Log(e.Exception.Message);
            //This next line is only here until we can work out why it's happening - it's a non-breaking error on x64 windows 7, with no apparent failure of functionality.
            //if (e.Exception.InnerException != null && e.Exception.InnerException.Message == "Attempted to read or write protected memory. This is often an indication that other memory is corrupt.") return;
            
            MessageBox.Show(string.Format("MeTL has encountered an unexpected error and has to close:{0}\n{1} ",
                e.Exception.Message,
                e.Exception.InnerException == null? 
                    "No inner exception": e.Exception.InnerException.Message));
            this.Shutdown();
        }
        private void AncilliaryButton_Click(object sender, RoutedEventArgs e)
        {
            var AncilliaryButton = (Button) sender;
            var CurrentGrid = (StackPanel)AncilliaryButton.Parent;
            var CurrentPopup = new System.Windows.Controls.Primitives.Popup();
            foreach (FrameworkElement f in CurrentGrid.Children)
                if (f.GetType().ToString() == "System.Windows.Controls.Primitives.Popup")
                    CurrentPopup = (System.Windows.Controls.Primitives.Popup)f;
            if (CurrentPopup.IsOpen == false)
            CurrentPopup.IsOpen = true;
            else CurrentPopup.IsOpen = false;
        }
        private NameValueCollection GetQueryStringParameters()
        {
            NameValueCollection nameValueTable = new NameValueCollection();
            if (ApplicationDeployment.IsNetworkDeployed)
            {
                string queryString = ApplicationDeployment.CurrentDeployment.ActivationUri.Query;
                if(queryString != null)
                    nameValueTable = HttpUtility.ParseQueryString(queryString);
            }
            return (nameValueTable);
        }
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                var parameters = GetQueryStringParameters();
                foreach (var key in parameters.Keys)
                    Application.Current.Properties.Add(key, parameters.Get((string)key));
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message);
            }
        }
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (loggingOut) return;
            WorkspaceStateProvider.SaveCurrentSettings();
        }
    }
}