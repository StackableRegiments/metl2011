﻿using Microsoft.Windows.Controls.Ribbon;
using MeTLLib;
using MeTLLib.DataTypes;
using MeTLLib.Providers.Connection;
using Microsoft.Practices.Composite.Presentation.Commands;
using SandRibbon.Components;
using SandRibbon.Components.Utility;
using SandRibbon.Pages.Collaboration;
using SandRibbon.Pages.ServerSelection;
using SandRibbon.Properties;
using SandRibbon.Providers;
using SandRibbon.Utils;
using SandRibbon.Utils.Connection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SandRibbon.Pages.Collaboration.Palettes;
using MahApps.Metro.Controls;
using SandRibbon.Pages.Conversations.Models;
using System.Web;
using System.Collections.ObjectModel;
using Awesomium.Windows.Controls;

namespace SandRibbon
{
    public partial class MainWindow : MetroWindow
    {
        private System.Windows.Threading.DispatcherTimer displayDispatcherTimer;

        private PowerPointLoader loader;
        private UndoHistory undoHistory;
        public string CurrentProgress { get; set; }
        public static RoutedCommand ProxyMirrorExtendedDesktop = new RoutedCommand();
        public string log
        {
            get { return Logger.log; }
        }
        public MainWindow()
        {
            InitializeComponent();
            DoConstructor();
            Commands.AllStaticCommandsAreRegistered();
            mainFrame.Navigate(new ServerSelectorPage());
            App.CloseSplashScreen();
        }
        private void DoConstructor()
        {
            Commands.SetIdentity.RegisterCommand(new DelegateCommand<object>(_arg =>
            {
                App.mark("Window1 knows about identity");
            }));
            Commands.UpdateConversationDetails.Execute(ConversationDetails.Empty);
            Commands.SetPedagogyLevel.DefaultValue = ConfigurationProvider.instance.getMeTLPedagogyLevel();
            Commands.MeTLType.DefaultValue = Globals.METL;
            Title = Strings.Global_ProductName;                      
            //create
            Commands.ImportPowerpoint.RegisterCommand(new DelegateCommand<object>(ImportPowerpoint));
            Commands.ImportPowerpoint.RegisterCommand(new DelegateCommand<object>(App.noop, mustBeLoggedIn));
            Commands.CreateBlankConversation.RegisterCommand(new DelegateCommand<object>(createBlankConversation, mustBeLoggedIn));
            Commands.CreateConversation.RegisterCommand(new DelegateCommand<object>(createConversation, canCreateConversation));
            Commands.ConnectToSmartboard.RegisterCommand(new DelegateCommand<object>(App.noop, mustBeInConversation));
            Commands.DisconnectFromSmartboard.RegisterCommand(new DelegateCommand<object>(App.noop, mustBeInConversation));
            Commands.ManuallyConfigureOneNote.RegisterCommand(new DelegateCommand<object>(openOneNoteConfiguration));
            Commands.BrowseOneNote.RegisterCommand(new DelegateCommand<OneNoteConfiguration>(browseOneNote));
            //conversation movement
            Commands.UpdateConversationDetails.RegisterCommand(new DelegateCommand<ConversationDetails>(UpdateConversationDetails));
            Commands.SetSync.RegisterCommand(new DelegateCommand<object>(setSync));
            Commands.EditConversation.RegisterCommand(new DelegateCommand<object>(App.noop, mustBeInConversationAndBeAuthor));
            Commands.MoveToOverview.RegisterCommand(new DelegateCommand<object>(MoveToOverview, mustBeInConversation));
            Commands.MoveToNext.RegisterCommand(new DelegateCommand<object>(o => Shift(1), mustBeInConversation));
            Commands.MoveToPrevious.RegisterCommand(new DelegateCommand<object>(o => Shift(-1), mustBeInConversation));
            Commands.MoveToNotebookPage.RegisterCommand(new DelegateCommand<NotebookPage>(NavigateToNotebookPage));
            
            Commands.LogOut.RegisterCommand(new DelegateCommand<object>(App.noop, mustBeLoggedIn));
            Commands.Redo.RegisterCommand(new DelegateCommand<object>(App.noop, mustBeInConversation));
            Commands.Undo.RegisterCommand(new DelegateCommand<object>(App.noop, mustBeInConversation));            

            Commands.PrintConversation.RegisterCommand(new DelegateCommand<object>(PrintConversation, mustBeInConversation));
            
            Commands.ImageDropped.RegisterCommand(new DelegateCommand<object>(App.noop, mustBeLoggedIn));
            Commands.SendQuiz.RegisterCommand(new DelegateCommand<object>(App.noop, mustBeLoggedIn));
            Commands.ToggleNavigationLock.RegisterCommand(new DelegateCommand<object>(toggleNavigationLock));
            Commands.SetConversationPermissions.RegisterCommand(new DelegateCommand<object>(SetConversationPermissions, CanSetConversationPermissions));                               

            Commands.FileUpload.RegisterCommand(new DelegateCommand<object>(App.noop, mustBeAuthor));

            Commands.ChangeLanguage.RegisterCommand(new DelegateCommand<System.Windows.Markup.XmlLanguage>(changeLanguage));
            Commands.CheckExtendedDesktop.RegisterCommand(new DelegateCommand<object>((_unused) => { CheckForExtendedDesktop(); }));

            Commands.Reconnecting.RegisterCommandToDispatcher(new DelegateCommand<bool>(Reconnecting));
            Commands.SetUserOptions.RegisterCommandToDispatcher(new DelegateCommand<UserOptions>(SetUserOptions));            
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Print, PrintBinding));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Help, HelpBinding, (_unused, e) => { e.Handled = true; e.CanExecute = true; }));

            Commands.ModifySelection.RegisterCommand(new DelegateCommand<IEnumerable<PrivateAwareStroke>>(ModifySelection));

            WorkspaceStateProvider.RestorePreviousSettings();
            getDefaultSystemLanguage();
            undoHistory = new UndoHistory();
            displayDispatcherTimer = createExtendedDesktopTimer();            
        }

        private void NavigateToNotebookPage(NotebookPage page)
        {
            mainFrame.Navigate(new OneNotePage(page));
        }

        private void browseOneNote(OneNoteConfiguration config)
        {
            var w = new WebControl();
            w.DocumentReady += W_DocumentReady;
            flyout.Content = w;
            flyout.Width = 600;
            flyout.IsOpen = true;
            var scope = "office.onenote_update";
            var responseType = "token";
            var clientId = config.apiKey;
            var redirectUri = "https://login.live.com/oauth20_desktop.srf";
            var req = "https://login.live.com/oauth20_authorize.srf?client_id={0}&scope={1}&response_type={2}&redirect_uri={3}";
            var uri = new Uri(String.Format(req, 
                config.apiKey, 
                scope, 
                responseType,
                redirectUri));
            w.Source = uri;
        }

        private void W_DocumentReady(object sender, Awesomium.Core.DocumentReadyEventArgs e)
        {                     
            var queryPart = e.Url.AbsoluteUri.Split('#');
            if (queryPart.Length > 1) {
                var ps = HttpUtility.ParseQueryString(queryPart[1]);
                var token = ps["access_token"];
                if (token != null) {
                    Console.WriteLine("Token: {0}", token);                                  
                    flyout.DataContext = Globals.OneNoteConfiguration;
                    var oneNoteModel = flyout.DataContext as OneNoteConfiguration;
                    oneNoteModel.Books.Clear();
                    foreach(var book in OneNote.Notebooks(token)) {
                        oneNoteModel.Books.Add(book);                        
                    }
                    flyout.Content = TryFindResource("oneNoteListing");
                }
            }            
        }

        private void openOneNoteConfiguration(object obj)
        {            
            flyout.Content = TryFindResource("oneNoteConfiguration");
            flyout.DataContext = Globals.OneNoteConfiguration;
            flyout.IsOpen = true;
        }

        private void MoveToOverview(object obj)
        {
            mainFrame.Navigate(new ConversationOverviewPage(Globals.conversationDetails));
        }

        private void Shift(int direction)
        {
            var details = Globals.conversationDetails;
            var slides = details.Slides.OrderBy(s => s.index).Select(s => s.id).ToList();
            var currentIndex = slides.IndexOf(Globals.location.currentSlide);
            if (currentIndex < 0) return;
            var end = slides.Count - 1;
            var targetIndex = 0;
            if (direction >= 0 && currentIndex == end) targetIndex = 0;
            else if (direction >= 0) targetIndex = currentIndex + 1;
            else if (currentIndex == 0) targetIndex = end;
            else targetIndex = currentIndex - 1;

            mainFrame.Navigate(new GroupCollaborationPage(slides[targetIndex]));
        }


        private void ModifySelection(IEnumerable<PrivateAwareStroke> obj)
        {
            this.flyout.Content = TryFindResource("worm");
            this.flyout.IsOpen= !this.flyout.IsOpen;
        }

        [System.STAThreadAttribute()]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public static void Main()
        {
            // want to customise the main function so we'll create one here instead of the automatically generated one from app.g.cs
            SandRibbon.App.ShowSplashScreen();
            SandRibbon.App app = new SandRibbon.App();

            app.InitializeComponent();
            app.Run();
        }

        private static int checkExtendedInProgress = 0;

        private void dispatcherEventHandler(Object sender, EventArgs args)
        {
            if (1 == Interlocked.Increment(ref checkExtendedInProgress))
            {
                try
                {
                    /// There are three conditions we want to handle
                    /// 1. Extended mode activated, there are now 2 screens
                    /// 2. Extended mode deactivated, back to 1 screen
                    /// 3. Extended screen position has changed, so need to reinit the projector window

                    var screenCount = System.Windows.Forms.Screen.AllScreens.Count();


                    if (Projector.Window == null && screenCount > 1)
                        Commands.ProxyMirrorPresentationSpace.ExecuteAsync(null);
                    else if (Projector.Window != null && screenCount == 1)
                        Projector.Window.Close();
                    else if (Projector.Window != null && screenCount > 1)
                    {
                        // Case 3.
                        Commands.ProxyMirrorPresentationSpace.ExecuteAsync(null);
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref checkExtendedInProgress, 0);
                }
            }
        }

        private void CheckForExtendedDesktop()
        {
            if (!displayDispatcherTimer.IsEnabled)
            {
                //This dispather timer is left running in the background and is never stopped
                displayDispatcherTimer.Start();
            }
        }

        private System.Windows.Threading.DispatcherTimer createExtendedDesktopTimer()
        {
            displayDispatcherTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.ApplicationIdle, this.Dispatcher);
            displayDispatcherTimer.Interval = TimeSpan.FromSeconds(1);
            displayDispatcherTimer.Tick += new EventHandler(dispatcherEventHandler);
            displayDispatcherTimer.Start();
            return displayDispatcherTimer;
        }

        private void getDefaultSystemLanguage()
        {
            try
            {
                Commands.ChangeLanguage.Execute(System.Windows.Markup.XmlLanguage.GetLanguage(System.Globalization.CultureInfo.CurrentUICulture.IetfLanguageTag));
            }
            catch (Exception e)
            {
                Logger.Crash(e);
            }
        }
        #region helpLinks
        private void OpenEULABrowser(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(Properties.Settings.Default.UserAgreementUrl);
        }
        private void OpenTutorialBrowser(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(Properties.Settings.Default.TutorialUrl);
        }
        private void OpenReportBugBrowser(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(Properties.Settings.Default.BugReportUrl);
        }
        private void OpenAboutMeTLBrowser(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(Properties.Settings.Default.DescriptionUrl);
        }
        #endregion
        private void changeLanguage(System.Windows.Markup.XmlLanguage lang)
        {
            try
            {
                var culture = lang.GetSpecificCulture();
                FlowDirection = culture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                var mergedDicts = System.Windows.Application.Current.Resources.MergedDictionaries;
                var currentToolTips = mergedDicts.Where(rd => ((ResourceDictionary)rd).Source.ToString().ToLower().Contains("tooltips")).First();
                var rdUri = new Uri("Components\\ResourceDictionaries\\ToolTips_" + lang + ".xaml", UriKind.Relative);
                var newDict = (ResourceDictionary)App.LoadComponent(rdUri);
                var sourceUri = new Uri("ToolTips_" + lang + ".xaml", UriKind.Relative);
                newDict.Source = sourceUri;
                mergedDicts[mergedDicts.IndexOf(currentToolTips)] = newDict;
            }

            catch (Exception e)
            {
                Logger.Crash(e);
            }
        }
        private void ApplicationPopup_ShowOptions(object sender, EventArgs e)
        {
            Trace.TraceInformation("UserOptionsDialog_Show");
            if (mustBeLoggedIn(null))
            {
                var userOptions = new UserOptionsDialog();
                userOptions.Owner = Window.GetWindow(this);
                userOptions.ShowDialog();
            }
            else MeTLMessage.Warning("You must be logged in to edit your options");
        }
        private void ImportPowerpoint(object obj)
        {
            if (loader == null) loader = new PowerPointLoader();
            loader.ImportPowerpoint(this);
        }        
        private void createBlankConversation(object obj)
        {
            var element = Keyboard.FocusedElement;
            if (loader == null) loader = new PowerPointLoader();
            loader.CreateBlankConversation();
        }

        private void HelpBinding(object sender, EventArgs e)
        {
            LaunchHelp(null);
        }

        private void LaunchHelp(object _arg)
        {
            try
            {
                Process.Start("http://monash.edu/eeducation/metl/help.html");
            }
            catch (Exception)
            {
            }
        }
        private void PrintBinding(object sender, EventArgs e)
        {
            PrintConversation(null);
        }
        private void PrintConversation(object _arg)
        {
            if (Globals.UserOptions.includePrivateNotesOnPrint)
                new Printer().PrintPrivate(Globals.conversationDetails.Jid, Globals.me);
            else
                new Printer().PrintHandout(Globals.conversationDetails.Jid, Globals.me);
        }
        private void SetUserOptions(UserOptions options)
        {
            //this next line should be removed.
            SaveUserOptions(options);            
        }
        private void SaveUserOptions(UserOptions options)
        {
            //this should be wired to a new command, SaveUserOptions, which is commented out in SandRibbonInterop.Commands
            ClientFactory.Connection().SaveUserOptions(Globals.me, options);
        }        

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
        }

        private void Reconnecting(bool success)
        {
            if (success)
            {
                try
                {                    
                    var details = Globals.conversationDetails;
                    if (details == null || details.Equals(ConversationDetails.Empty))
                    {
                        Commands.UpdateConversationDetails.Execute(ConversationDetails.Empty);
                    }
                    else
                    {
                        var jid = Globals.conversationDetails.Jid;
                        Commands.UpdateConversationDetails.Execute(ClientFactory.Connection().DetailsOf(jid));
                        Commands.MoveToCollaborationPage.Execute(Globals.location.currentSlide);
                        SlideDisplay.SendSyncMove(Globals.location.currentSlide);
                        ClientFactory.Connection().getHistoryProvider().Retrieve<PreParser>(
                                    null,
                                    null,
                                    (parser) =>
                                    {
                                        Commands.PreParserAvailable.Execute(parser);                 
                                    },
                                    jid);
                    }
                }
                catch (NotSetException e)
                {
                    Logger.Crash(e);
                    Commands.UpdateConversationDetails.Execute(ConversationDetails.Empty);
                }
                catch (Exception e)
                {
                    Logger.Log(string.Format("CRASH: (Fixed) Window1::Reconnecting crashed {0}", e.Message));
                    Commands.UpdateConversationDetails.Execute(ConversationDetails.Empty);
                }
            }
            else
            {
                showReconnectingDialog();
            }
        }                    
        
        private string messageFor(ConversationDetails details)
        {
            var permissionLabel = Permissions.InferredTypeOf(details.Permissions).Label;
            if (details.Equals(ConversationDetails.Empty))
                return Strings.Global_ProductName;
            return string.Format("Collaboration {0}  -  {1}'s \"{2}\" - MeTL", (permissionLabel == "tutorial") ? "ENABLED" : "DISABLED", details.Author, details.Title);
        }                
        private void showReconnectingDialog()
        {
            var majorHeading = new TextBlock
                           {
                               Foreground = Brushes.White,
                               Text = "Connection lost...  Reconnecting",
                               FontSize = 72,
                               HorizontalAlignment = HorizontalAlignment.Center,
                               VerticalAlignment = VerticalAlignment.Center
                           };
            var minorHeading = new TextBlock
                           {
                               Foreground = Brushes.White,
                               Text = "You must have an active internet connection,\nand you must not be logged in twice with the same account.",
                               FontSize = 30,
                               HorizontalAlignment = HorizontalAlignment.Center,
                               VerticalAlignment = VerticalAlignment.Center
                           };            
        }
        private bool canCreateConversation(object obj)
        {
            return mustBeLoggedIn(obj);
        }
        private bool mustBeLoggedIn(object _arg)
        {
            var v = !Globals.credentials.ValueEquals(Credentials.Empty);
            return v;
        }                
        private bool mustBeInConversationAndBeAuthor(object _arg)
        {
            return mustBeInConversation(_arg) && mustBeAuthor(_arg);
        }
        private bool mustBeInConversation(object _arg)
        {
            var details = Globals.conversationDetails;
            if (!details.IsValid)
                if (Globals.credentials.authorizedGroups.Select(su => su.groupKey.ToLower()).Contains("superuser")) return true;
            var validGroups = Globals.credentials.authorizedGroups.Select(g => g.groupKey.ToLower()).ToList();
            validGroups.Add("unrestricted");
            if (!details.isDeleted && validGroups.Contains(details.Subject.ToLower())) return true;
            return false;
        }
        private bool mustBeAuthor(object _arg)
        {
            return Globals.isAuthor;
        }
        private void UpdateConversationDetails(ConversationDetails details)
        {
            if (details.IsEmpty) return;
            Dispatcher.adopt(delegate
                                 {
                                     if (details.Jid.GetHashCode() == Globals.location.activeConversation.GetHashCode() || String.IsNullOrEmpty(Globals.location.activeConversation))
                                     {
                                         UpdateTitle(details);
                                         if (!mustBeInConversation(null))
                                         {        
                                             Commands.LeaveLocation.Execute(null);
                                         }
                                     }
                                 });
        }
        private void UpdateTitle(ConversationDetails details)
        {
            if (Globals.conversationDetails != null && mustBeInConversation(null))
            {
#if DEBUG
                    Title = String.Format("{0} [Build: {1}]", messageFor(Globals.conversationDetails), "not merc");//SandRibbon.Properties.HgID.Version); 
#else
                Title = messageFor(Globals.conversationDetails);
#endif
            }
            else
                Title = Strings.Global_ProductName;
        }
        private DelegateCommand<object> canOpenFriendsOverride;
        private void applyPermissions(Permissions permissions)
        {
            if (canOpenFriendsOverride != null)
                Commands.ToggleFriendsVisibility.UnregisterCommand(canOpenFriendsOverride);
            canOpenFriendsOverride = new DelegateCommand<object>((_param) => { }, (_param) => true);
            Commands.ToggleFriendsVisibility.RegisterCommand(canOpenFriendsOverride);
        }
        
        private void createConversation(object detailsObject)
        {
            var details = (ConversationDetails)detailsObject;
            if (details == null) return;
            if (Commands.CreateConversation.CanExecute(details))
            {
                if (details.Tag == null)
                    details.Tag = "unTagged";
                details.Author = Globals.userInformation.credentials.name;
                var connection = ClientFactory.Connection();
                details = connection.CreateConversation(details);
                CommandManager.InvalidateRequerySuggested();
                if (Commands.JoinConversation.CanExecute(details.Jid))
                    Commands.JoinConversation.ExecuteAsync(details.Jid);
            }
        }
        private void setSync(object _obj)
        {
            Globals.userInformation.policy.isSynced = !Globals.userInformation.policy.isSynced;
        }
     
      
        public Visibility GetVisibilityOf(UIElement target)
        {
            return target.Visibility;
        }
        public void toggleNavigationLock(object _obj)
        {
           
                var details = Globals.conversationDetails;
                if (details == null)
                    return;
                details.Permissions.NavigationLocked = !details.Permissions.NavigationLocked;
                ClientFactory.Connection().UpdateConversationDetails(details);                       
        }
        private void SetConversationPermissions(object obj)
        {
            var style = (string)obj;
            try
            {
                var details = Globals.conversationDetails;
                if (details == null)
                    return;
                if (style == "lecture")
                    details.Permissions.applyLectureStyle();
                else
                    details.Permissions.applyTuteStyle();
                MeTLLib.ClientFactory.Connection().UpdateConversationDetails(details);
            }
            catch (NotSetException)
            {
                return;
            }
        }
        private bool CanSetConversationPermissions(object _style)
        {
            return Globals.isAuthor;
        }

        private void sleep(object _obj)
        {
            Dispatcher.adoptAsync(delegate
            {
                Hide();
            });
        }
        private void wakeUp(object _obj)
        {
            Dispatcher.adoptAsync(delegate
            {
                Show();
                WindowState = WindowState.Maximized;
            });
        }
        
        
        
        private void ribbonWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (App.AccidentallyClosing.AddMilliseconds(250) > DateTime.Now)
            {
                e.Cancel = true;
            }
            else
            {
                Commands.CloseApplication.Execute(null);
                Application.Current.Shutdown();
            }
        }
        private void ApplicationPopup_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            App.AccidentallyClosing = DateTime.Now;
        }

        private void ribbon_SelectedTabChanged(object sender, EventArgs e)
        {
            var ribbon = sender as Ribbon;
        }

        private void UserPreferences(object sender, RoutedEventArgs e)
        {
            mainFrame.Navigate(new CommandBarConfigurationPage());
        }        

        private void ShowDiagnostics(object sender, RoutedEventArgs e)
        {
            this.flyout.Content = TryFindResource("diagnostics");
            this.flyout.DataContext = Logger.logs;
            this.flyout.IsOpen = !this.flyout.IsOpen;
        }
    }
}
