﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MeTLLib.DataTypes;
using Microsoft.Practices.Composite.Presentation.Commands;
using Microsoft.Win32;
using SandRibbon.Components.Utility;
using SandRibbon.Providers;
using SandRibbon.Utils;
using SandRibbonObjects;

namespace SandRibbon.Components
{
    public class TagInformation
    {
        public string Author;
        public bool isPrivate;
        public string Id;
    }
    public class TextInformation : TagInformation
    {
        public double size;
        public FontFamily family;
        public bool underline;
        public bool bold;
        public bool italics;
        public bool strikethrough;
        public Color color;
    }
    public struct ImageDrop
    {
        public string Filename;
        public Point Point;
        public string Target;
        public int Position;
    }
    class ImageDropParameters {
        public string File { get; set; }
        public Point Location { get; set; }
    }
    public enum FileType
    {
        Video,
        Image,
        NotSupported
    }
    public partial class CollapsedCanvasStack : IClipboardHandler
    {
        List<MeTLTextBox> boxesAtTheStart = new List<MeTLTextBox>();
        private Color currentColor = Colors.Black;
        private readonly double defaultSize = 10.0;
        private readonly FontFamily defaultFamily = new FontFamily("Arial");
        private double currentSize = 10.0;
        private FontFamily currentFamily = new FontFamily("Arial");
        private bool canFocus = true;
        private bool focusable = true;
        public static Timer typingTimer = null;
        private string originalText;
        private MeTLTextBox myTextBox;
        private string target;
        private string defaultPrivacy;
        private bool _canEdit;
        private bool canEdit
        {
            get { return _canEdit; }
            set
            {
                _canEdit = value;
                Commands.RequerySuggested(Commands.SetInkCanvasMode);
            }
        }
        public string me
        {
            get { return Globals.me; }
        }
        private bool affectedByPrivacy { get { return target == "presentationSpace"; } }
        public string privacy { get { return affectedByPrivacy ? Globals.privacy: defaultPrivacy; } }
       
        private void wireInPublicHandlers()
        {
            PreviewKeyDown += keyPressed;
            MyWork.StrokeCollected += singleStrokeCollected;
            MyWork.SelectionChanged += selectionChanged;
            MyWork.StrokeErasing += erasingStrokes;
            MyWork.SelectionMoving += SelectionMovingOrResizing;
            MyWork.SelectionMoved += SelectionMovedOrResized;
            MyWork.SelectionResizing += SelectionMovingOrResizing;
            MyWork.SelectionResized += SelectionMovedOrResized;
            Loaded += (a, b) =>
            {
                MouseUp += (c, args) => placeCursor(this, args);
            };
        }
        public CollapsedCanvasStack()
        {
            InitializeComponent();
            wireInPublicHandlers();
            strokes = new List<StrokeChecksum>();
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, deleteSelectedElements));
            Commands.SetPrivacy.RegisterCommand(new DelegateCommand<string>(SetPrivacy));
            Commands.SetInkCanvasMode.RegisterCommandToDispatcher<string>(new DelegateCommand<string>(setInkCanvasMode));
            Commands.ReceiveStroke.RegisterCommandToDispatcher(new DelegateCommand<TargettedStroke>((stroke) => ReceiveStrokes(new[] { stroke })));
            Commands.ReceiveStrokes.RegisterCommandToDispatcher(new DelegateCommand<IEnumerable<TargettedStroke>>(ReceiveStrokes));
            Commands.ReceiveDirtyStrokes.RegisterCommand(new DelegateCommand<IEnumerable<TargettedDirtyElement>>(ReceiveDirtyStrokes));


            Commands.ReceiveImage.RegisterCommand(new DelegateCommand<IEnumerable<TargettedImage>>(ReceiveImages));
            Commands.ReceiveDirtyImage.RegisterCommand(new DelegateCommand<TargettedDirtyElement>(ReceiveDirtyImage));
            Commands.AddImage.RegisterCommandToDispatcher(new DelegateCommand<object>(addImageFromDisk));


            Commands.ReceiveTextBox.RegisterCommandToDispatcher(new DelegateCommand<TargettedTextBox>(ReceiveTextBox));
            Commands.UpdateTextStyling.RegisterCommand(new DelegateCommand<TextInformation>(updateStyling));
            Commands.RestoreTextDefaults.RegisterCommand(new DelegateCommand<object>(resetTextbox));
            Commands.EstablishPrivileges.RegisterCommand(new DelegateCommand<string>(setInkCanvasMode));
            Commands.SetTextCanvasMode.RegisterCommand(new DelegateCommand<string>(setInkCanvasMode));
            Commands.ReceiveDirtyText.RegisterCommand(new DelegateCommand<TargettedDirtyElement>(receiveDirtyText));
            
