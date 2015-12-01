<<<<<<< HEAD
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


    class ServerDisplay : INotifyPropertyChanged
    {
        public ServerDisplay(MeTLConfigurationProxy server)
        {
            imageUrl = server.imageUrl;
            name = server.name;
            host = server.host;
            config = server;
        }
        protected bool _networkReady = false;
        public bool NetworkReady
        {
            get { return _networkReady; }
            set
            {
                _networkReady = value;
                PropertyChanged(this, new PropertyChangedEventArgs("NetworkReady"));
                PropertyChanged(this, new PropertyChangedEventArgs("ShouldBlockInput"));
            }
        }
        public bool ShouldBlockInput
        {
            get { return !_networkReady; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Uri imageUrl { get; protected set; }
        public string name { get; protected set; }
        public Uri host { get; protected set; }
        public Uri serverStatus { get { return config.serverStatus; } }
        public MeTLConfigurationProxy config { get; protected set; }
    }
    public partial class Login : UserControl
    {
        public static RoutedCommand CheckAuthentication = new RoutedCommand();
        public static RoutedCommand LoginPending = new RoutedCommand();
        public string Version { get; private set; }
        protected WebBrowser logonBrowser;
        protected List<Uri> browseHistory = new List<Uri>();
        ObservableWithPropertiesCollection<ServerDisplay> serverConfigs = new ObservableWithPropertiesCollection<ServerDisplay>();
        public Login()
        {
            InitializeComponent();
            App.CloseSplashScreen();
            ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
            Version = ConfigurationProvider.instance.getMetlVersion();
            Commands.LoginFailed.RegisterCommand(new DelegateCommand<object>(ResetWebBrowser));
            Commands.SetIdentity.RegisterCommand(new DelegateCommand<Credentials>(SetIdentity));
            servers.ItemsSource = serverConfigs;
            serverConfigs.Add(new ServerDisplay(new MeTLConfigurationProxy("localhost", new Uri("http://localhost:8080/static/images/puppet.jpg"), new Uri("http://localhost:8080"))));
            App.availableServers().ForEach(s => serverConfigs.Add(new ServerDisplay(s)));
            Commands.AddWindowEffect.ExecuteAsync(null);
            pollServers = new System.Threading.Timer((s) =>
            {
                var wc = new WebClient();
                foreach (ServerDisplay server in serverConfigs)
                {
                    var success = false;
                    try
                    {
                        var newUri = server.serverStatus;
                        success = wc.DownloadString(newUri).Trim().ToLower() == "ok";
                    }
                    catch
                    {
                        success = false;
                    }
                    //success = (new Random((int)DateTime.Now.Ticks).Next(1, 5) > 3);
                    Dispatcher.adopt(delegate
                    {
                        server.NetworkReady = success;
                    });
                }
            }, null, 0, pollServersTimeout);
        }
        protected Timer pollServers;
        protected int pollServersTimeout = 5 * 1000;
        protected Timer showTimeoutButton;
        protected int loginTimeout = 5 * 1000;

        protected Timer maintainKeysTimer;
        protected int maintainKeysTimerTimeout = 30 * 1000;
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

            //this entire control is presently collapsed, I think - I'll need to fix that.
            loadingImage.Visibility = Visibility.Collapsed;
            hideResetButton();
            logonBrowserContainer.Visibility = Visibility.Visible;
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
        protected LoadCompletedEventHandler loginCheckingAction;
        protected LoadCompletedEventHandler keyMaintainAction;
        protected void ResetWebBrowser(object _unused)
        {
            var loginUri = new Uri(App.getCurrentServer.host, new Uri("/authenticationState",UriKind.Relative));// App.controller.config.authenticationUrl;
            DestroyWebBrowser(null);
            DeleteCookieForUrl(loginUri);
            logonBrowser = new WebBrowser();
            var gauge = new DiagnosticGauge(loginUri.ToString(), "embedded browser http request", DateTime.Now);
            var gaugeProgress = 0;
            logonBrowserContainer.Children.Add(logonBrowser);
            logonBrowser.Navigating += (sender, args) =>
            {
                browseHistory.Add(args.Uri);
                var cookie = "unknown";
                try
                {
                    cookie = Application.GetCookie(args.Uri);
                }
                catch (Exception e)
                {
                }
                Console.WriteLine(String.Format("{0} => {1}", args.Uri.ToString(), cookie));
                if (detectIEErrors(args.Uri))
                {
                    if (browseHistory.Last() != null)
                    {
                        App.auditor.updateGauge(gauge.update(GaugeStatus.Completed, 100));
                        gauge = new DiagnosticGauge(loginUri.ToString(), "embedded browser http request", DateTime.Now);
                        gaugeProgress = 0;
                        logonBrowser.Navigate(browseHistory.Last());
                    }
                    else
                    {
                        gauge.update(GaugeStatus.Completed, 100);
                        App.auditor.updateGauge(gauge);
                        ResetWebBrowser(null);
                    }
                }
                else
                {
                    gaugeProgress += 1;
                    gauge.update(GaugeStatus.InProgress, gaugeProgress);
                }
            };
            logonBrowser.Navigating += (sender, args) =>
            {
                gaugeProgress = gaugeProgress + ((100 - gaugeProgress) / 2);
                App.auditor.updateGauge(gauge.update(GaugeStatus.Completed, gaugeProgress));
                NavigatedEventHandler finalized = null;
                finalized = new NavigatedEventHandler((s, a) =>
                {
                    App.auditor.updateGauge(gauge.update(GaugeStatus.Completed, 100));
                    gauge = new DiagnosticGauge(loginUri.ToString(), "embedded browser http request", DateTime.Now);
                    gaugeProgress = 0;
                    logonBrowser.Navigated -= finalized;
                });
                logonBrowser.Navigated -= finalized;
                logonBrowser.Navigated += finalized;
            };
            loginCheckingAction = new LoadCompletedEventHandler((sender, args) =>
             {
                 var doc = ((sender as WebBrowser).Document as HTMLDocument);
                 checkWhetherWebBrowserAuthenticationSucceeded(doc, (authenticated, credentials) =>
                 {
                     Commands.AddWindowEffect.ExecuteAsync(null);
                     var config = default(MetlConfiguration);
                     if (App.availableServers().Contains(App.getCurrentServer))
                     {
                         config = App.metlConfigManager.getConfigFor(App.getCurrentServer);
                     }
                     else
                     {
                         config = App.metlConfigManager.parseConfig(App.getCurrentServer, doc).FirstOrDefault();
                     }
                     if (config == default(MetlConfiguration))
                     {
                         MessageBox.Show("No server found at the supplied address.  Please verify the address and restart MeTL.");
                         Application.Current.Shutdown();
                     }
                     App.SetBackend(config);
                     if (App.controller != null && App.controller.credentials != null)
                     {
                         App.controller.credentials.cookie = credentials.cookie;
                     }
                     App.Login(credentials);
                     logonBrowser.LoadCompleted -= loginCheckingAction;
                     if (maintainKeysTimer != null)
                     {
                         logonBrowser.LoadCompleted += keyMaintainAction;
                         maintainKeysTimer.Change(maintainKeysTimerTimeout, Timeout.Infinite);
                     }

                 }, () => { });
             });
            keyMaintainAction = new LoadCompletedEventHandler((sender, args) =>
            {
                var doc = ((sender as WebBrowser).Document as HTMLDocument);
                checkWhetherWebBrowserAuthenticationSucceeded(doc, (authenticated, credentials) =>
                {
                    this.Visibility = Visibility.Collapsed;
                    if (App.controller != null)
                        App.controller.credentials.update(credentials);
                        //App.controller.credentials.cookie = credentials.cookie;

                    Commands.DiagnosticMessage.Execute(new DiagnosticMessage("new creds: " + credentials, "login", DateTime.Now));
                    logonBrowserContainer.Visibility = Visibility.Collapsed;
                    logonBrowserContainer.IsHitTestVisible = false;
                    maintainKeysTimer.Change(maintainKeysTimerTimeout, Timeout.Infinite);
                }, () =>
                {
                    try
                    {
                        if (doc.url == new Uri(App.getCurrentServer.host,new Uri("/authenticationState",UriKind.Relative)).ToString())
                        {
                            showBrowser();
                            this.Visibility = Visibility.Visible;
                        } else
                        {
                            maintainKeysTimer.Change(100, Timeout.Infinite);
                        }
                    }
                    catch
                    {
                        maintainKeysTimer.Change(100, Timeout.Infinite);

                    }

                });
            });

            logonBrowser.LoadCompleted += loginCheckingAction;
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
            maintainKeysTimer = new System.Threading.Timer((s) =>
            {
                App.controller.credentials.cookie = Application.GetCookie(loginUri);
                Dispatcher.adopt(delegate
                {
                    logonBrowser.LoadCompleted -= loginCheckingAction;
                    logonBrowser.Navigate(loginUri);
                });
            }, null, Timeout.Infinite, Timeout.Infinite);
            logonBrowser.Navigate(loginUri);
        }
        protected List<XElement> getElementsByTag(List<XElement> x, String tagName)
        {
            // it's not recursive!
            var children = x.Select(xel => { return getElementsByTag(xel.Elements().ToList(), tagName); });
            var root = x.FindAll((xel) =>
            {
                return xel.Name.LocalName.ToString().Trim().ToLower() == tagName.Trim().ToLower();
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
                var authenticationUri = new Uri(App.getCurrentServer.host,new Uri("authenticationState",UriKind.Relative));
                return uri.Scheme == authenticationUri.Scheme && uri.AbsolutePath == authenticationUri.AbsolutePath && uri.Authority == authenticationUri.Authority;
            }
            catch (Exception e)
            {
                System.Console.WriteLine("exception in checking uri: " + e.Message);
                return false;
            }
        }

        protected void checkWhetherWebBrowserAuthenticationSucceeded(HTMLDocument doc, Action<bool, Credentials> onSuccess, Action onFailure)
        {
            if (doc != null && checkUri(doc.url))
            {
                if (doc.readyState != null && (doc.readyState == "complete" || doc.readyState == "interactive"))
                {
                    try
                    {
                        var authDataContainer = doc.getElementById("authData");
                        if (authDataContainer == null)
                        {
                            onFailure();
                            return;
                        }
                        var html = authDataContainer.innerHTML;
                        if (html == null)
                        {
                            onFailure();
                            return;
                        }
                        var xml = XDocument.Parse(html).Elements().ToList();
                        var authData = getElementsByTag(xml, "authdata");
                        var authenticated = getElementsByTag(authData, "authenticated").First().Value.ToString().Trim().ToLower() == "true";
                        var usernameNode = getElementsByTag(authData, "username").First();
                        var authGroupsNodes = getElementsByTag(authData, "authGroup");
                        var infoGroupsNodes = getElementsByTag(authData, "infoGroup");
                        var username = usernameNode.Value.ToString();
                        var authGroups = authGroupsNodes.Select((xel) => new AuthorizedGroup(xel.Attribute("name").Value.ToString(), xel.Attribute("type").Value.ToString())).ToList();
                        var emailAddressNode = infoGroupsNodes.Find((xel) => xel.Attribute("type").Value.ToString().Trim().ToLower() == "emailaddress");
                        var xmppPassword = "";
                        try {
                            xmppPassword = getElementsByTag(authData, "xmppPassword").First().Value.ToString();
                        } catch
                        {
                            Console.WriteLine("couldn't fetch xmpp credentials from authenticationState page");
                        }
                        var emailAddress = "";
                        if (emailAddressNode != null)
                        {
                            emailAddress = emailAddressNode.Attribute("name").Value.ToString();
                        }
                        var credentials = new Credentials(username, xmppPassword, authGroups, emailAddress);
                        credentials.cookie = Application.GetCookie(App.getCurrentServer.host);
                        if (authenticated)
                        {
                            onSuccess(true, credentials);
                        }
                        else onFailure();
                    }
                    catch (Exception e)
                    {
                        System.Console.WriteLine("exception in checking auth response data: " + e.Message);
                        onFailure();
                    }
                }
                else
                {
                    onFailure();
                }
            }
            else
            {
                onFailure();
            }
        }

        private void SetIdentity(Credentials identity)
        {
            Commands.RemoveWindowEffect.ExecuteAsync(null);
            Dispatcher.adoptAsync(() =>
            {
                logonBrowserContainer.Visibility = Visibility.Collapsed;
                var options = App.controller.client.UserOptionsFor(identity.name);
                Commands.SetUserOptions.Execute(options);
                Commands.SetPedagogyLevel.Execute(Pedagogicometer.level((Pedagogicometry.PedagogyCode)options.pedagogyLevel));
                //DestroyWebBrowser(null);
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
        private void SetBackend(object sender, RoutedEventArgs e)
        {
            serversContainer.Visibility = Visibility.Collapsed;
            if (pollServers != null)
            {
                pollServers.Change(Timeout.Infinite, Timeout.Infinite);
                pollServers.Dispose();
                pollServers = null;
            }
            Commands.RemoveWindowEffect.ExecuteAsync(null);
            var serverDisplay = (sender as Button).DataContext as ServerDisplay;
            var server = serverDisplay.config;
            App.SetBackendProxy(server);
            ResetWebBrowser(null);
        }
        private void SetCustomServer(object sender, RoutedEventArgs e)
        {
            serversContainer.Visibility = Visibility.Collapsed;
            if (pollServers != null)
            {
                pollServers.Change(Timeout.Infinite, Timeout.Infinite);
                pollServers.Dispose();
                pollServers = null;
            }
            Commands.RemoveWindowEffect.ExecuteAsync(null);
            var server = new MeTLConfigurationProxy(customServer.Text, new Uri(customServer.Text + "/static/images/server.png"), new Uri(customServer.Text + "/authenticationState"));
            App.SetBackendProxy(server);
            ResetWebBrowser(null);
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
=======
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
        protected WebBrowser logonBrowser;
        protected List<Uri> browseHistory = new List<Uri>();
        public Login()
        {
            InitializeComponent();
            App.CloseSplashScreen();
            ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
            Version = ConfigurationProvider.instance.getMetlVersion();
            Commands.LoginFailed.RegisterCommand(new DelegateCommand<object>(ResetWebBrowser));
            Commands.SetIdentity.RegisterCommand(new DelegateCommand<Credentials>(SetIdentity));            
            var backends = new Dictionary<String, MeTLServerAddress.serverMode> {
                { "StackableRegiments.com", MeTLServerAddress.serverMode.EXTERNAL },
                { "Saint Leo University", MeTLServerAddress.serverMode.PRODUCTION },
                { "MeTL Demo Server (this houses data in Amazon)", MeTLServerAddress.serverMode.STAGING }
            };
            servers.ItemsSource = backends;
            servers.SelectedIndex = 1;
            Commands.AddWindowEffect.ExecuteAsync(null);
            doSetBackend(MeTLServerAddress.serverMode.EXTERNAL);
            
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
            loadingImage.Visibility = Visibility.Collapsed;
            hideResetButton();
            logonBrowserContainer.Visibility = Visibility.Visible;            
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
            logonBrowser = new WebBrowser();
            logonBrowserContainer.Children.Add(logonBrowser);
            logonBrowser.Navigating += (sender, args) =>
            {
                browseHistory.Add(args.Uri);
                var cookie = "unknown";
                try
                {
                    cookie = Application.GetCookie(args.Uri);
                }
                catch (Exception e)
                {
                }
                Console.WriteLine(String.Format("{0} => {1}",args.Uri.ToString(),cookie));
                if (detectIEErrors(args.Uri))
                {
                    if (browseHistory.Last() != null)
                    {
                        logonBrowser.Navigate(browseHistory.Last());
                    }
                    else
                    {
                        ResetWebBrowser(null);
                    }
                }
            };
            logonBrowser.LoadCompleted += (sender, args) =>
            {
                var doc = ((sender as WebBrowser).Document as HTMLDocument);
                checkWhetherWebBrowserAuthenticationSucceeded(doc);
                
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
            logonBrowser.Navigate(loginUri);
        }
        protected List<XElement> getElementsByTag(List<XElement> x, String tagName)
        {
            // it's not recursive!
            var children = x.Select(xel => { return getElementsByTag(xel.Elements().ToList(), tagName); });
            var root = x.FindAll((xel) =>
            {
                return xel.Name.LocalName.ToString().Trim().ToLower() == tagName.Trim().ToLower();
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

        protected bool checkWhetherWebBrowserAuthenticationSucceeded(HTMLDocument doc)
        {
            if (doc != null && checkUri(doc.url))
            {
                if (doc.readyState != null && (doc.readyState == "complete" || doc.readyState == "interactive"))
                {
                    try
                    {
                        var authDataContainer = doc.getElementById("authData");
                        if (authDataContainer == null) return false;
                        var html = authDataContainer.innerHTML;
                        if (html == null) return false;
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
                    catch (Exception e)
                    {
                        System.Console.WriteLine("exception in checking auth response data: " + e.Message);
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private void SetIdentity(Credentials identity)
        {
            Commands.RemoveWindowEffect.ExecuteAsync(null);
            Dispatcher.adoptAsync(() =>
            {
                logonBrowserContainer.Visibility = Visibility.Collapsed;
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
        
        private void SetBackend(object sender, RoutedEventArgs e)
        {
            var backend = ((KeyValuePair<String, MeTLServerAddress.serverMode>)servers.SelectedItem).Value;
            doSetBackend(backend);            
        }

        private void doSetBackend(MeTLServerAddress.serverMode backend) {
            serversContainer.Visibility = Visibility.Collapsed;
            Commands.RemoveWindowEffect.ExecuteAsync(null);
            App.SetBackend(backend);
            ResetWebBrowser(null);
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
>>>>>>> origin
