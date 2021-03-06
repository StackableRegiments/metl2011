﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MeTLLib.DataTypes;
using MeTLLib.Providers.Connection;
using MeTLLib;

namespace MeTLLib
{
    #region EventHandlers
    public class MeTLLibEventHandlers
    {
        public delegate void SubmissionAvailableEventHandler(object sender, SubmissionAvailableEventArgs e);
        public delegate void FileAvailableEventHandler(object sender, FileAvailableEventArgs e);
        public delegate void StatusChangedEventHandler(object sender, StatusChangedEventArgs e);
        public delegate void PreParserAvailableEventHandler(object sender, PreParserAvailableEventArgs e);
        public delegate void StrokeAvailableEventHandler(object sender, StrokeAvailableEventArgs e);
        public delegate void ImageAvailableEventHandler(object sender, ImageAvailableEventArgs e);
        public delegate void TextBoxAvailableEventHandler(object sender, TextBoxAvailableEventArgs e);
        public delegate void DirtyElementAvailableEventHandler(object sender, DirtyElementAvailableEventArgs e);
        public delegate void VideoAvailableEventHandler(object sender, VideoAvailableEventArgs e);
        public delegate void AutoshapeAvailableEventHandler(object sender, AutoshapeAvailableEventArgs e);
        public delegate void LiveWindowAvailableEventHandler(object sender, LiveWindowAvailableEventArgs e);
        public delegate void BubbleAvailableEventHandler(object sender, BubbleAvailableEventArgs e);
        public delegate void DiscoAvailableEventHandler(object sender, DiscoAvailableEventArgs e);
        public delegate void QuizAnswerAvailableEventHandler(object sender, QuizAnswerAvailableEventArgs e);
        public delegate void QuizQuestionAvailableEventHandler(object sender, QuizQuestionAvailableEventArgs e);
        public delegate void ChatAvailableEventHandler(object sender, ChatAvailableEventArgs e);
        public delegate void ConversationDetailsAvailableEventHandler(object sender, ConversationDetailsAvailableEventArgs e);
        public delegate void CommandAvailableEventHandler(object sender, CommandAvailableEventArgs e);
    }
    #endregion
    #region EventArgs
    public class SubmissionAvailableEventArgs : EventArgs { public TargettedSubmission submission;}
    public class FileAvailableEventArgs : EventArgs { public TargettedFile file;}
    public class StatusChangedEventArgs : EventArgs { public bool isConnected; public Credentials credentials;}
    public class PreParserAvailableEventArgs : EventArgs { public PreParser parser; }
    public class StrokeAvailableEventArgs : EventArgs { public TargettedStroke stroke;}
    public class ImageAvailableEventArgs : EventArgs { public TargettedImage image;}
    public class VideoAvailableEventArgs : EventArgs { public TargettedVideo video;}
    public class TextBoxAvailableEventArgs : EventArgs { public TargettedTextBox textBox;}
    public class DirtyElementAvailableEventArgs : EventArgs { public TargettedDirtyElement dirtyElement;}
    public class AutoshapeAvailableEventArgs : EventArgs { public TargettedAutoShape autoshape;}
    public class LiveWindowAvailableEventArgs : EventArgs { public LiveWindowSetup livewindow;}
    public class BubbleAvailableEventArgs : EventArgs { public TargettedBubbleContext bubble;}
    public class DiscoAvailableEventArgs : EventArgs { public string disco;}
    public class QuizAnswerAvailableEventArgs : EventArgs { public QuizAnswer quizAnswer;}
    public class QuizQuestionAvailableEventArgs : EventArgs { public QuizQuestion quizQuestion;}
    public class ChatAvailableEventArgs : EventArgs { public TargettedTextBox chat;}
    public class ConversationDetailsAvailableEventArgs : EventArgs { public ConversationDetails conversationDetails;}
    public class CommandAvailableEventArgs : EventArgs { public string command;}
    #endregion
    public interface IReceiveEvents
    {
        void receiveSubmission(TargettedSubmission ts);
        void receiveQuiz(QuizQuestion qq);
        void receiveQuizAnswer(QuizAnswer qa);
        void receiveFileResource(TargettedFile tf);
        void receiveStroke(TargettedStroke ts);
        void receiveStrokes(TargettedStroke[] tsa);
        void receiveImages(TargettedImage[] tia);
        void receiveImage(TargettedImage ti);
        void receiveTextBox(TargettedTextBox ttb);
        void receiveVideo(TargettedVideo tv);
        void receiveDirtyStroke(TargettedDirtyElement tde);
        void receiveDirtyTextBox(TargettedDirtyElement tde);
        void receiveDirtyVideo(TargettedDirtyElement tde);
        void receiveDirtyImage(TargettedDirtyElement tde);
        void receiveChat(TargettedTextBox ttb);
        void receivePreParser(PreParser pp);
        void receiveLiveWindow(LiveWindowSetup lws);
        void receiveDirtyLiveWindow(TargettedDirtyElement tde);
        void receiveAutoShape(TargettedAutoShape tas);
        void receiveDirtyAutoShape(TargettedDirtyElement tde);
        void receiveBubble(TargettedBubbleContext tbc);
        void receiveConversationDetails(ConversationDetails cd);
        void statusChanged(bool isConnected, Credentials credentials);
        event MeTLLibEventHandlers.SubmissionAvailableEventHandler SubmissionAvailable;
        event MeTLLibEventHandlers.FileAvailableEventHandler FileAvailable;
        event MeTLLibEventHandlers.StatusChangedEventHandler StatusChanged;
        event MeTLLibEventHandlers.PreParserAvailableEventHandler PreParserAvailable;
        event MeTLLibEventHandlers.StrokeAvailableEventHandler StrokeAvailable;
        event MeTLLibEventHandlers.ImageAvailableEventHandler ImageAvailable;
        event MeTLLibEventHandlers.VideoAvailableEventHandler VideoAvailable;
        event MeTLLibEventHandlers.TextBoxAvailableEventHandler TextBoxAvailable;
        event MeTLLibEventHandlers.DirtyElementAvailableEventHandler DirtyTextBoxAvailable;
        event MeTLLibEventHandlers.DirtyElementAvailableEventHandler DirtyImageAvailable;
        event MeTLLibEventHandlers.DirtyElementAvailableEventHandler DirtyVideoAvailable;
        event MeTLLibEventHandlers.DirtyElementAvailableEventHandler DirtyStrokeAvailable;
        event MeTLLibEventHandlers.DirtyElementAvailableEventHandler DirtyLiveWindowAvailable;
        event MeTLLibEventHandlers.DirtyElementAvailableEventHandler DirtyAutoShapeAvailable;
        event MeTLLibEventHandlers.AutoshapeAvailableEventHandler AutoshapeAvailable;
        event MeTLLibEventHandlers.LiveWindowAvailableEventHandler LiveWindowAvailable;
        event MeTLLibEventHandlers.BubbleAvailableEventHandler BubbleAvailable;
        event MeTLLibEventHandlers.DiscoAvailableEventHandler DiscoAvailable;
        event MeTLLibEventHandlers.QuizQuestionAvailableEventHandler QuizQuestionAvailable;
        event MeTLLibEventHandlers.QuizAnswerAvailableEventHandler QuizAnswerAvailable;
        event MeTLLibEventHandlers.ChatAvailableEventHandler ChatAvailable;
        event MeTLLibEventHandlers.ConversationDetailsAvailableEventHandler ConversationDetailsAvailable;
        event MeTLLibEventHandlers.CommandAvailableEventHandler CommandAvailable;
    }

