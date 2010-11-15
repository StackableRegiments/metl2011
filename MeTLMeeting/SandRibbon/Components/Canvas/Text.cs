﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Microsoft.Practices.Composite.Presentation.Commands;
using SandRibbon.Components.Utility;
using SandRibbon.Providers;
using SandRibbon.Utils;
using SandRibbonInterop;
using MeTLLib.DataTypes;

using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input.StylusPlugIns;
using SandRibbonObjects;


namespace SandRibbon.Components.Canvas
{
    public class TextInformation : TagInformation
    {
        public double size;
        public FontFamily family;
        public bool underline;
        public bool bold;
        public bool italics;
        public bool strikethrough;
    }
    public class Text : AbstractCanvas
    {
        private double currentSize = 10.0;
        private Dictionary<string, List<MeTLLib.DataTypes.TargettedTextBox>> userText;
        private FontFamily currentFamily = new FontFamily("Arial");
        public Text()
        {
            userText = new Dictionary<String, List<MeTLLib.DataTypes.TargettedTextBox>>();
            EditingMode = InkCanvasEditingMode.None;
            Background = Brushes.Transparent;
            Loaded += (a, b) =>
            {
                MouseUp += (c, args) => placeCursor(this, args);
            };
            PreviewKeyDown += keyPressed;
            //SelectionMoved += SendTextBoxes;
            //SelectionMoving += dirtyText;
            SelectionMoved += textMovedorResized;
            SelectionMoving += textMovingorResizing;
            SelectionChanged += selectionChanged;
            SelectionChanging += selectingText;
            SelectionResizing += dirtyText;
            SelectionResized += SendTextBoxes;
            toggleFontBold = new DelegateCommand<object>(toggleBold, canUseTextCommands);
            Commands.ToggleBold.RegisterCommand(toggleFontBold);
            toggleFontItalic = new DelegateCommand<object>(toggleItalics, canUseTextCommands);
            Commands.ToggleItalic.RegisterCommand(toggleFontItalic);
            toggleFontUnderline = new DelegateCommand<object>(toggleUnderline, canUseTextCommands);
            Commands.ToggleUnderline.RegisterCommand(toggleFontUnderline);
            toggleFontStrikethrough = new DelegateCommand<object>(toggleStrikethrough, canUseTextCommands);
            Commands.ToggleStrikethrough.RegisterCommand(toggleFontStrikethrough);
            familyChanged = new DelegateCommand<FontFamily>(setFont, canUseTextCommands);
            Commands.FontChanged.RegisterCommand(familyChanged);
            sizeChanged = new DelegateCommand<double>(setTextSize, canUseTextCommands);
            Commands.FontSizeChanged.RegisterCommand(sizeChanged);
            colorChanged = new DelegateCommand<Color>(setTextColor, canUseTextCommands);
            Commands.SetTextColor.RegisterCommand(colorChanged);
            reset = new DelegateCommand<object>(resetTextbox, canUseTextCommands);
            Commands.RestoreTextDefaults.RegisterCommand(reset);
            Commands.EstablishPrivileges.RegisterCommand(new DelegateCommand<string>(setInkCanvasMode));
            Commands.ReceiveTextBox.RegisterCommandToDispatcher(new DelegateCommand<MeTLLib.DataTypes.TargettedTextBox>(ReceiveTextBox));
            Commands.SetTextCanvasMode.RegisterCommand(new DelegateCommand<string>(setInkCanvasMode));
            Commands.ReceiveDirtyText.RegisterCommand(new DelegateCommand<MeTLLib.DataTypes.TargettedDirtyElement>(receiveDirtyText));
            Commands.SetLayer.RegisterCommandToDispatcher<string>(new DelegateCommand<string>(SetLayer));
            Commands.MoveTo.RegisterCommand(new DelegateCommand<int>(MoveTo));
            Commands.SetPrivacyOfItems.RegisterCommand(new DelegateCommand<string>(changeSelectedItemsPrivacy));
            Commands.DeleteSelectedItems.RegisterCommandToDispatcher(new DelegateCommand<object>(deleteSelectedItems));
            Commands.HideConversationSearchBox.RegisterCommandToDispatcher(new DelegateCommand<object>(hideConversationSearchBox));
        }

