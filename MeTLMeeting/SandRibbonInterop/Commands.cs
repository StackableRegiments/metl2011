﻿using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Microsoft.Practices.Composite.Presentation.Commands;
using System;
using System.Collections.Generic;
using System.Windows;
using MeTLLib.DataTypes;
using System.Windows.Threading;

namespace SandRibbon
{
    public class NotSetException : Exception
    {
        public NotSetException(string msg) : base(msg) { }
    }
    public class DefaultableCompositeCommand : CompositeCommand
    {
        private bool isSet = false;
        private object commandValue = null;

        public object DefaultValue
        {
            get
            {
                Debug.Assert(isSet, "Default value has not been set");
                return commandValue;
            }
            set
            {
                isSet = true;
                commandValue = value;
            }
        }
        public DefaultableCompositeCommand()
        {
        }

        public DefaultableCompositeCommand(object newValue)
        {
            DefaultValue = newValue;
        }

        public bool IsInitialised
        {
            get
            {
                return isSet;
            }
        }

        public object LastValue()
        {
            Debug.Assert(isSet, "Default value has not been set");
            return DefaultValue;
        }
        public override void Execute(object arg)
        {
            DefaultValue = arg;
            base.Execute(arg);
        }

        public override void RegisterCommand(ICommand command)
        {
            if (RegisteredCommands.Contains(command)) return;
            base.RegisterCommand(command);
        }
        public override void UnregisterCommand(ICommand command)
        {
            base.UnregisterCommand(command);
        }
    }
    public class Commands
    {
        public static DefaultableCompositeCommand Mark = new DefaultableCompositeCommand();

        public static DefaultableCompositeCommand WordCloud = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand BrowseOneNote = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ManuallyConfigureOneNote = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SerializeConversationToOneNote = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand RequestMeTLUserInformations = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand MoveToNotebookPage = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveMeTLUserInformations = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand RequestTeacherStatus = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveTeacherStatus = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SetStackVisibility = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SendNewSlideOrder = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ChangeLanguage = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand PresentVideo = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReorderDragDone = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ConnectToSmartboard = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand DisconnectFromSmartboard = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ViewSubmissions = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ViewBannedContent = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand Reconnecting = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand LeaveAllRooms = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand BackstageModeChanged = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand UpdatePowerpointProgress = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ShowOptionsDialog = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SetUserOptions = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ZoomChanged = new DefaultableCompositeCommand();

        public static DefaultableCompositeCommand ExtendCanvasBySize = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ExtendCanvasUp = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ExtendCanvasDown = new DefaultableCompositeCommand();

        public static DefaultableCompositeCommand CheckExtendedDesktop = new DefaultableCompositeCommand();

        public static DefaultableCompositeCommand AddPrivacyToggleButton = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand RemovePrivacyAdorners = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand MirrorVideo = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand VideoMirrorRefreshRectangle = new DefaultableCompositeCommand();

        public static DefaultableCompositeCommand AnalyzeSelectedConversations = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SendWakeUp = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SendSleep = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveWakeUp = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveSleep = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SendMoveBoardToSlide = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveMoveBoardToSlide = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SendPing = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceivePong = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand DoWithCurrentSelection = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand BubbleCurrentSelection = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveNewBubble = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ExploreBubble = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ThoughtLiveWindow = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SetZoomRect = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand Highlight = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand RemoveHighlight = new DefaultableCompositeCommand();

        public static DefaultableCompositeCommand ServersDown = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand RequestScreenshotSubmission = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand GenerateScreenshot = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ScreenshotGenerated = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SendScreenshotSubmission = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveScreenshotSubmission = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ImportSubmission = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ImportSubmissions = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SaveFile = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand DummyCommandToProcessCanExecuteForTextTools = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand DummyCommandToProcessCanExecuteForPrivacyTools = new DefaultableCompositeCommand();

        public static DefaultableCompositeCommand PublishBrush = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ModifySelection = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand TogglePens = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SetPedagogyLevel = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand GetMainScrollViewer = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ShowConversationSearchBox = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand HideConversationSearchBox = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand AddWindowEffect = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand RemoveWindowEffect = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand NotImplementedYet = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand NoOp = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand MirrorPresentationSpace = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ProxyMirrorPresentationSpace = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand InitiateDig = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SendDig = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand DugPublicSpace = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SendLiveWindow = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SendDirtyLiveWindow = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveLiveWindow = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveDirtyLiveWindow = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand DeleteSelectedItems = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand BanhammerSelectedItems = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand VisualizeContent = new DefaultableCompositeCommand();

        public static DefaultableCompositeCommand SendAttendance = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveAttendance = new DefaultableCompositeCommand();

        //public static DefaultableCompositeCommand Relogin = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ToggleWorm = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SendWormMove = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveWormMove = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ConvertPresentationSpaceToQuiz = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SendQuiz = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SendQuizAnswer = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveQuiz = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveQuizAnswer = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand DisplayQuizResults = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand QuizResultsAvailableForSnapshot = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand QuizResultsSnapshotAvailable = new DefaultableCompositeCommand();
        //public static DefaultableCompositeCommand PlaceQuizSnapshot = new DefaultableCompositeCommand();