    class ProductionReceiveEvents : IReceiveEvents
    {
        public ProductionReceiveEvents()
        {
            this.AutoshapeAvailable += (sender, args) => { };
            this.BubbleAvailable += (sender, args) => { };
            this.ChatAvailable+= (sender, args) => { };
            this.CommandAvailable += (sender, args) => { };
            this.ConversationDetailsAvailable+= (sender, args) => { };
            this.DirtyAutoShapeAvailable+= (sender, args) => { };
            this.DirtyImageAvailable+= (sender, args) => { };
            this.DirtyLiveWindowAvailable+= (sender, args) => { };
            this.DirtyStrokeAvailable+= (sender, args) => { };
            this.DirtyTextBoxAvailable+= (sender, args) => { };
            this.DirtyVideoAvailable+= (sender, args) => { };
            this.DiscoAvailable+= (sender, args) => { };
            this.FileAvailable+= (sender, args) => { };
            this.ImageAvailable+= (sender, args) => { };
            this.LiveWindowAvailable+= (sender, args) => { };
            this.PreParserAvailable+= (sender, args) => { };
            this.QuizAnswerAvailable+= (sender, args) => { };
            this.QuizQuestionAvailable+= (sender, args) => { };
            this.StatusChanged += (sender,args) =>{ };
            this.StrokeAvailable+= (sender, args) => { };
            this.SubmissionAvailable+= (sender, args) => { };
            this.TextBoxAvailable+= (sender, args) => { };
            this.VideoAvailable+= (sender, args) => { };
        }
        void IReceiveEvents.receiveSubmission(TargettedSubmission ts)
        {
            SubmissionAvailable(this, new SubmissionAvailableEventArgs { submission = ts });
        }
        void IReceiveEvents.receiveQuiz(QuizQuestion qq)
        {
            QuizQuestionAvailable(this, new QuizQuestionAvailableEventArgs { quizQuestion = qq });
        }
        void IReceiveEvents.receiveQuizAnswer(QuizAnswer qa)
        {
            QuizAnswerAvailable(this, new QuizAnswerAvailableEventArgs { quizAnswer = qa });
        }
        void IReceiveEvents.receiveFileResource(TargettedFile tf)
        {
            FileAvailable(this, new FileAvailableEventArgs { file = tf });
        }
        void IReceiveEvents.receiveStroke(TargettedStroke ts)
        {
            StrokeAvailable(this, new StrokeAvailableEventArgs { stroke = ts });
        }
        void IReceiveEvents.receiveStrokes(TargettedStroke[] tsa)
        {
            foreach (TargettedStroke ts in tsa)
                StrokeAvailable(this, new StrokeAvailableEventArgs { stroke = ts });
        }
        void IReceiveEvents.receiveImages(TargettedImage[] tia)
        {
            foreach (TargettedImage ti in tia)
                ImageAvailable(this, new ImageAvailableEventArgs { image = ti });
        }
        void IReceiveEvents.receiveImage(TargettedImage ti)
        {
            ImageAvailable(this, new ImageAvailableEventArgs { image = ti });
        }
        void IReceiveEvents.receiveTextBox(TargettedTextBox ttb)
        {
            TextBoxAvailable(this, new TextBoxAvailableEventArgs { textBox = ttb });
        }
        void IReceiveEvents.receiveVideo(TargettedVideo tv)
        {
            VideoAvailable(this, new VideoAvailableEventArgs { video = tv });
        }
        void IReceiveEvents.receiveDirtyStroke(TargettedDirtyElement tde)
        {
            DirtyStrokeAvailable(this, new DirtyElementAvailableEventArgs { dirtyElement = tde });
        }
        void IReceiveEvents.receiveDirtyTextBox(TargettedDirtyElement tde)
        {
            DirtyTextBoxAvailable(this, new DirtyElementAvailableEventArgs { dirtyElement = tde });
        }
        void IReceiveEvents.receiveDirtyVideo(TargettedDirtyElement tde)
        {
            DirtyVideoAvailable(this, new DirtyElementAvailableEventArgs { dirtyElement = tde });
        }
        void IReceiveEvents.receiveDirtyImage(TargettedDirtyElement tde)
        {
            DirtyImageAvailable(this, new DirtyElementAvailableEventArgs { dirtyElement = tde });
        }
        void IReceiveEvents.receivePreParser(PreParser pp)
        {
            PreParserAvailable(this, new PreParserAvailableEventArgs { parser = pp });
        }
        public void receiveChat(TargettedTextBox ttb)
        {
            ChatAvailable(this, new ChatAvailableEventArgs { chat = ttb });
        }
        public void receiveLiveWindow(LiveWindowSetup lws)
        {
            LiveWindowAvailable(this, new LiveWindowAvailableEventArgs { livewindow = lws });
        }
        public void receiveDirtyLiveWindow(TargettedDirtyElement tde)
        {
            DirtyLiveWindowAvailable(this, new DirtyElementAvailableEventArgs { dirtyElement = tde });
        }
        public void receiveAutoShape(TargettedAutoShape tas)
        {
            AutoshapeAvailable(this, new AutoshapeAvailableEventArgs { autoshape = tas });
        }
        public void receiveDirtyAutoShape(TargettedDirtyElement tde)
        {
            DirtyAutoShapeAvailable(this, new DirtyElementAvailableEventArgs { dirtyElement = tde });
        }
        public void receiveBubble(TargettedBubbleContext tbc)
        {
            BubbleAvailable(this, new BubbleAvailableEventArgs { bubble = tbc });
        }
        public void receiveConversationDetails(ConversationDetails cd)
        {
            ConversationDetailsAvailable(this, new ConversationDetailsAvailableEventArgs { conversationDetails = cd });
        }
        public void statusChanged(bool isConnected, Credentials credentials)
        {
            StatusChanged(this, new StatusChangedEventArgs { isConnected = isConnected, credentials = credentials });
        }

