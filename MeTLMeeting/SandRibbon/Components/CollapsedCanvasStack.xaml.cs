﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MeTLLib.DataTypes;
using MeTLLib.Utilities;
using Microsoft.Practices.Composite.Presentation.Commands;
using Microsoft.Win32;
using SandRibbon.Components.Utility;
using SandRibbon.Providers;
using SandRibbon.Utils;
using SandRibbonObjects;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using Image = System.Windows.Controls.Image;
using Path = System.IO.Path;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace SandRibbon.Components
{
    public class SizeWithTarget
    {
        public SizeWithTarget(double width, double height, string target)
        {
            Size = new Size(width, height);
            Target = target;
        }

        public string Target { get; private set; }
        public Size Size { get; private set; }
        public double Width { get { return Size.Width; } }
        public double Height { get { return Size.Height; } }
    }

    public struct MoveDeltaMetrics
    {
        public Rect OldRectangle { get; set; }
        public Rect NewRectangle { get; set; }

        public void Update(Rect oldRect, Rect newRect)
        {
            OldRectangle = oldRect;
            NewRectangle = newRect;
        }

        public Vector Delta 
        {
            get
            {
                return NewRectangle.Location - OldRectangle.Location;
            }
        }

        public Vector Scale
        {
            get
            {
                return new Vector(NewRectangle.Width / OldRectangle.Width, NewRectangle.Height / OldRectangle.Height);
            }
        }
    }

    public class TagInformation
    {
        public string Author;
        public bool IsPrivate;
        public string Id;
    }
    public class TextInformation : TagInformation
    {
        public TextInformation()
        {
        }

        public TextInformation(TextInformation copyTextInfo)
        {
            Author = copyTextInfo.Author;
            IsPrivate = copyTextInfo.IsPrivate;
            Id = copyTextInfo.Id;
            Size = copyTextInfo.Size;
            Family = copyTextInfo.Family;
            Underline = copyTextInfo.Underline;
            Bold = copyTextInfo.Bold;
            Italics = copyTextInfo.Italics;
            Strikethrough = copyTextInfo.Strikethrough;
            Color = copyTextInfo.Color;
        }

        public double Size;
        public FontFamily Family;
        public bool Underline;
        public bool Bold;
        public bool Italics;
        public bool Strikethrough;
        public Color Color;
    }
    public struct ImageDrop
    {
        public string Filename;
        public Point Point;
        public string Target;
        public int Position;
        public bool OverridePoint;
    }
    public enum FileType
    {
        Video,
        Image,
        NotSupported
    }

    public class TypingTimedAction : TimedAction<Queue<Action>>
    {
        protected override void AddAction(Action timedAction)
        {
            timedActions.Enqueue(timedAction);
        }

        protected override Action GetTimedAction()
        {
            if (timedActions.Count == 0)
                return null;

            return timedActions.Dequeue();
        }
    }
    
    public partial class CollapsedCanvasStack : UserControl, IClipboardHandler
    {
        List<MeTLTextBox> _boxesAtTheStart = new List<MeTLTextBox>();
        private Color _currentColor = Colors.Black;
        private const double DefaultSize = 24.0;
        private readonly FontFamily _defaultFamily = new FontFamily("Arial");
        private double _currentSize = 24.0;
        private FontFamily _currentFamily = new FontFamily("Arial");
        private const bool CanFocus = true;
        private bool _focusable = true;
        public static TypingTimedAction TypingTimer;
        private string _originalText;
        private ContentBuffer contentBuffer;
        private string _target;
        private Privacy _defaultPrivacy;
        private readonly ClipboardManager clipboardManager = new ClipboardManager();
        private string _me = String.Empty;
        public string me
        {
            get {
                if (String.IsNullOrEmpty(_me))
                    return Globals.me;
                return _me;
            }
            set { _me = value; }
        }

        private MeTLTextBox _lastFocusedTextBox;
        private MeTLTextBox myTextBox
        {
            get
            {
                return _lastFocusedTextBox;
            }
            set
            {
                _lastFocusedTextBox = value;
            }
        }

        private bool affectedByPrivacy { get { return _target == "presentationSpace"; } }
        public Privacy privacy { get { return affectedByPrivacy ? (Privacy)Enum.Parse(typeof(Privacy), Globals.privacy, true) : _defaultPrivacy; } }
        private Point pos = new Point(15, 15);
        private void wireInPublicHandlers()
        {
            PreviewKeyDown += keyPressed;
            Work.StrokeCollected += singleStrokeCollected;
            Work.SelectionChanging += selectionChanging;
            Work.SelectionChanged += selectionChanged;
            Work.StrokeErasing += erasingStrokes;
            Work.SelectionMoving += SelectionMovingOrResizing;
            Work.SelectionMoved += SelectionMovedOrResized;
            Work.SelectionResizing += SelectionMovingOrResizing;
            Work.SelectionResized += SelectionMovedOrResized;
            Work.AllowDrop = true;
            Work.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(MyWork_PreviewMouseLeftButtonUp);
            Work.Drop += ImagesDrop;
            Loaded += (a, b) =>
            {
                MouseUp += (c, args) => placeCursor(this, args);
            };
        }

        void MyWork_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            pos = e.GetPosition(this);
        }
        public CollapsedCanvasStack()
        {
            InitializeComponent();
            wireInPublicHandlers();
            strokeChecksums = new List<StrokeChecksum>();
            contentBuffer = new ContentBuffer();
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, deleteSelectedElements, canExecute));
            Commands.SetPrivacy.RegisterCommand(new DelegateCommand<string>(SetPrivacy));
            Commands.SetInkCanvasMode.RegisterCommandToDispatcher<string>(new DelegateCommand<string>(setInkCanvasMode));
            Commands.ReceiveStroke.RegisterCommandToDispatcher(new DelegateCommand<TargettedStroke>((stroke) => ReceiveStrokes(new[] { stroke })));
            Commands.ReceiveStrokes.RegisterCommandToDispatcher(new DelegateCommand<IEnumerable<TargettedStroke>>(ReceiveStrokes));
            Commands.ReceiveDirtyStrokes.RegisterCommand(new DelegateCommand<IEnumerable<TargettedDirtyElement>>(ReceiveDirtyStrokes));
            Commands.ZoomChanged.RegisterCommand(new DelegateCommand<double>(ZoomChanged));

            Commands.ReceiveImage.RegisterCommand(new DelegateCommand<IEnumerable<TargettedImage>>(ReceiveImages));
            Commands.ReceiveDirtyImage.RegisterCommand(new DelegateCommand<TargettedDirtyElement>(ReceiveDirtyImage));
            Commands.AddImage.RegisterCommandToDispatcher(new DelegateCommand<object>(addImageFromDisk));
            Commands.ReceiveMoveDelta.RegisterCommandToDispatcher(new DelegateCommand<TargettedMoveDelta>(ReceiveMoveDelta));

            Commands.ReceiveTextBox.RegisterCommandToDispatcher(new DelegateCommand<TargettedTextBox>(ReceiveTextBox));
            Commands.UpdateTextStyling.RegisterCommand(new DelegateCommand<TextInformation>(updateStyling));
            Commands.RestoreTextDefaults.RegisterCommand(new DelegateCommand<object>(resetTextbox));
            Commands.EstablishPrivileges.RegisterCommand(new DelegateCommand<string>(setInkCanvasMode));
            Commands.SetTextCanvasMode.RegisterCommand(new DelegateCommand<string>(setInkCanvasMode));
            Commands.ReceiveDirtyText.RegisterCommand(new DelegateCommand<TargettedDirtyElement>(receiveDirtyText));
           
            #if TOGGLE_CONTENT
            Commands.SetContentVisibility.RegisterCommandToDispatcher<ContentVisibilityEnum>(new DelegateCommand<ContentVisibilityEnum>(SetContentVisibility));
            #endif
 
            Commands.ExtendCanvasBySize.RegisterCommandToDispatcher<SizeWithTarget>(new DelegateCommand<SizeWithTarget>(extendCanvasBySize));

            Commands.ImageDropped.RegisterCommandToDispatcher(new DelegateCommand<ImageDrop>(imageDropped));
            Commands.ImagesDropped.RegisterCommandToDispatcher(new DelegateCommand<List<ImageDrop>>(imagesDropped));
            Commands.MoveTo.RegisterCommand(new DelegateCommand<int>(MoveTo));
            Commands.SetLayer.RegisterCommandToDispatcher<string>(new DelegateCommand<string>(SetLayer));
            Commands.DeleteSelectedItems.RegisterCommandToDispatcher(new DelegateCommand<object>(deleteSelectedItems));
            Commands.SetPrivacyOfItems.RegisterCommand(new DelegateCommand<Privacy>(changeSelectedItemsPrivacy));
            Commands.SetDrawingAttributes.RegisterCommandToDispatcher(new DelegateCommand<DrawingAttributes>(SetDrawingAttributes));
            Commands.UpdateConversationDetails.RegisterCommandToDispatcher(new DelegateCommand<ConversationDetails>(UpdateConversationDetails));
            Commands.JoinConversation.RegisterCommand(new DelegateCommand<object>((_unused) => { JoinConversation(); }));
            Commands.ShowConversationSearchBox.RegisterCommandToDispatcher(new DelegateCommand<object>(hideAdorners));
            Commands.HideConversationSearchBox.RegisterCommandToDispatcher(new DelegateCommand<object>(HideConversationSearchBox));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, (sender, args) => HandlePaste(args), canExecute));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, (sender, args) => HandleCopy(args), canExecute));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, (sender, args) => HandleCut(args), canExecute));
            Loaded += (_sender, _args) => this.Dispatcher.adoptAsync(delegate
            {
                if (_target == null)
                {
                    _target = (string)FindResource("target");
                    _defaultPrivacy = (Privacy)FindResource("defaultPrivacy");

                    if (_target == "presentationSpace")
                    {
                        Globals.CurrentCanvasClipboardFocus = _target;
                    }
                }
            });
            Commands.ClipboardManager.RegisterCommand(new DelegateCommand<ClipboardAction>((action) => clipboardManager.OnClipboardAction(action)));
            clipboardManager.RegisterHandler(ClipboardAction.Paste, OnClipboardPaste, CanHandleClipboardPaste);
            clipboardManager.RegisterHandler(ClipboardAction.Cut, OnClipboardCut, CanHandleClipboardCut);
            clipboardManager.RegisterHandler(ClipboardAction.Copy, OnClipboardCopy, CanHandleClipboardCopy);
            Work.MouseMove += mouseMove;
            Work.StylusMove += stylusMove;
            Work.IsKeyboardFocusWithinChanged += Work_IsKeyboardFocusWithinChanged;
            Globals.CanvasClipboardFocusChanged += CanvasClipboardFocusChanged;
        }

        void Work_IsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
            {
                Globals.CurrentCanvasClipboardFocus = _target;
            }
        }

        void CanvasClipboardFocusChanged(object sender, EventArgs e)
        {
            if ((string)sender != _target)
            {
                ClipboardFocus.BorderBrush = new SolidColorBrush(Colors.Transparent);
            }
            else
            {
                ClipboardFocus.BorderBrush = new SolidColorBrush(Colors.Pink);
            }
        }

        private void JoinConversation()
        {
            if (myTextBox != null)
            {
                myTextBox.LostFocus -= textboxLostFocus;
                myTextBox = null;
            }
        }

        private void stylusMove(object sender, StylusEventArgs e)
        {
            GlobalTimers.ResetSyncTimer();
        }

        private void mouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                GlobalTimers.ResetSyncTimer();
            }
        }

        private void canExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Work.GetSelectedElements().Count > 0 || Work.GetSelectedStrokes().Count > 0 || myTextBox != null;
        }

        private void extendCanvasBySize(SizeWithTarget newSize)
        {
            if (_target == newSize.Target)
            {
                Height = newSize.Height;
                Width = newSize.Width;
            }
        }

        private InkCanvasEditingMode currentMode;
        private void hideAdorners(object obj)
        {
            currentMode = Work.EditingMode;
            Work.Select(new UIElement[]{});
            ClearAdorners();
        }
        private void keyPressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete  && (Work.GetSelectedElements().Count > 0 || Work.GetSelectedStrokes().Count >0))//&& myTextBox == null)
                deleteSelectedElements(null, null);
            if (e.Key == Key.PageUp || (e.Key == Key.Up && myTextBox == null))
            {
                if(Commands.MoveToPrevious.CanExecute(null))
                  Commands.MoveToPrevious.Execute(null);
                e.Handled = true;
            }
            if (e.Key == Key.PageDown || (e.Key == Key.Down && myTextBox == null))
            {
                if(Commands.MoveToNext.CanExecute(null))
                  Commands.MoveToNext.Execute(null);
                e.Handled = true;
            }
        }
        private void SetLayer(string newLayer)
        {
            if (me.ToLower() == Globals.PROJECTOR) return;
            switch (newLayer)
            {
                case "Text":
                    Work.EditingMode = InkCanvasEditingMode.None;
                    Work.UseCustomCursor = Work.EditingMode == InkCanvasEditingMode.Ink;
                    break;
                case "Insert":
                    Work.EditingMode = InkCanvasEditingMode.Select;
                    Work.UseCustomCursor = Work.EditingMode == InkCanvasEditingMode.Ink;
                    Work.Cursor = Cursors.Arrow;
                    break;
                case "Sketch":
                    Work.UseCustomCursor = true;
                    break;
            }
            _focusable = newLayer == "Text";
            setLayerForTextFor(Work);
        }
        private void setLayerForTextFor(InkCanvas canvas)
        {
            var curFocusable = _focusable;
            var curMe = Globals.me;

            foreach (var box in canvas.Children)
            {
                if (box.GetType() == typeof(MeTLTextBox))
                {
                    var tag = ((MeTLTextBox)box).tag();
                    ((MeTLTextBox)box).Focusable = curFocusable && (tag.author == curMe);
                }
            }
            contentBuffer.UpdateAllTextBoxes((textBox) =>
            {
                var tag = textBox.tag();
                textBox.Focusable = curFocusable && (tag.author == curMe);
            });
        }
        private UndoHistory.HistoricalAction deleteSelectedImages(IEnumerable<Image> selectedElements)
        {
            if (selectedElements.Count() == 0) return new UndoHistory.HistoricalAction(()=> { }, ()=> { }, 0, "No images selected");
             Action undo = () =>
                {
                    foreach (var element in selectedElements)
                    {
                        // if this element hasn't already been added
                        if (Work.ImageChildren().ToList().Where(i => ((Image)i).tag().id == ((Image)element).tag().id).Count() == 0)
                        {
                            contentBuffer.AddImage(element, (child) => Work.Children.Add(child));
                        }
                       sendThisElement(element);
                    }
                    Work.Select(selectedElements.Select(i => (UIElement)i));
                };
            Action redo = () =>
                {
                    foreach (var element in selectedElements)
                    {
                        var imagesToRemove = Work.ImageChildren().ToList().Where(i => ((Image)i).tag().id == ((Image)element).tag().id);
                        if (imagesToRemove.Count() > 0)
                        {
                            contentBuffer.RemoveImage(imagesToRemove.First(), (image) => Work.Children.Remove(image));
                        }
                        dirtyThisElement(element);
                    }
                };
            return new UndoHistory.HistoricalAction(undo, redo, 0, "Delete selected images");
        }
        private UndoHistory.HistoricalAction deleteSelectedInk(StrokeCollection selectedStrokes)
        {
            Action undo = () =>
                {
                    addStrokes(selectedStrokes.ToList());

                    if(Work.EditingMode == InkCanvasEditingMode.Select)
                        Work.Select(selectedStrokes);
                    
                };
            Action redo = () => removeStrokes(selectedStrokes.ToList());
            return new UndoHistory.HistoricalAction(undo, redo, 0, "Delete selected ink");
        }
        private UndoHistory.HistoricalAction deleteSelectedText(IEnumerable<MeTLTextBox> elements)
        {
            var selectedElements = elements.Where(t => t.tag().author == Globals.me).Select(b => ((MeTLTextBox)b).clone()).ToList();
            Action undo = () =>
                              {
                                  foreach (var box in selectedElements)
                                  {
                                      myTextBox = box;
                                      if(!alreadyHaveThisTextBox(box))
                                          AddTextBoxToCanvas(box);
                                      box.PreviewKeyDown += box_PreviewTextInput;
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
            return new UndoHistory.HistoricalAction(undo, redo, 0, "Delete selected text");
        }

        private void AddTextBoxToCanvas(MeTLTextBox box)
        {
            Panel.SetZIndex(box, 3);
            AddTextboxToMyCanvas(box);
        }

        private void deleteSelectedElements(object _sender, ExecutedRoutedEventArgs _handler)
        {
            if (me == Globals.PROJECTOR) return;
            var selectedImages = new List<Image>();
            var selectedText = new List<MeTLTextBox>();
            var selectedStrokes = new StrokeCollection();
            Dispatcher.adopt(() =>
                                 {
                                     selectedStrokes = Work.GetSelectedStrokes();
                                     selectedImages = Work.GetSelectedImages().ToList();
                                     selectedText = Work.GetSelectedTextBoxes().ToList();
                                 });
            var ink = deleteSelectedInk(filterOnlyMine(selectedStrokes));
            var images = deleteSelectedImages(filterOnlyMine(selectedImages.Cast<UIElement>()).Cast<Image>()); // TODO: fix the casting craziness
            var text = deleteSelectedText(filterOnlyMine(selectedText.Cast<UIElement>()).Cast<MeTLTextBox>()); // TODO: fix the casting craziness
            Action undo = () =>
                {
                    ink.undo();
                    text.undo();
                    images.undo();
                    ClearAdorners();
                    Work.Focus();
                };
            Action redo = () =>
                {
                    Keyboard.Focus(this); // set keyboard focus to the current canvas so the help button does not grey out
                    ink.redo();
                    text.redo();
                    images.redo();
                    ClearAdorners();
                    Work.Focus();
                };
            UndoHistory.Queue(undo, redo, "Delete selected items");
            redo();
        }
        private void HideConversationSearchBox(object obj)
        {
            Work.EditingMode = currentMode;
            AddAdorners();
        }

        private void UpdateConversationDetails(ConversationDetails details)
        {
            ClearAdorners();
            if (ConversationDetails.Empty.Equals(details)) return;
            Dispatcher.adoptAsync(delegate
                  {
                      var newStrokes = new StrokeCollection( Work.Strokes.Select( s => (Stroke) new PrivateAwareStroke(s, _target)));

                      contentBuffer.ClearStrokes(() => Work.Strokes.Clear());
                      contentBuffer.AddStrokes(newStrokes, (st) => Work.Strokes.Add(st));

                      foreach (Image image in Work.ImageChildren())
                          ApplyPrivacyStylingToElement(image, image.tag().privacy);
                      foreach (var item in Work.TextChildren())
                      {
                          MeTLTextBox box;
                          if (item.GetType() == typeof(TextBox))
                              box = ((TextBox)item).toMeTLTextBox();
                          else
                              box = (MeTLTextBox)item;
                          ApplyPrivacyStylingToElement(box, box.tag().privacy);
                          
                      }
                      if (myTextBox != null)
                      {
                          myTextBox.Focus();
                          Keyboard.Focus(myTextBox);
                      }

                  });
        }
        private void SetPrivacy(string privacy)
        {
            AllowDrop = true;
        }
        private void setInkCanvasMode(string modeString)
        {
            if (me == Globals.PROJECTOR) return;
            Work.EditingMode = (InkCanvasEditingMode)Enum.Parse(typeof(InkCanvasEditingMode), modeString);
            Work.UseCustomCursor = Work.EditingMode == InkCanvasEditingMode.Ink;
        }
        private Double zoom = 1;
        private void ZoomChanged(Double zoom)
        {
            // notepad does not currently support zoom
            if (_target == "notepad") 
                return;

            this.zoom = zoom;
            if (Commands.SetDrawingAttributes.IsInitialised && Work.EditingMode == InkCanvasEditingMode.Ink)
            {
                SetDrawingAttributes((DrawingAttributes)Commands.SetDrawingAttributes.LastValue());
            }
        }

        #region Events
        private List<Stroke> strokesAtTheStart = new List<Stroke>();
        private List<StrokeChecksum> strokeChecksums;
        private List<UIElement> imagesAtStartOfTheMove = new List<UIElement>();
        private MoveDeltaMetrics moveMetrics;
        private void SelectionMovingOrResizing (object sender, InkCanvasSelectionEditingEventArgs e)
        {
            // don't want to move or resize any uielements or strokes that weren't authored by the owner
            var inkCanvas = sender as InkCanvas;
            inkCanvas.Select(filterOnlyMine(inkCanvas.GetSelectedStrokes()), filterOnlyMine(inkCanvas.GetSelectedElements()));

            contentBuffer.ClearDeltaStrokes(() => strokesAtTheStart.Clear());
            contentBuffer.AddDeltaStrokes(inkCanvas.GetSelectedStrokes(), (st) => strokesAtTheStart.AddRange(st.Select(s => s.Clone())));

            contentBuffer.ClearDeltaImages(() => imagesAtStartOfTheMove.Clear());
            contentBuffer.AddDeltaImages(GetSelectedClonedImages().ToList(), (img) => imagesAtStartOfTheMove.AddRange(img));

            _boxesAtTheStart.Clear();
            _boxesAtTheStart = inkCanvas.GetSelectedElements().Where(b=> b is MeTLTextBox).Select(tb => ((MeTLTextBox)tb).clone()).ToList();

            if (e.NewRectangle.Width == e.OldRectangle.Width && e.NewRectangle.Height == e.OldRectangle.Height)
                return;

            moveMetrics.Update(e.OldRectangle, e.NewRectangle);

            // following code is image specific
            if (strokesAtTheStart.Count != 0)
                return;
            if (_boxesAtTheStart.Count != 0)
                return;

            Rect imageCanvasRect = new Rect(new Size(ActualWidth, ActualHeight));

            double resizeWidth;
            double resizeHeight;
            double imageX;
            double imageY;

            if (e.NewRectangle.Right > imageCanvasRect.Right)
                resizeWidth = MeTLMath.Clamp(imageCanvasRect.Width - e.NewRectangle.X, 0, imageCanvasRect.Width);
            else
                resizeWidth = e.NewRectangle.Width;

            if (e.NewRectangle.Height > imageCanvasRect.Height)
                resizeHeight = MeTLMath.Clamp(imageCanvasRect.Height - e.NewRectangle.Y, 0, imageCanvasRect.Height);
            else
                resizeHeight = e.NewRectangle.Height;

            imageX = MeTLMath.Clamp(e.NewRectangle.X, 0, e.NewRectangle.X);
            imageY = MeTLMath.Clamp(e.NewRectangle.Y, 0, e.NewRectangle.Y);

            // ensure image is being resized uniformly maintaining aspect ratio
            var aspectRatio = e.OldRectangle.Width / e.OldRectangle.Height;
            if (e.NewRectangle.Width != e.OldRectangle.Width)
            {
                resizeHeight = resizeWidth / aspectRatio;
            }
            else if (e.NewRectangle.Height != e.OldRectangle.Height)
                resizeWidth = resizeHeight * aspectRatio;
            else
                resizeWidth = resizeHeight * aspectRatio;

            e.NewRectangle = new Rect(imageX, imageY, resizeWidth, resizeHeight);
            moveMetrics.Update(e.OldRectangle, e.NewRectangle);
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
                        var imagesToRemove = Work.Children.ToList().Where(i => i is Image && ((Image)i).tag().id == element.tag().id);
                        if (imagesToRemove.Count() > 0)
                            contentBuffer.RemoveImage(imagesToRemove.First(), (image) => Work.Children.Remove(image));
                        if (!element.Tag.ToString().StartsWith("NOT_LOADED"))
                            dirtyThisElement(element);
                    }
                    foreach (var element in startingElements)
                    {
                        selection.Add(element);
                        if (Work.Children.ToList().Where(i => i is Image &&((Image)i).tag().id == element.tag().id).Count() == 0)
                            contentBuffer.AddImage(element, (image) => Work.Children.Add(image));
                        if (!element.Tag.ToString().StartsWith("NOT_LOADED"))
                            sendThisElement(element);
                    }
                };
            Action redo = () =>
                {
                    var mySelectedImages = selectedElements.Where(i => i is Image).Select(i => ((Image)i).clone()).ToList();
                    foreach (var element in startingElements)
                    {
                        var imagesToRemove = Work.Children.ToList().Where(i => i is Image && ((Image)i).tag().id == element.tag().id);
                          if (imagesToRemove.Count() > 0)
                              contentBuffer.RemoveImage(imagesToRemove.First(), (image) => Work.Children.Remove(image)); 
                        dirtyThisElement(element);
                    }
                    foreach (var element in mySelectedImages)
                    { 
                        if (Work.Children.ToList().Where(i => i is Image && ((Image)i).tag().id == element.tag().id).Count() == 0)
                        {
                           contentBuffer.AddImage(element, (image) => Work.Children.Add(image));
                        }
                       sendThisElement(element);
                    }
                };           
            return new UndoHistory.HistoricalAction(undo, redo, 0, "Image selection moved or resized");
        }
        private UndoHistory.HistoricalAction InkSelectionMovedOrResized(IEnumerable<Stroke> selectedStrokes, List<Stroke> undoStrokes)
        {
            Action undo = () =>
                {
                    removeStrokes(selectedStrokes);
                    addStrokes(undoStrokes); 
                    if (Work.EditingMode == InkCanvasEditingMode.Select)
                        Work.Select(new StrokeCollection(undoStrokes));
                };
            Action redo = () =>
                {
                    removeStrokes(undoStrokes); 
                    addStrokes(selectedStrokes); 
                    if(Work.EditingMode == InkCanvasEditingMode.Select)
                        Work.Select(new StrokeCollection(selectedStrokes));
                };           
            return new UndoHistory.HistoricalAction(undo, redo, 0, "Ink selection moved or resized");
        }
        private UndoHistory.HistoricalAction TextMovedOrResized(IEnumerable<UIElement> elements, List<MeTLTextBox> boxesAtTheStart)
        {
            Trace.TraceInformation("MovedTextbox");
            var startingText = boxesAtTheStart.Where(b=> b is MeTLTextBox).Select(b=> ((MeTLTextBox)b).clone()).ToList();
            List<UIElement> selectedElements = elements.Where(b=> b is MeTLTextBox).ToList();
            absoluteizeElements(selectedElements);
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
                      sendBox(applyDefaultAttributes(box));
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
                      sendBox(applyDefaultAttributes(box));
                  }
              };
            return new UndoHistory.HistoricalAction(undo, redo, 0, "Text selection moved or resized");
        }
        private void SelectionMovedOrResized(object sender, EventArgs e)
        {
            var selectedStrokes = absoluteizeStrokes(filterOnlyMine(Work.GetSelectedStrokes())).Select(s => s.Clone()).ToList();
            var selectedElements = absoluteizeElements(filterOnlyMine(Work.GetSelectedElements()));
            var startingSelectedImages = imagesAtStartOfTheMove.Where(i => i is Image).Select(i => ((Image)i).clone()).ToList();
            Trace.TraceInformation("MovingStrokes {0}", string.Join(",", selectedStrokes.Select(s => s.sum().checksum.ToString()).ToArray()));
            var undoStrokes = strokesAtTheStart.Select(stroke => stroke.Clone()).ToList();
            //var ink = InkSelectionMovedOrResized(filterOnlyMine(selectedStrokes), undoStrokes);
            var images = ImageSelectionMovedOrResized(filterOnlyMine(selectedElements), startingSelectedImages);
            var text = TextMovedOrResized(filterOnlyMine(selectedElements), _boxesAtTheStart);
            
            var moveDelta = TargettedMoveDelta.Create(Globals.slide, Globals.me, _target, privacy, selectedStrokes, selectedElements.OfType<TextBox>(), selectedElements.OfType<Image>());

            moveDelta.xTranslate = moveMetrics.Delta.X;
            moveDelta.yTranslate = moveMetrics.Delta.Y;
            moveDelta.xScale = moveMetrics.Scale.X;
            moveDelta.yScale = moveMetrics.Scale.Y;
            
            Commands.SendMoveDelta.ExecuteAsync(moveDelta);

            Action undo = () =>
                {
                    ClearAdorners();
                    //ink.undo();
                    text.undo();
                    images.undo();
                    AddAdorners();
                    Work.Focus();
                };
            Action redo = () =>
                {
                    ClearAdorners();
                    //ink.redo();
                    text.redo();
                    images.redo();
                    AddAdorners();
                    Work.Focus();
                };
            redo(); 
            UndoHistory.Queue(undo, redo, "Selection moved or resized");
        }
        private IEnumerable<UIElement> GetSelectedClonedImages()
        {
            var selectedElements = new List<UIElement>();
            foreach (var element in Work.GetSelectedElements())
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

        private void selectionChanging(object sender, InkCanvasSelectionChangingEventArgs e)
        {
            Dispatcher.adopt(() =>
             {
                 if (Globals.IsBanhammerActive)
                 {
                    e.SetSelectedElements(filterExceptMine(e.GetSelectedElements()));
                    e.SetSelectedStrokes(filterExceptMine(e.GetSelectedStrokes()));
                 }
                 else
                 {
                     e.SetSelectedElements(filterOnlyMine(e.GetSelectedElements()));
                     e.SetSelectedStrokes(filterOnlyMine(e.GetSelectedStrokes()));
                 }
             });
        }

        public List<String> GetSelectedAuthors()
        {
            var authorList = new List<string>();

            var strokeList = filterExceptMine(Work.GetSelectedStrokes());
            var elementList = filterExceptMine(Work.GetSelectedElements());

            foreach (var stroke in strokeList)
            {
                authorList.Add(stroke.tag().author);
            }
            foreach (var element in elementList)
            {
                if (element is Image) 
                {
                    authorList.Add((element as Image).tag().author);
                }
                else if (element is MeTLTextBox)
                {
                    authorList.Add((element as MeTLTextBox).tag().author);
                }
            }

            return authorList.Distinct().ToList();
        }

        public Dictionary<string, Color> ColourSelectedByAuthor(List<string> authorList)
        {
            var colors = ColorLookup.GetMediaColors();
            var authorColor = new Dictionary<string, Color>();
            foreach (var author in authorList)
                authorColor.Add(author, colors.ElementAt(authorList.IndexOf(author)));

            /*foreach (var stroke in Work.GetSelectedStrokes())
                stroke.DrawingAttributes.Color = authorColor[stroke.tag().author];
            foreach (var elem in Work.GetSelectedTextBoxes())
                ApplyHighlight((FrameworkElement)elem, authorColor[((MeTLTextBox)elem).tag().author]);
            foreach (var elem in Work.GetSelectedImages())
                ApplyHighlight((FrameworkElement)elem, authorColor[((Image)elem).tag().author]);*/

            return authorColor;
        }

        private StrokeCollection filterExceptMine(IEnumerable<Stroke> strokes)
        {
            var me = Globals.me;
            return new StrokeCollection(strokes.Where(s => s.tag().author != me));
        }

        private IEnumerable<UIElement> filterExceptMine(ReadOnlyCollection<UIElement> elements)
        {
            var me = Globals.me;
            var myText = elements.Where(e => e is MeTLTextBox && (e as MeTLTextBox).tag().author != me);
            var myImages = elements.Where(e => e is Image && (e as Image).tag().author != me);
            var myElements = new List<UIElement>();
            myElements.AddRange(myText);
            myElements.AddRange(myImages);
            return myElements;
        }

        private StrokeCollection filterOnlyMine(IEnumerable<Stroke> strokes)
        {
            return new StrokeCollection(strokes.Where(s => s.tag().author == Globals.me));
        }
        private IEnumerable<UIElement> filterOnlyMine(IEnumerable<UIElement> elements)
        {
            var myText = elements.Where(e => e is MeTLTextBox && ((MeTLTextBox) e).tag().author == Globals.me);
            var myImages = elements.Where(e => e is Image && ((Image) e).tag().author == Globals.me);
            var myElements = new List<UIElement>();
            myElements.AddRange(myText);
            myElements.AddRange(myImages);
            return myElements;
        }

        private T filterOnlyMine<T>(UIElement element) where T : UIElement
        {
            UIElement filteredElement = null;

            if (element == null)
                return null;

            if (element is MeTLTextBox)
            {
                filteredElement = ((MeTLTextBox)element).tag().author == Globals.me ? element : null; 
            }
            else if (element is Image)
            {
                filteredElement = ((Image)element).tag().author == Globals.me ? element : null; 
            }
            return filteredElement as T;
        }

        private void selectionChanged(object sender, EventArgs e)
        {
            myTextBox = (MeTLTextBox)Work.GetSelectedTextBoxes().FirstOrDefault();
            updateTools();
            AddAdorners();
        }
       
        protected internal void AddAdorners()
        {
            ClearAdorners();
            var selectedStrokes = Work.GetSelectedStrokes();
            var selectedElements = Work.GetSelectedElements();
            if (selectedElements.Count == 0 && selectedStrokes.Count == 0) return;
            var publicStrokes = selectedStrokes.Where(s => s.tag().privacy == Privacy.Public).ToList();
            var publicImages = selectedElements.Where(i => (((i is Image) && ((Image)i).tag().privacy == Privacy.Public))).ToList();
            var publicText = selectedElements.Where(i => (((i is MeTLTextBox) && ((MeTLTextBox)i).tag().privacy == Privacy.Public))).ToList();
            var publicCount = publicStrokes.Count + publicImages.Count + publicText.Count;
            var allElementsCount = selectedStrokes.Count + selectedElements.Count;
            string privacyChoice;
            if (publicCount == 0)
                privacyChoice = "show";
            else if (publicCount == allElementsCount)
                privacyChoice = "hide";
            else
                privacyChoice = "both";
            Commands.AddPrivacyToggleButton.Execute(new PrivacyToggleButton.PrivacyToggleButtonInfo(privacyChoice, allElementsCount != 0, Work.GetSelectionBounds(), _target));
        }
        private void ClearAdorners()
        {
            Commands.RemovePrivacyAdorners.ExecuteAsync(_target);
        }
#endregion
        #region ink
        private void SetDrawingAttributes(DrawingAttributes logicalAttributes)
        {
            if (logicalAttributes == null) return;
            if (me.ToLower() == Globals.PROJECTOR) return;
            var zoomCompensatedAttributes = logicalAttributes.Clone();
            try
            {
                zoomCompensatedAttributes.Width = logicalAttributes.Width * zoom;
                zoomCompensatedAttributes.Height = logicalAttributes.Height * zoom;
                var visualAttributes = logicalAttributes.Clone();
                visualAttributes.Width = logicalAttributes.Width * 2;
                visualAttributes.Height = logicalAttributes.Height * 2;
                Work.UseCustomCursor = true;
                Work.Cursor = CursorExtensions.generateCursor(visualAttributes);
            }
            catch (Exception e) {
                Trace.TraceInformation("Cursor failed (no crash):", e.Message);
            }
            Work.DefaultDrawingAttributes = zoomCompensatedAttributes;
        }
        public List<Stroke> PublicStrokes
        {
             get { 
                var canvasStrokes = new List<Stroke>();
                canvasStrokes.AddRange(Work.Strokes);
                return canvasStrokes.Where(s => s.tag().privacy == Privacy.Public).ToList();
            }
        }
        public List<Stroke> AllStrokes
        {
            get { 
                var canvasStrokes = new List<Stroke>();
                canvasStrokes.AddRange(Work.Strokes);
                return canvasStrokes;

            }
        }
        public void ReceiveDirtyStrokes(IEnumerable<TargettedDirtyElement> targettedDirtyStrokes)
        {
            if (targettedDirtyStrokes.Count() == 0) return;
            if (!(targettedDirtyStrokes.First().target.Equals(_target)) || targettedDirtyStrokes.First().slide != Globals.slide) return;
            Dispatcher.adopt(delegate
            {
                dirtyStrokes(Work, targettedDirtyStrokes);
            });
        }
        private void dirtyStrokes(InkCanvas canvas, IEnumerable<TargettedDirtyElement> targettedDirtyStrokes)
        {
            var dirtyChecksums = targettedDirtyStrokes.Select(t => t.identity);
            // 1. find the strokes in the contentbuffer that have matching checksums 
            // 2. remove those strokes and corresponding checksums in the content buffer
            // 3. for the strokes that also exist in the canvas, remove them and their checksums
            contentBuffer.RemoveStrokesAndMatchingChecksum(dirtyChecksums, cs =>
            {
                var dirtyStrokes = canvas.Strokes.Where(s => cs.Contains(s.sum().checksum.ToString())).ToList();
                foreach (var stroke in dirtyStrokes)
                {
                    strokeChecksums.Remove(stroke.sum());
                    canvas.Strokes.Remove(stroke);
                }
            });
        }

        public void SetContentVisibility(ContentVisibilityEnum contentVisibility)
        {
            if (_target == "notepad")
                return;

            Commands.UpdateContentVisibility.Execute(contentVisibility);

            ClearAdorners();
            
            Work.Strokes.Clear();
            Work.Strokes.Add(contentBuffer.FilteredStrokes(contentVisibility));
            Work.Children.Clear();
            foreach (var child in contentBuffer.FilteredTextBoxes(contentVisibility))
                Work.Children.Add(child);
            foreach (var child in contentBuffer.FilteredImages(contentVisibility))
                Work.Children.Add(child);
        }

        public void ReceiveStrokes(IEnumerable<TargettedStroke> receivedStrokes)
        {
            if (receivedStrokes.Count() == 0) return;
            if (receivedStrokes.First().slide != Globals.slide) return;
            var strokeTarget = _target;
            foreach (var targettedStroke in receivedStrokes.Where(targettedStroke => targettedStroke.target == strokeTarget))
            {
                if (targettedStroke.HasSameAuthor(me) || targettedStroke.HasSamePrivacy(Privacy.Public))
                    AddStrokeToCanvas(Work, new PrivateAwareStroke(targettedStroke.stroke, strokeTarget));
            }
        }
        public void AddStrokeToCanvas(InkCanvas canvas, PrivateAwareStroke stroke)
        {
            // stroke already exists on the canvas, don't do anything
            if (canvas.Strokes.Where(s => MeTLMath.ApproxEqual(s.sum().checksum, stroke.sum().checksum)).Count() != 0)
                return;

            contentBuffer.AddStroke(stroke, (st) => canvas.Strokes.Add(st));
        }

        private void RemoveExistingStrokeFromCanvas(InkCanvas canvas, Stroke stroke)
        {
            if (canvas.Strokes.Count == 0)
                return;

            var deadStrokes = new StrokeCollection(canvas.Strokes.Where(s => MeTLMath.ApproxEqual(s.sum().checksum, stroke.sum().checksum)));
            canvas.Strokes.Remove(deadStrokes);
        }

        private void addStrokes(IEnumerable<Stroke> strokes)
        {
            var newStrokes = new StrokeCollection(strokes.Select( s => (Stroke) new PrivateAwareStroke(s, _target)));
            foreach (var stroke in newStrokes)
            {
                doMyStrokeAddedExceptHistory(stroke, stroke.tag().privacy);
            }
        }
        private void removeStrokes(IEnumerable<Stroke> strokes)
        {
             foreach(var stroke in strokes)
             {
                 contentBuffer.RemoveStroke(stroke, (st) => RemoveExistingStrokeFromCanvas(Work, st));
                 doMyStrokeRemovedExceptHistory(stroke);
            }
        }
        public void deleteSelectedItems(object obj)
        {
            if (CanvasHasActiveFocus())
                deleteSelectedElements(null, null); 
        }
        private Privacy determineOriginalPrivacy(Privacy currentPrivacy)
        {
            if (currentPrivacy == Privacy.Private)
                return Privacy.Public;
            return Privacy.Private;
        }

        private UndoHistory.HistoricalAction changeSelectedInkPrivacy(List<Stroke> selectedStrokes, Privacy newPrivacy, Privacy oldPrivacy)
        {
            Action redo = () =>
            {
                var newStrokes = new StrokeCollection();
                foreach (var stroke in selectedStrokes.Where(i => i != null && i.tag().privacy != newPrivacy))
                {
                    var oldTag = stroke.tag();
                    // stroke exists on canvas
                    var strokesToUpdate = Work.Strokes.Where(s => MeTLMath.ApproxEqual(s.sum().checksum, stroke.sum().checksum));
                    if (strokesToUpdate.Count() > 0)
                    {
                        contentBuffer.RemoveStroke(strokesToUpdate.First(), (col) => Work.Strokes.Remove(col));
                        doMyStrokeRemovedExceptHistory(stroke);
                    }
                    var newStroke = stroke.Clone();
                    newStroke.tag(new StrokeTag(oldTag.author, newPrivacy, oldTag.id, oldTag.startingSum, oldTag.isHighlighter)); 
                    if (Work.Strokes.Where(s => MeTLMath.ApproxEqual(s.sum().checksum, newStroke.sum().checksum)).Count() == 0)
                    {
                        newStrokes.Add(newStroke);
                        contentBuffer.AddStroke(newStroke, (col) => Work.Strokes.Add(newStroke));
                        doMyStrokeAddedExceptHistory(newStroke, newPrivacy);
                    }
                   
                }
                Dispatcher.adopt(() => Work.Select(Work.EditingMode == InkCanvasEditingMode.Select ? newStrokes : new StrokeCollection()));
            };
            Action undo = () =>
            {
                var newStrokes = new StrokeCollection();
                foreach (var stroke in selectedStrokes.Where(i => i is Stroke && i.tag().privacy != newPrivacy))
                {
                    var oldTag = stroke.tag();
                    var newStroke = stroke.Clone();
                    newStroke.tag(new StrokeTag(oldTag.author, newPrivacy, oldTag.id, oldTag.startingSum, oldTag.isHighlighter));

                    if (Work.Strokes.Where(s => MeTLMath.ApproxEqual(s.sum().checksum, newStroke.sum().checksum)).Count() > 0)
                    {
                        Work.Strokes.Remove(Work.Strokes.Where(s => MeTLMath.ApproxEqual(s.sum().checksum, newStroke.sum().checksum)).First());
                        doMyStrokeRemovedExceptHistory(newStroke);
                    }
                    if (Work.Strokes.Where(s => MeTLMath.ApproxEqual(s.sum().checksum, stroke.sum().checksum)).Count() == 0)
                    {
                        newStrokes.Add(newStroke);
                        Work.Strokes.Add(stroke);
                        doMyStrokeAddedExceptHistory(stroke, stroke.tag().privacy);
                    }
                }
            };   
            return new UndoHistory.HistoricalAction(undo, redo, 0, "Change selected ink privacy");
        }
        private UndoHistory.HistoricalAction changeSelectedImagePrivacy(IEnumerable<UIElement> selectedElements, Privacy newPrivacy, Privacy oldPrivacy)
        {
            Action redo = () =>
            {

                foreach (Image image in selectedElements.Where(i => i is Image && ((Image)i).tag().privacy != newPrivacy))
                {
                    var oldTag = image.tag();
                    Commands.SendDirtyImage.ExecuteAsync(new TargettedDirtyElement (Globals.slide, image.tag().author, _target, image.tag().privacy, image.tag().id));
                    oldTag.privacy = newPrivacy;
                    image.tag(oldTag);
                    var privateRoom = string.Format("{0}{1}", Globals.slide, image.tag().author);
                    if (newPrivacy == Privacy.Private && Globals.isAuthor && me != image.tag().author)
                        Commands.SneakInto.Execute(privateRoom);
                    Commands.SendImage.ExecuteAsync(new TargettedImage(Globals.slide, image.tag().author, _target, newPrivacy, image.tag().id, image));
                    if (newPrivacy == Privacy.Private && Globals.isAuthor && me != image.tag().author)
                        Commands.SneakOutOf.Execute(privateRoom);
                        
                }
            };
            Action undo = () =>
            {
                foreach (Image image in selectedElements.Where(i => i is Image && ((Image)i).tag().privacy != oldPrivacy))
                {
                    var oldTag = image.tag();
                    Commands.SendDirtyImage.ExecuteAsync(new TargettedDirtyElement (Globals.slide, image.tag().author, _target, image.tag().privacy, image.tag().id));
                    oldTag.privacy = oldPrivacy;
                    image.tag(oldTag);
                    var privateRoom = string.Format("{0}{1}", Globals.slide, image.tag().author);
                    if (oldPrivacy == Privacy.Private && Globals.isAuthor && me != image.tag().author)
                        Commands.SneakInto.Execute(privateRoom);
                    Commands.SendImage.ExecuteAsync(new TargettedImage(Globals.slide, image.tag().author, _target, oldPrivacy, image.tag().id, image));
                    if (oldPrivacy == Privacy.Private && Globals.isAuthor && me != image.tag().author)
                        Commands.SneakOutOf.Execute(privateRoom);
                        
                }
            };   
            return new UndoHistory.HistoricalAction(undo, redo, 0, "Image selection privacy changed");
        }
        private UndoHistory.HistoricalAction changeSelectedTextPrivacy(IEnumerable<UIElement> selectedElements, Privacy newPrivacy)
        {
            if (selectedElements == null) throw new ArgumentNullException("selectedElements");
            Action redo = () => Dispatcher.adopt(delegate
                                                     {
                                                         var mySelectedElements = selectedElements.Where(e => e is MeTLTextBox).Select(t => ((MeTLTextBox)t).clone());
                                                         foreach (MeTLTextBox textBox in mySelectedElements.Where(i => i.tag().privacy != newPrivacy))
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
                                  var mySelectedElements = selectedElements.Where(t => t is MeTLTextBox).Select(t => ((MeTLTextBox)t).clone());
                                  foreach (MeTLTextBox box in mySelectedElements)
                                  {
                                      if(Work.TextChildren().ToList().Where(tb => ((MeTLTextBox)tb).tag().id == box.tag().id).ToList().Count != 0)
                                          dirtyTextBoxWithoutHistory((MeTLTextBox)Work.TextChildren().ToList().Where(tb => ((MeTLTextBox)tb).tag().id == box.tag().id).ToList().First());
                                      sendTextWithoutHistory(box, box.tag().privacy);

                                  }
                              };
            return new UndoHistory.HistoricalAction(undo, redo, 0, "Text selection changed privacy");
        }
        private void changeSelectedItemsPrivacy(Privacy newPrivacy)
        {
            ClearAdorners();
            if (me == Globals.PROJECTOR) return;
            var selectedElements = new List<UIElement>();
            Dispatcher.adopt(() => selectedElements = Work.GetSelectedElements().ToList());
            var selectedStrokes = new List<Stroke>();
            Dispatcher.adopt(() => selectedStrokes = Work.GetSelectedStrokes().ToList());
            var oldPrivacy = determineOriginalPrivacy(newPrivacy);
            var ink = changeSelectedInkPrivacy(selectedStrokes, newPrivacy, oldPrivacy);
            var images = changeSelectedImagePrivacy(selectedElements, newPrivacy, oldPrivacy);
            var text = changeSelectedTextPrivacy(selectedElements, newPrivacy);
            Action redo = () =>
                              {
                                  ink.redo();
                                  text.redo();
                                  images.redo();
                                  ClearAdorners();
                                  Work.Focus();
                              };
            Action undo = () =>
                              {
                                  ink.undo();
                                  text.undo();
                                  images.undo();
                                  ClearAdorners();
                                  Work.Focus();
                              };
            redo();
            UndoHistory.Queue(undo, redo, "Selected items changed privacy");
        }
     protected static IEnumerable<Stroke> absoluteizeStrokes(IEnumerable<Stroke> selectedElements)
        {
            foreach (var stroke in selectedElements)
            {
                if (stroke.GetBounds().Top < 0)
                {
                    var top = Math.Abs(stroke.GetBounds().Top);
                    var strokeCollection = new StylusPointCollection();
                    foreach(var point in stroke.StylusPoints.Clone())
                        strokeCollection.Add(new StylusPoint(point.X, point.Y + top));
                    stroke.StylusPoints = strokeCollection;
                }
                if (stroke.GetBounds().Left < 0)
                {
                    var left = Math.Abs(stroke.GetBounds().Left);
                    var strokeCollection = new StylusPointCollection();
                    foreach(var point in stroke.StylusPoints.Clone())
                        strokeCollection.Add(new StylusPoint(point.X + left, point.Y));
                    stroke.StylusPoints = strokeCollection;
                }
            }
            return selectedElements;
        }
        private void singleStrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            var checksum = e.Stroke.sum().checksum;
            e.Stroke.tag(new StrokeTag(Globals.me, privacy, checksum.ToString(), checksum, e.Stroke.DrawingAttributes.IsHighlighter));
            var privateAwareStroke = new PrivateAwareStroke(e.Stroke, _target);
            Work.Strokes.Remove(e.Stroke);
            privateAwareStroke.startingSum(checksum);
            contentBuffer.AddStroke(privateAwareStroke, (st)=> Work.Strokes.Add(st));
            doMyStrokeAdded(privateAwareStroke);
            Commands.RequerySuggested(Commands.Undo);
        }
        public void doMyStrokeAdded(Stroke stroke)
        {
            doMyStrokeAdded(stroke, privacy);
        }
        public void doMyStrokeAdded(Stroke stroke, Privacy intendedPrivacy)
        {
            doMyStrokeAddedExceptHistory(stroke, intendedPrivacy);
            var thisStroke = stroke.Clone();
            UndoHistory.Queue(
                () =>
                {
                    ClearAdorners();
                    var existingStroke = Work.Strokes.Where(s => MeTLMath.ApproxEqual(s.sum().checksum, thisStroke.sum().checksum)).FirstOrDefault();
                    if (existingStroke != null)
                    {
                        Work.Strokes.Remove(existingStroke);
                        doMyStrokeRemovedExceptHistory(existingStroke);
                    }
                },
                () =>
                {
                    ClearAdorners();
                    if (Work.Strokes.Where(s => MeTLMath.ApproxEqual(s.sum().checksum, thisStroke.sum().checksum)).Count() == 0)
                    {
                        Work.Strokes.Add(thisStroke);
                        doMyStrokeAddedExceptHistory(thisStroke, thisStroke.tag().privacy);
                    }
                    if(Work.EditingMode == InkCanvasEditingMode.Select)
                        Work.Select(new StrokeCollection(new [] {thisStroke}));
                    AddAdorners();
                }, String.Format("Added stroke [checksum {0}]", thisStroke.sum().checksum));
        }
        private void erasingStrokes(object sender, InkCanvasStrokeErasingEventArgs e)
        {
            try
            {
                if (e.Stroke.tag().author != me)
                {
                    e.Cancel = true;
                    return;
                }
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
            var canvas = Work;
            var undo = new Action(() =>
                                 {
                                     // if stroke doesn't exist on the canvas 
                                     if (canvas.Strokes.Where(s => MeTLMath.ApproxEqual(s.sum().checksum, stroke.sum().checksum)).Count() == 0)
                                     {
                                         contentBuffer.AddStroke(stroke, (col) => canvas.Strokes.Add(col));
                                         doMyStrokeAddedExceptHistory(stroke, stroke.tag().privacy);
                                     }
                                 });
            var redo = new Action(() =>
                                 {
                                     var strokesToRemove = canvas.Strokes.Where(s => MeTLMath.ApproxEqual(s.sum().checksum, stroke.sum().checksum));
                                     if (strokesToRemove.Count() > 0)
                                     {
                                         contentBuffer.RemoveStroke(strokesToRemove.First(), (col) => canvas.Strokes.Remove(col)); 
                                         doMyStrokeRemovedExceptHistory(stroke);
                                     }
                                 });
            redo();
            UndoHistory.Queue(undo, redo, String.Format("Deleted stroke [checksum {0}]", stroke.sum().checksum));
        }

        private void doMyStrokeRemovedExceptHistory(Stroke stroke)
        {
            contentBuffer.RemoveStrokeChecksum(stroke, (cs) => strokeChecksums.Remove(cs));

            var sum = stroke.sum().checksum.ToString();
            Commands.SendDirtyStroke.Execute(new MeTLLib.DataTypes.TargettedDirtyElement(Globals.slide, stroke.tag().author,_target,stroke.tag().privacy,sum));
        }
        private void doMyStrokeAddedExceptHistory(Stroke stroke, Privacy thisPrivacy)
        {
            contentBuffer.AddStrokeChecksum(stroke, (cs) => 
            {
                if (!strokeChecksums.Contains(cs))
                    strokeChecksums.Add(cs);
            });

            var oldTag = stroke.tag();
            stroke.tag(new StrokeTag(oldTag.author, thisPrivacy, oldTag.id, oldTag.startingSum, stroke.DrawingAttributes.IsHighlighter));
            SendTargettedStroke(stroke, thisPrivacy);
        }
        public void SendTargettedStroke(Stroke stroke, Privacy thisPrivacy)
        {
            if (!stroke.shouldPersist()) return;
            var privateRoom = string.Format("{0}{1}", Globals.slide, stroke.tag().author);
            if(thisPrivacy == Privacy.Private && Globals.isAuthor && me != stroke.tag().author)
                Commands.SneakInto.Execute(privateRoom);
            Commands.SendStroke.Execute(new TargettedStroke(Globals.slide,stroke.tag().author,_target,stroke.tag().privacy, stroke.tag().id, stroke, stroke.tag().startingSum));
            if (thisPrivacy == Privacy.Private && Globals.isAuthor && me != stroke.tag().author)
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

                    Commands.SendImage.Execute(new TargettedImage(Globals.slide, me, _target, newImage.tag().privacy, newImage.tag().id, newImage));
                    break;
               
            }
        }
        private void dirtyThisElement(UIElement element)
        {
            switch (element.GetType().ToString())
            {
                case "System.Windows.Controls.Image":
                    var image = (Image)element;
                    var dirtyElement = new TargettedDirtyElement(Globals.slide, Globals.me, _target,image.tag().privacy, image.tag().id );
                    ApplyPrivacyStylingToElement(image, image.tag().privacy);
                    Commands.SendDirtyImage.Execute(dirtyElement);
                    break;
            }

        }
        public void ReceiveMoveDelta(TargettedMoveDelta moveDelta)
        {
            if (me == Globals.PROJECTOR || moveDelta.HasSameAuthor(me)) return;

            if (moveDelta != null && moveDelta.HasSameTarget(_target))
            {
                if (moveDelta.isDeleted)
                {
                    BatchDelete(moveDelta);
                    return;
                }

                if (moveDelta.newPrivacy != Privacy.NotSet)
                {
                    BatchPrivacyUpdate(moveDelta);
                    return;
                }

                // define work to be done based on fields
                var xTrans = moveDelta.xTranslate;
                var yTrans = moveDelta.yTranslate;
                var xScale = moveDelta.xScale;
                var yScale = moveDelta.yScale;

                var transformMatrix = new Matrix();
                transformMatrix.Scale(xScale, yScale);
                transformMatrix.Translate(xTrans, yTrans);

                foreach (var inkId in moveDelta.inkIds)
                {
                    var deadStrokes = new List<Stroke>();
                    foreach (var stroke in Work.Strokes.Where((s) => s.tag().id == inkId.Identity))
                    {
                        stroke.Transform(transformMatrix, false);
                    }
                }

                foreach (var textId in moveDelta.textIds)
                {
                    foreach (var textBox in Work.TextChildren().Where((t) => t.tag().id == textId.Identity))
                    {
                        // translate
                        var left = InkCanvas.GetLeft(textBox) + xTrans;
                        var top = InkCanvas.GetTop(textBox) + yTrans;

                        InkCanvas.SetLeft(textBox, left);
                        InkCanvas.SetTop(textBox, top);

                        // scale
                        /*if (double.IsNaN(textBox.Width) || double.IsNaN(textBox.Height))
                        {
                            textBox.Width = textBox.ActualWidth;
                            textBox.Height = textBox.ActualHeight;
                        }*/
                        textBox.Width *= xScale;
                        textBox.Height *= yScale;
                    }
                }

                foreach (var imageId in moveDelta.imageIds)
                {
                    foreach (var image in Work.ImageChildren().Where((i) => i.tag().id == imageId.Identity))
                    {
                        var left = InkCanvas.GetLeft(image) + xTrans;
                        var top = InkCanvas.GetTop(image) + yTrans;

                        InkCanvas.SetLeft(image, left);
                        InkCanvas.SetTop(image, top);

                        image.Width *= xScale;
                        image.Height *= yScale;
                    }
                }
            }
        }

        private void BatchDelete(TargettedMoveDelta moveDelta) 
        {
            var deadStrokes = new List<Stroke>();
            var deadTextboxes = new List<MeTLTextBox>();
            var deadImages = new List<Image>();

            foreach (var inkId in moveDelta.inkIds)
            {
                foreach (var stroke in Work.Strokes.Where((s) => s.tag().id == inkId.Identity))
                {
                    deadStrokes.Add(stroke);
                }
            }

            foreach (var textId in moveDelta.textIds)
            {
                foreach (var textBox in Work.TextChildren().Where((t) => t.tag().id == textId.Identity))
                {
                    deadTextboxes.Add(textBox);
                }
            }

            foreach (var imageId in moveDelta.imageIds)
            {
                foreach (var image in Work.ImageChildren().Where((i) => i.tag().id == imageId.Identity))
                {
                    deadImages.Add(image);
                }
            }

            // improve the contentbuffer to remove items either:
            // - by using list.RemoveAll(predicate)
            // - iterating backwards and removing the item at the index
            // - find all the items to be removed then list.Except(listDeletions) on the list
            foreach (var text in deadTextboxes)
            {
                contentBuffer.RemoveTextBox(text, (tb) => Work.Children.Remove(tb));
            }

            foreach (var image in deadImages)
            {
                contentBuffer.RemoveImage(image, (img) => Work.Children.Remove(img));
            }

            foreach (var stroke in deadStrokes)
            {
                contentBuffer.RemoveStroke(stroke, (col) => Work.Strokes.Remove(col));
                contentBuffer.RemoveStrokeChecksum(stroke, (cs) => strokeChecksums.Remove(cs));
            }
        }

        private void BatchPrivacyUpdate(TargettedMoveDelta moveDelta)
        {
            var privacyStrokes = new List<Stroke>();
            var privacyTextboxes = new List<MeTLTextBox>();
            var privacyImages = new List<Image>();

            Func<Stroke, bool> wherePredicate = (s) => { /* compare tag identity and check if privacy differs*/ return true; };

            foreach (var inkId in moveDelta.inkIds)
            {
                foreach (var stroke in Work.Strokes.Where((s) => 
                    { 
                        var strokeTag = s.tag();
                        return strokeTag.id == inkId.Identity && strokeTag.privacy == moveDelta.newPrivacy; 
                    }))
                {
                    privacyStrokes.Add(stroke);
                }
            }

            foreach (var textId in moveDelta.textIds)
            {
                foreach (var textBox in Work.TextChildren().Where((t) => 
                    { 
                        var textTag = t.tag(); 
                        return textTag.id == textId.Identity && textTag.privacy == moveDelta.newPrivacy; 
                    }))
                {
                    privacyTextboxes.Add(textBox);
                }
            }

            foreach (var imageId in moveDelta.imageIds)
            {
                foreach (var image in Work.ImageChildren().Where((i) => 
                    {
                        var imageTag = i.tag();
                        return i.tag().id == imageId.Identity && imageTag.privacy == moveDelta.newPrivacy;
                    }))
                {
                    privacyImages.Add(image);
                }
            }

            foreach (var stroke in privacyStrokes)
            {
                var oldTag = stroke.tag();
                contentBuffer.RemoveStroke(stroke, (col) => Work.Strokes.Remove(col));

                stroke.tag(new StrokeTag(oldTag.author, moveDelta.newPrivacy, oldTag.id, oldTag.startingSum, oldTag.isHighlighter)); 
                contentBuffer.AddStroke(stroke, (col) => Work.Strokes.Add(col));
            }

            foreach (var image in privacyImages)
            {
                var oldTag = image.tag();
                oldTag.privacy = moveDelta.newPrivacy;

                image.tag(oldTag);
                ApplyPrivacyStylingToElement(image, moveDelta.newPrivacy);
            }

            foreach (var text in privacyTextboxes)
            {
                var oldTag = text.tag();
                oldTag.privacy = moveDelta.newPrivacy;

                text.tag(oldTag);
                ApplyPrivacyStylingToElement(text, moveDelta.newPrivacy);
            }
        }

        public void ReceiveImages(IEnumerable<TargettedImage> images)
        {
            foreach (var image in images)
            {
                if (image.slide == Globals.slide && image.target == _target)
                {
                    TargettedImage image1 = image;
                    if (image.HasSameAuthor(me) || image.HasSamePrivacy(Privacy.Public))
                    {
                        Dispatcher.adoptAsync(() => 
                        {
                            var receivedImage = image1.image;
                            AddImage(Work, receivedImage); 
                            ApplyPrivacyStylingToElement(receivedImage, receivedImage.tag().privacy);
                        });
                    }
                }
            }
            //ensureAllImagesHaveCorrectPrivacy();
        }

        private void AddImage(InkCanvas canvas, Image image)
        {
            if (canvas.ImageChildren().Any(i => ((Image) i).tag().id == image.tag().id)) return;

            contentBuffer.AddImage(image, (img) =>
            {
                Panel.SetZIndex(img, 2);
                canvas.Children.Add(img);
            });
        }
        public void ReceiveDirtyImage(TargettedDirtyElement element)
        {
            if (!(element.target.Equals(_target))) return;
            if (element.slide != Globals.slide) return;
            Dispatcher.adoptAsync(() => dirtyImage(element.identity));
        }

        private void dirtyImage(string imageId)
        {
            var imagesToRemove = new List<Image>();
            foreach (var currentImage in Work.Children.OfType<Image>())
            {
                if (imageId.Equals(currentImage.tag().id))
                    imagesToRemove.Add(currentImage);
            }

            foreach (var removeImage in imagesToRemove)
            {
                contentBuffer.RemoveImage(removeImage, (img) => Work.Children.Remove(removeImage));
            }
        }

        private void ensureAllImagesHaveCorrectPrivacy()
        {
            Dispatcher.adoptAsync(delegate
            {
                foreach (Image image in Work.Children.OfType<Image>())
                    ApplyPrivacyStylingToElement(image, image.tag().privacy);
            });
        }
        protected void ApplyPrivacyStylingToElement(FrameworkElement element, Privacy privacy)
        {
            if ((!Globals.conversationDetails.Permissions.studentCanPublish && !Globals.isAuthor) || (_target == "notepad"))
            {
                RemovePrivacyStylingFromElement(element);
                return;
            }
            if (privacy != Privacy.Private)
            {
                RemovePrivacyStylingFromElement(element);
                return;
            }

            applyShadowEffectTo(element, Colors.Black);
        }

        public FrameworkElement ApplyHighlight(FrameworkElement element, Color color)
        {
            element.Effect = new DropShadowEffect { BlurRadius = 20, Color = color, ShadowDepth = 0, Opacity = 1 };
            return element;
        }

        public FrameworkElement applyShadowEffectTo(FrameworkElement element, Color color)
        {
            element.Effect = new DropShadowEffect { BlurRadius = 50, Color = color, ShadowDepth = 0, Opacity = 1 };
            element.Opacity = 0.7;
            contentBuffer.UpdateChild(element, (elem) =>
            {
                elem.Effect = new DropShadowEffect { BlurRadius = 50, Color = color, ShadowDepth = 0, Opacity = 1 };
                elem.Opacity = 0.7;
            });
            return element;
        }
        protected void RemovePrivacyStylingFromElement(FrameworkElement element)
        {
            element.Effect = null;
            element.Opacity = 1;

            contentBuffer.UpdateChild(element, (elem) =>
            {
                elem.Effect = null;
                elem.Opacity = 1;
            });
        }
        #endregion
        #region imagedrop
        private void addImageFromDisk(object obj)
        {
            addResourceFromDisk((files) =>
            {
                var origin = new Point(0, 0);
                int i = 0;
                foreach (var file in files)
                    handleDrop(file, origin, true, i++, (source, offset, count) => { return offset; });
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
        private void imagesDropped(List<ImageDrop> images)
        {
            foreach(var image in images)
                imageDropped(image);
        }
        private void imageDropped(ImageDrop drop)
        {
            try
            {
                if (drop.Target.Equals(_target) && me != Globals.PROJECTOR)
                    handleDrop(drop.Filename, new Point(0, 0), drop.OverridePoint, drop.Position, (source, offset, count) => { return drop.Point; });
            }
            catch (NotSetException)
            {
                //YAY
            }
        }
        private void addResourceFromDisk(Action<IEnumerable<string>> withResources)
        {
            const string filter = "Image files(*.jpeg;*.gif;*.bmp;*.jpg;*.png)|*.jpeg;*.gif;*.bmp;*.jpg;*.png|All files (*.*)|*.*";
            addResourceFromDisk(filter, withResources);
        }

        private void addResourceFromDisk(string filter, Action<IEnumerable<string>> withResources)
        {
            if (_target == "presentationSpace" && me != Globals.PROJECTOR)
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
            if (me == Globals.PROJECTOR) return;
            var validFormats = e.Data.GetFormats();
            var fileNames = new string[0];
            validFormats.Select(vf => {
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
                MeTLMessage.Information("Cannot drop this onto the canvas");
                return;
            }
            Commands.SetLayer.ExecuteAsync("Insert");
            var pos = e.GetPosition(this);
            var origin = new Point(pos.X, pos.Y);
            var maxHeight = 0d;

            Func<Image, Point, int, Point> positionUpdate = (source, offset, count) =>
                {
                    if (count == 0)
                    {
                        pos.Offset(offset.X, offset.Y);
                        origin.Offset(offset.X, offset.Y);
                    }

                    var curPos = new Point(pos.X, pos.Y);
                    pos.X += source.Source.Width + source.Margin.Left * 2 + 30;
                    if (source.Source.Height + source.Margin.Top * 2 > maxHeight) 
                        maxHeight = source.Source.Height + source.Margin.Top * 2;
                    if ((count + 1) % 4 == 0)
                    {
                        pos.X = origin.X;
                        pos.Y += (maxHeight + 30);
                        maxHeight = 0.0;
                    }

                    return curPos;
                };
            //lets try for a 4xN grid
            for (var i = 0; i < fileNames.Count(); i++)
            {
                var filename = fileNames[i];
                handleDrop(filename, origin, true, i, positionUpdate);
            }
            e.Handled = true;
        }
        protected void ImageDragOverCancel(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        public void handleDrop(string fileName, Point origin, bool overridePoint, int count, Func<Image, Point, int, Point> positionUpdate)
        {
            FileType type = GetFileType(fileName);
            switch (type)
            {
                case FileType.Image:
                    dropImageOnCanvas(fileName, origin, count, overridePoint, positionUpdate);
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
                MeTLMessage.Warning("Sorry, your filename is too long, must be less than 260 characters");
                return;
            }
            if (isFileLessThanXMB(unMangledFilename, fileSizeLimit))
            {
                var worker = new BackgroundWorker();
                worker.DoWork += (s, e) =>
                 {
                     File.Copy(unMangledFilename, filename);
                     MeTLLib.ClientFactory.Connection().UploadAndSendFile(
                         new MeTLStanzas.LocalFileInformation(Globals.slide, Globals.me, _target, Privacy.Public, filename, Path.GetFileNameWithoutExtension(filename), false, new FileInfo(filename).Length, DateTimeFactory.Now().Ticks.ToString(),Globals.generateId(filename)));
                     File.Delete(filename);
                 };
                worker.RunWorkerCompleted += (s, a) => Dispatcher.Invoke(DispatcherPriority.Send,
                                                                                   (Action)(() => MeTLMessage.Information(string.Format("Finished uploading {0}.", unMangledFilename))));
                worker.RunWorkerAsync();
            }
            else
            {
                MeTLMessage.Warning(String.Format("Sorry, your file is too large, must be less than {0}mb", fileSizeLimit));
                return;
            }
        }
        public void dropImageOnCanvas(string fileName, Point origin, int count, bool useDefaultMargin, Func<Image, Point, int, Point> positionUpdate)
        {
            if (!isFileLessThanXMB(fileName, fileSizeLimit))
            {
                MeTLMessage.Warning(String.Format("Sorry, your file is too large, must be less than {0}mb", fileSizeLimit));
                return;
            }
            Dispatcher.adoptAsync(() =>
            {
                var imagePos = new Point(0, 0);
                System.Windows.Controls.Image image = null;
                try
                {
                    image = createImageFromUri(new Uri(fileName, UriKind.RelativeOrAbsolute), useDefaultMargin);

                    if (useDefaultMargin)
                    {
                        if (imagePos.X < 50)
                            imagePos.X = 50;
                        if (imagePos.Y < 50)
                            imagePos.Y = 50;
                    }

                    imagePos = positionUpdate(image, imagePos, count);
                }
                catch (Exception e)
                {
                    MeTLMessage.Warning("Sorry could not create an image from this file :" + fileName + "\n Error: " + e.Message);
                    return;
                }
                if (image == null)
                    return;
                // center the image horizonally if there is no margin set. this is only used for dropping quiz result images on the canvas
                if (!useDefaultMargin)
                {
                    imagePos.X = (Globals.DefaultCanvasSize.Width / 2) - ((image.Width + (Globals.QuizMargin * 2)) / 2);
                    //pos.Y = (Globals.DefaultCanvasSize.Height / 2) - (image.Height / 2);
                }
                InkCanvas.SetLeft(image, imagePos.X);
                InkCanvas.SetTop(image, imagePos.Y);
                image.tag(new ImageTag(Globals.me, privacy, Globals.generateId(), false, 0));
                var myImage = image.clone();
                var currentSlide = Globals.slide;
                Action undo = () =>
                                  {
                                      ClearAdorners();
                                      contentBuffer.RemoveImage(myImage, (img) => Work.Children.Remove(myImage));
                                      /*if (Work.ImageChildren().Any(i => ((Image)i).tag().id == myImage.tag().id))
                                      {
                                          var imageToRemove = Work.ImageChildren().First(i => ((Image) (i)).tag().id == myImage.tag().id);
                                          Work.Children.Remove(imageToRemove);
                                      }*/
                                      dirtyThisElement(myImage);
                                  };
                Action redo = () =>
                {
                    if (!fileName.StartsWith("http"))
                        MeTLLib.ClientFactory.Connection().UploadAndSendImage(new MeTLStanzas.LocalImageInformation(currentSlide, Globals.me, _target, privacy, myImage, fileName, false));
                    else
                        MeTLLib.ClientFactory.Connection().SendImage(new TargettedImage(currentSlide, Globals.me, _target, privacy, myImage.tag().id, myImage));

                    ClearAdorners();
                    InkCanvas.SetLeft(myImage, imagePos.X);
                    InkCanvas.SetTop(myImage, imagePos.Y);
                    contentBuffer.AddImage(myImage, (img) => 
                    {
                        if (!Work.Children.Contains(img))
                            Work.Children.Add(img);
                    });
                    //sendThisElement(myImage);

                    Work.Select(new[] { myImage });
                    AddAdorners();
                };
                redo();
                UndoHistory.Queue(undo, redo, "Dropped Image");
            });
        }

        public System.Windows.Controls.Image createImageFromUri(Uri uri, bool useDefaultMargin)
        {
            var image = new System.Windows.Controls.Image();

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = uri;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();

            image.Source = bitmapImage;
            // images with a high dpi eg 300 were being drawn relatively small compared to images of similar size with dpi ~100
            // which is correct but not what people expect.
            // image size is determined from dpi

            // next two lines were used
            //image.Height = bitmapImage.Height;
            //image.Width = bitmapImage.Width;

            // image size will match reported size
            //image.Height = jpgFrame.PixelHeight;
            //image.Width = jpgFrame.PixelWidth;
            image.Stretch = Stretch.Uniform;
            image.StretchDirection = StretchDirection.Both;
            if (useDefaultMargin)
            {
                image.Margin = new Thickness(5);
            }
            else
            {
                image.Margin = new Thickness(Globals.QuizMargin);
            }
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
            if (Work.EditingMode != InkCanvasEditingMode.None) return;
            if(me == Globals.PROJECTOR) return;
            var pos = e.GetPosition(this);
            MeTLTextBox box = createNewTextbox();
            AddTextBoxToCanvas(box);
            InkCanvas.SetLeft(box, pos.X);
            InkCanvas.SetTop(box, pos.Y);
            myTextBox = box;
            box.Focus();
        }
        private void AddTextboxToMyCanvas(MeTLTextBox box)
        {
            contentBuffer.AddTextBox(applyDefaultAttributes(box), (text) => Work.Children.Add(text));
        }
        public void DoText(TargettedTextBox targettedBox)
        {
            Dispatcher.adoptAsync(delegate
                                      {
                                          if (targettedBox.target != _target) return;
                                          if (targettedBox.slide == Globals.slide &&
                                              (targettedBox.HasSamePrivacy(Privacy.Public) || targettedBox.HasSameAuthor(me)))
                                          {

                                              //var box = targettedBox.box.toMeTLTextBox();
                                              var box = UpdateTextBoxWithId(targettedBox);
                                              //RemoveTextBoxWithMatchingId(targettedBox.identity);
                                              //AddTextBoxToCanvas(box); 
                                              if (box != null)
                                              {
                                                  if (!(targettedBox.HasSameAuthor(me) && _focusable))
                                                      box.Focusable = false;
                                                  ApplyPrivacyStylingToElement(box, targettedBox.privacy);
                                              }
                                          }
                                      });

        }

        private MeTLTextBox UpdateTextBoxWithId(TargettedTextBox textbox)
        {
            // find textbox if it exists, otherwise create it
            var createdNew = false;
            var box = textBoxFromId(textbox.identity);
            if (box == null)
            {
                box = textbox.box.toMeTLTextBox();
                createdNew = true;
            }

            var oldBox = textbox.box;
            // update with changes
            box.AcceptsReturn = true;
            box.TextWrapping = TextWrapping.WrapWithOverflow;
            box.BorderThickness = new Thickness(0);
            box.BorderBrush = new SolidColorBrush(Colors.Transparent);
            box.Background = new SolidColorBrush(Colors.Transparent);
            box.tag(oldBox.tag());
            box.FontFamily = oldBox.FontFamily;
            box.FontStyle = oldBox.FontStyle;
            box.FontWeight = oldBox.FontWeight;
            box.TextDecorations = oldBox.TextDecorations;
            box.FontSize = oldBox.FontSize;
            box.Foreground = oldBox.Foreground;
            box.TextChanged -= SendNewText;
            var caret = box.CaretIndex;
            box.Text = oldBox.Text;
            box.CaretIndex = caret;
            box.TextChanged += SendNewText;
            box.Width = oldBox.Width;
            //box.Height = OldBox.Height;
            InkCanvas.SetLeft(box, InkCanvas.GetLeft(oldBox));
            InkCanvas.SetTop(box, InkCanvas.GetTop(oldBox));

            if (createdNew)
                AddTextBoxToCanvas(box);

            return box;
        }

        private MeTLTextBox applyDefaultAttributes(MeTLTextBox box)
        {
            box.TextChanged -= SendNewText;
            box.AcceptsReturn = true;
            box.TextWrapping = TextWrapping.WrapWithOverflow;
            box.GotFocus += textboxGotFocus;
            box.LostFocus += textboxLostFocus;
            box.PreviewKeyDown += box_PreviewTextInput;
            box.TextChanged += SendNewText;
            box.IsUndoEnabled = false;
            box.UndoLimit = 0;
            box.BorderThickness = new Thickness(0);
            box.BorderBrush = new SolidColorBrush(Colors.Transparent);
            box.Background = new SolidColorBrush(Colors.Transparent);
            box.Focusable = CanFocus;
            box.ContextMenu = new ContextMenu {IsEnabled = true};
            box.ContextMenu.IsEnabled = false;
            box.ContextMenu.IsOpen = false;
            box.PreviewMouseRightButtonUp += box_PreviewMouseRightButtonUp;
            return box;
        }

        private void box_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private MeTLTextBox FindOrCreateTextBoxFromId(MeTLTextBox textbox)
        {
            var box = textBoxFromId(textbox.tag().id);
            if (box == null)
            {
                box = textbox.clone();
                AddTextBoxToCanvas(box);
            }

            InkCanvas.SetLeft(box, InkCanvas.GetLeft(textbox));
            InkCanvas.SetTop(box, InkCanvas.GetTop(textbox));

            return box;
        }

        private MeTLTextBox UpdateTextBoxWithId(MeTLTextBox textbox, string newText)
        {
            // find textbox if it exists, otherwise create it
            var box = FindOrCreateTextBoxFromId(textbox); 

            box.TextChanged -= SendNewText;
            box.Text = newText;
            box.TextChanged += SendNewText;

            return box;
        }

        private void SendNewText(object sender, TextChangedEventArgs e)
        {
            if (_originalText == null) return; 
            if(me == Globals.PROJECTOR) return;
            var box = (MeTLTextBox)sender;
            var undoText = _originalText.Clone().ToString();
            var redoText = box.Text.Clone().ToString();
            ApplyPrivacyStylingToElement(box, box.tag().privacy);
            box.Height = Double.NaN;
            var mybox = box.clone();
            Action undo = () =>
            {
                ClearAdorners();
                var myText = undoText;
                var updatedTextBox = UpdateTextBoxWithId(mybox, myText);
                sendTextWithoutHistory(updatedTextBox, updatedTextBox.tag().privacy);
            };
            Action redo = () =>
            {
                ClearAdorners();
                var myText = redoText;
                var updatedTextBox = UpdateTextBoxWithId(mybox, myText);
                sendTextWithoutHistory(mybox, mybox.tag().privacy);
            }; 
            UndoHistory.Queue(undo, redo, String.Format("Added text [{0}]", redoText));

            // only do the change locally and let the timer do the rest
            ClearAdorners();
            UpdateTextBoxWithId(mybox, redoText);

            var currentSlide = Globals.slide;
            Action typingTimedAction = () =>
            {
                Dispatcher.adoptAsync(delegate
                {
                    var senderTextBox = sender as MeTLTextBox;
                    sendTextWithoutHistory(senderTextBox, privacy, currentSlide);
                    TypingTimer = null;
                    GlobalTimers.ExecuteSync();
                });
            };
            if (TypingTimer == null)
            {
                TypingTimer = new TypingTimedAction();
                TypingTimer.Add(typingTimedAction);
            }
            else
            {
                GlobalTimers.ResetSyncTimer();
                TypingTimer.ResetTimer();
            }
        }
        protected static IEnumerable<UIElement> absoluteizeElements(IEnumerable<UIElement> selectedElements)
        {
            foreach (FrameworkElement element in selectedElements)
            {
                if (InkCanvas.GetLeft(element) < 0)
                    InkCanvas.SetLeft(element, 0);
                if (InkCanvas.GetTop(element) < 0)
                    InkCanvas.SetTop(element, 0);

            }
            return selectedElements;
        }
        private void resetTextbox(object obj)
        {
            if (myTextBox == null && Work.GetSelectedElements().Count != 1) return;
            if (myTextBox == null)
                myTextBox = (MeTLTextBox)Work.GetSelectedElements().Where(b => b is MeTLTextBox).First();

            var currentTextBox = filterOnlyMine<MeTLTextBox>(myTextBox); 
            if (currentTextBox == null)
                return;

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
            UndoHistory.Queue(undo, redo, "Restored text defaults");
            redo();
        }
        private void resetText(MeTLTextBox box)
        {
            RemovePrivacyStylingFromElement(box);
            _currentColor = Colors.Black;
            box.FontWeight = FontWeights.Normal;
            box.FontStyle = FontStyles.Normal;
            box.TextDecorations = new TextDecorationCollection();
            box.FontFamily = new FontFamily("Arial");
            box.FontSize = 24;
            box.Foreground = Brushes.Black;
            var info = new TextInformation
                           {
                               Family = box.FontFamily,
                               Size = box.FontSize,
                           };
            Commands.TextboxFocused.ExecuteAsync(info);
            sendTextWithoutHistory(box, box.tag().privacy);
        }
        private void updateStyling(TextInformation info)
        {
            try
            {
                var selectedTextBoxes = new List<MeTLTextBox>();
                selectedTextBoxes.AddRange(Work.GetSelectedElements().OfType<MeTLTextBox>());
                if (filterOnlyMine<MeTLTextBox>(myTextBox) != null)
                    selectedTextBoxes.Add(myTextBox);
                var selectedTextBox = selectedTextBoxes.FirstOrDefault(); // only support changing style for one textbox at a time

                if (selectedTextBox != null)
                {
                    // create a clone of the selected textboxes and their textinformation so we can keep a reference to something that won't be changed
                    var clonedTextBox = selectedTextBox.clone();
                    var clonedTextInfo = getInfoOfBox(selectedTextBox); 

                    Action undo = () =>
                      {
                          //ClearAdorners();

                          // find the textboxes on the canvas, if they've been deleted recreate and add to the canvas again
                          var activeTextbox = FindOrCreateTextBoxFromId(clonedTextBox);
                          if (activeTextbox != null)
                          {
                              var activeTextInfo = clonedTextInfo;
                              activeTextbox.TextChanged -= SendNewText;
                              applyStylingTo(activeTextbox, activeTextInfo);
                              Commands.TextboxFocused.ExecuteAsync(activeTextInfo);
                              //AddAdorners();
                              sendTextWithoutHistory(activeTextbox, activeTextbox.tag().privacy);
                              activeTextbox.TextChanged += SendNewText;
                          }
                      };
                    Action redo = () =>
                      {
                          //ClearAdorners();

                          var activeTextbox = FindOrCreateTextBoxFromId(clonedTextBox);
                          if (activeTextbox != null)

                          {
                              var activeTextInfo = info; 
                              activeTextbox.TextChanged -= SendNewText;
                              applyStylingTo(activeTextbox, activeTextInfo);
                              Commands.TextboxFocused.ExecuteAsync(activeTextInfo);
                              //AddAdorners();
                              sendTextWithoutHistory(activeTextbox, activeTextbox.tag().privacy);
                              activeTextbox.TextChanged += SendNewText;
                          }
                      };
                    UndoHistory.Queue(undo, redo, "Styling of text changed");
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
            currentTextBox.FontStyle = info.Italics ? FontStyles.Italic : FontStyles.Normal;
            currentTextBox.FontWeight = info.Bold ? FontWeights.Bold : FontWeights.Normal;
            currentTextBox.TextDecorations = new TextDecorationCollection();
            if(info.Underline)
                currentTextBox.TextDecorations = TextDecorations.Underline;
            else if(info.Strikethrough)
                currentTextBox.TextDecorations= TextDecorations.Strikethrough;
            currentTextBox.FontSize = info.Size;
            currentTextBox.FontFamily = info.Family;
            currentTextBox.Foreground = new SolidColorBrush(info.Color);
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
                           Bold = box.FontWeight == FontWeights.Bold,
                           Italics = box.FontStyle == FontStyles.Italic,
                           Size = box.FontSize,
                           Underline = underline,
                           Strikethrough = strikethrough,
                           Family = box.FontFamily,
                           Color = ((SolidColorBrush) box.Foreground).Color
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
            if(!Work.Children.ToList().Any(c => c is MeTLTextBox &&((MeTLTextBox)c).tag().id == box.tag().id))
                AddTextBoxToCanvas(box);
            box.PreviewKeyDown += box_PreviewTextInput;
            sendTextWithoutHistory(box, box.tag().privacy);
        }
        public void sendTextWithoutHistory(MeTLTextBox box, Privacy thisPrivacy)
        {
            sendTextWithoutHistory(box, thisPrivacy, Globals.slide);
        }
        public void sendTextWithoutHistory(MeTLTextBox box, Privacy thisPrivacy, int slide)
        {
            if (box.tag().privacy != privacy)
                dirtyTextBoxWithoutHistory(box);
            var oldTextTag = box.tag();
            var newTextTag = new TextTag(oldTextTag.author, thisPrivacy, oldTextTag.id);
            box.tag(newTextTag);
            var privateRoom = string.Format("{0}{1}", Globals.slide, box.tag().author);
            if (thisPrivacy == Privacy.Private && Globals.isAuthor && me != box.tag().author)
                Commands.SneakInto.Execute(privateRoom);
            Commands.SendTextBox.ExecuteAsync(new TargettedTextBox(slide, box.tag().author, _target, thisPrivacy, box.tag().id, box));
            if (thisPrivacy == Privacy.Private && Globals.isAuthor && me != box.tag().author)
                Commands.SneakOutOf.Execute(privateRoom);
        }
        private void dirtyTextBoxWithoutHistory(MeTLTextBox box)
        {
            dirtyTextBoxWithoutHistory(box, Globals.slide);
        }

        private void dirtyTextBoxWithoutHistory(MeTLTextBox box, int slide)
        {
            RemovePrivacyStylingFromElement(box);
            RemoveTextBoxWithMatchingId(box.tag().id);
            Commands.SendDirtyText.ExecuteAsync(new TargettedDirtyElement(slide, box.tag().author, _target, box.tag().privacy, box.tag().id));
        }

        private void box_PreviewTextInput(object sender, KeyEventArgs e)
        {
            _originalText = ((MeTLTextBox)sender).Text;
            e.Handled = false;
        }
        
        private void textboxLostFocus(object sender, RoutedEventArgs e)
        {
            var box = (MeTLTextBox)sender;
            var currentTag = box.tag();
            ClearAdorners();
            /*if (currentTag.privacy != Globals.privacy)
            {
                Commands.SendDirtyText.ExecuteAsync(new TargettedDirtyElement(Globals.slide, Globals.me, _target, currentTag.privacy, currentTag.id));
                currentTag.privacy = privacy;
                box.tag(currentTag);
                Commands.SendTextBox.ExecuteAsync(new TargettedTextBox(Globals.slide, Globals.me, _target, currentTag.privacy, box));
            }*/
            myTextBox = null;
            requeryTextCommands();
            if (box.Text.Length == 0)
            {
                if (TextBoxExistsOnCanvas(box, true))
                    dirtyTextBoxWithoutHistory(box);
                    //Work.Children.Remove(box);
            }
            else
                setAppropriatePrivacyHalo(box);
        }

        private void setAppropriatePrivacyHalo(MeTLTextBox box)
        {
            if (!Work.Children.Contains(box)) return;
            ApplyPrivacyStylingToElement(box, privacy);
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
            if (((MeTLTextBox)sender).tag().author != me) return; //cannot edit other peoples textboxes
            if (me != Globals.PROJECTOR)
            {
                myTextBox = (MeTLTextBox)sender;
            } 
            CommandManager.InvalidateRequerySuggested();
            if (myTextBox == null) 
                return;
            updateTools();
            requeryTextCommands();
            _originalText = myTextBox.Text;
            Commands.ChangeTextMode.ExecuteAsync("None");
        }
        private void updateTools()
        {
            var info = new TextInformation
            {
                Family = _defaultFamily,
                Size = DefaultSize,
                Bold = false,
                Italics = false,
                Strikethrough = false,
                Underline = false,
                Color = Colors.Black
            };
            if (myTextBox != null)
            {
                info = new TextInformation
                {
                    Family = myTextBox.FontFamily,
                    Size = myTextBox.FontSize,
                    Bold = myTextBox.FontWeight == FontWeights.Bold,
                    Italics = myTextBox.FontStyle == FontStyles.Italic,
                    Color = ((SolidColorBrush)myTextBox.Foreground).Color
                };
                
                if (myTextBox.TextDecorations.Count > 0)
                {
                    info.Strikethrough = myTextBox.TextDecorations.First().Location.ToString().ToLower() == "strikethrough";
                    info.Underline = myTextBox.TextDecorations.First().Location.ToString().ToLower() == "underline";
                }
                info.IsPrivate = myTextBox.tag().privacy == Privacy.Private ? true : false; 
                Commands.TextboxFocused.ExecuteAsync(info);
            }

        }

        private bool TextBoxExistsOnCanvas(MeTLTextBox box, bool privacyMatches)
        {
            var result = false;
            Dispatcher.adopt(() =>
            {
                var boxId = box.tag().id;
                var privacy = box.tag().privacy;
                foreach (var text in Work.Children)
                    if (text is MeTLTextBox)
                        if (((MeTLTextBox)text).tag().id == boxId && ((MeTLTextBox)text).tag().privacy == privacy) result = true;
            });
            return result;
        }

        private bool alreadyHaveThisTextBox(MeTLTextBox box)
        {
            return TextBoxExistsOnCanvas(box, true);
        }

        private void RemoveTextBoxWithMatchingId(string id)
        {
            var removeTextbox = textBoxFromId(id);
            if (removeTextbox != null)
            {
                contentBuffer.RemoveTextBox(removeTextbox, (tb) => Work.Children.Remove(tb));
            }
        }
        private void receiveDirtyText(TargettedDirtyElement element)
        {
            if (!(element.target.Equals(_target))) return;
            if (element.slide != Globals.slide) return;
            Dispatcher.adoptAsync(delegate
            {
                if (myTextBox != null && element.HasSameIdentity(myTextBox.tag().id)) return;
                if (element.author == me) return;
                RemoveTextBoxWithMatchingId(element.identity);
            });
        }

        private bool TargettedTextBoxIsFocused(TargettedTextBox targettedBox)
        {
            var focusedTextBox = Keyboard.FocusedElement as MeTLTextBox;
            if (focusedTextBox != null && focusedTextBox.tag().id == targettedBox.identity)
            {
                return true;
            }
            return false; 
        }

        public void ReceiveTextBox(TargettedTextBox targettedBox)
        {
            if (targettedBox.target != _target ) return;
            //if (targettedBox.box.Text.Length == 0) return;

            if (me != Globals.PROJECTOR && TargettedTextBoxIsFocused(targettedBox))
                return;

            if (targettedBox.HasSameAuthor(me) && alreadyHaveThisTextBox(targettedBox.box.toMeTLTextBox()) && me != Globals.PROJECTOR)
            {
                /*var box = textBoxFromId(targettedBox.identity);
                if (box != null)
                    ApplyPrivacyStylingToElement(box, box.tag().privacy);*/
                DoText(targettedBox);
                return;
            }//I never want my live text to collide with me.
            if (targettedBox.slide == Globals.slide && (targettedBox.HasSamePrivacy(Privacy.Private) || me == Globals.PROJECTOR))
                RemoveTextBoxWithMatchingId(targettedBox.identity);
            if (targettedBox.slide == Globals.slide && (targettedBox.HasSamePrivacy(Privacy.Public) || (targettedBox.HasSameAuthor(me) && me != Globals.PROJECTOR)))
                  DoText(targettedBox);
        }

        private bool CanvasHasActiveFocus()
        {
            return Globals.CurrentCanvasClipboardFocus == _target;
        }

        private MeTLTextBox textBoxFromId(string boxId)
        {
            MeTLTextBox result = null;
            var boxes = new List<MeTLTextBox>();
            boxes.AddRange(Work.Children.OfType<MeTLTextBox>());
            Dispatcher.adopt(() =>
            {
                result = boxes.Where(text => text.tag().id == boxId).FirstOrDefault();
            });
            return result;
        }
        public bool CanHandleClipboardPaste()
        {
            return CanvasHasActiveFocus(); 
        }

        public bool CanHandleClipboardCut()
        {
            return CanvasHasActiveFocus(); 
        }

        public bool CanHandleClipboardCopy()
        {
            return CanvasHasActiveFocus(); 
        }

        public void OnClipboardPaste()
        {
            HandlePaste(null);
        }
        public MeTLTextBox createNewTextbox()
        {
            var box = new MeTLTextBox();
            box.tag(new TextTag
                        {
                            author = Globals.me,
                            privacy = privacy,
                            id = string.Format("{0}:{1}", Globals.me, DateTimeFactory.Now().Ticks)
                        });
            box.FontFamily = _currentFamily;
            box.FontSize = _currentSize;
            box.Foreground = new SolidColorBrush(_currentColor);
            box.UndoLimit = 0;
            box.LostFocus += (_sender, _args) =>
            {
                myTextBox = null;

            };
            return applyDefaultAttributes(box);
        }
        public void OnClipboardCopy()
        {
            HandleCopy(null);
        }

        public void OnClipboardCut()
        {
            HandleCut(null);
        }
        public void HandleInkPasteUndo(List<Stroke> newStrokes)
        {
            //var selection = new StrokeCollection();
            ClearAdorners();
            foreach (var stroke in newStrokes)
            {
                if (Work.Strokes.Where(s => MeTLMath.ApproxEqual(s.sum().checksum, stroke.sum().checksum)).Count() > 0)
                {
                    //selection = new StrokeCollection(selection.Where(s => !MeTLMath.ApproxEqual(s.sum().checksum, stroke.sum().checksum)));
                    doMyStrokeRemovedExceptHistory(stroke);
                }
            }
        }
        public void HandleInkPasteRedo(List<Stroke> newStrokes)
        {
            //var selection = new StrokeCollection();
            ClearAdorners();
            foreach (var stroke in newStrokes)
            {
                if (Work.Strokes.Where(s => MeTLMath.ApproxEqual(s.sum().checksum, stroke.sum().checksum)).Count() == 0)
                {
                    //stroke.tag(new StrokeTag(stroke.tag().author, privacy, stroke.tag().startingSum, stroke.tag().isHighlighter));
                    //selection.Add(stroke);
                    doMyStrokeAddedExceptHistory(stroke, stroke.tag().privacy);
                }
            }
        }
        private List<Image> createImages(List<BitmapSource> selectedImages)
        {
            var images = new List<Image>();
            foreach (var imageSource in selectedImages)
            {
                var tmpFile = "tmpImage.png";
                using (FileStream fileStream = new FileStream(tmpFile, FileMode.OpenOrCreate))
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(imageSource));
                    encoder.Save(fileStream);
                }
                if (File.Exists(tmpFile))
                {
                    var uri =
                        MeTLLib.ClientFactory.Connection().NoAuthUploadResource(
                            new Uri(tmpFile, UriKind.RelativeOrAbsolute), Globals.slide);
                    var image = new Image
                                    {
                                        Source = new BitmapImage(uri)
                                    };
                    image.tag(new ImageTag(Globals.me, privacy, Globals.generateId(), false, -1)); // ZIndex was -1
                    InkCanvas.SetLeft(image, 15);
                    InkCanvas.SetTop(image, 15);
                    images.Add(image);
                }
            }
            return images;
        }
        private void HandleImagePasteRedo(List<Image> selectedImages)
        {
            foreach (var image in selectedImages)
                Commands.SendImage.ExecuteAsync(new TargettedImage(Globals.slide, Globals.me, _target, privacy, image.tag().id, image));
        }
        private void HandleImagePasteUndo(List<Image> selectedImages)
        {
            foreach(var image in selectedImages)
                dirtyThisElement(image);
        }
        private MeTLTextBox setWidthOf(MeTLTextBox box)
        {
            if (box.Text.Length > 540)
                box.Width = 540;
            return box;
        }
        private void HandleTextPasteRedo(List<MeTLTextBox> selectedText, MeTLTextBox currentBox)
        {
            foreach (var textBox in selectedText)
            {
                if (currentBox != null )
                {
                    var caret = currentBox.CaretIndex;
                    var redoText = currentBox.Text.Insert(currentBox.CaretIndex, textBox.Text);
                    ClearAdorners();
                    var box = textBoxFromId(currentBox.tag().id);
                    if (box == null)
                    {
                        AddTextBoxToCanvas(currentBox);
                        box = currentBox;
                    }
                    box.TextChanged -= SendNewText;
                    box.Text = redoText;
                    box.CaretIndex = caret + textBox.Text.Length;
                    box = setWidthOf(box);
                    sendTextWithoutHistory(box, box.tag().privacy);
                    box.TextChanged += SendNewText;
                    myTextBox = null;
                }
                else
                {
                    textBox.tag(new TextTag(textBox.tag().author, privacy, textBox.tag().id));
                    var box = setWidthOf(textBox);
                    AddTextBoxToCanvas(box);
                    sendTextWithoutHistory(box, box.tag().privacy);
                }
            }
        }
        private void HandleTextPasteUndo(List<MeTLTextBox> selectedText, MeTLTextBox currentBox)
        {
            foreach (var text in selectedText)
            {
                if (currentBox!= null)
                {
                    var undoText = currentBox.Text;
                    var caret = currentBox.CaretIndex;
                    var currentTextBox = currentBox.clone();
                    var box = ((MeTLTextBox) Work.TextChildren().ToList().FirstOrDefault(c => ((MeTLTextBox) c).tag().id == currentTextBox.tag().id));
                    box.TextChanged -= SendNewText;
                    box.Text = undoText;
                    box.CaretIndex = caret;
                    sendTextWithoutHistory(box, box.tag().privacy);
                    box.TextChanged += SendNewText;
                }
                else
                    dirtyTextBoxWithoutHistory(text);
            }
        }
        private List<MeTLTextBox> createPastedBoxes(List<string> selectedText)
        {
            var boxes = new List<MeTLTextBox>();
            foreach (var text in selectedText)
            {
                System.Threading.Thread.Sleep(2);
                MeTLTextBox box = createNewTextbox();
                InkCanvas.SetLeft(box, pos.X);
                InkCanvas.SetTop(box, pos.Y);
                pos = new Point(15, 15);
                box.TextChanged -= SendNewText;
                box.Text = text;
                box.TextChanged += SendNewText;
                boxes.Add(box);
            }
            return boxes;
        }
        protected void HandlePaste(object _args)
        {
            if (me == Globals.PROJECTOR) return;
            var currentBox = myTextBox.clone();

            if (Clipboard.ContainsData(MeTLClipboardData.Type))
            {
                var data = (MeTLClipboardData) Clipboard.GetData(MeTLClipboardData.Type);
                var currentChecksums = Work.Strokes.Select(s => s.sum().checksum);
                var ink = data.Ink.Where(s => !currentChecksums.Contains(s.sum().checksum)).ToList();
                var boxes = createPastedBoxes(data.Text.ToList());
                var images = createImages(data.Images.ToList());
                Action undo = () =>
                                  {
                                      ClearAdorners();
                                      HandleInkPasteUndo(ink);
                                      HandleImagePasteUndo(images);
                                      HandleTextPasteUndo(boxes, currentBox);
                                      AddAdorners();
                                  };
                Action redo = () =>
                                  {
                                      ClearAdorners();
                                      HandleInkPasteRedo(ink);
                                      HandleImagePasteRedo(images);
                                      HandleTextPasteRedo(boxes, currentBox);
                                      AddAdorners();
                                  };
                UndoHistory.Queue(undo, redo, "Pasted items");
                redo();
            }
            else
            {
                if(Clipboard.ContainsText())
                {
                    var boxes = createPastedBoxes(new List<string> {Clipboard.GetText()});
                    Action undo = () => HandleTextPasteUndo(boxes, currentBox);
                    Action redo = () => HandleTextPasteRedo(boxes, currentBox);
                    UndoHistory.Queue(undo, redo, "Pasted text");
                    redo();
                }
                else if(Clipboard.ContainsImage())
                {
                    Action undo = () => HandleImagePasteUndo(createImages(new List<BitmapSource>{Clipboard.GetImage()}));
                    Action redo = () =>  HandleImagePasteRedo(createImages(new List<BitmapSource>{Clipboard.GetImage()}));
                    UndoHistory.Queue(undo, redo, "Pasted images");
                    redo();
                }
            }
        }
        private List<string> HandleTextCopyRedo(List<UIElement> selectedBoxes, string selectedText)
        {
            var clipboardText = new List<string>();
            if (selectedText != String.Empty && selectedText.Length > 0)
                clipboardText.Add(selectedText);
            clipboardText.AddRange(selectedBoxes.Where(b => b is MeTLTextBox).Select(b => ((MeTLTextBox) b).Text).Where(t => t != selectedText));
            return clipboardText;
        }
        private IEnumerable<BitmapSource> HandleImageCopyRedo(List<UIElement> selectedImages)
        {
            return selectedImages.Where(i => i is Image).Select(i => (BitmapSource)((Image)i).Source);
        }
        private List<Stroke> HandleStrokeCopyRedo(List<Stroke> selectedStrokes)
        {
            return selectedStrokes;
        }
        protected void HandleCopy(object _args)
        {
            if (me == Globals.PROJECTOR) return;
           //text 
            var selectedElements = filterOnlyMine(Work.GetSelectedElements()).ToList();
            var selectedStrokes = filterOnlyMine(Work.GetSelectedStrokes()).Select((s => s.Clone())).ToList();
            string selectedText = "";
            if(filterOnlyMine<MeTLTextBox>(myTextBox) != null)
                selectedText = myTextBox.SelectedText;

            // copy previously was an undoable action, ie restore the clipboard to what it previously was
            var images = HandleImageCopyRedo(selectedElements);
            var text = HandleTextCopyRedo(selectedElements, selectedText);
            var copiedStrokes = HandleStrokeCopyRedo(selectedStrokes);
            Clipboard.SetData(MeTLClipboardData.Type,new MeTLClipboardData(text,images,copiedStrokes));
        }
        private void HandleImageCutUndo(IEnumerable<Image> selectedImages)
        {
                var selection = new List<UIElement>();
                foreach (var element in selectedImages)
                {
                    if (!Work.ImageChildren().Contains(element))
                        Work.Children.Add(element);
                    sendThisElement(element);
                    selection.Add(element);
                }
                Work.Select(selection);
        }
        private IEnumerable<BitmapSource> HandleImageCutRedo(IEnumerable<Image> selectedImages)
        {
            foreach (var img in selectedImages)
            {
                ApplyPrivacyStylingToElement(img, img.tag().privacy);
                Work.Children.Remove(img);
                Commands.SendDirtyImage.Execute(new TargettedDirtyElement(Globals.slide, Globals.me, _target, img.tag().privacy, img.tag().id));
            }
            return selectedImages.Select(i => (BitmapSource)i.Source);
        }
        protected void HandleTextCutUndo(List<MeTLTextBox> selectedElements, MeTLTextBox currentTextBox)
        {
            if (currentTextBox != null && currentTextBox.SelectionLength > 0)
            {
                var text = currentTextBox.Text;
                var start = currentTextBox.SelectionStart;
                var length = currentTextBox.SelectionLength;
                if(!Work.TextChildren().Select(t => ((MeTLTextBox)t).tag().id).Contains(currentTextBox.tag().id))
                {
                    var box = applyDefaultAttributes(currentTextBox);
                    box.tag(new TextTag(box.tag().author, box.tag().privacy, Globals.generateId()));
                    Work.Children.Add(box);

                }
                var activeTextbox = ((MeTLTextBox) Work.TextChildren().ToList().FirstOrDefault(c => ((MeTLTextBox) c).tag().id == currentTextBox.tag().id));
                activeTextbox.Text = text;
                activeTextbox.CaretIndex = start + length;
                sendTextWithoutHistory(currentTextBox, currentTextBox.tag().privacy);   
              
            }
            else
            {
                var mySelectedElements = selectedElements.Where(t => t is MeTLTextBox).Select(t => ((MeTLTextBox) t).clone());
                foreach (var box in mySelectedElements)
                   sendBox(box.toMeTLTextBox());
            }
        }
        protected List<string> HandleTextCutRedo(List<MeTLTextBox> elements, MeTLTextBox currentTextBox)
        {
            var clipboardText = new List<string>();
            if (currentTextBox != null && currentTextBox.SelectionLength > 0)
            {
                var selection = currentTextBox.SelectedText;
                var start = currentTextBox.SelectionStart;
                var length = currentTextBox.SelectionLength;
                clipboardText.Add(selection);
                var activeTextbox = ((MeTLTextBox) Work.TextChildren().ToList().Where( c => ((MeTLTextBox) c).tag().id == currentTextBox.tag().id). FirstOrDefault());
                if (activeTextbox == null) return clipboardText;
                activeTextbox.Text = activeTextbox.Text.Remove(start, length);
                activeTextbox.CaretIndex = start;
                if (activeTextbox.Text.Length == 0)
                {
                    ClearAdorners();
                    myTextBox = null;
                    dirtyTextBoxWithoutHistory(currentTextBox);
                }
            }
            else
            {
                var listToCut = new List<MeTLTextBox>();
                var selectedElements = elements.Where(t => t is MeTLTextBox).Select(tb => ((MeTLTextBox)tb).clone()).ToList();
                foreach (MeTLTextBox box in selectedElements)
                {
                    clipboardText.Add(box.Text);
                    listToCut.Add(box);
                }
                myTextBox = null;
                foreach (var element in listToCut)
                   dirtyTextBoxWithoutHistory(element);
            }
            return clipboardText;
        }
        private void HandleInkCutUndo(IEnumerable<Stroke> strokesToCut)
        {
            foreach (var s in strokesToCut)
            {
                Work.Strokes.Add(s);
                doMyStrokeAddedExceptHistory(s, s.tag().privacy);
            }
        }
        private List<Stroke> HandleInkCutRedo(IEnumerable<Stroke> selectedStrokes)
        {
            var listToCut = selectedStrokes.Select(stroke => new TargettedDirtyElement(Globals.slide, stroke.tag().author, _target, stroke.tag().privacy, stroke.sum().checksum.ToString())).ToList();
            foreach (var element in listToCut)
                Commands.SendDirtyStroke.Execute(element);
            return selectedStrokes.ToList();
        }
        protected void HandleCut(object _args)
        {
            if (me == Globals.PROJECTOR) return;
            var strokesToCut = filterOnlyMine(Work.GetSelectedStrokes()).Select(s => s.Clone());
            var currentTextBox = filterOnlyMine<MeTLTextBox>(myTextBox.clone());
            var selectedImages = filterOnlyMine(Work.GetSelectedImages().Cast<UIElement>()).Select(i => ((Image)i).clone()).ToList(); // TODO: fix the casting craziness
            var selectedText = filterOnlyMine(Work.GetSelectedTextBoxes().Cast<UIElement>()).Select(t => ((MeTLTextBox) t).clone()).ToList(); // TODO: fix the casting craziness

            Action redo = () =>
            {
                ClearAdorners();
                var text = HandleTextCutRedo(selectedText, currentTextBox);
                var images = HandleImageCutRedo(selectedImages);
                var ink = HandleInkCutRedo(strokesToCut);
                Clipboard.SetData(MeTLClipboardData.Type, new MeTLClipboardData(text, images, ink));
            };
            Action undo = () =>
            {
                ClearAdorners();
                if(Clipboard.ContainsData(MeTLClipboardData.Type))
                {
                    Clipboard.GetData(MeTLClipboardData.Type);
                    HandleTextCutUndo(selectedText, currentTextBox);
                    HandleImageCutUndo(selectedImages);
                    HandleInkCutUndo(strokesToCut);
                }
            };
            redo();
            UndoHistory.Queue(undo, redo, "Cut items");
        }
        #endregion
        private void MoveTo(int _slide)
        {
            if (contentBuffer != null)
            {
                contentBuffer.Clear();
            }
            if (myTextBox != null)
            {
                var textBox = myTextBox;
                textBox.Focusable = false;
            }
            myTextBox = null;
        }
        public void Flush()
        {
            ClearAdorners();
            Work.Strokes.Clear();
            Work.Children.Clear();
            Height = Double.NaN;
            Width = Double.NaN;
        }

        public void SetEditable(bool b)
        {
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new CollapsedCanvasStackAutomationPeer(this);
        }
    }
}
