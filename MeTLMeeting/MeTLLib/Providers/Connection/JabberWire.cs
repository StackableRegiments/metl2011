﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Authentication;
using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.protocol.x.muc;
using agsXMPP.Xml.Dom;
using Microsoft.Practices.Composite.Presentation.Commands;
using agsXMPP.protocol.iq.disco;
using MeTLLib.DataTypes;
using MeTLLib.Providers.Structure;
using System.Diagnostics;
using Ninject;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Net.NetworkInformation;

namespace MeTLLib.Providers.Connection
{
    public class JabberWireFactory
    {
        public Credentials credentials { private get; set; }
        [Inject]
        public ConfigurationProvider configurationProvider { private get; set; }
        [Inject]
        public IConversationDetailsProvider conversationDetailsProvider { private get; set; }
        [Inject]
        public HttpHistoryProvider historyProvider { private get; set; }
        [Inject]
        public CachedHistoryProvider cachedHistoryProvider { private get; set; }
        [Inject]
        public MeTLServerAddress metlServerAddress { private get; set; }
        [Inject]
        public ResourceCache cache { private get; set; }
        [Inject]
        public IReceiveEvents receiveEvents { private get; set; }
        [Inject]
        public IWebClientFactory clientFactory { private get; set; }
        [Inject]
        public HttpResourceProvider resourceProvider { private get; set; }
        private JabberWire instance;
        private object instanceLock = new object();
        public JabberWire wire()
        {
            lock (instanceLock)
            {
                if (credentials == null) throw new InvalidOperationException("The JabberWireFactory does not yet have credentials to create a wire");
                if (instance == null)
                {
                    instance = new JabberWire(
                        credentials,
                        conversationDetailsProvider,
                        historyProvider,
                        cachedHistoryProvider,
                        metlServerAddress,
                        cache,
                        receiveEvents,
                        clientFactory,
                        resourceProvider);
                    instance.openConnection();
                }
                return instance;
            };
        }
        public PreParser preParser(int room)
        {
            if (credentials == null) throw new InvalidOperationException("The JabberWireFactory does not yet have credentials to create a preParser");
            return new PreParser(
                credentials,
                room,
                conversationDetailsProvider,
                historyProvider,
                cachedHistoryProvider,
                metlServerAddress,
                cache, receiveEvents, clientFactory, resourceProvider);
        }
        public T preParser<T>(int room) where T : PreParser
        {
            return (T)Activator.CreateInstance(typeof(T),
                credentials,
                room,
                conversationDetailsProvider,
                historyProvider,
                cachedHistoryProvider,
                metlServerAddress,
                cache,
                receiveEvents,
                clientFactory,
                resourceProvider);
        }
        public virtual PreParser create<T>(int room) where T : PreParser
        {
            return preParser<T>(room);
        }
    }
    public partial class JabberWire
    {
        protected const string WORM = "/WORM_MOVES";
        protected const string SUBMISSION = "/SUBMISSION";
        protected const string UPDATE_CONVERSATION_DETAILS = "/UPDATE_CONVERSATION_DETAILS";
        protected const string SYNC_MOVE = "/SYNC_MOVE";
        protected const string GO_TO_SLIDE = "/GO_TO_SLIDE";
        protected const string WAKEUP = "/WAKEUP";
        protected const string SLEEP = "/SLEEP";
        protected const string PING = "/PING";
        protected const string PONG = "/PONG";
        protected const uint HEARTBEAT_PERIOD = 20000;
        public Credentials credentials;
        public Location location = Location.Empty; 
        protected IReceiveEvents receiveEvents;
        protected IWebClientFactory webClientFactory;
        protected HttpResourceProvider resourceProvider;
        protected static string privacy = "PUBLIC";
        private XmppClientConnection conn;
        private Timer heartbeat;
        protected Jid jid;