        private void hideConversationSearchBox(object obj)
        {
            addAdorners();
        }

        private void deleteSelectedItems(object obj)
        {
            var selectedElements = GetSelectedElements().Select(b => Clone((TextBox)b)).ToList();
            if (selectedElements.Count == 0) return;
            Action undo = () =>
                              {
                                  var selection = new List<UIElement>();
                                  foreach (var box in selectedElements)
                                  {
                                      myTextBox = box;
                                      selection.Add(box);
                                      if(Children.ToList().Where(c => ((TextBox)c).tag().id == box.tag().id).ToList().Count == 0)
                                          Children.Add(box);
                                      box.TextChanged += SendNewText;
                                      sendTextWithoutHistory(box, box.tag().privacy);
                                  }
                                  Select(selection);
                                  addAdorners();
                              };
            Action redo = () =>
                              {
                                  foreach(var box in selectedElements)
                                  {
                                      myTextBox = null;
                                      dirtyTextBoxWithoutHistory(box);
                                  }
                                  ClearAdorners();
                              };
            redo();
            UndoHistory.Queue(undo, redo);
        }


        private void MoveTo(int _slide)
        {
            userText.Clear();
            myTextBox = null;
        }
        private bool focusable = true;
        private void SetLayer(string layer)
        {
            focusable = layer == "Text";
            foreach (var box in Children)
            {
                if (box.GetType() == typeof(TextBox))
                {
                    MeTLLib.DataTypes.TextTag tag = ((TextBox)box).tag();
                    ((TextBox)box).Focusable = focusable && (tag.author == Globals.me);
                }
            }
        }
        private void keyPressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                deleteSelectedItems(null);
        }
        protected void ApplyPrivacyStylingToElement(FrameworkElement element, string privacy)
        {
            if (!Globals.isAuthor || Globals.conversationDetails.Permissions == MeTLLib.DataTypes.Permissions.LECTURE_PERMISSIONS) return;
            if (element.GetType() != typeof(TextBox)) return;
            var box = (TextBox)element;
            Dispatcher.adopt(delegate
            {
                updateSelectionAdorners();
                if (privacy == "private")
                    element.Effect = new DropShadowEffect
                    {
                        BlurRadius = 50,
                        Color = Colors.Black,
                        ShadowDepth = 0,
                        Opacity = 1
                    };
                else
                    RemovePrivacyStylingFromElement(element);
            });
        }
        private void dirtyTextBoxWithoutHistory(TextBox box)
        {
            RemovePrivacyStylingFromElement(box);
            if (Children.ToList().Where(c => ((TextBox)c).tag().id == box.tag().id).ToList().Count != 0)
                Children.Remove(Children.ToList().Where(b => ((TextBox)b).tag().id == box.tag().id).First());
            Commands.SendDirtyText.ExecuteAsync(new TargettedDirtyElement(currentSlide, Globals.me, target, box.tag().privacy, box.tag().id));
        }

        private void receiveDirtyText(TargettedDirtyElement element)
        {
            if (!(element.target.Equals(target))) return;
            if (!(element.slide == currentSlide)) return;
            Dispatcher.adoptAsync(delegate
            {
                if (myTextBox != null && element.identifier == myTextBox.tag().id) return;
                if (element.author == me) return;
                for (int i = 0; i < Children.Count; i++)
                {
                    var currentTextbox = (TextBox)Children[i];
                    if (element.identifier.Equals(currentTextbox.tag().id))
                        Children.Remove(currentTextbox);
                }
            });
        }
        private bool textboxSelectedProperty;

