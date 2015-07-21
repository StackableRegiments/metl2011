﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using MeTLLib;
using MeTLLib.DataTypes;
using Microsoft.Practices.Composite.Presentation.Commands;
using SandRibbon.Components.Sandpit;
using SandRibbon.Providers;
using SandRibbon.Components.Utility;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;
using mshtml;

namespace SandRibbon.Components
{
    public class UserCredentials : IDataErrorInfo
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public string Error
        {
            get { throw new NotImplementedException(); }
        }

        public string this[string credential]
        {
            get
            {
                string result = "";
                if (credential == "Username")
                {
                    if (string.IsNullOrEmpty(Username))
                        result = "Please enter your username";
                }
                if (credential == "Password")
                {
                    if (string.IsNullOrEmpty(Password))
                        result = "Please enter your password";
                }
                return result;
            }
        }
    }

    public partial class Login : UserControl
    {
        public static RoutedCommand CheckAuthentication = new RoutedCommand();
        public static RoutedCommand LoginPending = new RoutedCommand();
        public string Version { get; private set; }
        protected Awesomium.Windows.Controls.WebControl logonBrowser;
        protected List<Uri> browseHistory = new List<Uri>();
        public Login()
        {
            InitializeComponent();
            this.DataContext = this;
            var failingCredentials = new Credentials("forbidden", "", null, "");
            App.Login(failingCredentials);
            ResetWebBrowser(null);
            SandRibbon.App.CloseSplashScreen();
            Commands.AddWindowEffect.ExecuteAsync(null);
            Version = ConfigurationProvider.instance.getMetlVersion();
            Commands.LoginFailed.RegisterCommand(new DelegateCommand<object>(ResetWebBrowser));
            Commands.SetIdentity.RegisterCommand(new DelegateCommand<Credentials>(SetIdentity));
            ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
        }
        protected Timer showTimeoutButton;
        protected int loginTimeout = 5 * 1000;
        protected void restartLoginProcess(object sender, RoutedEventArgs e)
        {
            ResetWebBrowser(null);
        }
        protected void hideBrowser()
        {
            logonBrowserContainer.Visibility = Visibility.Collapsed;
            loadingImage.Visibility = Visibility.Visible;
            logonBrowserContainer.IsHitTestVisible = true;
            restartLoginProcessContainer.Visibility = Visibility.Collapsed;
            showTimeoutButton.Change(loginTimeout, Timeout.Infinite);
        }
        protected void showBrowser()
        {
            hideResetButton();
            logonBrowserContainer.Visibility = Visibility.Visible;
            loadingImage.Visibility = Visibility.Collapsed;
            logonBrowserContainer.IsHitTestVisible = true;
        }
        protected void showResetButton()
        {
            restartLoginProcessContainer.Visibility = Visibility.Visible;
            showTimeoutButton.Change(Timeout.Infinite, Timeout.Infinite);
        }
        protected void hideResetButton()
        {
            restartLoginProcessContainer.Visibility = Visibility.Collapsed;
            showTimeoutButton.Change(Timeout.Infinite, Timeout.Infinite);
        }
        protected List<String> updatedCookieKeys = new List<String> { "expires", "path", "domain" };
        protected void DeleteCookieForUrl(Uri uri)
        {
            try
            {
                DateTime expiration = DateTime.UtcNow - TimeSpan.FromDays(1);
                var newCookie = Application.GetCookie(uri);
                var finalCookies = newCookie.Split(';').Where(cps => cps.Contains('=')).Select(cps =>
                {
                    var parts = cps.Split('=');
                    return new KeyValuePair<String, String>(parts[0], parts[1]);
                });
                foreach (var cp in finalCookies)
                {
                    try
                    {
                        var replacementCookie = String.Format(@"{0}={1}; Expires={2}; Path=/; Domain={3}", cp.Key, cp.Value, expiration.ToString("R"), uri.Host);
                        Application.SetCookie(uri, replacementCookie);
                        Application.SetCookie(new Uri("http://" + uri.Host + "/"), replacementCookie);
                    }
                    catch (Exception e)
                    {
                        System.Console.WriteLine("Failed to delete cookie for: " + uri.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                if (e.Message.ToLower().Contains("no more data")) { }
                else
                    System.Console.WriteLine("Failed to read cookie prior to deletion for: " + uri.ToString());
            }
        }
        class UriHostComparer : IEqualityComparer<System.Uri>
        {

            public bool Equals(Uri x, Uri y)
            {
                return x.Host == y.Host;
            }

            public int GetHashCode(Uri obj)
            {
                return obj.Host.GetHashCode();
            }
        }
        protected void DestroyWebBrowser(object _unused)
        {
            if (logonBrowser != null)
            {
                browseHistory.Distinct(new UriHostComparer()).ToList().ForEach((uri) => DeleteCookieForUrl(uri));
                logonBrowserContainer.Children.Clear();
                logonBrowser.Dispose();
                logonBrowser = null;
                browseHistory.Clear();
            }
        }
        protected Boolean detectIEErrors(Uri uri)
        {
            return (uri.Scheme == "res");
        }
        protected void ResetWebBrowser(object _unused)
        {
            var loginUri = ClientFactory.Connection().server.webAuthenticationEndpoint;
            DestroyWebBrowser(null); 
            DeleteCookieForUrl(loginUri);
            logonBrowser = new Awesomium.Windows.Controls.WebControl();
            logonBrowserContainer.Children.Add(logonBrowser);
            logonBrowser.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            logonBrowser.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            logonBrowser.CertificateError += new Awesomium.Core.CertificateErrorEventHandler(logonBrowser_CertificateError);
            logonBrowser.DocumentReady += (sender, args) => {
                var d = (sender as Awesomium.Windows.Controls.WebControl).HTML;
                var authResult = checkWhetherWebBrowserAuthenticationSucceeded(d);
                if (!authResult)
                {
                    showBrowser();
                }
                else {
                    hideBrowser();
                }
            };
            if (showTimeoutButton != null)
            {
                showTimeoutButton.Change(Timeout.Infinite, Timeout.Infinite);
                showTimeoutButton.Dispose();
                showTimeoutButton = null;
            }
            showTimeoutButton = new System.Threading.Timer((s) =>
            {
                Dispatcher.adoptAsync(delegate
                {
                    showResetButton();
                });
            }, null, Timeout.Infinite, Timeout.Infinite);
            logonBrowser.Source = loginUri;
        }

        void logonBrowser_CertificateError(object sender, Awesomium.Core.CertificateErrorEventArgs e)
        {
            e.Ignore = true;
            Console.WriteLine("Ignoring certificate exception");
        }
        protected List<XElement> getElementsByTag(List<XElement> x, String tagName)
        {
            // it's not recursive!
            var children = x.Select(xel => { return getElementsByTag(xel.Elements().ToList(), tagName); });
            var root = x.FindAll((xel) =>
            {
                return xel.Name.ToString().Trim().ToLower() == tagName.Trim().ToLower();
            });
            foreach (List<XElement> child in children)
            {
                root.AddRange(child);
            }
            return root;
        }
        protected Boolean checkUri(String url)
        {
            try
            {
                var uri = new Uri(url);
                browseHistory.Add(uri);
                var authenticationUri = ClientFactory.Connection().server.webAuthenticationEndpoint;
                return uri.Scheme == authenticationUri.Scheme && uri.AbsolutePath == authenticationUri.AbsolutePath && uri.Authority == authenticationUri.Authority;
            }
            catch (Exception e)
            {
                System.Console.WriteLine("exception in checking uri: " + e.Message);
                return false;
            }
        }
        protected bool checkWhetherWebBrowserAuthenticationSucceeded(string html)
        {
            try
            {
                var xml = XDocument.Parse(html).Elements().ToList();
                var authData = getElementsByTag(xml, "authdata");
                var authenticated = getElementsByTag(authData, "authenticated").First().Value.ToString().Trim().ToLower() == "true";
                var usernameNode = getElementsByTag(authData, "username").First();
                var authGroupsNodes = getElementsByTag(authData, "authGroup");
                var infoGroupsNodes = getElementsByTag(authData, "infoGroup");
                var username = usernameNode.Value.ToString();
                var authGroups = authGroupsNodes.Select((xel) => new AuthorizedGroup(xel.Attribute("name").Value.ToString(), xel.Attribute("type").Value.ToString())).ToList();
                var emailAddressNode = infoGroupsNodes.Find((xel) => xel.Attribute("type").Value.ToString().Trim().ToLower() == "emailaddress");
                var emailAddress = "";
                if (emailAddressNode != null)
                {
                    emailAddress = emailAddressNode.Attribute("name").Value.ToString();
                }
                var credentials = new Credentials(username, "", authGroups, emailAddress);
                if (authenticated)
                {
                    Commands.AddWindowEffect.ExecuteAsync(null);
                    App.Login(credentials);
                }
                return authenticated;
            }
            catch (Exception e) {
                return false;
            }
        }
        private void SetIdentity(Credentials identity)
        {
            Commands.RemoveWindowEffect.ExecuteAsync(null);
            Dispatcher.adoptAsync(() =>
            {
                var options = ClientFactory.Connection().UserOptionsFor(identity.name);
                Commands.SetUserOptions.Execute(options);
                Commands.SetPedagogyLevel.Execute(Pedagogicometer.level((Pedagogicometry.PedagogyCode)options.pedagogyLevel));
                DestroyWebBrowser(null);
                this.Visibility = Visibility.Collapsed;
            });
            App.mark("Login knows identity");
            Commands.ShowConversationSearchBox.ExecuteAsync(null);
        }
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new LoginAutomationPeer(this);
        }
    }
    class LoginAutomationPeer : FrameworkElementAutomationPeer, IValueProvider
    {
        public LoginAutomationPeer(Login parent) : base(parent) { }
        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Value)
                return this;
            return base.GetPattern(patternInterface);
        }
        public bool IsReadOnly
        {
            get { return false; }
        }
        public void SetValue(string value)
        {
            //Constants.JabberWire.SERVER = value;
        }
        public string Value
        {
            get { return ""; } //Constants.JabberWire.SERVER; }
        }
    }
}