        private void registerCommands()
        {
            Commands.SendWakeUp.RegisterCommand(new DelegateCommand<string>(WakeUp, CanWakeUp));
            Commands.SendSleep.RegisterCommand(new DelegateCommand<string>(GoToSleep));
            Commands.SendMoveBoardToSlide.RegisterCommand(new DelegateCommand<BoardMove>(SendMoveBoardToSlide));
            Commands.SendPing.RegisterCommand(new DelegateCommand<string>(SendPing));
        }
        public ResourceCache cache;
        public JabberWire(Credentials credentials, IConversationDetailsProvider conversationDetailsProvider, HttpHistoryProvider historyProvider, CachedHistoryProvider cachedHistoryProvider, MeTLServerAddress metlServerAddress, ResourceCache cache, IReceiveEvents events, IWebClientFactory webClientFactory, HttpResourceProvider resourceProvider)
        {
            this.credentials = credentials;
            this.conversationDetailsProvider = conversationDetailsProvider;
            this.historyProvider = historyProvider;
            this.cachedHistoryProvider = cachedHistoryProvider;
            this.metlServerAddress = metlServerAddress;
            this.cache = cache;
            this.receiveEvents = events;
            this.webClientFactory = webClientFactory;
            this.resourceProvider = resourceProvider;
            this.jid = createJid(credentials.name);
            new MeTLLib.DataTypes.MeTLStanzasConstructor();
            if (this.GetType() != typeof(PreParser))
            {
                receiveEvents.StatusChanged += listenToStatusChangedForReset;
                establishHeartBeat();
            }
        }
        private void establishHeartBeat()
        {
            heartbeat = new Timer((_unused) => { checkConnection(); }, null, HEARTBEAT_PERIOD, HEARTBEAT_PERIOD);
        } 

        private void checkConnection()
        {
            if (!this.IsConnected()) Reset("resetting on heartbeat");
        }
        private void listenToStatusChangedForReset(object sender, StatusChangedEventArgs e)
        {
            if (!e.isConnected && !this.IsConnected()) Reset("Jabberwire::listenToStatusChangedForResest");
        }
        private void makeAvailableNewSocket()
        {
            if (this.conn != null) { 
                conn.OnAuthError -= OnAuthError;
                conn.OnLogin -= OnLogin;
                conn.OnMessage -= OnMessage;
                conn.OnSocketError -= HandlerError;
                conn.OnError -= HandlerError;
                conn.OnRegisterError -= ElementError;
                conn.OnStreamError -= ElementError;
                conn.OnClose -= OnClose;
    #if DEBUG
                conn.OnReadXml -= ReadXml;
                conn.OnWriteXml -= WriteXml;
    #endif
            }
            this.conn = new XmppClientConnection(jid.Server);
            conn.UseSSL = false;
            conn.AutoAgents = false;
            conn.OnAuthError += OnAuthError;
            conn.OnLogin += OnLogin;
            conn.OnMessage += OnMessage;
            conn.OnSocketError += HandlerError;
            conn.OnError += HandlerError;
            conn.OnRegisterError += ElementError;
            conn.OnStreamError += ElementError;
            conn.OnClose += OnClose;
#if DEBUG
            conn.OnReadXml += ReadXml;
            conn.OnWriteXml += WriteXml;
#endif
        }
        private void SendNewBubble(TargettedBubbleContext selection)
        {
            stanza(new MeTLStanzas.Bubble(selection));
        }
        private void SendPing(string who)
        {
            directCommand(who, string.Format("{0} {1}", PING, credentials.name));
        }
        private Jid createJid(string username)
        {
            return new Jid(username + "@" + metlServerAddress.host);
        }
        public void SendLiveWindow(LiveWindowSetup window)
        {
            stanza(new MeTLStanzas.LiveWindow(window));
        }
        public void SendDirtyLiveWindow(TargettedDirtyElement dirty)
        {
            stanza(new MeTLStanzas.DirtyLiveWindow(dirty));
        }
        public void SendMoveBoardToSlide(BoardMove boardMove)
        {
            directCommand(boardMove.boardUsername, string.Format("{0} {1}", GO_TO_SLIDE, boardMove.roomJid));
        }
        private void OnAuthError(object _sender, Element error)
        {
            if (error.TagName == "failure")
            {
                usernameToBeRegistered = conn.Username;
                Reset("Auth failed");
            }
            else
            {
                throw new AuthenticationException(error.ToString());
            }
        }
        private static string usernameToBeRegistered;
        public void openConnection()
        {
            var resource = DateTimeFactory.Now().Ticks.ToString();
            jid.Resource = resource;
            makeAvailableNewSocket();
            if (!String.IsNullOrEmpty(usernameToBeRegistered) && usernameToBeRegistered == jid.User)
            {
                conn.RegisterAccount = true;
                usernameToBeRegistered = null;
            }
            conn.Open(jid.User, "examplePassword", resource, 1);
        }
        private void OnLogin(object o)
        {
            receiveEvents.statusChanged(true, this.credentials);
            joinRooms();
            catchUpDisconnectedWork();
        }
        private void OnMessage(object sender, Message message)
        {
            if (message.To.Resource == jid.Resource)
                ReceivedMessage(message);
        }
        private void HandlerError(object sender, Exception ex)
        {
            Trace.TraceError(string.Format("MeTLLib::Providers::Connection:JabberWire:Handler error: {0}", ex.Message));
            Reset("JabberWire::HandlerError");
        }
        private void ElementError(object sender, Element element)
        {
            Trace.TraceError(string.Format("MeTLLib::Providers::Connection:JabberWire:Element error: {0}", element.ToString()));
            Reset("JabberWire::ElementError");
        }
        protected virtual void ReadXml(object sender, string xml)
        {
            if (!xml.Contains("/WORM_MOVES"))
                Trace.TraceInformation("IN:" + xml);
        }
        protected virtual void WriteXml(object sender, string xml)
        {
            if (!xml.Contains("/WORM_MOVES"))
                Trace.TraceInformation("OUT:" + xml);
        }
        private void OnClose(object sender)
        {
            receiveEvents.statusChanged(false, credentials);
            Reset("OnClose");
        }
        private static Queue<Action> actionsAfterRelogin = new Queue<Action>();
        public void AddActionToReloginQueue(Action action)
        {
            receiveEvents.statusChanged(false, credentials);
            actionsAfterRelogin.Enqueue(action);
        }
        private void catchUpDisconnectedWork() 
        {
            while (IsConnected() && actionsAfterRelogin.Count > 0)
            {
                var item = (Action)actionsAfterRelogin.Peek();//Do not alter the queue, we might be back here any second
                try
                {
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.adopt(item);
                    actionsAfterRelogin.Dequeue();//We only lift it off the top after successful execution.
                }
                catch (Exception e)
                {
                    Trace.TraceError("CRASH: MeTLLib::Providers:JabberWire:AttemptReloginAfter Failed to execute item on relogin-queue.  Exception: " + e.Message);
                    break;
                }
            }
        }
        