        protected override void CanEditChanged()
        {
            canEdit = base.canEdit;
            if (privacy == "private") canEdit = true;
        }
        private bool canEdit
        {
            get { return base.canEdit; }
            set
            {
                base.canEdit = value;
                toggleFontBold.RaiseCanExecuteChanged();
                toggleFontItalic.RaiseCanExecuteChanged();
                toggleFontUnderline.RaiseCanExecuteChanged();
                toggleFontStrikethrough.RaiseCanExecuteChanged();
                familyChanged.RaiseCanExecuteChanged();
                sizeChanged.RaiseCanExecuteChanged();
                colorChanged.RaiseCanExecuteChanged();
                reset.RaiseCanExecuteChanged();
            }
        }

        private void selectionChanged(object sender, EventArgs e)
        {
            if(GetSelectedElements().Count == 0)
                ClearAdorners();
            else
                addAdorners();
        }
        private void updateSelectionAdorners()
        {
            addAdorners();
        }
        public void addAdorners()
        {
            var selectedElements = GetSelectedElements();
            if (selectedElements.Count == 0) return;
            var publicElements = selectedElements.Where(t => ((TextBox)t).tag().privacy.ToLower() == "public").ToList();
            string privacyChoice;
            if (publicElements.Count == 0)
                privacyChoice = "show";
            else if (publicElements.Count == selectedElements.Count)
                privacyChoice = "hide";
            else
                privacyChoice = "both";
            foreach (TextBox box in GetSelectedElements().Where(e => e is TextBox).ToList())
            {
                if (box != null)
                    box.UpdateLayout();
            }
            Commands.AddPrivacyToggleButton.ExecuteAsync(new PrivacyToggleButton.PrivacyToggleButtonInfo(privacyChoice, GetSelectionBounds()));
        }
        private void selectingText(object sender, InkCanvasSelectionChangingEventArgs e)
        {
            e.SetSelectedElements(filterMyText(e.GetSelectedElements()));
        }
        private IEnumerable<UIElement> filterMyText(IEnumerable<UIElement> elements)
        {
            if (inMeeting()) return elements;
            var myText = new List<UIElement>();
            foreach (TextBox text in elements)
            {
                if (text.tag().author == Globals.me)
                    myText.Add(text);
            }
            return myText;
        }
        private void SendTextBoxes(object sender, EventArgs e)
        {
            ClearAdorners();
            foreach (TextBox box in GetSelectedElements())
            {
                myTextBox = box;
                sendText(box, box.tag().privacy);
            }
            addAdorners();
        }
        private void dirtyText(object sender, InkCanvasSelectionEditingEventArgs e)
        {
            ClearAdorners();
            foreach (var box in GetSelectedElements())
            {
                myTextBox = (TextBox)box;
            }
        }
        List<TextBox> boxesAtTheStart = new List<TextBox>();
        private void textMovingorResizing(object sender, InkCanvasSelectionEditingEventArgs e)
        {
            boxesAtTheStart.Clear();
            boxesAtTheStart = GetSelectedElements().Select(tb => Clone((TextBox)tb)).ToList();
        }
        private void textMovedorResized(object sender, EventArgs e)
        {
            var startingText = boxesAtTheStart.Select(Clone).ToList();
            var selectedElements =GetSelectedElements().Select(tb => Clone((TextBox)tb)).ToList();
            Action undo = () =>
              {
                  ClearAdorners();
                  var mySelectedElements = selectedElements.Select(Clone);
                  foreach (TextBox box in mySelectedElements)
                  {
                      myTextBox = box;
                      dirtyTextBoxWithoutHistory(box);
                      myTextBox = null;
                  }
                  var selection = new List<UIElement>();
                  foreach (var box in startingText)
                  {
                      myTextBox = box;
                      selection.Add(box);
                      if(Children.ToList().Where(c => ((TextBox)c).tag().id == box.tag().id).ToList().Count == 0)
                          Children.Add(box);
                      box.TextChanged += SendNewText;
                      sendTextWithoutHistory(box, box.tag().privacy);
                  }
                  Select(selection);
                  addAdorners();
              };
            Action redo = () =>
              {
                  ClearAdorners();
                  var mySelectedElements = selectedElements.Select(Clone);
                  var selection = new List<UIElement>();
                  foreach (var box in startingText)
                  {
                      myTextBox = box;
                      dirtyTextBoxWithoutHistory(box);
                      myTextBox = null;
                  }
                  foreach (var box in mySelectedElements)
                  {
                      selection.Add(box);
                      myTextBox = box;
                      if(Children.ToList().Where(c => ((TextBox)c).tag().id == box.tag().id).ToList().Count == 0)
                          Children.Add(box);
                      box.TextChanged += SendNewText;
                      sendTextWithoutHistory(box, box.tag().privacy);
                  }
                  Select(selection);
                  addAdorners();
              };
            redo();
            UndoHistory.Queue(undo, redo);
        }
       
