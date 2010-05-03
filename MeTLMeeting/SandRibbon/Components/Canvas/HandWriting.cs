﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Media;
using Microsoft.Practices.Composite.Presentation.Commands;
using SandRibbon.Utils;
using SandRibbonInterop;
using SandRibbonInterop.MeTLStanzas;

namespace SandRibbon.Components.Canvas
{
    public class HandWriting : AbstractCanvas
    {
        private static string currentJid;
        public HandWriting()
        {
            Loaded += new System.Windows.RoutedEventHandler(HandWriting_Loaded);
            StrokeCollected+=singleStrokeCollected; 
            SelectionChanging+=selectingStrokes; 
            SelectionChanged+=selectionChanged;
            StrokeErasing+=erasingStrokes; 
            SelectionMoving+=dirtySelectedRegions;
            SelectionMoved+=transmitSelectionAltered;
            SelectionResizing+=dirtySelectedRegions;
            SelectionResized+=transmitSelectionAltered;
            DefaultDrawingAttributesReplaced += announceDrawingAttributesChanged;
            Background=Brushes.Transparent;
            defaultWidth = DefaultDrawingAttributes.Width;
            defaultHeight = DefaultDrawingAttributes.Height;
            modeChangedCommand = new DelegateCommand<string>(setInkCanvasMode, canChangeMode);
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, deleteSelectedStrokes));
            Commands.SetInkCanvasMode.RegisterCommand(modeChangedCommand);
            Commands.ChangePenSize.RegisterCommand(new DelegateCommand<double>(penSize =>
            {
                var newAttributes = DefaultDrawingAttributes.Clone();
                newAttributes.Width = penSize;
                newAttributes.Height = penSize;
                DefaultDrawingAttributes = newAttributes;
            }));
            Commands.IncreasePenSize.RegisterCommand(new DelegateCommand<object>(_obj => 
            {
                var newAttributes = DefaultDrawingAttributes.Clone();
                newAttributes.Width += 5;
                newAttributes.Height += 5;
                DefaultDrawingAttributes = newAttributes;
            }));
            Commands.DecreasePenSize.RegisterCommand(new DelegateCommand<object>(_obj =>
             {
                 if ((DefaultDrawingAttributes.Width - 0.5) <= 0) return;
                 var newAttributes = DefaultDrawingAttributes.Clone();
                 newAttributes.Width -= .5;
                 newAttributes.Height -= .5;
                 DefaultDrawingAttributes = newAttributes;
             }));
            Commands.RestorePenSize.RegisterCommand(new DelegateCommand<object>(_obj =>
            {
                var newAttributes = DefaultDrawingAttributes.Clone();
                newAttributes.Width = defaultWidth;
                newAttributes.Height = defaultHeight;
                DefaultDrawingAttributes = newAttributes;
            }));
            Commands.SetDrawingAttributes.RegisterCommand(new DelegateCommand<DrawingAttributes>(attributes =>
             {
                 DefaultDrawingAttributes = attributes;
             }));
            Commands.ToggleHighlighterMode.RegisterCommand(new DelegateCommand<object>(_obj =>
            {
                var newAttributes = DefaultDrawingAttributes.Clone();
                if ( newAttributes.IsHighlighter)
                    newAttributes.IsHighlighter = false;
                else
                    newAttributes.IsHighlighter = true;
                DefaultDrawingAttributes = newAttributes;
            }));
            Commands.SetHighlighterMode.RegisterCommand(new DelegateCommand<bool>(newIsHighlighter =>
            {
                var newAttributes = DefaultDrawingAttributes.Clone();
                newAttributes.IsHighlighter = newIsHighlighter;
                DefaultDrawingAttributes = newAttributes;
            }));
            colorChangedCommand = new DelegateCommand<object>((colorObj) =>
            {
                var newAttributes = DefaultDrawingAttributes.Clone();
                if (colorObj is Color)
                    newAttributes.Color = (Color)colorObj;
                else if (colorObj is string)
                    newAttributes.Color = ColorLookup.ColorOf((string)colorObj);
                DefaultDrawingAttributes = newAttributes;
            });
            Commands.SetPenColor.RegisterCommand(colorChangedCommand);
            setAuthor = new DelegateCommand<SandRibbon.Utils.Connection.JabberWire.Credentials>(
                author => me = author.name);
            Commands.SetIdentity.RegisterCommand(setAuthor);
            Commands.ReceiveStroke.RegisterCommand(new DelegateCommand<TargettedStroke>((stroke) => ReceiveStrokes(new[] { stroke })));
            Commands.ReceiveStrokes.RegisterCommand(
                new DelegateCommand<IEnumerable<TargettedStroke>>(ReceiveStrokes));
            Commands.ReceiveDirtyStrokes.RegisterCommand(new DelegateCommand<IEnumerable<TargettedDirtyElement>>(ReceiveDirtyStrokes));
            Commands.JoinConversation.RegisterCommand(new DelegateCommand<string>(jid=>currentJid=jid));
        }
        private void HandWriting_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            DefaultPenAttributes();
        }
        public static Guid STROKE_PROPERTY = Guid.NewGuid();
        public List<StrokeChecksum> strokes = new List<StrokeChecksum>();
        private DelegateCommand<string> modeChangedCommand;
        private DelegateCommand<object> colorChangedCommand;
        private DelegateCommand<SandRibbon.Utils.Connection.JabberWire.Credentials> setAuthor;
        protected override void CanEditChanged()
        {
            canEdit = base.canEdit;
            if(privacy == "private") canEdit = true;
        }
        private bool canEdit{ 
            get { return base.canEdit;}
            set { 
                base.canEdit = value;
                SetEditingMode();
                modeChangedCommand.RaiseCanExecuteChanged();
            }
        }
        private double defaultWidth;
        private double defaultHeight;
        private bool canChangeMode(string arg)
        {
            return true;
        }
        private void setInkCanvasMode(string modeString)
        {
            if (!canEdit)
                EditingMode = InkCanvasEditingMode.None;
            else
                EditingMode = (InkCanvasEditingMode)Enum.Parse(typeof(InkCanvasEditingMode), modeString);
        }
        public void SetEditingMode()
        {
            if(canEdit)
                Enable();
            else
                Disable();
        }
        public void DefaultPenAttributes()
        {
            DefaultDrawingAttributes = new DrawingAttributes
                                           {
                                               Color = Colors.Black,
                                               Width = 1,
                                               Height = 1,
                                               IsHighlighter = false
                                           };
        }
        private void announceDrawingAttributesChanged(object sender, DrawingAttributesReplacedEventArgs e)
        {
            Commands.ReportDrawingAttributes.Execute(this.DefaultDrawingAttributes);            
        }
        private static List<TimeSpan> strokeReceiptDurations = new List<TimeSpan>();
        private static double averageStrokeReceiptDuration()
        {
            return strokeReceiptDurations.Aggregate(0.0,(acc,item)=>acc+item.TotalMilliseconds)/strokeReceiptDurations.Count();
        }
        public void ReceiveStrokes(IEnumerable<TargettedStroke> receivedStrokes)
        {
            if (receivedStrokes.Count() == 0) return;
            if (receivedStrokes.First().slide != currentSlideId) return;
            var strokeTarget = target;
            var doStrokes = (Action)delegate
            {
                var count = receivedStrokes.Count();
                var start = SandRibbonObjects.DateTimeFactory.Now();
                var newStrokes = new StrokeCollection(
                    receivedStrokes.Where(ts=> ts.target == strokeTarget)
                    .Where(s=>s.privacy == "public" || s.author == me)
                    .Select(s=>s.stroke)
                    .Where(s=>!(this.strokes.Contains(s.sum()))));
                        Strokes.Add(newStrokes);
                this.strokes.AddRange(newStrokes.Select(s=>s.sum()));
                
                foreach(var stroke in receivedStrokes)
                {
                    if (stroke.privacy == "private")
                        if (stroke.target == target)
                            addPrivateRegion(stroke.stroke);
                }
                var duration = SandRibbonObjects.DateTimeFactory.Now() - start;
                HandWriting.strokeReceiptDurations.Add(duration);
                if(count > 1)
                    Logger.Log(string.Format("Handwriting.ReceiveStrokes: {0} strokes took {1}", receivedStrokes.Count(), duration));
            };
            if (Thread.CurrentThread != Dispatcher.Thread)
                Dispatcher.BeginInvoke(doStrokes);
            else
                doStrokes();
        }
        #region eventHandlers
        private void addPrivateRegion(Stroke stroke)
        {
            var bounds = stroke.GetBounds();
            addPrivateRegion(new[] { bounds.TopLeft, bounds.TopRight, bounds.BottomRight, bounds.BottomLeft });
        }
        private void selectionChanged(object sender, EventArgs e)
        {
        }
        public StrokeCollection GetSelectedStrokes()
        {
            return filter(base.GetSelectedStrokes(), me);
        }
        private void selectingStrokes(object sender, InkCanvasSelectionChangingEventArgs e)
        {
            var selectedStrokes = e.GetSelectedStrokes();
            var myStrokes = filter(selectedStrokes, me);
            
            /*
             * This is the heat graph proof of concept code
            var theirStrokes = selectedStrokes.Except(myStrokes);
            var strokesByUser = theirStrokes.GroupBy(s=>s.tag().author);
            var colorIndex = 0;
            var availableColors = new[] { Colors.Red, Colors.Yellow, Colors.Blue, Colors.Green, Colors.Wheat, Colors.Honeydew };
            foreach (var group in strokesByUser)
            {
                foreach(var stroke in group)
                    highlight(stroke, availableColors[colorIndex]);
                colorIndex++;
            }
            var iter = availableColors.GetEnumerator();
            Commands.HighlightUser.Execute(strokesByUser.Select(group =>
            {
                iter.MoveNext();
                var result = new UserHighlight { color = (Color)iter.Current, user = group.Key };
                return result;
            }).ToList(), this);
            */
            e.SetSelectedStrokes(myStrokes);
        }
        private void singleStrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            if (currentJid == null) return;
            try
            {
                ((DelegateCommand<object>) Commands.Undo.RegisteredCommands.First()).RaiseCanExecuteChanged();
            }
            catch(Exception error)
            {
               //sadness 
            }
            doMyStrokeAdded(e.Stroke);
        }
        private void erasingStrokes(object sender, InkCanvasStrokeErasingEventArgs e)
        {
            try
            {
                if (!(filter(Strokes, me).Contains(e.Stroke)))
                {
                    e.Cancel = true;
                    return;
                }
                doMyStrokeRemoved(e.Stroke);
            }
            catch (Exception ex)
            {
                //Tag can be malformed if app state isn't fully logged in
            }
        }
        private void doMyStrokeRemoved(Stroke stroke)
        {
            doMyStrokeRemovedExceptHistory(stroke);
            UndoHistory.Queue(
                ()=>{
                    if (Strokes.Where(s => s.sum().checksum == stroke.sum().checksum).Count() ==0)
                    {
                        Strokes.Add(stroke);
                        doMyStrokeAddedExceptHistory(stroke, stroke.tag().privacy);
                    }
                },
                ()=>
                {
                    if(Strokes.Where(s => s.sum().checksum == stroke.sum().checksum).Count() > 0)
                    {
                        Strokes.Remove(stroke);
                        strokes.Remove(stroke.sum());
                        doMyStrokeRemoved(stroke);
                    }
                });
        }
        private void doMyStrokeRemovedExceptHistory(Stroke stroke)
        {
            var sum = stroke.sum().checksum.ToString();
            var bounds = stroke.GetBounds();
            removePrivateRegion(new[]
                                    {
                                        bounds.TopLeft, bounds.TopRight, bounds.BottomRight, bounds.BottomLeft
                                    });
            Commands.SendDirtyStroke.Execute(new TargettedDirtyElement
            {
                identifier = sum, 
                author = me,
                slide = currentSlideId,
                privacy = stroke.tag().privacy,
                target = target
            });
        }
        private void transmitSelectionAltered(object sender, EventArgs e)
        {
            foreach (var stroke in GetSelectedStrokes()) 
                doMyStrokeAdded(stroke);
        }
        private void deleteSelectedStrokes(object _sender, ExecutedRoutedEventArgs _handler)
        {
            dirtySelectedRegions(null, null);
        }
        private void dirtySelectedRegions(object _sender, InkCanvasSelectionEditingEventArgs _e)
        {
            foreach (var stroke in GetSelectedStrokes())
                doMyStrokeRemoved(stroke);
        }
        #endregion
        #region CommandMethods
        private void alwaysTrue(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void setAuthorIdentity(object sender, ExecutedRoutedEventArgs e)
        {
            me = e.Parameter.ToString();
        }
        #endregion
        #region utilityFunctions
        private StrokeCollection filter(IEnumerable<Stroke> from, string author)
        {
            return new StrokeCollection(from.Where(s => s.tag().author == author));
        }
        public void doMyStrokeAdded(Stroke stroke)
        {
            doMyStrokeAddedExceptHistory(stroke, privacy);
            UndoHistory.Queue(
                () =>
                {
                    var existingStroke = Strokes.Where(s => s.sum().checksum == stroke.sum().checksum).FirstOrDefault();
                    if(existingStroke != null)
                        doMyStrokeRemovedExceptHistory(existingStroke);
                },
                () =>
                {
                    if(Strokes.Where(s => s.sum().checksum == stroke.sum().checksum).Count() == 0)
                    {
                        Strokes.Add(stroke);
                        doMyStrokeAddedExceptHistory(stroke, stroke.tag().privacy);
                    }
                });
        }
        private void doMyStrokeAddedExceptHistory(Stroke stroke, string thisPrivacy)
        {
            if(!strokes.Contains(stroke.sum()))
                strokes.Add(stroke.sum());
            stroke.tag(new StrokeTag { author = me, privacy=thisPrivacy });
            SendTargettedStroke(stroke, thisPrivacy);
        }
        public void SendTargettedStroke(Stroke stroke, string thisPrivacy)
        {
            Commands.SendStroke.Execute(new TargettedStroke
            {
                stroke = stroke,
                target = target,
                author = me,
                privacy = thisPrivacy,
                slide = currentSlideId
            });
        }
        public void FlushStrokes()
        {
            var doFlush = (Action)delegate { Strokes.Clear(); };
            if (Thread.CurrentThread != Dispatcher.Thread)
                Dispatcher.BeginInvoke(doFlush);
            else
                doFlush();
            strokes = new List<StrokeChecksum>();
        }
        public void Disable()
        {
            EditingMode = InkCanvasEditingMode.None;
        }
        public void Enable()
        {
            if(EditingMode == InkCanvasEditingMode.None)
                EditingMode = InkCanvasEditingMode.Ink;
        }
        public void setPenColor(Color color)
        {
            DefaultDrawingAttributes.Color = color;
        }
        public void SetEditingMode(InkCanvasEditingMode mode)
        {
            EditingMode = mode;
        }
        #endregion
        protected override void HandlePaste()
        {
            var strokesBeforePaste = Strokes.Select(s => s).ToList();
            Paste();
            var newStrokes = Strokes.Where(s => !strokesBeforePaste.Contains(s));
            foreach(var stroke in newStrokes)
                doMyStrokeAdded(stroke);
        }
        protected override void HandleCopy()
        {
            CopySelection();
        }
        protected override void HandleCut()
        {
            var listToCut = new List<TargettedDirtyElement>();
            foreach(var stroke in GetSelectedStrokes())
                listToCut.Add(new TargettedDirtyElement
                {
                    identifier = stroke.sum().checksum.ToString(),
                    author = me,
                    slide = currentSlideId,
                    privacy = stroke.tag().privacy,
                    target = target
                });
            CutSelection();
            foreach(var element in listToCut)
                Commands.SendDirtyStroke.Execute(element);
        }
        public void ReceiveDirtyStrokes(IEnumerable<TargettedDirtyElement> targettedDirtyStrokes)
        {
            if (targettedDirtyStrokes.Count() == 0) return;
            if (!(targettedDirtyStrokes.First().target.Equals(target)) || targettedDirtyStrokes.First().slide != currentSlideId) return;
            var doReceiveDirty = (Action)delegate
            {
                var dirtyChecksums = targettedDirtyStrokes.Select(t => t.identifier);
                var presentDirtyStrokes = Strokes.Where(s => dirtyChecksums.Contains(s.sum().checksum.ToString())).ToList();
                for (int i = 0; i < presentDirtyStrokes.Count(); i++)
                {
                    var stroke = presentDirtyStrokes[i];
                    strokes.Remove(stroke.sum());
                    Strokes.Remove(stroke);
                }
            };
            if (Thread.CurrentThread != Dispatcher.Thread)
                Dispatcher.BeginInvoke(doReceiveDirty);
            else
                doReceiveDirty();
        }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new HandWritingAutomationPeer(this);
        }
    }
    class HandWritingAutomationPeer : FrameworkElementAutomationPeer, IValueProvider
    {
        public HandWritingAutomationPeer(HandWriting parent) : base(parent) { }
        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Value)
                return this;
            return base.GetPattern(patternInterface);
        }
        private HandWriting HandWriting
        {
            get { return (HandWriting)base.Owner; }
        }
        protected override string GetAutomationIdCore()
        {
            return "handwriting";
        }
        public void SetValue(string value)
        {
            HandWriting.ParseInjectedStream(value, element =>{
                HandWriting.Dispatcher.Invoke((Action)delegate
                {
                    foreach (var ink in element.SelectElements<MeTLStanzas.Ink>(true))
                    {
                        var stroke = ink.Stroke.stroke;
                        HandWriting.doMyStrokeAdded(stroke);
                        HandWriting.strokes.Remove(stroke.sum());//Pretend we haven't seen it - IRL it would be on the screen already.
                    }
                });
            });
        }
        bool IValueProvider.IsReadOnly
        {
            get { return false; }
        }
        string IValueProvider.Value
        {
            get { 
                var hw = (HandWriting)base.Owner;
                var sb = new StringBuilder("<strokes>");
                foreach(var toString in from stroke in hw.Strokes
                    select new MeTLStanzas.Ink(new TargettedStroke
                    {
                        author = hw.me,
                        privacy = hw.privacy,
                        slide = hw.currentSlideId,
                        stroke = stroke,
                        target = hw.target
                    }).ToString())
                sb.Append(toString);
                sb.Append("</strokes>");
                return sb.ToString();
            }
        }
    }
    public class LiveInkCanvas : HandWriting
    {//Warning!  This one is the biggest message hog in the universe!  But it's live transmitting ink
        public LiveInkCanvas()
            : base()
        {
            this.StylusPlugIns.Add(new LiveNotifier(this));
        }

    }
    public class LiveNotifier : StylusPlugIn
    {
        private LiveInkCanvas parent;
        public LiveNotifier(LiveInkCanvas parent)
        {
            this.parent = parent;
        }
        protected override void OnStylusDown(RawStylusInput rawStylusInput)
        {
            base.OnStylusDown(rawStylusInput);
            rawStylusInput.NotifyWhenProcessed(null);
        }
        protected override void OnStylusMove(RawStylusInput rawStylusInput)
        {
            base.OnStylusMove(rawStylusInput);
            rawStylusInput.NotifyWhenProcessed(rawStylusInput.GetStylusPoints());
        }
        protected override void OnStylusUp(RawStylusInput rawStylusInput)
        {
            base.OnStylusUp(rawStylusInput);
            rawStylusInput.NotifyWhenProcessed(null);
        }
        protected override void OnStylusDownProcessed(object callbackData, bool targetVerified)
        {
            base.OnStylusDownProcessed(callbackData, targetVerified);
            if(parent.target == "presentationSpace")
                Projector.PenUp();
        }
        protected override void OnStylusMoveProcessed(object callbackData, bool targetVerified)
        {
            base.OnStylusMoveProcessed(callbackData, targetVerified);
            if(parent.target == "presentationSpace")
                Projector.PenMoving(callbackData as StylusPointCollection);
        }
        protected override void OnStylusUpProcessed(object callbackData, bool targetVerified)
        {
         	base.OnStylusUpProcessed(callbackData, targetVerified);
            if(parent.target == "presentationSpace")
                Projector.PenUp();
        }
    }
}