        public static DefaultableCompositeCommand ShowDiagnostics = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SetInkCanvasMode = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SetPrivacyOfItems = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand GotoThread = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SetDrawingAttributes = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SetPenAttributes = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReplacePenAttributes = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand RequestReplacePenAttributes = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand RequestResetPenAttributes = new DefaultableCompositeCommand();


        public static DefaultableCompositeCommand SendStroke = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveStroke = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveStrokes = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SendDirtyStroke = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveDirtyStrokes = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SetPrivacy = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SetContentVisibility = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand UpdateContentVisibility = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ForcePageRefresh = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand OriginalView = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand CreateQuizStructure = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand Flush = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ZoomIn = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ZoomOut = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ExtendCanvasBothWays = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ToggleBrowser = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ToggleBrowserControls = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand MoreImageOptions = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand PickImages = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ImageDropped = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ImagesDropped = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand AddImage = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand FileUpload = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SendImage = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveImage = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SendMoveDelta = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveMoveDelta = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SendDirtyImage = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveDirtyImage = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SendDirtyAutoShape = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveAutoShape = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveDirtyAutoShape = new DefaultableCompositeCommand();

        /*These are fired after the content buffers have compensated for negative cartesian space and checked ownership*/
        public static DefaultableCompositeCommand StrokePlaced = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ImagePlaced = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand TextPlaced = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ToggleLens = new DefaultableCompositeCommand();

        public static DefaultableCompositeCommand SendFileResource = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveFileResource = new DefaultableCompositeCommand();

        /*
        public static DefaultableCompositeCommand FontSizeChanged = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand FontChanged = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SetTextColor = new DefaultableCompositeCommand();        
        */
        public static DefaultableCompositeCommand ChangeTextMode = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand TextboxFocused = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand TextboxSelected = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SendDirtyText = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveDirtyText = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SetTextCanvasMode = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand IncreaseFontSize = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand DecreaseFontSize = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SendTextBox = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveTextBox = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand RestoreTextDefaults = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand NewTextCursorPosition = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand InitiateGrabZoom = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand EndGrabZoom = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand MoveCanvasByDelta = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand FitToView = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand FitToPageWidth = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand UpdateTextStyling = new DefaultableCompositeCommand();

        public static DefaultableCompositeCommand SetFontSize = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SetFont = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SetTextColor = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SetTextBold = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SetTextItalic = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SetTextUnderline = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SetTextStrikethrough = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand FontSizeNotify = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand FontNotify = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand TextColorNotify = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand TextBoldNotify = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand TextItalicNotify = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand TextUnderlineNotify = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand TextStrikethroughNotify = new DefaultableCompositeCommand();

        public static DefaultableCompositeCommand ToggleBold = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ToggleItalic = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ToggleUnderline = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ToggleStrikethrough = new DefaultableCompositeCommand();

        public static DefaultableCompositeCommand MoreTextOptions = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand RegisterPowerpointSourceDirectoryPreference = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand MeTLType = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand LogOut = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand BackendSelected = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand LoginFailed = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SetIdentity = new DefaultableCompositeCommand(Credentials.Empty);
        public static DefaultableCompositeCommand NoNetworkConnectionAvailable = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand EstablishPrivileges = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand CloseApplication = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SetLayer = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand UpdateForeignConversationDetails = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand RememberMe = new DefaultableCompositeCommand(false);
        public static DefaultableCompositeCommand ClipboardManager = new DefaultableCompositeCommand();

        public static DefaultableCompositeCommand Undo = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand Redo = new DefaultableCompositeCommand();
        public static RoutedCommand ProxyJoinConversation = new RoutedCommand();
        public static DefaultableCompositeCommand ChangeTab = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SetRibbonAppearance = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SaveUIState = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand RestoreUIState = new DefaultableCompositeCommand();
        /*Moving is a metaphor which implies that I am only in one location.  Watching can happen to many places.*/
        public static DefaultableCompositeCommand WatchRoom = new DefaultableCompositeCommand();

        public static DefaultableCompositeCommand SyncedMoveRequested = new DefaultableCompositeCommand(0);
        public static DefaultableCompositeCommand SendSyncMove = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand MoveToCollaborationPage = new DefaultableCompositeCommand(0);
        public static DefaultableCompositeCommand SneakInto = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SneakIntoAndDo = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SneakOutOf = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand PreParserAvailable = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SignedRegions = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ConversationPreParserAvailable = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand MoveToPrevious = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand MoveToNext = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SetConversationPermissions = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ToggleNavigationLock = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand JoinConversation = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand LeaveConversation = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand LeaveLocation = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SendDirtyConversationDetails = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand UpdateConversationDetails = new DefaultableCompositeCommand(ConversationDetails.Empty);
        public static DefaultableCompositeCommand ReceiveDirtyConversationDetails = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SetSync = new DefaultableCompositeCommand(false);
        public static DefaultableCompositeCommand ToggleSync = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand AddSlide = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand UpdateNewSlideOrder = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand CreateBlankConversation = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ShowEditSlidesDialog = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand CreateConversation = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand DuplicateSlide = new DefaultableCompositeCommand(new KeyValuePair<ConversationDetails, Slide>(ConversationDetails.Empty, Slide.Empty));
        public static DefaultableCompositeCommand DuplicateConversation = new DefaultableCompositeCommand(ConversationDetails.Empty);
        public static DefaultableCompositeCommand CreateGrouping = new DefaultableCompositeCommand();
        //public static DefaultableCompositeCommand PreEditConversation = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand EditConversation = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand BlockInput = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand UnblockInput = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand BlockSearch = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand UnblockSearch = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand CanEdit = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand PrintConversation = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand HideProgressBlocker = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SendChatMessage = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveChatMessage = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand BanhammerActive = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ManageBannedContent = new DefaultableCompositeCommand();