        public bool textBoxSelected
        {
            get { return textboxSelectedProperty; }
            set
            {
                textboxSelectedProperty = value;
                toggleFontBold.RaiseCanExecuteChanged();
                colorChanged.RaiseCanExecuteChanged();
                toggleFontItalic.RaiseCanExecuteChanged();
                toggleFontUnderline.RaiseCanExecuteChanged();
                toggleFontStrikethrough.RaiseCanExecuteChanged();
                familyChanged.RaiseCanExecuteChanged();
                sizeChanged.RaiseCanExecuteChanged();
                reset.RaiseCanExecuteChanged();
            }
        }
        private Color currentColor = Colors.Black;
        private TextBox myTextBox;
        public DelegateCommand<object> toggleFontBold;
        public DelegateCommand<object> toggleFontItalic;
        public DelegateCommand<object> toggleFontUnderline;
        public DelegateCommand<object> toggleFontStrikethrough;
        public DelegateCommand<FontFamily> familyChanged;
        public DelegateCommand<double> sizeChanged;
        public DelegateCommand<Color> colorChanged;
        public DelegateCommand<object> reset;

        private void setInkCanvasMode(string modeString)
        {
            if (!canEdit)
                EditingMode = InkCanvasEditingMode.None;
            else
                EditingMode = (InkCanvasEditingMode)Enum.Parse(typeof(InkCanvasEditingMode), modeString);
        }
        public void FlushText()
        {
            Dispatcher.adoptAsync(delegate
            {
                Children.Clear();
            });
        }
        private void resetTextbox(object obj)
        {
            if (myTextBox == null) return;
            resetText(myTextBox);
        }
        private void resetText(TextBox box)
        {
            RemovePrivacyStylingFromElement(box);
            currentColor = Colors.Black;
            box.FontWeight = FontWeights.Normal;
            box.FontStyle = FontStyles.Normal;
            box.TextDecorations = new TextDecorationCollection();
            box.FontFamily = new FontFamily("Arial");
            box.FontSize = 10;
            box.Foreground = Brushes.Black;
            var info = new TextInformation
                           {
                               family = box.FontFamily,
                               size = box.FontSize,
                           };
            Commands.TextboxFocused.ExecuteAsync(info);
            sendText(box);
        }
        private void setTextSize(double size)
        {
            currentSize = size;
            Dispatcher.adoptAsync(() =>
            {
                var selection = GetSelectedElements();
                if (myTextBox == null) return;
                RemovePrivacyStylingFromElement(myTextBox);
                myTextBox.FontSize = size;
                myTextBox.Focus();
                Select(null, null);
                sendText(myTextBox);
                myTextBox.UpdateLayout();
                Select(new[] { myTextBox });
            });
        }
        private void setTextColor(Color color)
        {
            currentColor = color;
            if (myTextBox == null) return;
            myTextBox.Foreground = new SolidColorBrush(color);
            sendText(myTextBox);
        }
        private void setFont(FontFamily font)
        {
            currentFamily = font;
            if (myTextBox == null) return;
            myTextBox.FontFamily = font;
            sendText(myTextBox);
        }
        private bool canUseTextCommands(Color arg)
        {
            return canUseTextCommands(1.0);
        }
        private bool canUseTextCommands(object arg)
        {
            return canUseTextCommands(1.0);
        }
        private bool canUseTextCommands(double arg)
        {
            return true;
        }
        private void toggleStrikethrough(object obj)
        {
            if (myTextBox == null) return;
            var currentTextbox = myTextBox;
            if (!Children.Contains(currentTextbox)) return;
            var decorations = currentTextbox.TextDecorations.Select(s => s.Location).Where(t => t.ToString() == "Strikethrough");
            if (decorations.Count() > 0)
                currentTextbox.TextDecorations = new TextDecorationCollection();
            else
                currentTextbox.TextDecorations = TextDecorations.Strikethrough;
            sendText(currentTextbox);
            updateTools();
        }
        private void toggleItalics(object obj)
        {
            if (myTextBox == null) return;
            var currentTextbox = myTextBox;
            currentTextbox.FontStyle = currentTextbox.FontStyle == FontStyles.Italic ? FontStyles.Normal : FontStyles.Italic;
            sendText(currentTextbox);
            updateTools();
        }
        private void toggleUnderline(object obj)
        {
            if (myTextBox == null) return;
            var currentTextbox = myTextBox;
            if (!Children.Contains(currentTextbox)) return;
            var decorations = currentTextbox.TextDecorations.Select(s => s.Location).Where(t => t.ToString() == "Underline");
            if (decorations != null && decorations.Count() > 0)
                currentTextbox.TextDecorations = new TextDecorationCollection();
            else
                currentTextbox.TextDecorations = TextDecorations.Underline;
            sendText(currentTextbox);
            updateTools();
        }
        private void toggleBold(object obj)
        {
            if (myTextBox == null) return;
            var currentTextbox = myTextBox;
            currentTextbox.FontWeight = currentTextbox.FontWeight == FontWeights.Bold ? FontWeights.Normal : FontWeights.Bold;
            sendText(currentTextbox);
            updateTools();
        }
        private void placeCursor(object sender, MouseButtonEventArgs e)
        {
            if (EditingMode != InkCanvasEditingMode.None) return;
            if (!canEdit) return;
            var pos = e.GetPosition(this);
            var source = (InkCanvas)sender;
            TextBox box = createNewTextbox();
            Children.Add(box);
            SetLeft(box, pos.X);
            SetTop(box, pos.Y);
            myTextBox = box;
            box.Focus();
        }
        public TextBox createNewTextbox()
        {
            var box = new TextBox();
            box.tag(new MeTLLib.DataTypes.TextTag
                        {
                            author = Globals.me,
                            privacy = privacy,
                            id = string.Format("{0}:{1}", Globals.me, SandRibbonObjects.DateTimeFactory.Now())
                        });
            box.FontFamily = currentFamily;
            box.FontSize = currentSize;
            box.Foreground = new SolidColorBrush(currentColor);
            box.LostFocus += (_sender, _args) =>
            {
                myTextBox = null;

            };
            return applyDefaultAttributes(box);
        }
        private TextBox applyDefaultAttributes(TextBox box)
        {
            box.AcceptsReturn = true;
            box.TextWrapping = TextWrapping.WrapWithOverflow;
            box.GotFocus += textboxGotFocus;
            box.LostFocus += textboxLostFocus;
            box.TextChanged += SendNewText;
            box.BorderThickness = new Thickness(0);
            box.BorderBrush = new SolidColorBrush(Colors.Transparent);
            box.Background = new SolidColorBrush(Colors.Transparent);
            box.Focusable = canEdit;
            return box;
        }
        private void textboxLostFocus(object sender, RoutedEventArgs e)
        {
            var box = (TextBox)sender;
            var currentTag = box.tag();
            ClearAdorners();
            if (currentTag.privacy != Globals.privacy)
            {
                Commands.SendDirtyText.ExecuteAsync(new MeTLLib.DataTypes.TargettedDirtyElement(currentSlide, Globals.me, target, currentTag.privacy, currentTag.id));
                currentTag.privacy = privacy;
                box.tag(currentTag);
                Commands.SendTextBox.ExecuteAsync(new MeTLLib.DataTypes.TargettedTextBox(currentSlide, Globals.me, target, currentTag.privacy, box));
            }
            myTextBox = null;
            textBoxSelected = false;
            if (box.Text.Length == 0)
                Children.Remove(box);
            else
                setAppropriatePrivacyHalo(box);
        }
        private void textboxGotFocus(object sender, RoutedEventArgs e)
        {
            myTextBox = (TextBox)sender;
            updateTools();
            textBoxSelected = true;
        }
        private void updateTools()
        {
            bool strikethrough = false;
            bool underline = false;
            if (myTextBox.TextDecorations.Count > 0)
            {
                strikethrough = myTextBox.TextDecorations.First().Location.ToString().ToLower() == "strikethrough";
                underline = myTextBox.TextDecorations.First().Location.ToString().ToLower() == "underline";
            }
            var info = new TextInformation
                           {
                               family = myTextBox.FontFamily,
                               size = myTextBox.FontSize,
                               bold = myTextBox.FontWeight == FontWeights.Bold,
                               italics = myTextBox.FontStyle == FontStyles.Italic,
                               strikethrough = strikethrough,
                               underline = underline

                           };
            Commands.TextboxFocused.ExecuteAsync(info);
        }

