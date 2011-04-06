﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Divelements.SandRibbon;
using Microsoft.Practices.Composite.Presentation.Commands;
using SandRibbon.Components;
using SandRibbon.Components.SimpleImpl;
using SandRibbon.Providers;
using SandRibbon.Providers.Structure;
using SandRibbon.Quizzing;
using SandRibbon.Utils;
using SandRibbon.Utils.Connection;
using SandRibbonInterop;
using MeTLLib.DataTypes;
using System.Diagnostics;
using System.Windows.Shapes;
using SandRibbon.Components.Sandpit;
using SandRibbon.Components.Pedagogicometry;
using SandRibbon.Tabs;
using SandRibbon.Tabs.Groups;
using System.Collections;
using System.Windows.Ink;
using System.Collections.ObjectModel;
using SandRibbon.Components.Utility;
using System.Windows.Documents;
using MeTLLib;

namespace SandRibbon
{
    public partial class Window1
    {
        public readonly string RECENT_DOCUMENTS = "recentDocuments.xml";
        #region SurroundingServers
        #endregion
        private PowerPointLoader loader;
        private UndoHistory history = new UndoHistory();
        public ConversationDetails details = null;
        public string CurrentProgress { get; set; }
        public static RoutedCommand ProxyMirrorExtendedDesktop = new RoutedCommand();
        public string log
        {
            get { return Logger.log; }
        }
        public Window1()
        {
            DoConstructor();
            Commands.AllStaticCommandsAreRegistered();
        }
        private void DoConstructor()
        {
            InitializeComponent();
            var level = ConfigurationProvider.instance.getMeTLPedagogyLevel();
            CommandParameterProvider.parameters[Commands.SetPedagogyLevel] = Pedagogicometer.level(level);
            Title = "MeTL 2011";
                //Globals.MeTLType;
            try {
                Icon = (ImageSource)new ImageSourceConverter().ConvertFromString("resources\\" + Globals.MeTLType + ".ico");
            }
            catch (Exception) { }
            Globals.userInformation.policy = new Policy(false, false);
            //create
            Commands.ImportPowerpoint.RegisterCommand(new DelegateCommand<object>(ImportPowerpoint));
            Commands.CreateBlankConversation.RegisterCommand(new DelegateCommand<object>(createBlankConversation));
            Commands.CreateConversation.RegisterCommand(new DelegateCommand<object>(createConversation, canCreateConversation));
            Commands.PreEditConversation.RegisterCommand(new DelegateCommand<object>(EditConversation, mustBeAuthor));
            Commands.ShowEditSlidesDialog.RegisterCommand(new DelegateCommand<object>(ShowEditSlidesDialog, mustBeInConversation));
            
            //conversation movement
            Commands.MoveTo.RegisterCommand(new DelegateCommand<int>(ExecuteMoveTo, CanExecuteMoveTo));
            Commands.JoinConversation.RegisterCommandToDispatcher(new DelegateCommand<string>(JoinConversation, mustBeLoggedIn));
            Commands.UpdateConversationDetails.RegisterCommand(new DelegateCommand<ConversationDetails>(UpdateConversationDetails));
            Commands.SetSync.RegisterCommand(new DelegateCommand<object>(setSync));

            Commands.ChangeTab.RegisterCommand(new DelegateCommand<string>(ChangeTab));
            Commands.LogOut.RegisterCommand(new DelegateCommand<object>(App.noop, mustBeLoggedIn));
            
            //zoom
            Commands.FitToView.RegisterCommand(new DelegateCommand<object>(App.noop, conversationSearchMustBeClosed));
            Commands.OriginalView.RegisterCommand(new DelegateCommand<object>(OriginalView, conversationSearchMustBeClosed));
            Commands.InitiateGrabZoom.RegisterCommand(new DelegateCommand<object>(App.noop, conversationSearchMustBeClosed));
            Commands.FitToView.RegisterCommand(new DelegateCommand<object>(FitToView));
            Commands.FitToPageWidth.RegisterCommand(new DelegateCommand<object>(FitToPageWidth));
            Commands.ExtendCanvasBothWays.RegisterCommand(new DelegateCommand<object>(App.noop, conversationSearchMustBeClosed));
            Commands.SetZoomRect.RegisterCommandToDispatcher(new DelegateCommand<Rectangle>(SetZoomRect));
 
            Commands.PrintConversation.RegisterCommand(new DelegateCommand<object>(PrintConversation, mustBeInConversation));
            
            Commands.ShowConversationSearchBox.RegisterCommandToDispatcher(new DelegateCommand<object>(ShowConversationSearchBox, mustBeLoggedIn));
            Commands.HideConversationSearchBox.RegisterCommandToDispatcher(new DelegateCommand<object>(HideConversationSearchBox));
            
            Commands.MirrorPresentationSpace.RegisterCommand(new DelegateCommand<object>(App.noop, mustBeInConversation));
            Commands.ProxyMirrorPresentationSpace.RegisterCommand(new DelegateCommand<object>(ProxyMirrorPresentationSpace));
            
            Commands.BlockInput.RegisterCommand(new DelegateCommand<string>(BlockInput));
            Commands.UnblockInput.RegisterCommand(new DelegateCommand<object>(UnblockInput));

            Commands.DummyCommandToProcessCanExecute.RegisterCommand(new DelegateCommand<object>(App.noop, conversationSearchMustBeClosed));
            Commands.ImageDropped.RegisterCommand(new DelegateCommand<object>(App.noop, mustBeLoggedIn));
            Commands.SetTutorialVisibility.RegisterCommandToDispatcher<object>(new DelegateCommand<object>(SetTutorialVisibility, mustBeInConversation));
            Commands.SendQuiz.RegisterCommand(new DelegateCommand<object>(App.noop, mustBeLoggedIn));
            
            Commands.SetConversationPermissions.RegisterCommand(new DelegateCommand<object>(SetConversationPermissions, CanSetConversationPermissions));
            Commands.AddWindowEffect.RegisterCommand(new DelegateCommand<object>(AddWindowEffect));
            Commands.RemoveWindowEffect.RegisterCommandToDispatcher(new DelegateCommand<object>(RemoveWindowEffect));
            Commands.SendWakeUp.RegisterCommand(new DelegateCommand<object>(App.noop, mustBeLoggedIn));
            Commands.ReceiveWakeUp.RegisterCommand(new DelegateCommand<object>(wakeUp));
            Commands.ReceiveSleep.RegisterCommand(new DelegateCommand<object>(sleep));
            
            //canvas stuff
            Commands.SetInkCanvasMode.RegisterCommand(new DelegateCommand<object>(SetInkCanvasMode, mustBeInConversation));
            Commands.SetLayer.RegisterCommand(new DelegateCommand<object>(App.noop, conversationSearchMustBeClosed));
            Commands.SetLayer.RegisterCommand(new DelegateCommand<object>(SetLayer, conversationSearchMustBeClosed));
            Commands.MoveCanvasByDelta.RegisterCommandToDispatcher(new DelegateCommand<Point>(GrabMove));
            Commands.AddImage.RegisterCommand(new DelegateCommand<object>(App.noop, conversationSearchMustBeClosed));
            Commands.SetTextCanvasMode.RegisterCommand(new DelegateCommand<object>(App.noop, conversationSearchMustBeClosed));
            Commands.UpdateTextStyling.RegisterCommand(new DelegateCommand<object>(App.noop, conversationSearchMustBeClosed));
            Commands.RestoreTextDefaults.RegisterCommand(new DelegateCommand<object>(App.noop, conversationSearchMustBeClosed));
            Commands.ToggleFriendsVisibility.RegisterCommand(new DelegateCommand<object>(ToggleFriendsVisibility, conversationSearchMustBeClosed));Commands.SetInkCanvasMode.RegisterCommand(new DelegateCommand<object>(App.noop, conversationSearchMustBeClosed));
            
            Commands.SetPedagogyLevel.RegisterCommand(new DelegateCommand<PedagogyLevel>(SetPedagogyLevel, mustBeLoggedIn));
            Commands.SetLayer.ExecuteAsync("Sketch");
            
            Commands.AddPrivacyToggleButton.RegisterCommand(new DelegateCommand<PrivacyToggleButton.PrivacyToggleButtonInfo>(AddPrivacyButton));
            Commands.RemovePrivacyAdorners.RegisterCommand(new DelegateCommand<object>(RemovePrivacyAdorners));
            Commands.DummyCommandToProcessCanExecuteForPrivacyTools.RegisterCommand(new DelegateCommand<object>(App.noop, conversationSearchMustBeClosedAndMustBeAllowedToPublish));
            Commands.FileUpload.RegisterCommand(new DelegateCommand<object>(App.noop, mustBeAuthor));

            Commands.ListenToAudio.RegisterCommand(new DelegateCommand<int>(ListenToAudio));
   
            Commands.Reconnecting.RegisterCommandToDispatcher(new DelegateCommand<bool>(Reconnecting));
            Commands.SetUserOptions.RegisterCommandToDispatcher(new DelegateCommand<UserOptions>(SetUserOptions));
            Commands.SetRibbonAppearance.RegisterCommandToDispatcher(new DelegateCommand<RibbonAppearance>(SetRibbonAppearance));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Print, PrintBinding));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Help, HelpBinding));
            adornerScroll.scroll = scroll;
            adornerScroll.scroll.SizeChanged += adornerScroll.scrollChanged;
            adornerScroll.scroll.ScrollChanged += adornerScroll.scroll_ScrollChanged;
            AddWindowEffect(null);
            ribbon.Loaded += ribbon_Loaded;
            WorkspaceStateProvider.RestorePreviousSettings();
            CommandManager.InvalidateRequerySuggested();
            player.LoadedBehavior = MediaState.Manual;
            player.Loaded += playMedia;
            //player.MediaOpened += playMedia;
            RibbonApplicationPopup.Opened += ApplicationButtonPopup_Opened;
            RibbonApplicationPopup.Closed += ApplicationButtonPopup_Closed;
        }
        private void ApplicationButtonPopup_Closed(object sender, EventArgs e)
        {
            Trace.TraceInformation("ApplicationButtonPopup_Closed");
            Commands.SetTutorialVisibility.ExecuteAsync(Visibility.Collapsed);
        }
        private void ApplicationButtonPopup_Opened(object sender, EventArgs e)
        {
            if (ribbon.Tabs.Count < 1)
                    RibbonApplicationPopup.IsOpen = false;
            Trace.TraceInformation("ApplicationButtonPopup_Opened");
            Commands.SetTutorialVisibility.ExecuteAsync(Visibility.Visible);
        }
        #region helpLinks
        private void OpenEULABrowser(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://metl.adm.monash.edu.au/MeTL/docs/tabletSupport/MLS_UserAgreement.html");
        }
        private void OpenTutorialBrowser(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://metl.adm.monash.edu.au/MeTL/docs/tabletSupport/MLS_Tutorials.html");
        }
        private void OpenReportBugBrowser(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://metl.adm.monash.edu.au/MeTL/docs/report_a_bug.html");
        }
        private void OpenAboutMeTLBrowser(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.monash.edu.au/eeducation/myls2010/students/resources/software/metl/");
        }
        #endregion
        private void ApplicationPopup_ShowOptions(object sender, EventArgs e)
        {
            Trace.TraceInformation("UserOptionsDialog_Show");
            new UserOptionsDialog().Show();
        }
        private void playMedia(object sender, EventArgs e)
        {
            player.Position = new TimeSpan(0, 0, 0);
            player.Play();
        }
        private void ListenToAudio(int jid) {
            player.Source = new Uri("http://radar.adm.monash.edu:8500/MeTLStream1.m3u");
        }
        private void SetLayer(object layer) {
            Trace.TraceInformation("SelectedMode {0}", layer);
        }
        private void ImportPowerpoint(object obj)
        {
            if(loader == null) loader = new PowerPointLoader();
            loader.ImportPowerpoint();
        }
        private void SetRibbonAppearance(RibbonAppearance appearance)
        {
            Appearance = appearance;
        }
        private void createBlankConversation(object obj)
        {
            if(loader == null) loader = new PowerPointLoader();
            loader.CreateBlankConversation();
        }
        private void PrintBinding(object sender, EventArgs e) {
            PrintConversation(null);
        }
        private void HelpBinding(object sender, EventArgs e) {
            LaunchHelp(null);
        }
        private void LaunchHelp(object _arg)
        {
            try
            {
                Process.Start("http://monash.edu/eeducation/myls2010/metlhelp.html");
            }
            catch (Exception)
            {
            }
        }
        private void PrintConversation(object _arg) {
            if(Globals.UserOptions.includePrivateNotesOnPrint)
                new Printer().PrintPrivate(Globals.conversationDetails.Jid, Globals.me);
            else
                new Printer().PrintHandout(Globals.conversationDetails.Jid, Globals.me);
        }
        private void SetUserOptions(UserOptions options) {
            //this next line should be removed.
            SaveUserOptions(options);

            if (!ribbon.IsMinimized && currentConversationSearchBox.Visibility == Visibility.Visible)
                    ribbon.ToggleMinimize();
        }
        private void SaveUserOptions(UserOptions options)
        {
            //this should be wired to a new command, SaveUserOptions, which is commented out in SandRibbonInterop.Commands
            ClientFactory.Connection().SaveUserOptions(Globals.me, options);
        }
        void ribbon_Loaded(object sender, RoutedEventArgs e)
        {
            DelegateCommand<object> hideRibbon = null;
            hideRibbon = new DelegateCommand<object>((_obj) =>{
            
                Commands.SetPedagogyLevel.UnregisterCommand(hideRibbon);
                if (!ribbon.IsMinimized)
                    ribbon.ToggleMinimize();
            });
            Commands.SetPedagogyLevel.RegisterCommand(hideRibbon);
        }
        private void ShowConversationSearchBox(object _arg)
        {
            if (!ribbon.IsMinimized)
                ribbon.ToggleMinimize();
        }
        private void HideConversationSearchBox(object _arg)
        {
            if (ribbon.IsMinimized)
                ribbon.ToggleMinimize();
        }
        private void Reconnecting(bool success) {
            if (success)
            {
                Commands.UpdateConversationDetails.Execute(ConversationDetails.Empty);
                hideReconnectingDialog();
            }
            else
            {
                Trace.TraceInformation("Reconnected");
                showReconnectingDialog();
            }
        }
        private void ToggleFriendsVisibility(object unused)
        {
            if (chatGridsplitter.Visibility == Visibility.Visible)
            {
                chatGridsplitter.Visibility = Visibility.Collapsed;
                leftDrawer.Visibility = Visibility.Collapsed;
                LHSSplitterDefinition.Width = new GridLength(0);
                LHSDrawerDefinition.Width = new GridLength(0);
            }
            else
            {
                chatGridsplitter.Visibility = Visibility.Visible;
                leftDrawer.Visibility = Visibility.Visible;
                LHSSplitterDefinition.Width = new GridLength(10);
                LHSDrawerDefinition.Width = new GridLength((columns.ActualWidth - rightDrawer.ActualWidth) / 4);
            }
        }
        private void ShowEditSlidesDialog(object unused)
        {
            new SlidesEditingDialog().ShowDialog();
            var seDialog = new SlidesEditingDialog();
            seDialog.Owner = Window.GetWindow(this);
            seDialog.ShowDialog();
        }
        private void SetInkCanvasMode(object unused)
        {
            Commands.SetLayer.ExecuteAsync("Sketch");
        }
        private void AddPrivacyButton(PrivacyToggleButton.PrivacyToggleButtonInfo info)
        {
            var adorner = ((FrameworkElement)canvasViewBox);
            Dispatcher.adoptAsync(() =>
            {
                var adornerRect = new Rect(canvas.TranslatePoint(info.ElementBounds.TopLeft, canvasViewBox), canvas.TranslatePoint(info.ElementBounds.BottomRight, canvasViewBox));
                if (adornerRect.Right < 0 || adornerRect.Right > canvasViewBox.ActualWidth
                    || adornerRect.Top < 0 || adornerRect.Top > canvasViewBox.ActualHeight) return;
                AdornerLayer.GetAdornerLayer(adorner).Add(new UIAdorner(adorner, new PrivacyToggleButton(info.privacyChoice, adornerRect)));
            });
        }
        private Adorner[] getPrivacyAdorners()
        {
            var adornerLayer = AdornerLayer.GetAdornerLayer(canvasViewBox);
            if (adornerLayer == null) return null;
            return adornerLayer.GetAdorners(canvasViewBox);
        }
        private void UpdatePrivacyAdorners()
        {
            var privacyAdorners = getPrivacyAdorners();
            RemovePrivacyAdorners(null);
            if (privacyAdorners != null && privacyAdorners.Count() > 0)
                try
                {
                    var lastValue = Commands.AddPrivacyToggleButton.lastValue();
                    if (lastValue != null)
                        AddPrivacyButton((PrivacyToggleButton.PrivacyToggleButtonInfo)lastValue);
                }
                catch (NotSetException) { }
        }
        private void RemovePrivacyAdorners(object _unused)
        {
            Dispatcher.adoptAsync(delegate
            {
                var adorners = getPrivacyAdorners();
                var adornerLayer = AdornerLayer.GetAdornerLayer(canvasViewBox);
                if (adorners != null)
                    foreach (var adorner in adorners)
                        adornerLayer.Remove(adorner);
            });
        }
        private void ProxyMirrorPresentationSpace(object unused)
        {
            Commands.MirrorPresentationSpace.ExecuteAsync(this);
        }
        private void GrabMove(Point moveDelta)
        {
            if (moveDelta.X != 0)
                scroll.ScrollToHorizontalOffset(scroll.HorizontalOffset + moveDelta.X);
            if (moveDelta.Y != 0)
                scroll.ScrollToVerticalOffset(scroll.VerticalOffset + moveDelta.Y);
            try
            {
                if (moveDelta.X != 0)
                {
                    var HZoomRatio = (scroll.ExtentWidth / scroll.Width);
                    scroll.ScrollToHorizontalOffset(scroll.HorizontalOffset + (moveDelta.X * HZoomRatio));
                }
                if (moveDelta.Y != 0)
                {
                    var VZoomRatio = (scroll.ExtentHeight / scroll.Height);
                    scroll.ScrollToVerticalOffset(scroll.VerticalOffset + moveDelta.Y * VZoomRatio);
                }
            }
            catch (Exception e)
            {//out of range exceptions and the like 
            }
        }
        private void ChangeTab(string which)
        {
            foreach (var tab in ribbon.Tabs)
                if (((RibbonTab)tab).Text == which)
                    ribbon.SelectedTab = (RibbonTab)tab;
        }
        private void BroadcastZoom()
        {
            var currentZoomHeight = scroll.ActualHeight / canvasViewBox.ActualHeight;
            var currentZoomWidth = scroll.ActualWidth / canvasViewBox.ActualWidth;
            var currentZoom = Math.Max(currentZoomHeight, currentZoomWidth);
            Commands.ZoomChanged.Execute(currentZoom);
        }
        private void SetTutorialVisibility(object visibilityObject)
        {
            var newVisibility = (Visibility)visibilityObject;
            switch (newVisibility)
            {
                case Visibility.Visible:
                    ShowTutorial();
                    break;
                default:
                    HideTutorial();
                    break;
            }
        }
        private void CreateConversation(object _unused)
        {
            Trace.TraceInformation("CreatedBlankConversation");
            Commands.CreateBlankConversation.ExecuteAsync(null);
        }
        private void EditConversation(object _unused)
        {
            ShowPowerpointBlocker("Editing Conversation Dialog Open");
            Commands.EditConversation.ExecuteAsync(Globals.location.activeConversation);
        }
        private void BlockInput(string message)
        {
            ShowPowerpointBlocker(message);
        }
        private void UnblockInput(object _unused)
        {
            Dispatcher.adoptAsync((HideProgressBlocker));
        }
        private void ConnectWithAuthenticatedCredentials(MeTLLib.DataTypes.Credentials credentials)
        {
            Commands.AllStaticCommandsAreRegistered();
            Commands.RequerySuggested();
            Pedagogicometer.SetPedagogyLevel(Globals.pedagogy);
            Commands.SetIdentity.ExecuteAsync(credentials);
        }
        private void SetZoomRect(Rectangle viewbox)
        {
            var topleft = new Point(Canvas.GetLeft(viewbox), Canvas.GetTop(viewbox));
            scroll.Width = viewbox.Width;
            scroll.Height = viewbox.Height;
            scroll.ScrollToHorizontalOffset(topleft.X);
            scroll.ScrollToVerticalOffset(topleft.Y);
            Trace.TraceInformation("ZoomRect changed to X:{0},Y:{1},W:{2},H:{3}", topleft.X, topleft.Y, viewbox.Width, viewbox.Height);
        }
        private bool SmartBoardMeTLAlreadyLoaded
        {
            get
            {
                var thisProcess = Process.GetCurrentProcess();
                return Process.GetProcessesByName("MeTL").Any(p =>
                    p.Id != thisProcess.Id &&
                    (p.MainWindowTitle.StartsWith("S15") || p.MainWindowTitle.Equals("")));
            }
        }
        private void AddWindowEffect(object _o)
        {
            CanvasBlocker.Visibility = Visibility.Visible;
        }
        private void RemoveWindowEffect(object _o)
        {
            CanvasBlocker.Visibility = Visibility.Collapsed;
        }
        private void ExecuteMoveTo(int slide)
        {
            MoveTo(slide);
        }
        private bool CanExecuteMoveTo(int slide)
        {
            return mustBeInConversation(slide);
        }
        private void JoinConversation(string title)
        {
            if(ribbon.SelectedTab!=null)
                ribbon.SelectedTab = ribbon.Tabs[0];
            var thisDetails = ClientFactory.Connection().DetailsOf(title);
            MeTLLib.ClientFactory.Connection().AsyncRetrieveHistoryOf(Int32.Parse(title));
            Commands.SetPrivacy.Execute(thisDetails.Author == Globals.me ? "public" : "private");
            applyPermissions(thisDetails.Permissions);
            Commands.RequerySuggested(Commands.SetConversationPermissions);
            Commands.SetLayer.ExecuteAsync("Sketch");
        }
        private string messageFor(ConversationDetails details)
        {
            var permissionLabel = Permissions.InferredTypeOf(details.Permissions).Label;
            return string.Format("Collaboration {0}  -  {1}'s \"{2}\" - MeTL", (permissionLabel == "tutorial") ? "ENABLED" : "DISABLED", details.Author, details.Title);
        }
        private void MoveTo(int slide)
        {
            
            Dispatcher.adoptAsync(delegate
                                     {
                                         if (canvas.Visibility == Visibility.Collapsed)
                                             canvas.Visibility = Visibility.Visible;
                                         scroll.Width = Double.NaN;
                                         scroll.Height = Double.NaN;
                                         canvas.Width = Double.NaN;
                                         canvas.Height = Double.NaN;
                                     });
            CommandManager.InvalidateRequerySuggested();
        }
        private void hideReconnectingDialog() { 
            ProgressDisplay.Children.Clear();
            InputBlocker.Visibility = Visibility.Collapsed;
        }
        private void showReconnectingDialog()
        {
            ProgressDisplay.Children.Clear();
            var sp = new StackPanel();
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
            sp.Children.Add(majorHeading);
            sp.Children.Add(minorHeading);
            ProgressDisplay.Children.Add(sp);
            InputBlocker.Visibility = Visibility.Visible;
        }
        private void ShowTutorial()
        {
            TutorialLayer.Visibility = Visibility.Visible;
        }
        private void HideTutorial()
        {
            if (Globals.userInformation.location != null && !String.IsNullOrEmpty(Globals.userInformation.location.activeConversation))
                TutorialLayer.Visibility = Visibility.Collapsed;
        }
        private bool canCreateConversation(object obj)
        {
            return doesConversationAlreadyExist(obj) && mustBeLoggedIn(obj);
        }
        private bool doesConversationAlreadyExist(object obj)
        {
            if (!(obj is ConversationDetails))
                return true;
            var details = (ConversationDetails)obj;
            if (details == null)
                return true;
            if (details.Subject.ToLower() == "deleted")
                return true;
            if (details.Title.Length == 0)
                return true;
            var currentConversations = MeTLLib.ClientFactory.Connection().AvailableConversations;
            bool conversationExists = currentConversations.Any(c => c.Title.Equals(details.Title));
            return !conversationExists;
        }
        private bool mustBeLoggedIn(object _arg)
        {
            try
            {
                return Globals.credentials != null;
            }
            catch (NotSetException)
            {
                return false;
            }
        }
        private bool conversationSearchMustBeClosed(object _obj)
        {
            return currentConversationSearchBox.Visibility == Visibility.Collapsed && mustBeInConversation(null);
        }
        private bool conversationSearchMustBeClosedAndMustBeAllowedToPublish(object _obj)
        {
            if (conversationSearchMustBeClosed(null))
                return Globals.isAuthor || Globals.conversationDetails.Permissions.studentCanPublish;
            else return false;
        }

        private bool mustBeInConversation(object _arg)
        {
            ConversationDetails details;
            try
            {
                details = Globals.conversationDetails;
            }
            catch (NotSetException)
            {
                return false;
            }
            if (details == null) return false;
            if(details.Subject != "Deleted" && details.Jid != "")
                    return true;
            return false;
        }
        private bool mustBeAuthor(object _arg)
        {
            try
            {
                return Globals.isAuthor;
            }
            catch (NotSetException)
            {
                return false;
            }
        }
        private void UpdateConversationDetails(ConversationDetails details)
        {
            Dispatcher.adoptAsync(delegate
            {
                if (details != null)
                    HideTutorial();
                if (details.Jid == Globals.location.activeConversation)
                    UpdateTitle(details);
                this.details = details;
            });
        }
        private void UpdateTitle(ConversationDetails details)
        {
            try
            {
                if (details.Subject.ToLower() != "deleted")
                    Title = messageFor(Globals.conversationDetails);
                else
                    Title = "MeTL 2011";
            }
            catch (NotSetException)
            {
                Title = "MeTL 2011";
            }
        }
        private DelegateCommand<object> canOpenFriendsOverride;
        private void applyPermissions(Permissions permissions)
        {
            if (canOpenFriendsOverride != null)
                Commands.ToggleFriendsVisibility.UnregisterCommand(canOpenFriendsOverride);
            canOpenFriendsOverride = new DelegateCommand<object>((_param) => { }, (_param) => true);
            Commands.ToggleFriendsVisibility.RegisterCommand(canOpenFriendsOverride);
        }
        private void showPowerPointProgress(string progress)
        {
            var text = new TextBlock
                           {
                               Foreground = Brushes.WhiteSmoke,
                               Text = progress
                           };
            ProgressDisplay.Children.Add(text);
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
                var connection = MeTLLib.ClientFactory.Connection();
                details = connection.CreateConversation(details);
                CommandManager.InvalidateRequerySuggested();
                if (Commands.JoinConversation.CanExecute(details.Jid))
                    Commands.JoinConversation.ExecuteAsync(details.Jid);
            }
        }
        private void debugTrue(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            Console.WriteLine(sender.GetType());
        }
        private void closeApplication(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }
        private static void SetVisibilityOf(UIElement target, Visibility visibility)
        {
            target.Visibility = visibility;
        }
        private void setSync(object _obj)
        {
            Globals.userInformation.policy.isSynced = !Globals.userInformation.policy.isSynced;
        }
        private void OriginalView(object _unused)
        {
            Trace.TraceInformation("ZoomToOriginalView");
            var currentSlide = Globals.conversationDetails.Slides.Where(s => s.id == Globals.slide).FirstOrDefault();
            if (currentSlide == null || currentSlide.defaultHeight == 0 || currentSlide.defaultWidth == 0) return;
            scroll.Width = currentSlide.defaultWidth;
            scroll.Height = currentSlide.defaultHeight;
            scroll.ScrollToLeftEnd();
            scroll.ScrollToTop();
        }
        private void FitToView(object _unused)
        {
            scroll.Height = double.NaN;
            scroll.Width = double.NaN;
            canvas.Height = double.NaN;
            canvas.Width = double.NaN;
        }
        private void FitToPageWidth(object _unused)
        {
            if (scroll != null)
            {
                var ratio = adornerGrid.ActualWidth / adornerGrid.ActualHeight;
                scroll.Height = canvas.ActualWidth / ratio;
                scroll.Width = canvas.ActualWidth;
            }
        }
        private void ShowPowerpointBlocker(string explanation)
        {
            Dispatcher.adoptAsync(() =>
            {
                showPowerPointProgress(explanation);
                Canvas.SetTop(ProgressDisplay, (canvas.ActualHeight / 2));
                Canvas.SetLeft(ProgressDisplay, (ActualWidth / 2) - 100);
                InputBlocker.Visibility = Visibility.Visible;
            });
        }
        private void HideProgressBlocker()
        {
            ProgressDisplay.Children.Clear();
            InputBlocker.Visibility = Visibility.Collapsed;
        }
        private void canZoomIn(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !(scroll == null) && mustBeInConversation(null) && conversationSearchMustBeClosed(null);
        }
        private void canZoomOut(object sender, CanExecuteRoutedEventArgs e)
        {
            if (scroll == null)
                e.CanExecute = false;
            else
            {
                var cvHeight = adornerGrid.ActualHeight;
                var cvWidth = adornerGrid.ActualWidth;
                var cvRatio = cvWidth / cvHeight;
                bool hTrue = scroll.ViewportWidth < scroll.ExtentWidth;
                bool vTrue = scroll.ViewportHeight < scroll.ExtentHeight;
                var scrollRatio = scroll.ActualWidth / scroll.ActualHeight;
                if (scrollRatio > cvRatio)
                {
                    e.CanExecute = hTrue;
                }
                if (scrollRatio < cvRatio)
                {
                    e.CanExecute = vTrue;
                }
                e.CanExecute = (hTrue || vTrue) && mustBeInConversation(null) && conversationSearchMustBeClosed(null);
            }
        }
        private void adornerGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //FixCanvasAspectAfterWindowSizeChanges(e.PreviousSize, e.NewSize);
        }
        private void FixCanvasAspectAfterWindowSizeChanges(System.Windows.Size oldSize, System.Windows.Size newSize)
        {
            var cvHeight = adornerGrid.ActualHeight;
            var cvWidth = adornerGrid.ActualWidth;
            var cvRatio = cvWidth / cvHeight;
            var scrollRatio = scroll.ActualWidth / scroll.ActualHeight;
            if (oldSize.Height == newSize.Height)
            {
                if (scroll.ActualHeight * cvRatio > scroll.ExtentWidth && !Double.IsNaN(scroll.Width))
                {
                    scroll.Width = scroll.ExtentWidth;
                    return;
                }
                scroll.Width = scroll.ActualHeight * cvRatio;
                return;
            }
            if (oldSize.Width == newSize.Width && !Double.IsNaN(scroll.Height))
            {
                if (scroll.ActualWidth / cvRatio > scroll.ExtentHeight)
                {
                    scroll.Height = scroll.ExtentHeight;
                    return;
                }
                scroll.Height = scroll.ActualWidth / cvRatio;
                return;
            }
            if (scrollRatio > cvRatio && !Double.IsNaN(scroll.Width))
            {
                var newWidth = scroll.ActualHeight * cvRatio;
                scroll.Width = newWidth;
                return;
            }
            if (scrollRatio < cvRatio && !Double.IsNaN(scroll.Height))
            {
                var newHeight = scroll.Width / cvRatio;
                scroll.Height = newHeight;
                return;
            }
            if (Double.IsNaN(scrollRatio))
            {
                scroll.Width = scroll.ExtentWidth;
                scroll.Height = scroll.ExtentHeight;
                return;
            }
        }
        private void doZoomIn(object sender, ExecutedRoutedEventArgs e)
        {
            Trace.TraceInformation("ZoomIn pressed");
            var ZoomValue = 0.9;
            var scrollHOffset = scroll.HorizontalOffset;
            var scrollVOffset = scroll.VerticalOffset;
            var cvHeight = adornerGrid.ActualHeight;
            var cvWidth = adornerGrid.ActualWidth;
            var cvRatio = cvWidth / cvHeight;
            double newWidth = 0;
            double newHeight = 0;
            double oldWidth = scroll.ActualWidth;
            double oldHeight = scroll.ActualHeight;
            var scrollRatio = oldWidth / oldHeight;
            if (scrollRatio > cvRatio)
            {
                newWidth = scroll.ActualWidth * ZoomValue;
                if (newWidth > scroll.ExtentWidth)
                    newWidth = scroll.ExtentWidth;
                scroll.Width = newWidth;
                newHeight = newWidth / cvRatio;
                if (newHeight > scroll.ExtentHeight)
                    newHeight = scroll.ExtentHeight;
                scroll.Height = newHeight;
            }
            if (scrollRatio < cvRatio)
            {
                newHeight = scroll.ActualHeight * ZoomValue;
                if (newHeight > scroll.ExtentHeight)
                    newHeight = scroll.ExtentHeight;
                scroll.Height = newHeight;
                newWidth = newHeight * cvRatio;
                if (newWidth > scroll.ExtentWidth)
                    newWidth = scroll.ExtentWidth;
                scroll.Width = newWidth;
            }
            if (scrollRatio == cvRatio)
            {
                newHeight = scroll.ActualHeight * ZoomValue;
                if (newHeight > scroll.ExtentHeight)
                    newHeight = scroll.ExtentHeight;
                scroll.Height = newHeight;
                newWidth = scroll.ActualWidth * ZoomValue;
                if (newWidth > scroll.ExtentWidth)
                    newWidth = scroll.ExtentWidth;
                scroll.Width = newWidth;
            }
            scroll.ScrollToHorizontalOffset(scrollHOffset + ((oldWidth - newWidth) / 2));
            scroll.ScrollToVerticalOffset(scrollVOffset + ((oldHeight - newHeight) / 2));
        }
        private void doZoomOut(object sender, ExecutedRoutedEventArgs e)
        {
            Trace.TraceInformation("ZoomOut pressed");
            var ZoomValue = 1.1;
            var scrollHOffset = scroll.HorizontalOffset;
            var scrollVOffset = scroll.VerticalOffset;
            var cvHeight = adornerGrid.ActualHeight;
            var cvWidth = adornerGrid.ActualWidth;
            var cvRatio = cvWidth / cvHeight;
            var scrollRatio = scroll.ActualWidth / scroll.ActualHeight;
            double newWidth = 0;
            double newHeight = 0;
            double oldWidth = scroll.ActualWidth;
            double oldHeight = scroll.ActualHeight;
            if (scrollRatio > cvRatio)
            {
                newWidth = scroll.ActualWidth * ZoomValue;
                if (newWidth > scroll.ExtentWidth)
                    newWidth = scroll.ExtentWidth;
                scroll.Width = newWidth;
                newHeight = newWidth / cvRatio;
                if (newHeight > scroll.ExtentHeight)
                    newHeight = scroll.ExtentHeight;
                scroll.Height = newHeight;
            }
            if (scrollRatio < cvRatio)
            {
                newHeight = scroll.ActualHeight * ZoomValue;
                if (newHeight > scroll.ExtentHeight)
                    newHeight = scroll.ExtentHeight;
                scroll.Height = newHeight;
                newWidth = newHeight * cvRatio;
                if (newWidth > scroll.ExtentWidth)
                    newWidth = scroll.ExtentWidth;
                scroll.Width = newWidth;
            }
            if (scrollRatio == cvRatio)
            {
                newHeight = scroll.ActualHeight * ZoomValue;
                if (newHeight > scroll.ExtentHeight)
                    newHeight = scroll.ExtentHeight;
                scroll.Height = newHeight;
                newWidth = scroll.ActualWidth * ZoomValue;
                if (newWidth > scroll.ExtentWidth)
                    newWidth = scroll.ExtentWidth;
                scroll.Width = newWidth;
            }
            scroll.ScrollToHorizontalOffset(scrollHOffset + ((oldWidth - newWidth) / 2));
            scroll.ScrollToVerticalOffset(scrollVOffset + ((oldHeight - newHeight) / 2));
        }
        public Visibility GetVisibilityOf(UIElement target)
        {
            return target.Visibility;
        }
        private void SetConversationPermissions(object obj)
        {
            var style = (string)obj;
            /*foreach (var s in new[]{
                Permissions.LABORATORY_PERMISSIONS,
                Permissions.TUTORIAL_PERMISSIONS,
                Permissions.LECTURE_PERMISSIONS,
                Permissions.MEETING_PERMISSIONS})
                if (s.Label == style)
                    details.Permissions = s;
            MeTLLib.ClientFactory.Connection().UpdateConversationDetails(details);*/
            try
            {
                details = Globals.conversationDetails;
                foreach (var s in new[]
                                      {
                                          Permissions.LABORATORY_PERMISSIONS,
                                          Permissions.TUTORIAL_PERMISSIONS,
                                          Permissions.LECTURE_PERMISSIONS,
                                          Permissions.MEETING_PERMISSIONS
                                      })
                    if (s.Label == style)
                        details.Permissions = s;
                var client = MeTLLib.ClientFactory.Connection();
                client.UpdateConversationDetails(details);
                //ConversationDetailsProviderFactory.Provider.Update(details);
            }
            catch (NotSetException e)
            {
                return;
            }
        }
        private bool CanSetConversationPermissions(object _style)
        {
            return details != null && Globals.userInformation.credentials.name == details.Author;
            try
            {
                return Globals.conversationDetails != null && Globals.userInformation.credentials.name == Globals.conversationDetails.Author;
            }
            catch (NotSetException e)
            {
                return false;
            }
        }
        /*taskbar management*/
        private System.Windows.Forms.NotifyIcon m_notifyIcon;
        private void sleep(object _obj)
        {
            Dispatcher.adoptAsync(delegate { Hide(); });
        }
        private void wakeUp(object _obj)
        {
            Dispatcher.adoptAsync(delegate
            {
                Show();
                WindowState = System.Windows.WindowState.Maximized;
            });
        }
        void ShowTrayIcon(bool show)
        {
            if (m_notifyIcon != null)
                m_notifyIcon.Visible = show;
        }
        public void SetPedagogyLevel(PedagogyLevel level)
        {
            SetupUI(level);
        }
        public void ClearUI()
        {
            Commands.UnregisterAllCommands();
            Dispatcher.adoptAsync(() =>
            {
                ribbon.Tabs.Clear();
                privacyTools.Children.Clear();
                RHSDrawerDefinition.Width = new GridLength(0);
            });
        }
        private GridSplitter split()
        {
            return new GridSplitter
            {
                ShowsPreview = true,
                VerticalAlignment = VerticalAlignment.Stretch,
                Width = 10,
                Height = Double.NaN,
                ResizeBehavior = GridResizeBehavior.PreviousAndNext
            };
        }
        public void SetupUI(PedagogyLevel level)
        {
            Dispatcher.adopt(() =>
            {
                List<FrameworkElement> homeGroups = new List<FrameworkElement>();
                List<FrameworkElement> tabs = new List<FrameworkElement>();
                foreach (var i in Enumerable.Range(0, level.code + 1))
                {
                    switch (i)
                    {
                        case 0:
                            ClearUI();
                            homeGroups.Add(new EditingOptions());
                            break;
                        case 1:
                            tabs.Add(new Tabs.Quizzes());
                            tabs.Add(new Tabs.Submissions());
                            tabs.Add(new Tabs.Attachments());
                            homeGroups.Add(new EditingModes());
                            break;
                        case 2:
                            //ribbon.ApplicationPopup = new Chrome.ApplicationPopup();
                            RHSDrawerDefinition.Width = new GridLength(180);
                            homeGroups.Add(new ZoomControlsHost());
                            homeGroups.Add(new MiniMap());
                            break;
                        case 3:
                            homeGroups.Add(new PrivacyToolsHost());
                            break;
                        case 4:
                            homeGroups.Add(new Notes());
                            tabs.Add(new Tabs.Analytics());
                            tabs.Add(new Tabs.Plugins());
                            break;
                        default:
                            break;
                    }
                }
                var home = new Tabs.Home { DataContext = scroll };
                homeGroups.Sort(new PreferredDisplayIndexComparer());
                foreach (var group in homeGroups)
                    home.Items.Add((RibbonGroup)group);
                tabs.Add(home);
                tabs.Sort(new PreferredDisplayIndexComparer());
                foreach (var tab in tabs)
                    ribbon.Tabs.Add((RibbonTab)tab);
                ribbon.SelectedTab = home;
                if (!ribbon.IsMinimized && currentConversationSearchBox.Visibility == Visibility.Visible)
                    ribbon.ToggleMinimize();
            });
            CommandManager.InvalidateRequerySuggested();
            Commands.RequerySuggested();
            Commands.SetLayer.ExecuteAsync("Sketch");
        }
        private class PreferredDisplayIndexComparer : IComparer<FrameworkElement>
        {
            public int Compare(FrameworkElement anX, FrameworkElement aY)
            {
                try
                {
                    var x = Int32.Parse((string)anX.FindResource("preferredDisplayIndex"));
                    var y = Int32.Parse((string)aY.FindResource("preferredDisplayIndex"));
                    return x - y;
                }
                catch (FormatException e)
                {
                    return 0;
                }
            }
        }
        private void zoomConcernedControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePrivacyAdorners();
            BroadcastZoom();
        }
        private void scroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdatePrivacyAdorners();
            BroadcastZoom();
        }
        private void ribbonWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (App.AccidentallyClosing.AddMilliseconds(250) > DateTime.Now)
            {
                e.Cancel = true;
            }
            else
            {
                Application.Current.Shutdown();
            }
        }
        private void ApplicationPopup_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            App.AccidentallyClosing = DateTime.Now;
        }
    }
}