        public static DefaultableCompositeCommand ImportPowerpoint = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand UploadPowerpoint = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand PowerpointFinished = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand JoinCreatedConversation = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveMove = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveJoin = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceivePing = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceiveFlush = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand SendMeTLType = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ToggleScratchPadVisibility = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ToggleFriendsVisibility = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand ReceivePublicChat = new DefaultableCompositeCommand();

        public static DefaultableCompositeCommand DummyCommandToProcessCanExecute = new DefaultableCompositeCommand();

        public static RoutedCommand HighlightFriend = new RoutedCommand();
        public static RoutedCommand PostHighlightFriend = new RoutedCommand();
        public static RoutedCommand HighlightUser = new RoutedCommand();
        public static RoutedCommand PostHighlightUser = new RoutedCommand();


        public static DefaultableCompositeCommand LaunchDiagnosticWindow = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand DiagnosticMessage = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand DiagnosticGaugeUpdated = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand DiagnosticMessagesUpdated = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand DiagnosticGaugesUpdated = new DefaultableCompositeCommand();

        public static DefaultableCompositeCommand ShowProjector = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand HideProjector = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand EnableProjector = new DefaultableCompositeCommand();

        public static DefaultableCompositeCommand AddFlyoutCard = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand CloseFlyoutCard = new DefaultableCompositeCommand();
        public static DefaultableCompositeCommand CreateDummyCard = new DefaultableCompositeCommand(); // for testing only


        Commands()
        {
            NotImplementedYet.RegisterCommand(new DelegateCommand<object>((_param) => { }, (_param) => false));
        }
        public static int HandlerCount
        {
            get
            {
                return all.Aggregate(0, (acc, item) => acc += item.RegisteredCommands.Count());
            }
        }
        private static List<ICommand> staticHandlers = new List<ICommand>();

        public static void AllStaticCommandsAreRegistered()
        {
            foreach (var command in all)
            {
                foreach (var handler in command.RegisteredCommands)
                    staticHandlers.Add(handler);
            }
        }
        private static IEnumerable<DefaultableCompositeCommand> all
        {
            get
            {
                return typeof(Commands).GetFields()
                    .Where(p => p.FieldType == typeof(DefaultableCompositeCommand))
                    .Select(f => (DefaultableCompositeCommand)f.GetValue(null));
            }
        }


        public static IEnumerable<ICommand> allHandlers()
        {
            var handlers = new List<ICommand>();
            foreach (var command in all)
                foreach (var handler in command.RegisteredCommands)
                    handlers.Add(handler);
            return handlers.ToList();
        }
        public static void UnregisterAllCommands()
        {
            foreach (var command in all)
                foreach (var handler in command.RegisteredCommands)
                    if (!staticHandlers.Contains(handler))
                        command.UnregisterCommand(handler);
        }
        public static string which(ICommand command)
        {
            foreach (var field in typeof(Commands).GetFields())
                if (field.GetValue(null) == command)
                    return field.Name;
            return "Not a member of commands";
        }
        public static DefaultableCompositeCommand called(string name)
        {
            return (DefaultableCompositeCommand)typeof(Commands).GetField(name).GetValue(null);
        }
        public static void RequerySuggested()
        {
            RequerySuggested(all.ToArray());
        }
        public static void RequerySuggested(params DefaultableCompositeCommand[] commands)
        {
            foreach (var command in commands)
                Requery(command);
        }
        private static void Requery(DefaultableCompositeCommand command)
        {
            if (command.RegisteredCommands.Count() > 0)
            {
                //wrapping this in a try-catch for those commands who have non-nullable types on their canExecutes
                try
                {
                    var delegateCommand = command.RegisteredCommands[0];
                    delegateCommand.GetType().InvokeMember("RaiseCanExecuteChanged", BindingFlags.InvokeMethod, null, delegateCommand, new object[] { });
                }
                catch (Exception e)
                {
                    Console.WriteLine("exception while requerying: " + e.Message);
                }
            }
        }
    }
    public static class CommandExtensions
    {
        public static void ExecuteAsync(this DefaultableCompositeCommand command, object arg)
        {
            if (command.CanExecute(arg))
            {
                command.Execute(arg);
            }
        }
    }
}