        #region Events
        public event MeTLLibEventHandlers.SubmissionAvailableEventHandler SubmissionAvailable;
        public event MeTLLibEventHandlers.FileAvailableEventHandler FileAvailable;
        public event MeTLLibEventHandlers.StatusChangedEventHandler StatusChanged;
        public event MeTLLibEventHandlers.PreParserAvailableEventHandler PreParserAvailable;
        public event MeTLLibEventHandlers.StrokeAvailableEventHandler StrokeAvailable;
        public event MeTLLibEventHandlers.ImageAvailableEventHandler ImageAvailable;
        public event MeTLLibEventHandlers.VideoAvailableEventHandler VideoAvailable;
        public event MeTLLibEventHandlers.TextBoxAvailableEventHandler TextBoxAvailable;
        public event MeTLLibEventHandlers.DirtyElementAvailableEventHandler DirtyTextBoxAvailable;
        public event MeTLLibEventHandlers.DirtyElementAvailableEventHandler DirtyImageAvailable;
        public event MeTLLibEventHandlers.DirtyElementAvailableEventHandler DirtyVideoAvailable;
        public event MeTLLibEventHandlers.DirtyElementAvailableEventHandler DirtyStrokeAvailable;
        public event MeTLLibEventHandlers.AutoshapeAvailableEventHandler AutoshapeAvailable;
        public event MeTLLibEventHandlers.LiveWindowAvailableEventHandler LiveWindowAvailable;
        public event MeTLLibEventHandlers.BubbleAvailableEventHandler BubbleAvailable;
        public event MeTLLibEventHandlers.DiscoAvailableEventHandler DiscoAvailable;
        public event MeTLLibEventHandlers.QuizQuestionAvailableEventHandler QuizQuestionAvailable;
        public event MeTLLibEventHandlers.QuizAnswerAvailableEventHandler QuizAnswerAvailable;
        public event MeTLLibEventHandlers.ChatAvailableEventHandler ChatAvailable;
        public event MeTLLibEventHandlers.ConversationDetailsAvailableEventHandler ConversationDetailsAvailable;
        public event MeTLLibEventHandlers.CommandAvailableEventHandler CommandAvailable;
        public event MeTLLibEventHandlers.DirtyElementAvailableEventHandler DirtyLiveWindowAvailable;
        public event MeTLLibEventHandlers.DirtyElementAvailableEventHandler DirtyAutoShapeAvailable;
        #endregion Events
        #region VirtualEventSubscribers
        protected virtual void onSubmissionAvailable(SubmissionAvailableEventArgs e)
        { SubmissionAvailable(this, e); }
        protected virtual void onFileAvailable(FileAvailableEventArgs e)
        { FileAvailable(this, e); }
        protected virtual void onStatusChanged(StatusChangedEventArgs e)
        { StatusChanged(this, e); }
        protected virtual void onPreParserAvailable(PreParserAvailableEventArgs e)
        { PreParserAvailable(this, e); }
        protected virtual void onStrokeAvailable(StrokeAvailableEventArgs e)
        { StrokeAvailable(this, e); }
        protected virtual void onDirtyStrokeAvailable(DirtyElementAvailableEventArgs e)
        { DirtyStrokeAvailable(this, e); }
        protected virtual void onDirtyTextBoxAvailable(DirtyElementAvailableEventArgs e)
        { DirtyTextBoxAvailable(this, e); }
        protected virtual void onDirtyImageAvailable(DirtyElementAvailableEventArgs e)
        { DirtyImageAvailable(this, e); }
        protected virtual void onDirtyVideoAvailable(DirtyElementAvailableEventArgs e)
        { DirtyVideoAvailable(this, e); }
        protected virtual void onImageAvailable(ImageAvailableEventArgs e)
        { ImageAvailable(this, e); }
        protected virtual void onVideoAvailable(VideoAvailableEventArgs e)
        { VideoAvailable(this, e); }
        protected virtual void onTextBoxAvailable(TextBoxAvailableEventArgs e)
        { TextBoxAvailable(this, e); }
        protected virtual void onAutoshapeAvailable(AutoshapeAvailableEventArgs e)
        { AutoshapeAvailable(this, e); }
        protected virtual void onLiveWindowAvailable(LiveWindowAvailableEventArgs e)
        { LiveWindowAvailable(this, e); }
        protected virtual void onBubbleAvailable(BubbleAvailableEventArgs e)
        { BubbleAvailable(this, e); }
        protected virtual void onDiscoAvailable(DiscoAvailableEventArgs e)
        { DiscoAvailable(this, e); }
        protected virtual void onQuizQuestionAvailable(QuizQuestionAvailableEventArgs e)
        { QuizQuestionAvailable(this, e); }
        protected virtual void onQuizAnswerAvailable(QuizAnswerAvailableEventArgs e)
        { QuizAnswerAvailable(this, e); }
        protected virtual void onChatAvailable(ChatAvailableEventArgs e)
        { ChatAvailable(this, e); }
        protected virtual void onConversationDetailsAvailable(ConversationDetailsAvailableEventArgs e)
        { ConversationDetailsAvailable(this, e); }
        protected virtual void onCommandAvailable(CommandAvailableEventArgs e)
        { CommandAvailable(this, e); }
        #endregion
    }
}