            Commands.MoveTo.RegisterCommand(new DelegateCommand<int>(MoveTo));
            Commands.SetLayer.RegisterCommandToDispatcher<string>(new DelegateCommand<string>(SetLayer));
            Commands.DeleteSelectedItems.RegisterCommandToDispatcher(new DelegateCommand<object>(deleteSelectedItems));
            Commands.SetPrivacyOfItems.RegisterCommand(new DelegateCommand<string>(changeSelectedItemsPrivacy));
            Commands.SetDrawingAttributes.RegisterCommandToDispatcher(new DelegateCommand<DrawingAttributes>(SetDrawingAttributes));
            Commands.UpdateConversationDetails.RegisterCommandToDispatcher(new DelegateCommand<ConversationDetails>(UpdateConversationDetails));
            Commands.HideConversationSearchBox.RegisterCommandToDispatcher(new DelegateCommand<object>(hideConversationSearchBox));
            Loaded += (_sender, _args) => this.Dispatcher.adoptAsync(delegate
            {
                if (target == null)
                {
                    target = (string)FindResource("target");
                    defaultPrivacy = (string)FindResource("defaultPrivacy");
                }
            });
        }
        private void keyPressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                deleteSelectedElements(null, null);
            }
        }
        private void SetLayer(string newLayer)
        {
            if (me.ToLower() == "projector") return;
            switch (newLayer)
            {
                case "Text":
                    MyWork.EditingMode = InkCanvasEditingMode.None;
                    MyWork.UseCustomCursor = MyWork.EditingMode == InkCanvasEditingMode.Ink;
                    break;
                case "Insert":
                    MyWork.EditingMode = InkCanvasEditingMode.Select;
                    MyWork.UseCustomCursor = MyWork.EditingMode == InkCanvasEditingMode.Ink;
                    break;
            }
            focusable = newLayer == "Text";
            setLayerForTextFor(MyWork);
            
        }
        private void setLayerForTextFor(InkCanvas canvas)
        {
            foreach (var box in canvas.Children)
            {
                if (box.GetType() == typeof(MeTLTextBox))
                {
                    var tag = ((MeTLTextBox)box).tag();
                    ((MeTLTextBox)box).Focusable = focusable && (tag.author == Globals.me);
                }
            }
        }
        private UndoHistory.HistoricalAction deleteSelectedImages(List<UIElement> selectedElements)
        {
             Action undo = () =>
                {
                    foreach (var element in selectedElements)
                    {
                       if (MyWork.Children.ToList().Where(i => ((Image)i).tag().id == ((Image)element).tag().id).Count() == 0)
                            MyWork.Children.Add(element);
                        sendThisElement(element);
                    }
                    MyWork.Select(selectedElements);
                };
            Action redo = () =>
                {
                    foreach (var element in selectedElements)
                    {
                        if (MyWork.Children.ToList().Where(i => ((Image)i).tag().id == ((Image)element).tag().id).Count() > 0)
                            MyWork.Children.Remove(MyWork.Children.ToList().Where(i =>((Image)i).tag().id == ((Image)element).tag().id ).First());
                        dirtyThisElement(element);
                    }
                };
            return new UndoHistory.HistoricalAction(undo, redo, 0);
         
        }
        private UndoHistory.HistoricalAction deleteSelectedInk(StrokeCollection selectedStrokes)
        {
            Action undo = () =>
                {
                    addStrokes(selectedStrokes.ToList());
                    if(MyWork.EditingMode == InkCanvasEditingMode.Select)
                        MyWork.Select(selectedStrokes);
                    
                };
            Action redo = () => removeStrokes(selectedStrokes.ToList());
            return new UndoHistory.HistoricalAction(undo, redo, 0);
        }
        private UndoHistory.HistoricalAction deleteSelectedText(List<UIElement> elements)
        {
            var selectedElements = elements.Where(t => ((MeTLTextBox)t).tag().author == Globals.me).Select(b => ((MeTLTextBox)b).clone()).ToList();
            Action undo = () =>
                              {
                                  var selection = new List<UIElement>();
                                  foreach (var box in selectedElements)
                                  {
                                      myTextBox = box;
                                      selection.Add(box);
                                      if(!alreadyHaveThisTextBox(box))
                                          AddTextBoxToCanvas(box);
                                      box.TextChanged += SendNewText;
                                      box.PreviewTextInput += box_PreviewTextInput;
                                      sendTextWithoutHistory(box, box.tag().privacy);
                                  }
                              };
            Action redo = () =>
                              {
                                  foreach(var box in selectedElements)
                                  {
                                      myTextBox = null;
                                      dirtyTextBoxWithoutHistory(box);
                                  }
                                  // set keyboard focus to the current canvas so the help button does not grey out
                                  Keyboard.Focus(this);
                                  ClearAdorners();
                              };
            return new UndoHistory.HistoricalAction(undo, redo, 0);
        }

        private void AddTextBoxToCanvas(MeTLTextBox box)
        {
            if(box.tag().author == me)
                MyWork.Children.Add(box);
            else
            {
                OtherWork.Children.Add(box);
            }
        }

        private void deleteSelectedElements(object _sender, ExecutedRoutedEventArgs _handler)
        {
            var selectedElements = new List<UIElement>();
            var selectedStrokes = new StrokeCollection();
            Dispatcher.adopt(() =>
                                 {
                                     selectedStrokes = MyWork.GetSelectedStrokes();
                                     selectedElements = GetSelectedClonedImages().Where(i=> ((System.Windows.Controls.Image)i).tag().author == Globals.me).ToList();
                                 });
            var ink = deleteSelectedInk(selectedStrokes);
            var images = deleteSelectedImages(selectedElements);
            var text = deleteSelectedText(selectedElements);
            Action undo = () =>
                {
                    ClearAdorners();
                    ink.undo();
                    text.undo();
                    images.undo();
                    AddAdorners();
                };
            Action redo = () =>
                {
                    // set keyboard focus to the current canvas so the help button does not grey out
                    Keyboard.Focus(this);
                    ClearAdorners();
                    ink.redo();
                    text.redo();
                    images.redo();
                    AddAdorners();
                };
         
            redo();
            UndoHistory.Queue(undo, redo);
        }
        private void hideConversationSearchBox(object obj)
        {
            AddAdorners();
        }
        private void UpdateConversationDetails(ConversationDetails details)
        {
            ClearAdorners();
            if (ConversationDetails.Empty.Equals(details)) return;
            Dispatcher.adoptAsync(delegate
                  {
                       canEdit = Globals.isAuthor || details.Permissions.studentCanPublish;
                       var newStrokes = new StrokeCollection( MyWork.Strokes.Select( s => (Stroke) new PrivateAwareStroke(s, target)));
                       MyWork.Strokes.Clear();
                       MyWork.Strokes.Add(newStrokes);
                       foreach (Image image in MyWork.ImageChildren())
                           ApplyPrivacyStylingToElement(image, image.tag().privacy);
                       foreach (var item in MyWork.TextChildren())
                       {
                           MeTLTextBox box;
                           if (item.GetType() == typeof(TextBox))
                               box = ((TextBox)item).toMeTLTextBox();
                           else
                               box = (MeTLTextBox)item;
                           ApplyPrivacyStylingToElement(box, box.tag().privacy);
                       }
                  });
        }
        private void SetPrivacy(string privacy)
        {
            canEdit = privacy == Globals.PRIVATE || Globals.conversationDetails.Permissions.studentCanPublish || Globals.conversationDetails.Author == Globals.me;
            AllowDrop = canEdit;
        }
        private void setInkCanvasMode(string modeString)
        {
            if (!canEdit)
                MyWork.EditingMode = InkCanvasEditingMode.None;
            else
            {
                MyWork.EditingMode = (InkCanvasEditingMode)Enum.Parse(typeof(InkCanvasEditingMode), modeString);
                MyWork.UseCustomCursor = MyWork.EditingMode == InkCanvasEditingMode.Ink;
            }
        }
        private Double zoom = 1;
        private void ZoomChanged(Double zoom)
        {
            this.zoom = zoom;
            if (Commands.SetDrawingAttributes.IsInitialised)
            {
                SetDrawingAttributes((DrawingAttributes)Commands.SetDrawingAttributes.LastValue());
            }
        }

        #region Events
        private List<Stroke> strokesAtTheStart = new List<Stroke>();
        private List<StrokeChecksum> strokes;
        private List<UIElement> imagesAtStartOfTheMove = new List<UIElement>();
        private void SelectionMovingOrResizing (object _sender, InkCanvasSelectionEditingEventArgs e)
        {
            strokesAtTheStart.Clear();
            foreach (var stroke in MyWork.GetSelectedStrokes())
                strokesAtTheStart.Add(stroke.Clone());
            imagesAtStartOfTheMove.Clear();
            imagesAtStartOfTheMove = GetSelectedClonedImages();
            boxesAtTheStart.Clear();
            boxesAtTheStart = MyWork.GetSelectedElements().Where(b=> b is MeTLTextBox).Select(tb => ((MeTLTextBox)tb).clone()).ToList();

            if (e.NewRectangle.Width == e.OldRectangle.Width && e.NewRectangle.Height == e.OldRectangle.Height)
                return;

            Rect imageCanvasRect = new Rect(new Size(ActualWidth, ActualHeight));

            double resizeWidth;
            double resizeHeight;
            double imageX;
            double imageY;

            if (e.NewRectangle.Right > imageCanvasRect.Right)
                resizeWidth = Clamp(imageCanvasRect.Width - e.NewRectangle.X, 0, imageCanvasRect.Width);
            else
                resizeWidth = e.NewRectangle.Width;

            if (e.NewRectangle.Height > imageCanvasRect.Height)
                resizeHeight = Clamp(imageCanvasRect.Height - e.NewRectangle.Y, 0, imageCanvasRect.Height);
            else
                resizeHeight = e.NewRectangle.Height;

            imageX = Clamp(e.NewRectangle.X, 0, e.NewRectangle.X);
            imageY = Clamp(e.NewRectangle.Y, 0, e.NewRectangle.Y);
            e.NewRectangle = new Rect(imageX, imageY, resizeWidth, resizeHeight);
        }
        private double Clamp(double val, double min, double max)
        {
            if (val < min) 
                return min;
            if ( val > max)
                return max;
            return val;
        }
        private UndoHistory.HistoricalAction ImageSelectionMovedOrResized(IEnumerable<UIElement> elements, List<Image> startingElements)
        {

            var selectedElements = elements.Where(i => i is Image).Select(i => ((Image) i).clone()); 
            Action undo = () =>
                {
                    var selection = new List<UIElement>();
                    var mySelectedElements = selectedElements.Where(i => i is Image).Select(i => ((Image)i).clone()).ToList();
                    foreach (var element in mySelectedElements)
                    {
                        if (MyWork.Children.ToList().Where(i => i is Image &&((Image)i).tag().id == element.tag().id).Count() > 0)
                            MyWork.Children.Remove(MyWork.Children.ToList().Where(i => i is Image &&((Image)i).tag().id == element.tag().id).FirstOrDefault());
                        if (!element.Tag.ToString().StartsWith("NOT_LOADED"))
                            dirtyThisElement(element);
                    }
                    foreach (var element in startingElements)
                    {
                        selection.Add(element);
                        if (MyWork.Children.ToList().Where(i => i is Image &&((Image)i).tag().id == element.tag().id).Count() == 0)
                            MyWork.Children.Add(element);
                        if (!element.Tag.ToString().StartsWith("NOT_LOADED"))
                            sendThisElement(element);
                    }
                };
            Action redo = () =>
                {
                    var selection = new List<UIElement>();
                    var mySelectedImages = selectedElements.Where(i => i is Image).Select(i => ((Image)i).clone()).ToList();
                    foreach (var element in startingElements)
                    {
                          if (MyWork.Children.ToList().Where(i => i is Image && ((Image)i).tag().id == element.tag().id).Count() > 0)
                              MyWork.Children.Remove(MyWork.Children.ToList().Where(i =>i is Image && ((Image)i).tag().id == element.tag().id).FirstOrDefault());
                        dirtyThisElement(element);
                    }
                    foreach (var element in mySelectedImages)
                    { 
                        selection.Add(element);
                        if (MyWork.Children.ToList().Where(i =>i is Image && ((Image)i).tag().id == element.tag().id).Count() == 0)
                           MyWork.Children.Add(element);
                        sendThisElement(element);
                  }
                };           
            return new UndoHistory.HistoricalAction(undo, redo, 0);
        }
        private UndoHistory.HistoricalAction InkSelectionMovedOrResized(List<Stroke> selectedStrokes, List<Stroke> undoStrokes)
        {
 
            Action undo = () =>
                {
                    removeStrokes(selectedStrokes);
                    addStrokes(undoStrokes); 
                    if(MyWork.EditingMode == InkCanvasEditingMode.Select)
                        MyWork.Select(new StrokeCollection(undoStrokes));
                };
            Action redo = () =>
                {
                    removeStrokes(undoStrokes); 
                    addStrokes(selectedStrokes); 
                    if(MyWork.EditingMode == InkCanvasEditingMode.Select)
                        MyWork.Select(new StrokeCollection(selectedStrokes));
                };           
            return new UndoHistory.HistoricalAction(undo, redo, 0);
        }
        private UndoHistory.HistoricalAction TextMovedOrResized(IEnumerable<UIElement> elements, List<MeTLTextBox> boxesAtTheStart)
        {
            Trace.TraceInformation("MovedTextbox");
            var startingText = boxesAtTheStart.Where(b=> b is MeTLTextBox).Select(b=> ((MeTLTextBox)b).clone()).ToList();
            List<UIElement> selectedElements = elements.Where(b=> b is MeTLTextBox).ToList();
            abosoluteizeElements(selectedElements);
            Action undo = () =>
              {
                  ClearAdorners();
                  var mySelectedElements = selectedElements.Select(element => ((MeTLTextBox)element).clone());
                  foreach (MeTLTextBox box in mySelectedElements)
                  {
                      removeBox(box);
                  }
                  var selection = new List<UIElement>();
                  foreach (var box in startingText)
                  {
                      selection.Add(box);
                      sendBox(box);
                  }
              };
            Action redo = () =>
              {
                  ClearAdorners();
                  var mySelectedElements = selectedElements.Select(element => ((MeTLTextBox)element).clone());
                  var selection = new List<UIElement>();
                  foreach (var box in startingText)
                      removeBox(box);
                  foreach (var box in mySelectedElements)
                  {
                      selection.Add(box);
                      sendBox(box); 
                  }
              };
            return new UndoHistory.HistoricalAction(undo, redo, 0);
        }
        private void SelectionMovedOrResized(object sender, EventArgs e)
        {
            var selectedElements = MyWork.GetSelectedElements();
            var startingSelectedImages = imagesAtStartOfTheMove.Where(i => i is Image).Select(i => ((Image)i).clone()).ToList();
            var selectedStrokes = MyWork.GetSelectedStrokes().Select(stroke => stroke.Clone()).ToList();
            Trace.TraceInformation("MovingStrokes {0}", string.Join(",", selectedStrokes.Select(s => s.sum().checksum.ToString()).ToArray()));
            var undoStrokes = strokesAtTheStart.Select(stroke => stroke.Clone()).ToList();
            abosoluteizeStrokes(selectedStrokes);
            var ink = InkSelectionMovedOrResized(selectedStrokes, undoStrokes);
            var images = ImageSelectionMovedOrResized(selectedElements, startingSelectedImages);
            var text = TextMovedOrResized(selectedElements, boxesAtTheStart);
            Action undo = () =>
                {
                   
                    ClearAdorners();
                    ink.undo();
                    text.undo();
                    images.undo();
                    AddAdorners();
                };
            Action redo = () =>
                {
                    ClearAdorners();
                    ink.redo();
                    text.redo();
                    images.redo();
                    AddAdorners();
                };
            redo(); 
            UndoHistory.Queue(undo, redo);
        }
        private List<UIElement> GetSelectedClonedImages()
        {
            var selectedElements = new List<UIElement>();
            foreach (var element in MyWork.GetSelectedElements())
            {
                if (element is Image)
                {
                    selectedElements.Add(((Image) element).clone());
                    InkCanvas.SetLeft(selectedElements.Last(), InkCanvas.GetLeft(element));
                    InkCanvas.SetTop(selectedElements.Last(), InkCanvas.GetTop(element));
                }
            }
            return selectedElements;
        }

        private void selectionChanged(object sender, EventArgs e)
        {
            Dispatcher.adoptAsync(() =>
                                      {
                                          ClearAdorners();
                                          AddAdorners();
                                          if (MyWork.GetSelectedElements().Count == 0 && myTextBox != null) return;
                                          myTextBox = null;
                                          if (MyWork.GetSelectedElements().Count == 1 && MyWork.GetSelectedElements()[0] is MeTLTextBox)
                                              myTextBox = ((MeTLTextBox)MyWork.GetSelectedElements()[0]);
                                          updateTools();
                                      });
        }
       
        protected internal void AddAdorners()
        {
            ClearAdorners();
            var selectedStrokes = MyWork.GetSelectedStrokes();
            var selectedElements = MyWork.GetSelectedElements();
            if (selectedElements.Count == 0 && selectedStrokes.Count == 0) return;
            var publicStrokes = selectedStrokes.Where(s => s.tag().privacy.ToLower() == "public").ToList();
            var publicElements = selectedElements.Where(i => (((i is Image) && ((Image)i).tag().privacy.ToLower() == "public")) || ((i is Video) && ((Video)i).tag().privacy.ToLower() == "public")).ToList();
            var publicCount = publicStrokes.Count + publicElements.Count;
            var allElementsCount = selectedStrokes.Count + selectedElements.Count;
            string privacyChoice;
            if (publicCount == 0)
                privacyChoice = "show";
            else if (publicCount == allElementsCount)
                privacyChoice = "hide";
            else
                privacyChoice = "both";
            Commands.AddPrivacyToggleButton.Execute(new PrivacyToggleButton.PrivacyToggleButtonInfo(privacyChoice, allElementsCount != 0, MyWork.GetSelectionBounds()));
        }
        private void ClearAdorners()
        {
            Commands.RemovePrivacyAdorners.ExecuteAsync(null);
        }
