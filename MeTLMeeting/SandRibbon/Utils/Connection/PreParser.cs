﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using SandRibbonInterop;
using SandRibbonInterop.MeTLStanzas;
using SandRibbonObjects;

namespace SandRibbon.Utils.Connection
{
    public class PreParser : JabberWire
    {
        public Dictionary<string, TargettedImage> images = new Dictionary<string, TargettedImage>();
        public Dictionary<string, TargettedAutoShape> autoshapes = new Dictionary<string, TargettedAutoShape>();
        public List<TargettedStroke> ink = new List<TargettedStroke>();
        public List<QuizStatusDetails> quizStatus = new List<QuizStatusDetails>();
        public List<QuizDetails> quizs = new List<QuizDetails>();
        public List<QuizAnswer> quizAnswers = new List<QuizAnswer>();
        public List<TargettedBubbleContext> bubbleList = new List<TargettedBubbleContext>();
        public Dictionary<string, TargettedTextBox> text = new Dictionary<string, TargettedTextBox>();
        public Dictionary<string, LiveWindowSetup> liveWindows = new Dictionary<string, LiveWindowSetup>();
        public PreParser(int slide):base()
        {
            if (this.location == null)
                this.location = new Location();
            this.location.currentSlide = slide;
        }
        public InkCanvas ToVisual()
        {
            var canvas = new InkCanvas();
            foreach (var stroke in ink)
                canvas.Strokes.Add(stroke.stroke);
            foreach (var image in images)
                canvas.Children.Add(image.Value.image);
            foreach (var textbox in text)
                canvas.Children.Add(textbox.Value.box);
            foreach (var shape in autoshapes)
                canvas.Children.Add(shape.Value.autoshape);
            return canvas;
        }
        public T merge<T>(T otherParser) where T : PreParser
        {
            var returnParser = (T)Activator.CreateInstance(typeof(T), location.currentSlide);
            foreach (var parser in new[] { this, otherParser })
            {
                returnParser.ink.AddRange(parser.ink);
                returnParser.quizs.AddRange(parser.quizs);
                returnParser.quizStatus.AddRange(parser.quizStatus);
                foreach (var kv in parser.text)
                    returnParser.text.Add(kv.Key, kv.Value);
                foreach (var kv in parser.images)
                    if(!returnParser.images.ContainsKey(kv.Key))
                        returnParser.images.Add(kv.Key, kv.Value);
                foreach (var kv in parser.autoshapes)
                    returnParser.autoshapes.Add(kv.Key, kv.Value);
                foreach (var kv in parser.liveWindows)
                    returnParser.liveWindows.Add(kv.Key, kv.Value);
            }
            return returnParser;
        }
        public void Regurgitate()
        {
            Commands.ReceiveStrokes.Execute(ink);
            foreach (var autoshape in autoshapes.Values)
                Commands.ReceiveAutoShape.Execute(autoshape);
            if(images.Values.Count > 0)
                Commands.ReceiveImage.Execute(images.Values);
            foreach (var box in text.Values)
                Commands.ReceiveTextBox.Execute(box);
            foreach(var quiz in quizs)
                Commands.ReceiveQuiz.Execute(quiz);
            foreach(var status in quizStatus)
                Commands.ReceiveQuizStatus.Execute(status);
            foreach (var window in liveWindows.Values)
                Commands.ReceiveLiveWindow.Execute(window);
            Commands.AllContentSent.Execute(location.currentSlide);
            Logger.Log(string.Format("{1} regurgitate finished {0}", DateTimeFactory.Now(), this.location.currentSlide));
        }
        [Obsolete("Intended to handle compressed history for caching")]
        public string RegurgitateToXml()
        {    
            StringBuilder builder = new StringBuilder("<logCollection>");
            foreach (var stroke in ink)
                builder.Append(new MeTLStanzas.Ink(stroke)).ToString();
            foreach (var image in images.Values)
                builder.Append(image.imageSpecification.ToString());
            foreach (var autoshape in autoshapes.Values)
                builder.Append(new MeTLStanzas.AutoShape(autoshape).ToString());
            foreach (var box in text.Values)
                builder.Append(box.boxSpecification.ToString());
            foreach(var quiz in quizs)
                builder.Append(new MeTLStanzas.Quiz(quiz).ToString());
            foreach(var status in quizStatus)
                builder.Append(new MeTLStanzas.QuizStatus(status).ToString());
            /*
            foreach (var window in liveWindows.Values)
                builder.Append(new MeTLStanzas.LiveWindow(window).ToString());
             */
            return builder.ToString();
        }
        public override void actOnDirtyImageReceived(SandRibbonInterop.MeTLStanzas.MeTLStanzas.DirtyImage image)
        {
            if(images.ContainsKey(image.element.identifier))
                images.Remove(image.element.identifier);
        }
        public override void actOnDirtyAutoshapeReceived(MeTLStanzas.DirtyAutoshape element)
        {
            if (autoshapes.ContainsKey(element.element.identifier))
                autoshapes.Remove(element.element.identifier);
        }
        public override void actOnDirtyTextReceived(MeTLStanzas.DirtyText element)
        {
            if(text.ContainsKey(element.element.identifier))
                text.Remove(element.element.identifier);
        }
        public override void actOnDirtyStrokeReceived(MeTLStanzas.DirtyInk dirtyInk)
        {
            var strokesToRemove = ink.Where(s => 
                s.stroke.sum().checksum.ToString().Equals(dirtyInk.element.identifier)).ToList();
            foreach(var stroke in strokesToRemove)
                ink.Remove(stroke);
        }
        public override void actOnImageReceived(TargettedImage image)
        {
            images[image.id] = image;
        }
        public override void actOnAutoShapeReceived(TargettedAutoShape autoshape)
        {
            return;
            try
            {
                autoshapes[(string)autoshape.autoshape.Tag] = autoshape;
            }
            catch (NullReferenceException)
            {
                Logger.Log("Null reference in collecting autoshape from preparser");
            }
        }
        public override void actOnStrokeReceived(TargettedStroke stroke)
        {
            ink.Add(stroke);
        }

        public override void actOnQuizAnswerReceived(QuizAnswer answer)
        {
            quizAnswers.Add(answer);
        }
        public override void actOnQuizReceived(QuizDetails quizDetails)
        {
            quizs.Add(quizDetails);
        }
        public override void actOnTextReceived(TargettedTextBox box)
        {
            try
            {
                text[box.identity] = box;
            }
            catch (NullReferenceException)
            {
                Logger.Log("Null reference in collecting text from preparser");
            }
        }
        public override void actOnLiveWindowReceived(LiveWindowSetup window)
        {
            liveWindows[window.snapshotAtTimeOfCreation] = window;
        }
        public override void actOnDirtyLiveWindowReceived(TargettedDirtyElement element)
        {
            liveWindows.Remove(element.identifier);
        }
        public override void actOnQuizStatus(QuizStatusDetails status)
        {
            quizStatus.Add(status);
        }
        public override void actOnBubbleReceived(TargettedBubbleContext bubble)
        {
            bubbleList.Add(bubble);
        }
        public static int ParentRoom(string room)
        {
            var regex = new Regex(@"(\d+).*");
            var parent = regex.Matches(room)[0].Groups[1].Value;
            return Int32.Parse(parent);
        }
    }
}