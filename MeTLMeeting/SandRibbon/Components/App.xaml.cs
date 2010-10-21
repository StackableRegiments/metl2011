﻿using System;
using System.Collections.Specialized;
using System.Deployment.Application;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using SandRibbon.Components.Sandpit;
using SandRibbon.Utils;
using System.Security.Permissions;
using SandRibbon.Providers;
using SandRibbon.Utils.Connection;
using SandRibbon.Quizzing;
using Microsoft.Practices.Composite.Presentation.Commands;
using SandRibbon.Components;
using System.Security;

[assembly:UIPermission(SecurityAction.RequestMinimum)]

namespace SandRibbon
{
    public partial class App : Application
    {
        public static NetworkController controller;
        public static bool isStaging = false;

        public static void Login(String username, String password)
        {
            string finalUsername = username;
            if (username.Contains("_"))
            {
                var parts = username.Split('_');
                finalUsername = parts[0];
                parts[0] = "";
                foreach (String part in parts)
                {
                    switch (part)
                    {
                        case "prod":
                            isStaging = false;
                            break;
                        case "production":
                            isStaging = false;
                            break;
                        case "staging":
                            isStaging = true;
                            break;
                    }
                }
            }
            controller = new NetworkController();
            MeTLLib.ClientFactory.Connection().Connect(finalUsername, password);
        }
        
        public static void dontDoAnything()
        {
        }

        public static void dontDoAnything(int _obj, int _obj2)
        {
        }

        public static string Now(string title){
            var now = SandRibbonObjects.DateTimeFactory.Now();
            var s = string.Format("{2} {0}:{1}", now, now.Millisecond, title);
            Logger.Log(s);
            Console.WriteLine(s);
            return s;
        }
        static App() {
            Now("Static App start");
            setDotNetPermissionState();
        }
        private static void setDotNetPermissionState()
        {
            //Creating permissionState to allow all actions
            PermissionSet set = new PermissionSet(PermissionState.None);
            //set.SetPermission(new SecurityPermission(SecurityPermissionFlag.AllFlags));
            set.SetPermission(new UIPermission(UIPermissionWindow.AllWindows, UIPermissionClipboard.AllClipboard));
            //set.SetPermission(new RegistryPermission(PermissionState.None));
            //set.SetPermission(new PrincipalPermission(PermissionState.None));
            //set.SetPermission(new MediaPermission(MediaPermissionAudio.AllAudio, MediaPermissionVideo.AllVideo, MediaPermissionImage.AllImage));
            //set.SetPermission(new EnvironmentPermission(EnvironmentPermissionAccess.Read,"{pathsToEnvironmentVariablesSeparatedBySemiColons}"));
            //set.SetPermission(new PublisherIdentityPermission(new System.Security.Cryptography.X509Certificates.X509Certificate("{pathToCertificate}"));
            //set.SetPermission(new FileDialogPermission(FileDialogPermissionAccess.OpenSave));
            //set.SetPermission(new UrlIdentityPermission(PermissionState.Unrestricted));
            //set.SetPermission(new SiteIdentityPermission(PermissionState.Unrestricted));
            //set.SetPermission(new ReflectionPermission(ReflectionPermissionFlag.NoFlags));
            //set.SetPermission(new StrongNameIdentityPermission(PermissionState.Unrestricted));
            //set.SetPermission(new GacIdentityPermission(PermissionState.Unrestricted));
            //set.SetPermission(new FileIOPermission(PermissionState.Unrestricted));
            //set.SetPermission(new IsolatedStorageFilePermission(PermissionState.Unrestricted));
            //set.SetPermission(new WebBrowserPermission(WebBrowserPermissionLevel.None));
            //set.SetPermission(new ZoneIdentityPermission(PermissionState.Unrestricted));
            //Asserting new permission set to all referenced assemblies
            set.Assert();
        }
        
        private void LogOut(object _Unused)
        {
            WorkspaceStateProvider.ClearSettings();
            ThumbnailProvider.ClearThumbnails();
            Application.Current.Shutdown();
        }
        
        protected override void OnStartup(StartupEventArgs e)
        {
#if DEBUG
            isStaging = true;
#else
            isStaging = false;
#endif
            base.OnStartup(e);
            new Worm();
            new Printer();
            new CommandParameterProvider();
            Commands.LogOut.RegisterCommand(new DelegateCommand<object>(LogOut));
            DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
        }
        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.Log(e.Exception.Message);
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
        }
    }
}