#endregion
        #region ink
        private void SetDrawingAttributes(DrawingAttributes logicalAttributes)
        {
            if (logicalAttributes == null) return;
            if (me.ToLower() == "projector") return;
            var zoomCompensatedAttributes = logicalAttributes.Clone();
            try
            {
                zoomCompensatedAttributes.Width = logicalAttributes.Width * zoom;
                zoomCompensatedAttributes.Height = logicalAttributes.Height * zoom;
                var visualAttributes = logicalAttributes.Clone();
                visualAttributes.Width = logicalAttributes.Width * 2;
                visualAttributes.Height = logicalAttributes.Height * 2;
                MyWork.UseCustomCursor = true;
                Cursor = CursorExtensions.generateCursor(visualAttributes);
            }
            catch (Exception e) {
                Trace.TraceInformation("Cursor failed (no crash):", e.Message);
            }
            MyWork.DefaultDrawingAttributes = zoomCompensatedAttributes;
        }
        public List<Stroke> PublicStrokes
        {
             get { 
                var canvasStrokes = new List<Stroke>();
                canvasStrokes.AddRange(OtherWork.Strokes);
                canvasStrokes.AddRange(MyWork.Strokes);
                return canvasStrokes.Where(s => s.tag().privacy == Globals.PUBLIC).ToList();
            }
        }
        public List<Stroke> AllStrokes
        {
            get { 
                var canvasStrokes = new List<Stroke>();
                canvasStrokes.AddRange(OtherWork.Strokes);
                canvasStrokes.AddRange(MyWork.Strokes);
                return canvasStrokes;

            }
        }
        public void ReceiveDirtyStrokes(IEnumerable<TargettedDirtyElement> targettedDirtyStrokes)
        {
            if (targettedDirtyStrokes.Count() == 0) return;
            if (!(targettedDirtyStrokes.First().target.Equals(target)) || targettedDirtyStrokes.First().slide != Globals.slide) return;
            Dispatcher.adopt(delegate
            {
                dirtyStrokes(MyWork, targettedDirtyStrokes);
                dirtyStrokes(OtherWork, targettedDirtyStrokes);

            });
        }
        private void dirtyStrokes(InkCanvas canvas, IEnumerable<TargettedDirtyElement> targettedDirtyStrokes)
        {
            var dirtyChecksums = targettedDirtyStrokes.Select(t => t.identifier);
            var presentDirtyStrokes = canvas.Strokes.Where(s => dirtyChecksums.Contains(s.sum().checksum.ToString())).ToList();
            for (int i = 0; i < presentDirtyStrokes.Count(); i++)
            {
                var stroke = presentDirtyStrokes[i];
                strokes.Remove(stroke.sum());
                canvas.Strokes.Remove(stroke);

            }
        }
        public void ReceiveStrokes(IEnumerable<TargettedStroke> receivedStrokes)
        {
            if (receivedStrokes.Count() == 0) return;
            if (receivedStrokes.First().slide != Globals.slide) return;
            var strokeTarget = target;
            foreach (var targettedStroke in receivedStrokes.Where(targettedStroke => targettedStroke.target == strokeTarget))
            {
                if (targettedStroke.author == Globals.me)
                    MyWork.Strokes.Add(new PrivateAwareStroke(targettedStroke.stroke, strokeTarget));
                else if (targettedStroke.privacy == Globals.PUBLIC)
                    OtherWork.Strokes.Add(new PrivateAwareStroke(targettedStroke.stroke, strokeTarget));
            }
        }
        private void addStrokes(List<Stroke> strokes)
        {
            foreach(var stroke in strokes)
            {
                MyWork.Strokes.Add(new PrivateAwareStroke(stroke, target));
                doMyStrokeAddedExceptHistory(stroke, stroke.tag().privacy);
            }
        }
        private void removeStrokes(List<Stroke> strokes)
        {
             foreach(var stroke in strokes)
             {
                if(stroke.tag().privacy == Globals.PUBLIC)
                    MyWork.Strokes.Remove(new PrivateAwareStroke(stroke, target));
                else
                    MyWork.Strokes.Remove(new PrivateAwareStroke(stroke, target));
                 doMyStrokeRemovedExceptHistory(stroke);
            }
        }

        public void deleteSelectedItems(object obj)
        {
            deleteSelectedElements(null, null); 
        }
        private string determineOriginalPrivacy(string currentPrivacy)
        {
            if (currentPrivacy == Globals.PRIVATE)
                return Globals.PUBLIC;
            return Globals.PRIVATE;
        }
        private UndoHistory.HistoricalAction changeSelectedInkPrivacy(List<Stroke> selectedStrokes, string newPrivacy, string oldPrivacy)
        {
             Action redo = () =>
            {

                var newStrokes = new StrokeCollection();
                foreach (var stroke in selectedStrokes.Where(i => i != null && i.tag().privacy != newPrivacy))
                {
                    var oldTag = stroke.tag();

                    if (MyWork.Strokes.Where(s => s.sum().checksum == stroke.sum().checksum).Count() > 0)
                    {
                        MyWork.Strokes.Remove(MyWork.Strokes.Where(s => s.sum().checksum == stroke.sum().checksum).First());
                        doMyStrokeRemovedExceptHistory(stroke);
                    }
                    var newStroke = stroke.Clone();
                    newStroke.tag(new StrokeTag { author = oldTag.author, privacy = newPrivacy, startingSum = oldTag.startingSum });
                    if (MyWork.Strokes.Where(s => s.sum().checksum == newStroke.sum().checksum).Count() == 0)
                    {
                        newStrokes.Add(newStroke);
                        MyWork.Strokes.Add(newStroke);
                        doMyStrokeAddedExceptHistory(newStroke, newPrivacy);
                    }
                   
                }
                Dispatcher.adopt(() => MyWork.Select(MyWork.EditingMode == InkCanvasEditingMode.Select ? newStrokes : new StrokeCollection()));
            };
            Action undo = () =>
            {
                var newStrokes = new StrokeCollection();
                foreach (var stroke in selectedStrokes.Where(i => i is Stroke && i.tag().privacy != newPrivacy))
                {
                    var oldTag = stroke.tag();
                    var newStroke = stroke.Clone();
                    newStroke.tag(new StrokeTag { author = oldTag.author, privacy = newPrivacy, startingSum = oldTag.startingSum });

                    if (MyWork.Strokes.Where(s => s.sum().checksum == newStroke.sum().checksum).Count() > 0)
                    {
                        MyWork.Strokes.Remove(MyWork.Strokes.Where(s => s.sum().checksum == newStroke.sum().checksum).First());
                        doMyStrokeRemovedExceptHistory(newStroke);
                    }
                    if (MyWork.Strokes.Where(s => s.sum().checksum == stroke.sum().checksum).Count() == 0)
                    {
                        newStrokes.Add(newStroke);
                        MyWork.Strokes.Add(stroke);
                        doMyStrokeAddedExceptHistory(stroke, stroke.tag().privacy);
                    }
                }
            };   
            return new UndoHistory.HistoricalAction(undo, redo, 0);
        }
        private UndoHistory.HistoricalAction changeSelectedImagePrivacy(IEnumerable<UIElement> selectedElements, string newPrivacy, string oldPrivacy)
        {
            Action redo = () =>
            {

                foreach (Image image in selectedElements.Where(i => i is Image && ((Image)i).tag().privacy != newPrivacy))
                {
                    var oldTag = image.tag();
                    Commands.SendDirtyImage.ExecuteAsync(new TargettedDirtyElement (Globals.slide, image.tag().author, target, image.tag().privacy, image.tag().id));
                    oldTag.privacy = newPrivacy;
                    image.tag(oldTag);
                    var privateRoom = string.Format("{0}{1}", Globals.slide, image.tag().author);
                    if(newPrivacy.ToLower() == "private" && Globals.isAuthor && Globals.me != image.tag().author)
                        Commands.SneakInto.Execute(privateRoom);
                    Commands.SendImage.ExecuteAsync(new TargettedImage(Globals.slide, image.tag().author, target, newPrivacy, image));
                    if(newPrivacy.ToLower() == "private" && Globals.isAuthor && Globals.me != image.tag().author)
                        Commands.SneakOutOf.Execute(privateRoom);
                        
                }
            };
            Action undo = () =>
            {
                foreach (Image image in selectedElements.Where(i => i is Image && ((Image)i).tag().privacy != oldPrivacy))
                {
                    var oldTag = image.tag();
                    Commands.SendDirtyImage.ExecuteAsync(new TargettedDirtyElement (Globals.slide, image.tag().author, target, image.tag().privacy, image.tag().id));
                    oldTag.privacy = oldPrivacy;
                    image.tag(oldTag);
                    var privateRoom = string.Format("{0}{1}", Globals.slide, image.tag().author);
                    if(oldPrivacy.ToLower() == Globals.PRIVATE && Globals.isAuthor && Globals.me != image.tag().author)
                        Commands.SneakInto.Execute(privateRoom);
                    Commands.SendImage.ExecuteAsync(new TargettedImage(Globals.slide, image.tag().author, target, oldPrivacy, image));
                    if(oldPrivacy.ToLower() == Globals.PRIVATE && Globals.isAuthor && Globals.me != image.tag().author)
                        Commands.SneakOutOf.Execute(privateRoom);
                        
                }
            };   
            return new UndoHistory.HistoricalAction(undo, redo, 0);
        }
        private UndoHistory.HistoricalAction changeSelectedTextPrivacy(IEnumerable<UIElement> selectedElements,string newPrivacy)
        {
            if (selectedElements == null) throw new ArgumentNullException("selectedElements");
            Action redo = () => Dispatcher.adopt(delegate
                                                     {
                                                         var mySelectedElements = selectedElements.Where(e => e is MeTLTextBox).Select(t => ((MeTLTextBox)t).clone());
                                                         foreach (MeTLTextBox textBox in mySelectedElements.Where(i => i.tag(). privacy != newPrivacy))
                                                         {
                                                             var oldTag = textBox.tag();
                                                             oldTag.privacy = newPrivacy;
                                                             dirtyTextBoxWithoutHistory(textBox);
                                                             textBox.tag(oldTag);
                                                             sendTextWithoutHistory(textBox, newPrivacy);
                                                         }
                                                     });
            Action undo = () =>
                              {
                                  var mySelectedElements = selectedElements.Select(t => ((MeTLTextBox)t).clone());
                                  foreach (MeTLTextBox box in mySelectedElements)
                                  {
                                      if(MyWork.Children.ToList().Where(tb => ((MeTLTextBox)tb).tag().id == box.tag().id).ToList().Count != 0)
                                          dirtyTextBoxWithoutHistory((MeTLTextBox)MyWork.Children.ToList().Where(tb => ((MeTLTextBox)tb).tag().id == box.tag().id).ToList().First());
                                      sendTextWithoutHistory(box, box.tag().privacy);

                                  }
                              };
            return new UndoHistory.HistoricalAction(undo, redo, 0);
        }
        private void changeSelectedItemsPrivacy(string newPrivacy)
        {
            ClearAdorners();
            if (me == Globals.PROJECTOR) return;
            var selectedElements = new List<UIElement>();
            Dispatcher.adopt(() => selectedElements = GetSelectedClonedImages());
            var selectedStrokes = new List<Stroke>();
            Dispatcher.adopt(() => selectedStrokes = MyWork.GetSelectedStrokes().ToList());
            var oldPrivacy = determineOriginalPrivacy(newPrivacy);
            var ink = changeSelectedInkPrivacy(selectedStrokes, newPrivacy, oldPrivacy);
            var images = changeSelectedImagePrivacy(selectedElements, newPrivacy, oldPrivacy);
            var text = changeSelectedTextPrivacy(selectedElements, newPrivacy);
            Action redo = () =>
                              {
                                  ClearAdorners();
                                  ink.redo();
                                  text.redo();
                                  images.redo();
                                  AddAdorners();
                              };
            Action undo = () =>
                              {
                                  ClearAdorners();
                                  ink.undo();
                                  text.undo();
                                  images.undo();
                                  AddAdorners();
                              };
            redo();
            UndoHistory.Queue(undo, redo);
        }
     protected static void abosoluteizeStrokes(List<Stroke> selectedElements)
        {
            foreach (var stroke in selectedElements)
            {
                if (stroke.GetBounds().Top < 0)
                    stroke.GetBounds().Offset(0, Math.Abs(stroke.GetBounds().Top));
                if (stroke.GetBounds().Left < 0)
                    stroke.GetBounds().Offset(Math.Abs(stroke.GetBounds().Left), 0);
                if (stroke.GetBounds().Left < 0 && stroke.GetBounds().Top < 0)
                    stroke.GetBounds().Offset(Math.Abs(stroke.GetBounds().Left), Math.Abs(stroke.GetBounds().Top));
            }
           
        }
        private void singleStrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            e.Stroke.tag(new StrokeTag { author = Globals.me, privacy = Globals.privacy, isHighlighter = e.Stroke.DrawingAttributes.IsHighlighter });
            var privateAwareStroke = new PrivateAwareStroke(e.Stroke, target);
            MyWork.Strokes.Remove(e.Stroke);
            privateAwareStroke.startingSum(privateAwareStroke.sum().checksum);
            MyWork.Strokes.Add(privateAwareStroke);
            doMyStrokeAdded(privateAwareStroke);
            Commands.RequerySuggested(Commands.Undo);
        }
        public void doMyStrokeAdded(Stroke stroke)
        {
            doMyStrokeAdded(stroke, privacy);
        }
        public void doMyStrokeAdded(Stroke stroke, string intendedPrivacy)
        {
            doMyStrokeAddedExceptHistory(stroke, intendedPrivacy);
            var thisStroke = stroke.Clone();
            UndoHistory.Queue(
                () =>
                {
                    ClearAdorners();
                    var existingStroke = MyWork.Strokes.Where(s => s.sum().checksum == thisStroke.sum().checksum).FirstOrDefault();
                    if (existingStroke != null)
                    {
                        MyWork.Strokes.Remove(existingStroke);
                        doMyStrokeRemovedExceptHistory(existingStroke);
                    }
                },
                () =>
                {
                    ClearAdorners();
                    if (MyWork.Strokes.Where(s => s.sum().checksum == thisStroke.sum().checksum).Count() == 0)
                    {
                        MyWork.Strokes.Add(thisStroke);
                        doMyStrokeAddedExceptHistory(thisStroke, thisStroke.tag().privacy);
                    }
                    if(MyWork.EditingMode == InkCanvasEditingMode.Select)
                        MyWork.Select(new StrokeCollection(new [] {thisStroke}));
                    AddAdorners();
                });
        }
        private void erasingStrokes(object sender, InkCanvasStrokeErasingEventArgs e)
        {
            try
            {
                Trace.TraceInformation("ErasingStroke {0}", e.Stroke.sum().checksum);
                doMyStrokeRemoved(e.Stroke);
            }
            catch
            {
                //Tag can be malformed if app state isn't fully logged in
            }
        }
        private void doMyStrokeRemoved(Stroke stroke)
        {
            ClearAdorners();
            var canvas = MyWork;
            var undo = new Action(() =>
                                 {
                                     if (canvas.Strokes.Where(s => s.sum().checksum == stroke.sum().checksum).Count() == 0)
                                     {
                                         canvas.Strokes.Add(stroke);
                                         doMyStrokeAddedExceptHistory(stroke, stroke.tag().privacy);
                                     }
                                 });
            var redo = new Action(() =>
                                 {
                                     if (canvas.Strokes.Where(s => s.sum().checksum == stroke.sum().checksum).Count() > 0)
                                     {
                                         canvas.Strokes.Remove(canvas.Strokes.Where(s=> s.sum().checksum == stroke.sum().checksum).First());
                                         doMyStrokeRemovedExceptHistory(stroke);
                                     }
                                 });
            redo();
            UndoHistory.Queue(undo, redo);
        }

        private void doMyStrokeRemovedExceptHistory(Stroke stroke)
        {
            var sum = stroke.sum().checksum.ToString();
            strokes.Remove(stroke.sum());
            Commands.SendDirtyStroke.Execute(new MeTLLib.DataTypes.TargettedDirtyElement(Globals.slide, stroke.tag().author,target,stroke.tag().privacy,sum));
        }
        private void doMyStrokeAddedExceptHistory(Stroke stroke, string thisPrivacy)
        {
            if (!strokes.Contains(stroke.sum()))
                strokes.Add(stroke.sum());
            stroke.tag(new StrokeTag { author = stroke.tag().author, privacy = thisPrivacy, isHighlighter = stroke.DrawingAttributes.IsHighlighter });
            SendTargettedStroke(stroke, thisPrivacy);
        }
        public void SendTargettedStroke(Stroke stroke, string thisPrivacy)
        {
            if (!stroke.shouldPersist()) return;
            var privateRoom = string.Format("{0}{1}", Globals.slide, stroke.tag().author);
            if(thisPrivacy.ToLower() == "private" && Globals.isAuthor && Globals.me != stroke.tag().author)
                Commands.SneakInto.Execute(privateRoom);
            Commands.SendStroke.Execute(new TargettedStroke(Globals.slide,stroke.tag().author,target,stroke.tag().privacy,stroke, stroke.tag().startingSum));
            if (thisPrivacy.ToLower() == "private" && Globals.isAuthor && Globals.me != stroke.tag().author)
                Commands.SneakOutOf.Execute(privateRoom);
        }