        public static Timer typingTimer = null;
        private void SendNewText(object sender, TextChangedEventArgs e)
        {
            var box = (TextBox)sender;
            ApplyPrivacyStylingToElement(box, box.tag().privacy);
            box.Height = Double.NaN;
            if (typingTimer == null)
            {
                typingTimer = new Timer(delegate
                {
                    Dispatcher.adoptAsync(delegate
                                                    {
                                                        sendText((TextBox)sender);
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
        public void sendText(TextBox box)
        {
            sendText(box, Globals.privacy);
        }
        public void sendText(TextBox box, string intendedPrivacy)
        {
            UndoHistory.Queue(
            () =>
            {
                dirtyTextBoxWithoutHistory(box);
            },
            () =>
            {
                sendTextWithoutHistory(box, intendedPrivacy);
            });
            GlobalTimers.resetSyncTimer();
            sendTextWithoutHistory(box, intendedPrivacy);
            if (GetSelectedElements().Count > 0) addAdorners();
        }
        private void sendTextWithoutHistory(TextBox box, string thisPrivacy)
        {
            RemovePrivacyStylingFromElement(box);
            if (box.tag().privacy != Globals.privacy)
                dirtyTextBoxWithoutHistory(box);
            var oldTextTag = box.tag();
            var newTextTag = new MeTLLib.DataTypes.TextTag(oldTextTag.author, thisPrivacy, oldTextTag.id);
            box.tag(newTextTag);
            Commands.SendTextBox.ExecuteAsync(new MeTLLib.DataTypes.TargettedTextBox(currentSlide, Globals.me, target, thisPrivacy, box));
        }

        private void setAppropriatePrivacyHalo(TextBox box)
        {
            if (!Children.Contains(box)) return;
            ApplyPrivacyStylingToElement(box, privacy);
        }

        public void RemoveTextboxWithTag(string tag)
        {
            for (var i = 0; i < Children.Count; i++)
            {
                if (((TextBox)Children[i]).Tag.ToString() == tag)
                    Children.Remove(Children[i]);
            }
        }
      
        public void ReceiveTextBox(MeTLLib.DataTypes.TargettedTextBox targettedBox)
        {
            if (targettedBox.target != target) return;
            if (targettedBox.author == Globals.me && alreadyHaveThisTextBox(targettedBox.box) && me != "projector")
            {
                var box = textBoxFromId(targettedBox.identity);
                if (box != null)
                    ApplyPrivacyStylingToElement(box, box.tag().privacy);
                return;
            }//I never want my live text to collide with me.
            if (targettedBox.slide == currentSlide && (targettedBox.privacy == "private" || me == "projector"))
                removeDoomedTextBoxes(targettedBox);
            if (targettedBox.slide == currentSlide && (targettedBox.privacy == "public" || (targettedBox.author == Globals.me && me != "projector")))
                    doText(targettedBox);
        }
        private void removeDoomedTextBoxes(MeTLLib.DataTypes.TargettedTextBox targettedBox)
        {
            var box = targettedBox.box;
            var doomedChildren = new List<FrameworkElement>();
            foreach (var child in Children)
            {
                if (child is TextBox)
                    if (((TextBox)child).tag().id.Equals(box.tag().id))
                        doomedChildren.Add((FrameworkElement)child);
            }
            foreach (var child in doomedChildren)
                Children.Remove(child);
        }

        private bool alreadyHaveThisTextBox(TextBox box)
        {
            bool result = false;
            Dispatcher.adopt(() =>
            {
                var boxId = box.tag().id;
                var privacy = box.tag().privacy;
                foreach (var text in Children)
                    if (text is TextBox)
                        if (((TextBox)text).tag().id == boxId && ((TextBox)text).tag().privacy == privacy) result = true;
            });
            return result;
        }
        private TextBox textBoxFromId(string boxId)
        {
            TextBox result = null;
            Dispatcher.adopt(() =>
            {
                foreach (var text in Children)
                    if (text.GetType() == typeof(TextBox))
                        if (((TextBox)text).tag().id == boxId) result = (TextBox)text;
            });
            return result;
        }
        public void doText(MeTLLib.DataTypes.TargettedTextBox targettedBox)
        {
            Dispatcher.adoptAsync(delegate
                                      {
                                          var author = targettedBox.author == Globals.conversationDetails.Author ? "Teacher" : targettedBox.author;
                                          if (targettedBox.target != target) return;
                                          //if (targettedBox.author == Globals.me &&
                                          //  alreadyHaveThisTextBox(targettedBox.box))
                                          //return; //I never want my live text to collide with me.
                                          if (targettedBox.slide == currentSlide &&
                                              (targettedBox.privacy == "public" || targettedBox.author == Globals.me))
                                          {

                                              var box = targettedBox.box;
                                              removeDoomedTextBoxes(targettedBox);
                                              Children.Add(applyDefaultAttributes(box));
                                              if (!(targettedBox.author == Globals.me && focusable))
                                                  box.Focusable = false;
                                              ApplyPrivacyStylingToElement(box, targettedBox.privacy);
                                          }
                                      });
        }
        public TextBox Clone(TextBox OldBox)
        {


            var box = new TextBox();
            box.AcceptsReturn = true;
            box.TextWrapping = TextWrapping.WrapWithOverflow;
            box.GotFocus += textboxGotFocus;
            box.LostFocus += textboxLostFocus;
            box.BorderThickness = new Thickness(0);
            box.BorderBrush = new SolidColorBrush(Colors.Transparent);
            box.Background = new SolidColorBrush(Colors.Transparent);
            box.Focusable = canEdit;
            box.tag(OldBox.tag());
            box.FontFamily = OldBox.FontFamily;
            box.FontSize = OldBox.FontSize;
            box.Foreground = OldBox.Foreground;
            box.Text = OldBox.Text;
            SetLeft(box, GetLeft(OldBox));
            SetTop(box, GetTop(OldBox));
            return box;
        }
        public static IEnumerable<Point> getTextPoints(TextBox text)
        {
            if (text == null) return null;
            var y = InkCanvas.GetTop(text);
            var x = InkCanvas.GetLeft(text);
            var width = text.FontSize * text.Text.Count();
            var height = (text.Text.Where(l => l.Equals('\n')).Count() + 1) * text.FontSize + 2;
            return new[]
            {
                new Point(x, y),
                new Point(x + width, y),
                new Point(x + width, y + height),
                new Point(x, y + height)
            };

        }
        protected override void HandlePaste()
        {
            if (Clipboard.ContainsText())
            {
                TextBox box = createNewTextbox();
                Children.Add(box);
                SetLeft(box, 15);
                SetTop(box, 15);
                box.Text = Clipboard.GetText();

            }
        }
        protected override void HandleCopy()
        {
            foreach (var box in GetSelectedElements().Where(e => e is TextBox))
                Clipboard.SetText(((TextBox)box).Text);
        }
        protected override void HandleCut()
        {
            var listToCut = new List<MeTLLib.DataTypes.TargettedDirtyElement>();
            foreach (TextBox box in GetSelectedElements().Where(e => e is TextBox))
            {

                Clipboard.SetText(box.Text);
                listToCut.Add(new MeTLLib.DataTypes.TargettedDirtyElement(currentSlide, Globals.me, target, box.tag().privacy, box.tag().id));
            }
            foreach (var element in listToCut)
                Commands.SendDirtyText.ExecuteAsync(element);
        }
        public override void showPrivateContent()
        {
            foreach (UIElement child in Children)
                if (child.GetType() == typeof(System.Windows.Controls.TextBox) && ((System.Windows.Controls.TextBox)child).tag().privacy == "private")
                    child.Visibility = Visibility.Visible;
        }
        public override void hidePrivateContent()
        {
            foreach (UIElement child in Children)
                if (child.GetType() == typeof(System.Windows.Controls.TextBox) && ((System.Windows.Controls.TextBox)child).tag().privacy == "private")
                    child.Visibility = Visibility.Collapsed;
        }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new TextAutomationPeer(this);
        }
        private void changeSelectedItemsPrivacy(string newPrivacy)
        {
            if (me != "projector")
            {
                Dispatcher.adopt(delegate{
                    List<UIElement> selectedElements = new List<UIElement>();
                    selectedElements = GetSelectedElements().ToList();
                    foreach (System.Windows.Controls.TextBox textBox in selectedElements.Where(i =>
                        i is System.Windows.Controls.TextBox
                        && ((System.Windows.Controls.TextBox)i).tag().privacy != newPrivacy))
                    {
                        var oldTag = ((TextBox)textBox).tag();
                        oldTag.privacy = newPrivacy;
                        dirtyTextBoxWithoutHistory(textBox);
                        ((TextBox)textBox).tag(oldTag);
                        sendText(textBox, newPrivacy);
                    }
                    Select(new List<UIElement>());
                });
            }
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
        
    }
    public class TextAutomationPeer : FrameworkElementAutomationPeer, IValueProvider
    {
        public TextAutomationPeer(Text owner)
            : base(owner) { }
        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Value)
                return this;
            return base.GetPattern(patternInterface);
        }
        private Text Text
        {
            get { return (Text)base.Owner; }
        }
        protected override string GetAutomationIdCore()
        {
            return "text";
        }
        public void SetValue(string value)
        {
            var box = Text.createNewTextbox();
            box.Text = value;
            box.FontSize = 36;
            Text.sendText(box);
        }
        bool IValueProvider.IsReadOnly
        {
            get { return false; }
        }
        string IValueProvider.Value
        {
            get
            {
                var text = Text;
                var sb = new StringBuilder("<text>");
                foreach (var toString in from UIElement box in text.Children
                                         select new MeTLLib.DataTypes.MeTLStanzas.TextBox(
                                              new MeTLLib.DataTypes.TargettedTextBox(Globals.slide, Globals.me, text.target, text.privacy, (TextBox)box)
                                                          ).ToString())
                    sb.Append(toString);
                sb.Append("</text>");
                return sb.ToString();
            }
        }
    }
}