        public void Logout()
        {
            conn.OnClose -= OnClose;//Don't automatically reconnect this time
            conn.Close();
        }
        public void Login()
        {
        }
        protected IConversationDetailsProvider conversationDetailsProvider;
        protected HttpHistoryProvider historyProvider;
        protected CachedHistoryProvider cachedHistoryProvider;
        protected MeTLServerAddress metlServerAddress;
        private object resetLock = new object();
        public void Reset(string caller)
        {
            lock (resetLock)
            {
                if (conn != null)
                    switch (conn.XmppConnectionState)
                    {
                        case XmppConnectionState.Disconnected:
                            Trace.TraceWarning(string.Format("CRASH: JabberWire::Reset: Resetting.  {0}", caller));
                            openConnection();
                            break;
                        //case XmppConnectionState.Authenticating:
                        //    openConnection();
                        //    break;
                    }
                else
                {
                    Trace.TraceWarning(string.Format("CRASH: JabberWire::Reset: Conn is null - openingConnection.  {0}", caller));
                    openConnection();
                }
            }
        }
        public void leaveRooms()
        {
            var rooms = new[]{
                metlServerAddress.global,
                new Jid(credentials.name, metlServerAddress.muc, jid.Resource)
            }.ToList();
            if (location != null)
                rooms.AddRange(
                    new[]{
                        new Jid(location.activeConversation,metlServerAddress.muc,jid.Resource),
                        new Jid(location.currentSlide.ToString(), metlServerAddress.muc,jid.Resource),
                        new Jid(string.Format("{0}{1}", location.currentSlide, credentials.name), metlServerAddress.muc,jid.Resource)
                    });
            foreach (var room in rooms)
            {
                var alias = credentials.name + conn.Resource;
                new MucManager(conn).LeaveRoom(room, alias);
            }
        }
        private bool isLocationValid()
        {
            return (location != null && !String.IsNullOrEmpty(location.activeConversation) && location.availableSlides.Count > 0 && location.currentSlide > 0);
        }
        private void joinRooms()
        {
            leaveRooms();
            joinRoom(metlServerAddress.global);
            if (isLocationValid())
            {
                var rooms = new[]
                {
                    new Jid(credentials.name, metlServerAddress.muc, jid.Resource),
                    new Jid(location.activeConversation,metlServerAddress.muc,jid.Resource),
                    new Jid(location.currentSlide.ToString(), metlServerAddress.muc,jid.Resource),
                    new Jid(string.Format("{0}{1}", location.currentSlide, credentials.name), metlServerAddress.muc,jid.Resource)
                };
                foreach (var room in rooms.Where(r => r.User != null && r.User != "0"))
                {
                        joinRoom(room);
                }
            }
        }
        private void joinRoom(Jid room)
        {
            try
            {
                var alias = credentials.name + conn.Resource;
                new MucManager(conn).JoinRoom(room, alias, true);
                Trace.TraceInformation(string.Format("MeTLLib::Providers::Connection::JabberWire:JoinRoom {0}", room));
            }
            catch (Exception e)
            {
                Trace.TraceError(string.Format("Couldn't join room {0}: {1}", room, e.Message));
            }
        }
        private void send(string target, string message)
        {
            send(new Message(new Jid(target + "@" + metlServerAddress.muc), jid, MessageType.groupchat, message));
        }
        protected virtual void send(Message message)
        {
            conn.Send(message);
        }
        public void stanza(string target, Element stanza)
        {
            var message = new Message();
            string modifiedTarget =
                stanza.GetTag(MeTLStanzas.privacyTag) == "private" ?
                string.Format("{0}{1}", target, stanza.GetTag("author")) : target;
            message.To = new Jid(string.Format("{0}@{1}", modifiedTarget, metlServerAddress.muc));
            message.From = jid;
            message.Type = MessageType.groupchat;
            message.AddChild(stanza);
            send(message);
        }
        public void stanza(Element stanza)
        {
            this.stanza(location.currentSlide.ToString(), stanza);
        }
        private void command(string where, string message)
        {
            send(where, message);
        }
        private void command(string message)
        {
            send("global", message);
        }
        private void directCommand(string target, string message)
        {
            send(new Message(new Jid(target + "@" + metlServerAddress.host), jid, MessageType.chat, message));
        }
        private void onStart()
        {
        }
        private void onProgress(int upTo, int outOf)
        {
            //Commands.RetrievedHistoryPortion.Execute(new[] { upTo, outOf });
        }
        public void SendDirtyText(TargettedDirtyElement element)
        {
            stanza(new MeTLStanzas.DirtyText(element));
        }
        public void SendDirtyVideo(TargettedDirtyElement element)
        {
            stanza(new MeTLStanzas.DirtyVideo(element));
        }
        public void SendDirtyImage(TargettedDirtyElement element)
        {
            stanza(new MeTLStanzas.DirtyImage(element));
        }
        public void SendDirtyAutoShape(TargettedDirtyElement element)
        {
            stanza(new MeTLStanzas.DirtyImage(element));
        }
        public bool IsConnected()
        {
            try
            {
                Func<Boolean> checkPing = delegate
                {
                    var healthy = false;
                    var uri = metlServerAddress.uri;
                    var ping = new System.Net.NetworkInformation.Ping();
                    var reply = ping.Send(uri.Host, 2000);
                    if (reply != null && reply.Status == IPStatus.Success)
                    {
                        healthy = true;
                    }
                    return healthy;
                };
                return conn.Authenticated && checkPing();
            }
            catch (Exception) {
                return false;
            }
        }
        public void GetHistory(int where)
        {
            historyProvider.Retrieve<PreParser>(
                onStart,
                onProgress,
                finishedParser => receiveEvents.receivePreParser(finishedParser),
                where.ToString());
            historyProvider.RetrievePrivateContent<PreParser>(
                onStart,
                onProgress,
                finishedParser => receiveEvents.receivePreParser(finishedParser),
                credentials.name,
                where.ToString());
        }
        public void MoveTo(int where)
        {
            try
            {
                new MucManager(conn).LeaveRoom(
                    new Jid(string.Format("{0}{1}", location.currentSlide, credentials.name), metlServerAddress.muc, jid.Resource), credentials.name);
                var currentDetails = conversationDetailsProvider.DetailsOf(location.activeConversation);
                location.availableSlides = currentDetails.Slides.Select(s => s.id).ToList();
                location.currentSlide = where;
                Globals.conversationDetails = currentDetails;
                Globals.slide = where;
                joinRooms();
                historyProvider.Retrieve<PreParser>(
                    onStart,
                    onProgress,
                    finishedParser => receiveEvents.receivePreParser(finishedParser),
                    location.currentSlide.ToString());
                historyProvider.RetrievePrivateContent<PreParser>(
                    onStart,
                    onProgress,
                    finishedParser => receiveEvents.receivePreParser(finishedParser),
                    credentials.name,
                    location.currentSlide.ToString());
            }
            catch (Exception e) {
                Trace.TraceInformation("CRASH: MeTLLib::JabberWire:MoveTo {0}", e.Message);
            }
        }
        public void SendStroke(TargettedStroke stroke)
        {
            stanza(stroke.slide.ToString(), new MeTLStanzas.Ink(stroke));
        }
        public void SendScreenshotSubmission(TargettedSubmission submission)
        {
            stanza(Globals.location.activeConversation, new MeTLStanzas.ScreenshotSubmission(submission));
        }
        public void SendSyncMoveTo(int where)
        {
            command(location.activeConversation, SYNC_MOVE + " " + where);
        }
        public void SendImage(TargettedImage image)
        {
            stanza(image.slide.ToString(), new MeTLStanzas.Image(image));
        }
        public void sendFileResource(TargettedFile file)
        {
            stanza(Globals.location.activeConversation, new MeTLStanzas.FileResource(file));
        }
        public void SendVideo(TargettedVideo video)
        {
            stanza(video.slide.ToString(), new MeTLStanzas.Video(video));
        }
        public void SendStanza(string where, Element what) {
            stanza(where, what);
        }
        public void SendAutoShape(TargettedAutoShape autoshape)
        {
            stanza(autoshape.slide.ToString(), new MeTLStanzas.AutoShape(autoshape));
        }
        private void SendChat(TargettedTextBox message)
        {
            stanza(location.activeConversation, new MeTLStanzas.TextBox(message));
        }
        public void SendTextbox(TargettedTextBox box)
        {
            stanza(box.slide.ToString(), new MeTLStanzas.TextBox(box));
        }
        public void SendQuiz(QuizQuestion parameters)
        {
            var quiz = new MeTLStanzas.Quiz(parameters);
            stanza(Globals.location.activeConversation, quiz);
        }
        public void SendQuizAnswer(QuizAnswer parameters)
        {
            var quiz = new MeTLStanzas.QuizResponse(parameters);
            stanza(Globals.location.activeConversation, quiz);
        }
        public void SendDirtyConversationDetails(string jid)
        {
            command(UPDATE_CONVERSATION_DETAILS + " " + (jid));
        }
        public bool CanWakeUp(string _param)
        {
            return true;
        }
        public void WakeUp(string room)
        {
            Commands.WakeUpBoards.Execute(room);
            /*This next bit needs to be moved back into the application
            foreach (var board in BoardManager.boards[room])
            {
                directCommand(board.name, WAKEUP);
            }*/
        }
        public void CommandBoardToMoveTo(string board, string slide)
        {
            directCommand(board, string.Format("{0} {1}", GO_TO_SLIDE, slide));
        }
        public void GoToSleep(string room)
        {
            Commands.SleepBoards.Execute(room);
            /*This bit needs to be moved back into the application
            foreach (var board in BoardManager.boards[room])
                directCommand(room, SLEEP);*/
        }
        public void sendDirtyStroke(TargettedDirtyElement element)
        {
        //    stanza(element.slide.ToString(), new MeTLStanzas.DirtyInk(element));
            stanza(new MeTLStanzas.DirtyInk(element));
        }
        public virtual void ReceiveCommand(string message)
        {
            try
            {
                if (message.Length == 0) return;
                var parts = message.Split(new[] { " " }, 2, StringSplitOptions.RemoveEmptyEntries);
                switch (parts[0].ToUpper())
                {
                    case SYNC_MOVE:
                        handleSyncMoveReceived(parts);
                        break;
                    case UPDATE_CONVERSATION_DETAILS:
                        handleConversationDetailsUpdated(parts);
                        break;
                    case GO_TO_SLIDE:
                        handleGoToSlide(parts);
                        break;
                    case WAKEUP:
                        handleWakeUp(parts);
                        break;
                    case SLEEP:
                        handleSleep(parts);
                        break;
                    case PING:
                        handlePing(parts);
                        break;
                    case PONG:
                        handlePong(parts);
                        break;
                    default:
                        handleUnknownMessage(message);
                        break;
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(string.Format("Uncaught exception in ReceivedMessage: {0}", e.Message));
            }
        }
        public virtual void handlePing(string[] parts)
        {
            directCommand(parts[1], string.Format("{0} {1}", PONG, credentials.name));
        }
        public virtual void handlePong(string[] parts)
        {
            Commands.ReceivePong.Execute(parts[1]);
        }
        public virtual void handleGoToConversation(string[] parts)
        {
            JoinConversation(parts[1]);
        }
        public virtual void handleGoToSlide(string[] parts)
        {
            var id = Int32.Parse(parts[1]);
            var desiredConversation = Slide.ConversationFor(id).ToString();
            MoveTo(id);
        }
        public virtual void handleWakeUp(string[] parts)
        {
            Commands.ReceiveWakeUp.Execute(null);
        }
        public virtual void handleSleep(string[] parts)
        {
            Commands.ReceiveSleep.Execute(null);
        }
        public virtual void ReceivedMessage(object obj)
        {
            var message = (Element)obj;
            if (message.GetAttribute("type") == "error")
            {
                Trace.TraceError("Wire received error message: {0}", message);
                return;
            }
            if (message.SelectSingleElement("body") != null)
            {
                ReceiveCommand(message.SelectSingleElement("body").InnerXml);
                return;
            }
            try
            {
                cachedHistoryProvider.HandleMessage(message.GetAttribute("from").Split('@')[0], message);
            }
            catch (Exception e)
            {
                Trace.TraceError("Exception in JabberWire.ReceivedMessage: {0}", e.Message);
            }
            ActOnUntypedMessage(message);
        }
        public void ActOnUntypedMessage(Element message)
        {
            foreach (var ink in message.SelectElements<MeTLStanzas.Ink>(true))
                actOnStrokeReceived(ink.Stroke);
            foreach (var submission in message.SelectElements<MeTLStanzas.ScreenshotSubmission>(true))
                actOnScreenshotSubmission(submission.injectDependencies(metlServerAddress).parameters);
            foreach (var box in message.SelectElements<MeTLStanzas.TextBox>(true))
                actOnTextReceived(box.Box);
            foreach (var image in message.SelectElements<MeTLStanzas.Image>(true))
                actOnImageReceived(image.injectDependencies(metlServerAddress, webClientFactory.client(), resourceProvider).Img);
            foreach (var quiz in message.SelectElements<MeTLStanzas.Quiz>(true))
                actOnQuizReceived(quiz.injectDependencies(metlServerAddress).parameters);
            foreach (var quizAnswer in message.SelectElements<MeTLStanzas.QuizResponse>(true))
                actOnQuizAnswerReceived(quizAnswer.parameters);
            foreach (var dirtyText in message.SelectElements<MeTLStanzas.DirtyText>(true))
                actOnDirtyTextReceived(dirtyText);
            foreach (var dirtyInk in message.SelectElements<MeTLStanzas.DirtyInk>(true))
                actOnDirtyStrokeReceived(dirtyInk);
            foreach (var dirtyImage in message.SelectElements<MeTLStanzas.DirtyImage>(true))
                actOnDirtyImageReceived(dirtyImage);
            foreach (var file in message.SelectElements<MeTLStanzas.FileResource>(true))
                actOnFileResource(file.injectDependencies(metlServerAddress));
        }
        public virtual void actOnFileResource(MeTLStanzas.FileResource resource)
        {
            receiveEvents.receiveFileResource(resource.fileResource);
        }
        public virtual void actOnScreenshotSubmission(TargettedSubmission submission)
        {
            receiveEvents.receiveSubmission(submission);
        }
        public virtual void actOnVideoReceived(TargettedVideo video)
        {
            receiveEvents.receiveVideo(video);
        }
        public virtual void actOnBubbleReceived(TargettedBubbleContext bubble)
        {
            receiveEvents.receiveBubble(bubble);
        }
        public virtual void actOnDirtyAutoshapeReceived(MeTLStanzas.DirtyAutoshape dirtyAutoShape)
        {
            receiveEvents.receiveDirtyAutoShape(dirtyAutoShape.element);
        }
        public virtual void actOnDirtyVideoReceived(MeTLStanzas.DirtyVideo dirtyVideo)
        {
            receiveEvents.receiveDirtyVideo(dirtyVideo.element);
        }
        public virtual void actOnDirtyImageReceived(MeTLStanzas.DirtyImage dirtyImage)
        {
            receiveEvents.receiveDirtyImage(dirtyImage.element);
        }
        public virtual void actOnDirtyStrokeReceived(MeTLStanzas.DirtyInk element)
        {
            receiveEvents.receiveDirtyStroke(element.element);
        }
        public virtual void actOnDirtyTextReceived(MeTLStanzas.DirtyText dirtyText)
        {
            receiveEvents.receiveDirtyTextBox(dirtyText.element);
        }
        public virtual void actOnImageReceived(TargettedImage image)
        {
            receiveEvents.receiveImage(image);
        }
        public virtual void actOnAutoShapeReceived(TargettedAutoShape autoshape)
        {
            receiveEvents.receiveAutoShape(autoshape);
        }
        public virtual void actOnQuizReceived(QuizQuestion quiz)
        {
            receiveEvents.receiveQuiz(quiz);
        }
        public virtual void actOnQuizAnswerReceived(QuizAnswer answer)
        {
            receiveEvents.receiveQuizAnswer(answer);
        }
        public virtual void actOnStrokeReceived(TargettedStroke stroke)
        {
            receiveEvents.receiveStroke(stroke);
        }
        public virtual void actOnTextReceived(TargettedTextBox box)
        {
            if (box.target == "chat")
                receiveEvents.receiveChat(box);
            else
                receiveEvents.receiveTextBox(box);
        }
        public virtual void actOnLiveWindowReceived(LiveWindowSetup window)
        {
            receiveEvents.receiveLiveWindow(window);
        }
        public virtual void actOnDirtyLiveWindowReceived(TargettedDirtyElement element)
        {
            receiveEvents.receiveDirtyLiveWindow(element);
        }
        public void SneakInto(string room)
        {
            var muc = new MucManager(conn);
            joinRoom(new Jid(room + "@" + metlServerAddress.muc));
        }
        public void SneakOutOf(string room)
        {
            var muc = new MucManager(conn);
            muc.LeaveRoom(new Jid(room + "@" + metlServerAddress.muc), credentials.name);
        }
        public void JoinConversation(string room)
        {
            if (location.activeConversation != null)
            {
                var muc = new MucManager(conn);
                muc.LeaveRoom(new Jid(location.activeConversation + "@" + metlServerAddress.muc), credentials.name);
                foreach (var slide in conversationDetailsProvider.DetailsOf(location.activeConversation).Slides.Select(s => s.id))
                    muc.LeaveRoom(new Jid(slide + "@" + metlServerAddress.muc), credentials.name);
            }
            location.activeConversation = room;
            var cd = conversationDetailsProvider.DetailsOf(room);
            location.availableSlides = cd.Slides.Select(s => s.id).ToList();
            joinRooms();
        }
        private void handleSyncMoveReceived(string[] parts)
        {
            var where = Int32.Parse(parts[1]);
            receiveEvents.syncMoveRequested(where);
        }
        private void handleConversationDetailsUpdated(string[] parts)
        {
            var jid = parts[1];
            conversationDetailsProvider.ReceiveDirtyConversationDetails(jid);
            var newDetails = conversationDetailsProvider.DetailsOf(jid);
            receiveEvents.receiveConversationDetails(newDetails);
        }
        protected virtual void handleUnknownMessage(string message)
        {
            Trace.TraceWarning(string.Format("Received unknown message: {0}", message));
        }
    }
}