#endregion
        #region Images
        
        private void sendThisElement(UIElement element)
        {
            switch (element.GetType().ToString())
            {
                case "System.Windows.Controls.Image":
                    var newImage = (Image)element;
                    newImage.UpdateLayout();

                    Commands.SendImage.Execute(new TargettedImage(Globals.slide, Globals.me, target, newImage.tag().privacy, newImage));
                    break;
               
            }
        }
        private void dirtyThisElement(UIElement element)
        {
            var thisImage = (Image)element;
            var dirtyElement = new TargettedDirtyElement(Globals.slide, Globals.me, target,thisImage.tag().privacy, thisImage.tag().id );
            switch (element.GetType().ToString())
            {
                case "System.Windows.Controls.Image":
                    var image = (System.Windows.Controls.Image)element;
                    ApplyPrivacyStylingToElement(image, image.tag().privacy);
                    Commands.SendDirtyImage.Execute(dirtyElement);
                    break;
            }

        }
        public void ReceiveImages(IEnumerable<TargettedImage> images)
        {
            foreach (var image in images)
            {
                TargettedImage image1 = image;
                if(image.author == Globals.me)
                    Dispatcher.adoptAsync(() => AddImage(MyWork, image1.image));
                else
                    Dispatcher.adoptAsync(() => AddImage(OtherWork, image1.image));
            }
            ensureAllImagesHaveCorrectPrivacy();
        }

        private void AddImage(InkCanvas canvas, Image image)
        {
            if(!existsOnCanvas(canvas, image))
            {
                Panel.SetZIndex(image, 2);
                canvas.Children.Add(image);
            }
        }
        public void ReceiveDirtyImage(TargettedDirtyElement element)
        {
            if (!(element.target.Equals(target))) return;
            if (element.slide != Globals.slide) return;
            Dispatcher.adoptAsync(() => dirtyImage(element.identifier));
        }
        private void dirtyImage(string imageId)
        {
            dirtyImageOnCanvas(MyWork, imageId);
            dirtyImageOnCanvas(OtherWork, imageId);
        
        }
        private void dirtyImageOnCanvas(InkCanvas canvas, string imageId)
        {
            for (int i = 0; i < canvas.Children.Count; i++)
            {
                if (canvas.Children[i] is Image)
                {
                    var currentImage = (Image)canvas.Children[i];
                    if (imageId.Equals(currentImage.tag().id))
                        canvas.Children.Remove(currentImage);
                }
            }
        }
        private bool existsOnCanvas(InkCanvas canvas, Image testImage)
        {
            return canvas.Children.OfType<Image>().Any(image => imageCompare(image, testImage));
        }
        private static bool imageCompare(Image image, Image currentImage)
        {
            if (System.Windows.Controls.Canvas.GetTop(currentImage) == System.Windows.Controls.Canvas.GetTop(image))
                return false;
            if (System.Windows.Controls.Canvas.GetLeft(currentImage) == System.Windows.Controls.Canvas.GetLeft(image))
                return false;
            if (image.Source.ToString() != currentImage.Source.ToString())
                return false;
            if (image.tag().id != currentImage.tag().id)
                return false;
            return true;
        }
        private void ensureAllImagesHaveCorrectPrivacy()
        {
            Dispatcher.adoptAsync(delegate
            {
                var images = MyWork.Children.OfType<Image>().ToList();
                foreach (Image image in images)
                    ApplyPrivacyStylingToElement(image, image.tag().privacy);

            });
        }
        protected void ApplyPrivacyStylingToElement(FrameworkElement element, string privacy)
        {
            if (!Globals.conversationDetails.Permissions.studentCanPublish && !Globals.isAuthor)
            {
                RemovePrivacyStylingFromElement(element);
                return;
            }
            if (privacy != "private")
            {
                RemovePrivacyStylingFromElement(element);
                return;
            }

            applyShadowEffectTo(element, Colors.Black);
        }
        public FrameworkElement applyShadowEffectTo(FrameworkElement element, Color color)
        {
            element.Effect = new DropShadowEffect { BlurRadius = 50, Color = color, ShadowDepth = 0, Opacity = 1 };
            element.Opacity = 0.7;
            return element;
        }
        protected void RemovePrivacyStylingFromElement(FrameworkElement element)
        {
            element.Effect = null;
            element.Opacity = 1;
        }
        #endregion
        #region imagedrop
        private void addImageFromDisk(object obj)
        {
            addResourceFromDisk((files) =>
            {
                int i = 0;
                foreach (var file in files)
                    handleDrop(file, new Point(0, 0), i++);
            });
        }
        private void uploadFile(object _obj)
        {
            addResourceFromDisk("All files (*.*)|*.*", (files) =>
                                    {
                                        foreach (var file in files)
                                        {
                                            uploadFileForUse(file);
                                        }
                                    });
        }
        private void addImageFromQuizSnapshot(ImageDropParameters parameters)
        {
            if (target == "presentationSpace" && me != "projector")
                handleDrop(parameters.File, parameters.Location, 1);
        }
        private void addResourceFromDisk(Action<IEnumerable<string>> withResources)
        {
            const string filter = "Image files(*.jpeg;*.gif;*.bmp;*.jpg;*.png)|*.jpeg;*.gif;*.bmp;*.jpg;*.png|All files (*.*)|*.*";
            addResourceFromDisk(filter, withResources);
        }

        private void addResourceFromDisk(string filter, Action<IEnumerable<string>> withResources)
        {
            if (target == "presentationSpace" && canEdit && me != "projector")
            {
                string initialDirectory = "c:\\";
                foreach (var path in new[] { Environment.SpecialFolder.MyPictures, Environment.SpecialFolder.MyDocuments, Environment.SpecialFolder.DesktopDirectory, Environment.SpecialFolder.MyComputer })
                    try
                    {
                        initialDirectory = Environment.GetFolderPath(path);
                        break;
                    }
                    catch (Exception)
                    {
                    }
                var fileBrowser = new OpenFileDialog
                                             {
                                                 InitialDirectory = initialDirectory,
                                                 Filter = filter,
                                                 FilterIndex = 1,
                                                 RestoreDirectory = true
                                             };
                DisableDragDrop(); 
                var dialogResult = fileBrowser.ShowDialog(Window.GetWindow(this));
                EnableDragDrop();

                if (dialogResult == true)
                    withResources(fileBrowser.FileNames);
            }
        }

        private void DisableDragDrop()
        {
            DragOver -= ImageDragOver;
            Drop -= ImagesDrop;
            DragOver += ImageDragOverCancel;
        }

        private void EnableDragDrop()
        {
            DragOver -= ImageDragOverCancel;
            DragOver += ImageDragOver;
            Drop += ImagesDrop;
        }

        protected void ImageDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            var fileNames = e.Data.GetData(DataFormats.FileDrop, true) as string[];
            if (fileNames == null) return;
            foreach (string fileName in fileNames)
            {
                FileType type = GetFileType(fileName);
                if (new[] { FileType.Image}.Contains(type))
                    e.Effects = DragDropEffects.Copy;
            }
            e.Handled = true;
        }
        protected void ImagesDrop(object sender, DragEventArgs e)
        {
            var tempImagePath = "temporaryDragNDropFileData.bmp";
            bool needsToRemoveTempFile = false;
            var validFormats = e.Data.GetFormats();
            var fileNames = new string[0];
            var allTypes = validFormats.Select(vf => {
                var outputData = "";
                try
                {
                    var rawData = e.Data.GetData(vf);
                    if (rawData is MemoryStream)
                    {
                        outputData = System.Text.Encoding.UTF8.GetString(((MemoryStream)e.Data.GetData(vf)).ToArray());
                    }
                    else if (rawData is String)
                    {
                        outputData = (String)rawData;
                    }
                    else if (rawData is Byte[])
                    {
                        outputData = System.Text.Encoding.UTF8.GetString((Byte[])rawData);
                    }
                    else throw new Exception("data was in an unexpected format: (" + outputData.GetType() + ") - "+outputData);
                }
                catch (Exception ex)
                {
                    outputData = "getData failed with exception (" + ex.Message + ")";
                }
                return vf + ":  "+ outputData;
            }).ToList();
            if (validFormats.Contains(DataFormats.FileDrop))
            {
                //local files will deliver filenames.  
                fileNames = e.Data.GetData(DataFormats.FileDrop, true) as string[];
            }
            else if (validFormats.Contains("text/html"))
            {
            }
            else if (validFormats.Contains("UniformResourceLocator"))
            {
                //dragged pictures from chrome will deliver the urls of the objects.  Firefox and IE don't drag images.
                var url = (MemoryStream)e.Data.GetData("UniformResourceLocator");
                if (url != null)
                {
                    fileNames = new string[] { System.Text.Encoding.Default.GetString(url.ToArray()) };
                }
            }
             
            if (fileNames.Length == 0)
            {
                MessageBox.Show("Cannot drop this onto the canvas");
                return;
            }
            Commands.SetLayer.ExecuteAsync("Insert");
            var pos = e.GetPosition(this);
            var origX = pos.X;
            //lets try for a 4xN grid
            var height = 0.0;
            for (var i = 0; i < fileNames.Count(); i++)
            {
                var filename = fileNames[i];
                var image = createImageFromUri(new Uri(filename, UriKind.RelativeOrAbsolute));
                Commands.ImageDropped.Execute(new ImageDrop { Filename = filename, Point = pos, Target = target, Position = i });
                pos.X += image.Width + 30;
                if (image.Height > height) height = image.Height;
                if ((i + 1) % 4 == 0)
                {
                    pos.X = origX;
                    pos.Y += (height + 30);
                    height = 0.0;
                }
            }
            if (needsToRemoveTempFile)
                File.Delete(tempImagePath);
            e.Handled = true;
        }
        protected void ImageDragOverCancel(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        public void handleDrop(string fileName, Point pos, int count)
        {
            FileType type = GetFileType(fileName);
            switch (type)
            {
                case FileType.Image:
                    dropImageOnCanvas(fileName, pos, count);
                    break;
                case FileType.Video:
                    break;
                default:
                    uploadFileForUse(fileName);
                    break;
            }
        }
        private const int KILOBYTE = 1024;
        private const int MEGABYTE = 1024 * KILOBYTE;
        private bool isFileLessThanXMB(string filename, int size)
        {
            if (filename.StartsWith("http")) return true;
            var info = new FileInfo(filename);
            if (info.Length > size * MEGABYTE)
            {
                return false;
            }
            return true;
        }
        private int fileSizeLimit = 50;
        private void uploadFileForUse(string unMangledFilename)
        {
            string filename = unMangledFilename + ".MeTLFileUpload";
            if (filename.Length > 260)
            {
                MessageBox.Show("Sorry, your filename is too long, must be less than 260 characters");
                return;
            }
            if (isFileLessThanXMB(unMangledFilename, fileSizeLimit))
            {
                var worker = new BackgroundWorker();
                worker.DoWork += (s, e) =>
                 {
                     File.Copy(unMangledFilename, filename);
                     MeTLLib.ClientFactory.Connection().UploadAndSendFile(
                         new MeTLStanzas.LocalFileInformation(Globals.slide, Globals.me, target, "public", filename, Path.GetFileNameWithoutExtension(filename), false, new FileInfo(filename).Length, DateTimeFactory.Now().Ticks.ToString()));
                     File.Delete(filename);
                 };
                worker.RunWorkerCompleted += (s, a) => Dispatcher.Invoke(DispatcherPriority.Send,
                                                                                   (Action)(() => MessageBox.Show(string.Format("Finished uploading {0}.", unMangledFilename))));
                worker.RunWorkerAsync();
            }
            else
            {
                MessageBox.Show(String.Format("Sorry, your file is too large, must be less than {0}mb", fileSizeLimit));
                return;
            }
        }
        public void dropImageOnCanvas(string fileName, Point pos, int count)
        {
            if (!isFileLessThanXMB(fileName, fileSizeLimit))
            {
                MessageBox.Show(String.Format("Sorry, your file is too large, must be less than {0}mb", fileSizeLimit));
                return;
            }
            Dispatcher.adopt(() =>
            {
                System.Windows.Controls.Image image = null;
                try
                {
                    image = createImageFromUri(new Uri(fileName, UriKind.RelativeOrAbsolute));
                }
                catch (Exception e)
                {
                    MessageBox.Show("Sorry could not create an image from this file :" + fileName + "\n Error: " + e.Message);
                    return;
                }
                if (image == null)
                    return;
                InkCanvas.SetLeft(image, pos.X);
                InkCanvas.SetTop(image, pos.Y);
                image.tag(new ImageTag(Globals.me, privacy, generateId(), false, 0));
                if (!fileName.StartsWith("http"))
                    MeTLLib.ClientFactory.Connection().UploadAndSendImage(new MeTLStanzas.LocalImageInformation(Globals.slide, Globals.me, target, privacy, image, fileName, false));
                else
                    MeTLLib.ClientFactory.Connection().SendImage(new TargettedImage(Globals.slide, Globals.me, target, privacy, image));
                var myImage = image.clone();
                Action undo = () =>
                                  {
                                      ClearAdorners();
                                      if (MyWork.Children.Contains(myImage))
                                          MyWork.Children.Remove(myImage);
                                      dirtyThisElement(myImage);
                                  };
                Action redo = () =>
                {
                    ClearAdorners();
                    InkCanvas.SetLeft(myImage, pos.X);
                    InkCanvas.SetTop(myImage, pos.Y);
                    if (!MyWork.Children.Contains(myImage))
                        MyWork.Children.Add(myImage);
                    sendThisElement(myImage);

                    MyWork.Select(new[] { myImage });
                    AddAdorners();
                };
                UndoHistory.Queue(undo, redo);
                MyWork.Children.Add(image);
            });
        }
        public static System.Windows.Controls.Image createImageFromUri(Uri uri)
        {
            var image = new System.Windows.Controls.Image();
            var jpgFrame = BitmapFrame.Create(uri);
            image.Source = jpgFrame;
            // images with a high dpi eg 300 were being drawn relatively small compared to images of similar size with dpi ~100
            // which is correct but not what people expect.
            // image size is determined from dpi
            image.Height = jpgFrame.Height;
            image.Width = jpgFrame.Width;
            // image size will match reported size
            //image.Height = jpgFrame.PixelHeight;
            //image.Width = jpgFrame.PixelWidth;
            image.Stretch = Stretch.Uniform;
            image.StretchDirection = StretchDirection.Both;
            image.Margin = new Thickness(5);
            return image;
        }
        public static FileType GetFileType(string fileName)
        {
            string extension = System.IO.Path.GetExtension(fileName).ToLower();
            var imageExtensions = new List<string>() { ".jpg", ".jpeg", ".bmp", ".gif", ".png", ".dib" };
            var videoExtensions = new List<string>() { ".wmv" };

            if (imageExtensions.Contains(extension))
                return FileType.Image;
            if (videoExtensions.Contains(extension))
                return FileType.Video;
            return FileType.NotSupported;
        }
        
        #endregion
        //text
        #region Text
        private void placeCursor(object sender, MouseButtonEventArgs e)
        {
            if (MyWork.EditingMode != InkCanvasEditingMode.None) return;
            if (!canEdit) return;
            var pos = e.GetPosition(this);
            MeTLTextBox box = createNewTextbox();
            MyWork.Children.Add(box);
            InkCanvas.SetLeft(box, pos.X);
            InkCanvas.SetTop(box, pos.Y);
            myTextBox = box;
            box.Focus();
        }
        public void DoText(TargettedTextBox targettedBox)
        {
            Dispatcher.adoptAsync(delegate
                                      {
                                          if (targettedBox.target != target) return;
                                          if (targettedBox.slide == Globals.slide &&
                                              (targettedBox.privacy == Globals.PUBLIC || targettedBox.author == Globals.me))
                                          {

                                              var box = targettedBox.box.toMeTLTextBox();
                                              removeDoomedTextBoxes(targettedBox);
                                              if (targettedBox.author == Globals.me)
                                                  MyWork.Children.Add(applyDefaultAttributes(box));
                                              else
                                              {
                                                  addBoxToOtherCanvas(box);
                                              }
                                              if (!(targettedBox.author == Globals.me && focusable))
                                                  box.Focusable = false;
                                              ApplyPrivacyStylingToElement(box, targettedBox.privacy);
                                          }
                                      });

        }

        private void addBoxToOtherCanvas(MeTLTextBox box)
        {
            var boxToAdd = applyDefaultAttributes(box);
            boxToAdd.Focusable = false;
            OtherWork.Children.Add(boxToAdd);
        }

        private MeTLTextBox applyDefaultAttributes(MeTLTextBox box)
        {
            box.AcceptsReturn = true;
            box.TextWrapping = TextWrapping.WrapWithOverflow;
            box.GotFocus += textboxGotFocus;
            box.LostFocus += textboxLostFocus;
            box.PreviewTextInput += box_PreviewTextInput;
            box.TextChanged += SendNewText;
            box.IsUndoEnabled = false;
            box.UndoLimit = 0;
            box.BorderThickness = new Thickness(0);
            box.BorderBrush = new SolidColorBrush(Colors.Transparent);
            box.Background = new SolidColorBrush(Colors.Transparent);
            box.Focusable = canEdit && canFocus;
            return box;
        }
        private void SendNewText(object sender, TextChangedEventArgs e)
        {
            if (originalText == null) return; 
            var box = (MeTLTextBox)sender;
            var undoText = originalText.Clone().ToString();
            var redoText = box.Text.Clone().ToString();
            ApplyPrivacyStylingToElement(box, box.tag().privacy);
            box.Height = Double.NaN;
            var mybox = box.clone();
            Action undo = () =>
            {
                ClearAdorners();
                var myText = undoText.Clone().ToString();
                dirtyTextBoxWithoutHistory(mybox);
                mybox.TextChanged -= SendNewText;
                mybox.Text = myText;
                sendTextWithoutHistory(mybox, mybox.tag().privacy);
                mybox.TextChanged += SendNewText;
            };
            Action redo = () =>
            {
                ClearAdorners();
                var myText = redoText;
                mybox.TextChanged -= SendNewText;
                mybox.Text = myText;
                dirtyTextBoxWithoutHistory(mybox);
                sendTextWithoutHistory(mybox, mybox.tag().privacy);
                mybox.TextChanged += SendNewText;
            }; 
            UndoHistory.Queue(undo, redo);


            mybox.Text = redoText;
            mybox.TextChanged += SendNewText;
            if (typingTimer == null)
            {
                typingTimer = new Timer(delegate
                {
                    Dispatcher.adoptAsync(delegate
                                                    {
                                                        sendTextWithoutHistory((MeTLTextBox)sender, privacy);
                                                        typingTimer = null;
                                                    });
                }, null, 600, Timeout.Infinite);
            }
            else
            {
                GlobalTimers.resetSyncTimer();
                typingTimer.Change(600, Timeout.Infinite);
            }

        }
        protected static void abosoluteizeElements(List<UIElement> selectedElements)
        {
            foreach (FrameworkElement element in selectedElements)
            {
                if(InkCanvas.GetLeft(element) < 0)
                    InkCanvas.SetLeft(element, (element is TextBox) ? 0 : 0 + ((element.Width != element.ActualWidth) ? ((element.Width - element.ActualWidth)/2) : 0));
                else
                    InkCanvas.SetLeft(element, (element is TextBox) ? InkCanvas.GetLeft(element) : InkCanvas.GetLeft(element) + ((element.Width != element.ActualWidth) ? ((element.Width - element.ActualWidth)/2) : 0));
                if(InkCanvas.GetTop(element) < 0)
                    InkCanvas.SetTop(element, (element is TextBox) ? 0 : 0 + ((element.Height != element.ActualHeight) ? ((element.Height - element.ActualHeight)/2) : 0));
                else
                    InkCanvas.SetTop(element, (element is TextBox) ? InkCanvas.GetTop(element) : InkCanvas.GetTop(element) + ((element.Height != element.ActualHeight) ? ((element.Height - element.ActualHeight)/2) : 0));

            }
        }
        private void resetTextbox(object obj)
        {
            if (myTextBox == null && MyWork.GetSelectedElements().Count != 1) return;
            if(myTextBox == null)
                myTextBox = (MeTLTextBox)MyWork.GetSelectedElements().Where(b => b is MeTLTextBox).First();
            var currentTextBox = myTextBox;
            var undoInfo = getInfoOfBox(currentTextBox);
            Action undo = () =>
            {
                ClearAdorners();
                applyStylingTo(currentTextBox, undoInfo);
                sendTextWithoutHistory(currentTextBox, currentTextBox.tag().privacy);
                updateTools();
            };
            Action redo = () =>
                              {
                                  ClearAdorners();
                                  resetText(currentTextBox);
                                  updateTools();
                              };
            UndoHistory.Queue(undo, redo);
            redo();
        }
        private void resetText(MeTLTextBox box)
        {
            RemovePrivacyStylingFromElement(box);
            currentColor = Colors.Black;
            box.FontWeight = FontWeights.Normal;
            box.FontStyle = FontStyles.Normal;
            box.TextDecorations = new TextDecorationCollection();
            box.FontFamily = new FontFamily("Arial");
            box.FontSize = 24;
            box.Foreground = Brushes.Black;
            var info = new TextInformation
                           {
                               family = box.FontFamily,
                               size = box.FontSize,
                           };
            Commands.TextboxFocused.ExecuteAsync(info);
            sendTextWithoutHistory(box, box.tag().privacy);
        }
        private void updateStyling(TextInformation info)
        {
            try
            {
                currentColor = info.color;
                currentFamily = info.family;
                currentSize = info.size;
                if (myTextBox != null)
                {
                    var caret = myTextBox.CaretIndex;
                    var currentTextBox = myTextBox.clone();
                    var oldInfo = getInfoOfBox(currentTextBox);

                    Action undo = () =>
                                      {
                                          ClearAdorners();
                                          var currentInfo = oldInfo;
                                          var activeTextbox = ((MeTLTextBox) MyWork.Children.ToList().Where( c => ((MeTLTextBox)c).tag().id == currentTextBox.tag().id). FirstOrDefault());
                                          activeTextbox.TextChanged -= SendNewText;
                                          applyStylingTo(activeTextbox, currentInfo);
                                          Commands.TextboxFocused.ExecuteAsync(currentInfo);
                                          AddAdorners();
                                          sendTextWithoutHistory(activeTextbox, currentTextBox.tag().privacy);
                                          activeTextbox.TextChanged += SendNewText;
                                      };
                    Action redo = () =>
                                      {
                                          ClearAdorners();
                                          var currentInfo = info;
                                          var activeTextbox = ((MeTLTextBox) MyWork.TextChildren().ToList().Where( c => ((MeTLTextBox)c).tag().id == currentTextBox.tag().id). FirstOrDefault());
                                          activeTextbox.TextChanged -= SendNewText;
                                          applyStylingTo(activeTextbox, currentInfo);
                                          Commands.TextboxFocused.ExecuteAsync(currentInfo);
                                          AddAdorners();
                                          sendTextWithoutHistory(activeTextbox, currentTextBox.tag().privacy);
                                          activeTextbox.TextChanged += SendNewText;

                                      };
                    UndoHistory.Queue(undo, redo);
                    redo();
                    myTextBox.GotFocus -= textboxGotFocus;
                    myTextBox.CaretIndex = caret;
                    myTextBox.Focus();
                    myTextBox.GotFocus += textboxGotFocus;
                }
                else if (MyWork.GetSelectedElements().Count > 0)
                {
                    var originalElements = MyWork.GetSelectedElements().ToList().Select(tb => ((MeTLTextBox)tb).clone());
                    Action undo = () =>
                                      {
                                          ClearAdorners();
                                          foreach (var originalElement in originalElements)
                                          {
                                              dirtyTextBoxWithoutHistory(originalElement);
                                              sendTextWithoutHistory(originalElement, originalElement.tag().privacy);
                                          }
                                          AddAdorners();
                                      };
                    Action redo = () =>
                                      {
                                          ClearAdorners();
                                          var ids = originalElements.Select(b => b.tag().id);
                                          var selection = MyWork.TextChildren().ToList().Where(b => ids.Contains(((MeTLTextBox)b).tag().id));
                                          foreach (MeTLTextBox currentTextBox in selection)
                                          {
                                              applyStylingTo(currentTextBox, info);
                                              sendTextWithoutHistory(currentTextBox, currentTextBox.tag().privacy);
                                          }
                                          AddAdorners();
                                      };
                    UndoHistory.Queue(undo, redo);
                    redo();

                }
            }
            catch (Exception e)
            {
                Logger.Fixed(string.Format("There was an ERROR:{0} INNER:{1}, it is now fixed", e, e.InnerException));
            }

        }
        private static void applyStylingTo(MeTLTextBox currentTextBox, TextInformation info)
        {
            currentTextBox.FontStyle = info.italics ? FontStyles.Italic : FontStyles.Normal;
            currentTextBox.FontWeight = info.bold ? FontWeights.Bold : FontWeights.Normal;
            currentTextBox.TextDecorations = new TextDecorationCollection();
            if(info.underline)
                currentTextBox.TextDecorations = TextDecorations.Underline;
            else if(info.strikethrough)
                currentTextBox.TextDecorations= TextDecorations.Strikethrough;
            currentTextBox.FontSize = info.size;
            currentTextBox.FontFamily = info.family;
            currentTextBox.Foreground = new SolidColorBrush(info.color);
        }
        private static TextInformation getInfoOfBox(MeTLTextBox box)
        {
            var underline = false;
            var strikethrough = false;
            if(box.TextDecorations.Count > 0)
            {
                underline = box.TextDecorations.First().Location.ToString().ToLower() == "underline";
                strikethrough = box.TextDecorations.First().Location.ToString().ToLower() == "strikethrough";
            }
            return new TextInformation
                       {
                           bold = box.FontWeight == FontWeights.Bold,
                           italics = box.FontStyle == FontStyles.Italic,
                           size = box.FontSize,
                           underline = underline,
                           strikethrough = strikethrough,
                           family = box.FontFamily,
                           color = ((SolidColorBrush) box.Foreground).Color
                       };
        }
        private void removeBox(MeTLTextBox box)
        {
            myTextBox = box;
            dirtyTextBoxWithoutHistory(box);
            myTextBox = null;
        }

        private void sendBox(MeTLTextBox box)
        {
            myTextBox = box;
            if(MyWork.Children.ToList().Where(c => ((MeTLTextBox)c).tag().id == box.tag().id).ToList().Count == 0)
                MyWork.Children.Add(box);
            box.TextChanged += SendNewText;
            box.PreviewTextInput += box_PreviewTextInput;
            sendTextWithoutHistory(box, box.tag().privacy);
        }
        public void sendTextWithoutHistory(MeTLTextBox box, string thisPrivacy)
        {
            sendTextWithoutHistory(box, thisPrivacy, Globals.slide);
        }
        public void sendTextWithoutHistory(MeTLTextBox box, string thisPrivacy, int slide)
        {
            RemovePrivacyStylingFromElement(box);
            if (box.tag().privacy != Globals.privacy)
                dirtyTextBoxWithoutHistory(box);
            var oldTextTag = box.tag();
            var newTextTag = new TextTag(oldTextTag.author, thisPrivacy, oldTextTag.id);
            box.tag(newTextTag);
            var privateRoom = string.Format("{0}{1}", Globals.slide, box.tag().author);
            if(thisPrivacy.ToLower() == "private" && Globals.isAuthor && Globals.me != box.tag().author)
                Commands.SneakInto.Execute(privateRoom);
            Commands.SendTextBox.ExecuteAsync(new TargettedTextBox(slide, box.tag().author, target, thisPrivacy, box));
            if(thisPrivacy.ToLower() == "private" && Globals.isAuthor && Globals.me != box.tag().author)
                Commands.SneakOutOf.Execute(privateRoom);
        }
        private void dirtyTextBoxWithoutHistory(MeTLTextBox box)
        {
            RemovePrivacyStylingFromElement(box);
            RemoveTextboxWithTag(box.tag().id);
            Commands.SendDirtyText.ExecuteAsync(new TargettedDirtyElement(Globals.slide, box.tag().author, target, box.tag().privacy, box.tag().id));
        }
        private void box_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            originalText = ((MeTLTextBox)sender).Text;
            e.Handled = false;
        }
        
        private void textboxLostFocus(object sender, RoutedEventArgs e)
        {
            var box = (MeTLTextBox)sender;
            var currentTag = box.tag();
            ClearAdorners();
            if (currentTag.privacy != Globals.privacy)
            {
                Commands.SendDirtyText.ExecuteAsync(new TargettedDirtyElement(Globals.slide, Globals.me, target, currentTag.privacy, currentTag.id));
                currentTag.privacy = privacy;
                box.tag(currentTag);
                Commands.SendTextBox.ExecuteAsync(new TargettedTextBox(Globals.slide, Globals.me, target, currentTag.privacy, box));
            }
            myTextBox = null;
            requeryTextCommands();
            if (box.Text.Length == 0)
            {
                if(checkCanvasForBox(MyWork, box))
                    MyWork.Children.Remove(box);
                else
                    OtherWork.Children.Remove(box);
            }
            else
                setAppropriatePrivacyHalo(box);
        }
        private void setAppropriatePrivacyHalo(MeTLTextBox box)
        {
            if (!(MyWork.Children.Contains(box) && OtherWork.Children.Contains(box))) return;
            ApplyPrivacyStylingToElement(box, privacy);
        }
        public void RemoveTextboxWithTag(string tag)
        {
            for (var i = 0; i < MyWork.Children.Count; i++)
            {
                if (MyWork.Children[i] is TextBox && ((TextBox)MyWork.Children[i]).Tag.ToString() == tag)
                    MyWork.Children.Remove(MyWork.Children[i]);
            }
           for (var i = 0; i < OtherWork.Children.Count; i++)
            {
                if (OtherWork.Children[i] is TextBox && ((TextBox)OtherWork.Children[i]).Tag.ToString() == tag)
                    OtherWork.Children.Remove(OtherWork.Children[i]);
            }
        }
        private static void requeryTextCommands()
        {
            Commands.RequerySuggested(new []{
                                                Commands.UpdateTextStyling,
                                                Commands.RestoreTextDefaults
                                            });
        }
        private void textboxGotFocus(object sender, RoutedEventArgs e)
        {
            if (((MeTLTextBox)sender).tag().author != Globals.me) return; //cannot edit other peoples textboxes
            myTextBox = (MeTLTextBox)sender;
            updateTools();
            requeryTextCommands();
            MyWork.Select(new List<UIElement>());
            originalText = myTextBox.Text;
            Commands.ChangeTextMode.Execute("None");
        }
        private void updateTools()
        {
            var info = new TextInformation
            {
                family = defaultFamily,
                size = defaultSize,
                bold = false,
                italics = false,
                strikethrough = false,
                underline = false,
                color = Colors.Black
            };
            if (myTextBox != null)
            {
                info = new TextInformation
                {
                    family = myTextBox.FontFamily,
                    size = myTextBox.FontSize,
                    bold = myTextBox.FontWeight == FontWeights.Bold,
                    italics = myTextBox.FontStyle == FontStyles.Italic,
                    color = ((SolidColorBrush)myTextBox.Foreground).Color
                };
                
                if (myTextBox.TextDecorations.Count > 0)
                {
                    info.strikethrough = myTextBox.TextDecorations.First().Location.ToString().ToLower() == "strikethrough";
                    info.underline = myTextBox.TextDecorations.First().Location.ToString().ToLower() == "underline";
                }
                info.isPrivate = myTextBox.tag().privacy.ToLower() == "private" ? true : false; 
            }
            Commands.TextboxFocused.ExecuteAsync(info);

        }
        private bool checkCanvasForBox(InkCanvas canvas, MeTLTextBox box)
        {
            var result = false;
            Dispatcher.adopt(() =>
            {
                var boxId = box.tag().id;
                var privacy = box.tag().privacy;
                foreach (var text in canvas.Children)
                    if (text is MeTLTextBox)
                        if (((MeTLTextBox)text).tag().id == boxId && ((MeTLTextBox)text).tag().privacy == privacy) result = true;
            });
            return result;
        }
        private bool alreadyHaveThisTextBox(MeTLTextBox box)
        {
            return checkCanvasForBox(MyWork, box) || checkCanvasForBox(OtherWork, box);
        }
        private void removeDoomedTextBoxes(TargettedTextBox targettedBox)
        {
            removeTextBoxesFrom(MyWork, targettedBox.identity);
            removeTextBoxesFrom(OtherWork, targettedBox.identity);
        }
        private void removeTextBoxesFrom(InkCanvas canvas, string id)
        {
            var doomedChildren = new List<FrameworkElement>();
            foreach (var child in canvas.Children)
            {
                if (child is MeTLTextBox)
                    if (((MeTLTextBox)child).tag().id.Equals(id))
                        doomedChildren.Add((FrameworkElement)child);
            }
            foreach (var child in doomedChildren)
                canvas.Children.Remove(child);
        }
        private void receiveDirtyText(TargettedDirtyElement element)
        {
            if (!(element.target.Equals(target))) return;
            if (element.slide != Globals.slide) return;
            Dispatcher.adoptAsync(delegate
            {
                if (myTextBox != null && element.identifier == myTextBox.tag().id) return;
                if (element.author == me) return;
                removeTextBoxesFrom(MyWork, element.identifier);
                removeTextBoxesFrom(OtherWork, element.identifier);
            });
        }
        public void ReceiveTextBox(TargettedTextBox targettedBox)
        {
            if (targettedBox.target != target) return;
            if (targettedBox.author == Globals.me && alreadyHaveThisTextBox(targettedBox.box.toMeTLTextBox()) && me != Globals.PROJECTOR)
            {
                var box = textBoxFromId(targettedBox.identity);
                if (box != null)
                    ApplyPrivacyStylingToElement(box, box.tag().privacy);
                return;
            }//I never want my live text to collide with me.
            if (targettedBox.slide == Globals.slide && (targettedBox.privacy == Globals.PRIVATE || me == Globals.PROJECTOR))
                removeDoomedTextBoxes(targettedBox);
            if (targettedBox.slide == Globals.slide && (targettedBox.privacy == Globals.PUBLIC || (targettedBox.author == Globals.me && me != Globals.PROJECTOR)))
                    DoText(targettedBox);
        }
        private MeTLTextBox textBoxFromId(string boxId)
        {
            MeTLTextBox result = null;
            var boxes = new List<UIElement>();
            boxes.AddRange(MyWork.Children.ToList());
            boxes.AddRange(OtherWork.Children.ToList());
            Dispatcher.adopt(() =>
            {
                foreach (var text in boxes)
                    if (text.GetType() == typeof(MeTLTextBox))
                        if (((MeTLTextBox)text).tag().id == boxId) result = (MeTLTextBox)text;
            });
            return result;
        }
        public bool CanHandleClipboardPaste()
        {
            return Clipboard.ContainsText();
        }

        public bool CanHandleClipboardCut()
        {
            return true;
        }

        public bool CanHandleClipboardCopy()
        {
            return true;
        }

        public void OnClipboardPaste()
        {
            try
            {
                if (myTextBox != null)
                {
                    var text = Clipboard.GetText();
                    var undoText = myTextBox.Text;
                    var caret = myTextBox.CaretIndex;
                    var redoText = myTextBox.Text.Insert(myTextBox.CaretIndex, text);
                    var currentTextBox = myTextBox.clone();
                    Action undo = () =>
                    {
                        var box = ((MeTLTextBox)MyWork.Children.ToList().Where(c => ((MeTLTextBox)c).tag().id ==  currentTextBox.tag().id).FirstOrDefault());
                        box.TextChanged -= SendNewText;
                        box.Text = undoText;
                        box.CaretIndex = caret;
                        sendTextWithoutHistory(box, box.tag().privacy);
                        box.TextChanged += SendNewText;
                    };
                    Action redo = () =>
                    {
                        ClearAdorners();
                        var box = ((MeTLTextBox)MyWork.Children.ToList().Where(c => ((MeTLTextBox)c).tag().id ==  currentTextBox.tag().id).FirstOrDefault());
                        box.TextChanged -= SendNewText;
                        // 1. Get visible canvas size
                        // 2. If the text being pasted will cause the text box width to be larger than the visible canvas width, wrap the text by inserting 
                        // carriage-returns in the appropriate place.
                        // Problems: What's the width of text? Dependant on font style, weighting and size.
                        //         : Where to insert the carriage-returns? Wrap on spaces? Would it work to just set the width of the text box and let it handle
                        //           the wrapping?
 
                        box.Text = redoText; 
                        if (box.Width > this.ActualWidth)
                        {
                            box.Width = (this.ActualWidth - InkCanvas.GetLeft(box)) * 0.95; // Decrease by a further 5%
                        }

                        box.CaretIndex = caret + text.Length;
                        sendTextWithoutHistory(box, box.tag().privacy);
                        box.TextChanged += SendNewText;
                    };
                    redo();
                    UndoHistory.Queue(undo, redo);
                }
                else
                {
                    MeTLTextBox box = createNewTextbox();
                    MyWork.Children.Add(box);
                    InkCanvas.SetLeft(box, 15);
                    InkCanvas.SetTop(box, 15);
                    box.TextChanged -= SendNewText;
                    box.Text = Clipboard.GetText();
                    box.TextChanged += SendNewText;
                    Action undo = () =>
                                      {
                                          dirtyTextBoxWithoutHistory(box);
                                      };
                         
                    Action redo = () =>
                                   {
                                       sendTextWithoutHistory(box, box.tag().privacy);
                                   };
                    UndoHistory.Queue(undo, redo);
                    redo();
                }
            }
            catch (Exception)
            {

            }
        }
        public MeTLTextBox createNewTextbox()
        {
            var box = new MeTLTextBox();
            box.tag(new TextTag
                        {
                            author = Globals.me,
                            privacy = privacy,
                            id = string.Format("{0}:{1}", Globals.me, SandRibbonObjects.DateTimeFactory.Now())
                        });
            box.FontFamily = currentFamily;
            box.FontSize = currentSize;
            box.Foreground = new SolidColorBrush(currentColor);
            box.UndoLimit = 0;
            box.LostFocus += (_sender, _args) =>
            {
                myTextBox = null;

            };
            return applyDefaultAttributes(box);
        }
        public void OnClipboardCopy()
        {

        }

        public void OnClipboardCut()
        {

        }


        #endregion
        private void MoveTo(int _slide)
        {
            if (myTextBox != null)
            {
                var textBox = myTextBox;
                textBox.Focusable = false;
            }
            myTextBox = null;
        }
        public void Flush()
        {
            MyWork.Strokes.Clear();
            MyWork.Children.Clear();
            OtherWork.Children.Clear();
            OtherWork.Strokes.Clear();
        }
        public string generateId()
        {
            return string.Format("{0}:{1}", Globals.me, DateTimeFactory.Now().Ticks);
        }

    }
    public class MeTLTextBox : TextBox
    {
        CommandBinding undoBinding;
        CommandBinding redoBinding;

        public MeTLTextBox()
        {
            UndoLimit = 1;

            undoBinding = new CommandBinding( ApplicationCommands.Undo, UndoExecuted, null);
            redoBinding = new CommandBinding( ApplicationCommands.Redo, RedoExecuted, null);
            CommandBindings.Add(undoBinding);
            CommandBindings.Add(redoBinding);

            CommandManager.AddPreviewCanExecuteHandler(this, canExecute);
        }

        private void canExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Cut || e.Command == ApplicationCommands.Copy || e.Command == ApplicationCommands.Paste)
            {
                e.ContinueRouting = true;
                e.Handled = true;
                e.CanExecute = false;
            }
        }
        private void UndoExecuted(object sender, ExecutedRoutedEventArgs args)
        {
            ApplicationCommands.Undo.Execute(null, Application.Current.MainWindow);
            Commands.Undo.Execute(null);
        }

        private void RedoExecuted(object sender, ExecutedRoutedEventArgs args)
        {
            ApplicationCommands.Redo.Execute(null, Application.Current.MainWindow);
            Commands.Redo.Execute(null);
        }
    }
    public static class InkCanvasExtensions
    {
        public static IEnumerable<UIElement> TextChildren(this InkCanvas canvas)
        {
            return canvas.Children.ToList().Where(t => t is MeTLTextBox);
        }
        public static IEnumerable<UIElement> ImageChildren(this InkCanvas canvas)
        {
            return canvas.Children.ToList().Where(t => t is Image);
        }
        public static IEnumerable<UIElement> GetSelectedImagesBoxes(this InkCanvas canvas)
        {
            return canvas.GetSelectedElements().Where(i => i is Image);
        }
        public static IEnumerable<UIElement> GetSelectedTextBoxes(this InkCanvas canvas)
        {
            return canvas.GetSelectedElements().Where(i => i is MeTLTextBox);
        }
    }
    public static class TextBoxExtensions
    {
        public static bool IsUnder(this TextBox box, Point point)
        {
            var boxOrigin = new Point(InkCanvas.GetLeft(box), InkCanvas.GetTop(box));
            var boxSize = new Size(box.ActualWidth, box.ActualHeight);
            var result = new Rect(boxOrigin, boxSize).Contains(point);
            return result;
        }
        public static MeTLTextBox clone(this MeTLTextBox box)
        {
            var newBox = new MeTLTextBox();
            newBox.Text = box.Text;
            newBox.TextAlignment = box.TextAlignment;
            newBox.TextDecorations = box.TextDecorations;
            newBox.FontFamily = box.FontFamily;
            newBox.FontSize = box.FontSize;
            newBox.Foreground = box.Foreground;
            newBox.Background = box.Background;
            newBox.tag(box.tag());
            InkCanvas.SetLeft(newBox, InkCanvas.GetLeft(box));
            InkCanvas.SetTop(newBox, InkCanvas.GetTop(box));

            return newBox;
        }
        public static MeTLTextBox toMeTLTextBox(this TextBox OldBox)
        {
            var box = new MeTLTextBox(); 
            box.AcceptsReturn = true;
            box.TextWrapping = TextWrapping.WrapWithOverflow;
            box.BorderThickness = new Thickness(0);
            box.BorderBrush = new SolidColorBrush(Colors.Transparent);
            box.Background = new SolidColorBrush(Colors.Transparent);
            box.tag(OldBox.tag());
            box.FontFamily = OldBox.FontFamily;
            box.FontStyle = OldBox.FontStyle;
            box.FontWeight = OldBox.FontWeight;
            box.TextDecorations = OldBox.TextDecorations;
            box.FontSize = OldBox.FontSize;
            box.Foreground = OldBox.Foreground;
            box.Text = OldBox.Text;
            box.Width = OldBox.Width;
            //box.Height = OldBox.Height;
            InkCanvas.SetLeft(box, InkCanvas.GetLeft(OldBox));
            InkCanvas.SetTop(box, InkCanvas.GetTop(OldBox));
            return box;
        }

    }
    public class PrivateAwareStroke: Stroke
    {
        private Stroke stroke;
        private Pen pen = new Pen();
        private bool isPrivate;
        private bool shouldShowPrivacy;
        private StreamGeometry geometry;
        private string target;
        private Stroke whiteStroke;
        private StrokeTag tag
        {
            get { return stroke.tag(); }
        }
        private StrokeChecksum sum
        {
            get { return stroke.sum(); }

        }
        public PrivateAwareStroke(Stroke stroke, string target) : base(stroke.StylusPoints, stroke.DrawingAttributes)
        {
            var cs = new[] {55, 0, 0, 0}.Select(i => (byte) i).ToList();
            pen = new Pen(new SolidColorBrush(
                new Color{
                       A=cs[0],
                       R=stroke.DrawingAttributes.Color.R,
                       G=stroke.DrawingAttributes.Color.G,
                       B=stroke.DrawingAttributes.Color.B
                    }), stroke.DrawingAttributes.Width * 4);
            this.stroke = stroke;
            this.target = target; 
            this.tag(stroke.tag());
            isPrivate = this.tag().privacy == "private";
            shouldShowPrivacy = (this.tag().author == Globals.conversationDetails.Author || Globals.conversationDetails.Permissions.studentCanPublish);
            
            if (!isPrivate) return;

            pen.Freeze();
        }
        protected override void DrawCore(DrawingContext drawingContext, DrawingAttributes drawingAttributes)
        {
            if (isPrivate && shouldShowPrivacy && target != "notepad")
            {
            
                if (!stroke.DrawingAttributes.IsHighlighter)
                {

                    whiteStroke = stroke.Clone();
                    whiteStroke.DrawingAttributes.Color = Colors.White;
                }
                var wideStroke = this.stroke.GetGeometry().GetWidenedPathGeometry(pen).GetFlattenedPathGeometry()
                    .Figures
                    .SelectMany(f => f.Segments.Where(s => s is PolyLineSegment).SelectMany(s=> ((PolyLineSegment)s).Points));
                geometry = new StreamGeometry();
                using(var context = geometry.Open())
                {
                    context.BeginFigure(wideStroke.First(),false , false);
                    for (var i = 0; i < stroke.StylusPoints.Count; i++)
                        context.LineTo(stroke.StylusPoints.ElementAt(i).ToPoint(), true, true);
                    context.LineTo(wideStroke.Reverse().First(), false, false);
                }
                    drawingContext.DrawGeometry(null, pen, geometry);
            }
            if(whiteStroke != null && isPrivate)
               whiteStroke.Draw(drawingContext);
            else
               base.DrawCore(drawingContext, drawingAttributes);
        }
    }
}
