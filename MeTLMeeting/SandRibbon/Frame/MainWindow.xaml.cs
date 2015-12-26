﻿using MeTLLib.DataTypes;
using Microsoft.Practices.Composite.Presentation.Commands;
using SandRibbon.Components;
using SandRibbon.Pages.Collaboration;
using SandRibbon.Pages.ServerSelection;
using SandRibbon.Properties;
using SandRibbon.Providers;
using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SandRibbon.Pages.Collaboration.Palettes;
using MahApps.Metro.Controls;
using SandRibbon.Pages.Conversations.Models;
using Microsoft.Win32;
using SandRibbon.Pages;
using SandRibbon.Frame.Flyouts;
using System.Collections.ObjectModel;

namespace SandRibbon
{
    public enum PresentationStrategy
    {
        WindowCoordinating, PageCoordinating
    }
    public partial class MainWindow : MetroWindow
    {
        private System.Windows.Threading.DispatcherTimer displayDispatcherTimer;

        public string CurrentProgress { get; set; }
        public static RoutedCommand ProxyMirrorExtendedDesktop = new RoutedCommand();

        public MainWindow()
        {
            InitializeComponent();
            DoConstructor();
            Commands.AllStaticCommandsAreRegistered();
            var globalState = new UserGlobalState();
            mainFrame.Navigate(new ServerSelectorPage(globalState));
            App.CloseSplashScreen();
        }

        private void DoConstructor()
        {
            Commands.MeTLType.DefaultValue = GlobalConstants.METL;
            Title = Strings.Global_ProductName;

            Commands.LaunchDiagnosticWindow.RegisterCommand(new DelegateCommand<object>(launchDiagnosticsWindow));

            Commands.ChangeLanguage.RegisterCommand(new DelegateCommand<System.Windows.Markup.XmlLanguage>(changeLanguage));
            Commands.CheckExtendedDesktop.RegisterCommand(new DelegateCommand<object>((_unused) => { CheckForExtendedDesktop(); }));

            Commands.SetUserOptions.RegisterCommand(new DelegateCommand<UserOptions>(SetUserOptions));

            Commands.SetWindowTitle.RegisterCommand(new DelegateCommand<string>(SetTitle));
            getDefaultSystemLanguage();
            displayDispatcherTimer = createExtendedDesktopTimer();

            Commands.AddFlyoutCard.RegisterCommand(new DelegateCommand<FlyoutCard>(addFlyoutCard));
            Commands.CloseFlyoutCard.RegisterCommand(new DelegateCommand<FlyoutCard>(closeFlyoutCard));
            Commands.CreateDummyCard.RegisterCommand(new DelegateCommand<object>(createDummyCard));
            cardControls.ItemsSource = flyoutCards;
            var flyoutReminderTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.ApplicationIdle, this.Dispatcher);
            flyoutReminderTimer.Interval = TimeSpan.FromSeconds(30);
            flyoutReminderTimer.Tick += new EventHandler((s, e) =>
            {
                refreshFlyoutState();
            });
            flyoutReminderTimer.Start();
            
        }
        
        protected void SetTitle(string newTitle)
        {
            this.Title = newTitle;
        }
        protected ObservableCollection<FlyoutCard> flyoutCards = new ObservableCollection<FlyoutCard>();
        protected void createDummyCard(object obj)
        {
            var ifc = new InformationFlyout("dummy flyout", "check this out! @ " + DateTime.Now.ToString());
            ifc.Width = new Random().Next(300) + 100;
            Commands.AddFlyoutCard.Execute(ifc);
        }
        protected void addFlyoutCard(object obj)
        {
            if (obj is FlyoutCard)
            {
                var fc = obj as FlyoutCard;
                flyoutCards.Add(fc);
                refreshFlyoutState();
            }
        }
        protected void closeFlyoutCard(object obj)
        {
            if (obj is FlyoutCard)
            {
                var fc = obj as FlyoutCard;
                flyoutCards.Remove(fc);
                refreshFlyoutState();
            }
        }
        protected void refreshFlyoutState()
        {
            Dispatcher.adopt(delegate
            {
                if (flyoutCards.Count > 0)
                {
                    flyout.Width = flyoutCards.Select(s => s.Width).Max();
                    flyout.IsOpen = true;
                }
                else
                {
                    flyout.IsOpen = false;
                }
            });
        }

        private void openProjectorWindow(object _unused)
        {
            Commands.MirrorPresentationSpace.Execute(this);
        }       
        
        
        private void serializeConversationToOneNote(OneNoteSynchronizationSet obj)
        {
            mainFrame.Navigate(obj.networkController.conversationSearchPage);// new ConversationSearchPage(obj.networkController));
        }

        private void PickImages(PickContext context)
        {
            var dialog = new OpenFileDialog();
            dialog.DefaultExt = "png";
            dialog.FileOk += delegate
            {
                foreach (var file in dialog.FileNames)
                {
                    context.Files.Add(file);
                }
            };
            dialog.ShowDialog();
        }
        private void openOneNoteConfiguration(OneNoteConfiguration obj)
        {
            flyout.Content = TryFindResource("oneNoteConfiguration");
            flyout.DataContext = obj;
            flyout.IsOpen = true;
        }

        private void MoveToOverview(SlideAwarePage obj)
        {
            mainFrame.Navigate(new ConversationOverviewPage(obj.UserGlobalState, obj.UserServerState, obj.UserConversationState, obj.NetworkController, obj.ConversationDetails));
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
                        Commands.ProxyMirrorPresentationSpace.ExecuteAsync(this);
                    else if (Projector.Window != null && screenCount == 1)
                        Projector.Window.Close();
                    else if (Projector.Window != null && screenCount > 1)
                    {
                        // Case 3.
                        Commands.ProxyMirrorPresentationSpace.ExecuteAsync(this);
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
            }
        }
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
                //Log(string.Format("Failure in language set: {0}", e.Message));
            }
        }
        private void SetUserOptions(UserOptions options)
        {
            Dispatcher.adopt(delegate
            {
                SaveUserOptions(options);
            });
        }
        private void SaveUserOptions(UserOptions options)
        {
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
        }

        private void UserPreferences(object sender, RoutedEventArgs e)
        {
            mainFrame.Navigate(new CommandBarConfigurationPage());
        }
        protected void launchDiagnosticsWindow(object _unused)
        {
            App.diagnosticWindow = new DiagnosticWindow(App.diagnosticController);
            App.diagnosticWindow.Show();
        }
    }
}
