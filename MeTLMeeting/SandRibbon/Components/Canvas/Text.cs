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
using MeTLLib.DataTypes;
using SandRibbonObjects;
using System.Diagnostics;


namespace SandRibbon.Components.Canvas
{
    /*internal class TextInformation : TagInformation
    {
        public double size;
        public FontFamily family;
        public bool underline;
        public bool bold;
        public bool italics;
        public bool strikethrough;
        public Color color;
    }*/
    public class Text : AbstractCanvas, IClipboardHandler
    {
        private readonly double defaultSize = 10.0;
        private readonly FontFamily defaultFamily = new FontFamily("Arial");
        private double currentSize = 10.0;
        private FontFamily currentFamily = new FontFamily("Arial");
        public Text()
        {
            EditingMode = InkCanvasEditingMode.None;
            Background = Brushes.Transparent;
            Loaded += (a, b) =>
            {
                MouseUp += (c, args) => placeCursor(this, args);
            };
            PreviewKeyDown += keyPressed;
            SelectionMoved += textMovedorResized;
            SelectionMoving += textMovingorResizing;
            SelectionChanged += selectionChanged;
            SelectionChanging += selectingText;
            SelectionResizing += dirtyText;
            SelectionResized += SendTextBoxes;

            Commands.UpdateTextStyling.RegisterCommand(new DelegateCommand<TextInformation>(updateStyling, canUseTextCommands));
            Commands.RestoreTextDefaults.RegisterCommand(new DelegateCommand<object>(resetTextbox, canUseTextCommands));
            Commands.EstablishPrivileges.RegisterCommand(new DelegateCommand<string>(setInkCanvasMode));
            Commands.ReceiveTextBox.RegisterCommandToDispatcher(new DelegateCommand<MeTLLib.DataTypes.TargettedTextBox>(ReceiveTextBox));
            Commands.SetTextCanvasMode.RegisterCommand(new DelegateCommand<string>(setInkCanvasMode));
            Commands.ReceiveDirtyText.RegisterCommand(new DelegateCommand<MeTLLib.DataTypes.TargettedDirtyElement>(receiveDirtyText));
            Commands.SetLayer.RegisterCommandToDispatcher<string>(new DelegateCommand<string>(SetLayer));
            Commands.MoveTo.RegisterCommand(new DelegateCommand<int>(MoveTo));
            Commands.SetPrivacyOfItems.RegisterCommand(new DelegateCommand<string>(changeSelectedItemsPrivacy));
            Commands.DeleteSelectedItems.RegisterCommandToDispatcher(new DelegateCommand<object>(deleteSelectedItems));
            Commands.HideConversationSearchBox.RegisterCommandToDispatcher(new DelegateCommand<object>(hideConversationSearchBox));
            Commands.UpdateConversationDetails.RegisterCommandToDispatcher(new DelegateCommand<object>(updateBoxesPrivacy));
        }
        private void updateBoxesPrivacy(object obj)
        {
            foreach (var item in Children)
            {
                MeTLTextBox box;
                if (item.GetType() == typeof(TextBox))
                    box = ((TextBox)item).toMeTLTextBox();
                else
                    box = (MeTLTextBox)item;
                ApplyPrivacyStylingToElement(box, box.tag().privacy);
            }
        }
  
        private void updateStyling(TextInformation info)
        {
            try
            {
                currentColor = info.Color;
                currentFamily = info.Family;
                currentSize = info.Size;
                if (myTextBox != null)
                {
                    var caret = myTextBox.CaretIndex;
                    var currentTextBox = Clone(myTextBox);
                    var oldInfo = getInfoOfBox(currentTextBox);

                    Action undo = () =>
                                      {
                                          ClearAdorners();
                                          var currentInfo = oldInfo;
                                          var activeTextbox =
                                              ((MeTLTextBox)
                                               Children.ToList().Where(
                                                   c => ((MeTLTextBox)c).tag().id == currentTextBox.tag().id).
                                                   FirstOrDefault());
                                          activeTextbox.TextChanged -= SendNewText;
                                          applyStylingTo(activeTextbox, currentInfo);
                                          Commands.TextboxFocused.ExecuteAsync(currentInfo);
                                          addAdorners();
                                          sendTextWithoutHistory(activeTextbox, currentTextBox.tag().privacy);
                                          activeTextbox.TextChanged += SendNewText;
                                      };
                    Action redo = () =>
                                      {
                                          ClearAdorners();
                                          var currentInfo = info;
                                          var activeTextbox = ((MeTLTextBox) Children.ToList().FirstOrDefault(c => ((MeTLTextBox)c).tag().id == currentTextBox.tag().id));
                                          activeTextbox.TextChanged -= SendNewText;
                                          applyStylingTo(activeTextbox, currentInfo);
                                          Commands.TextboxFocused.ExecuteAsync(currentInfo);
                                          addAdorners();
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
                else if (GetSelectedElements().Count > 0)
                {
                    var originalElements = GetSelectedElements().ToList().Select(tb => Clone((MeTLTextBox)tb));
                    Action undo = () =>
                                      {
                                          ClearAdorners();
                                          foreach (var originalElement in originalElements)
                                          {
                                              dirtyTextBoxWithoutHistory(originalElement);
                                              sendTextWithoutHistory(originalElement, originalElement.tag().privacy);
                                          }
                                          addAdorners();
                                      };
                    Action redo = () =>
                                      {
                                          ClearAdorners();
                                          var ids = originalElements.Select(b => b.tag().id);
                                          var selection =
                                              Children.ToList().Where(b => ids.Contains(((MeTLTextBox)b).tag().id));
                                          foreach (MeTLTextBox currentTextBox in selection)
                                          {
                                              applyStylingTo(currentTextBox, info);
                                              sendTextWithoutHistory(currentTextBox, currentTextBox.tag().privacy);
                                          }
                                          addAdorners();
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
        private void hideConversationSearchBox(object obj)
        {
            addAdorners();
        }
        public void deleteSelectedItems(object obj)
        {
            var rawElements = GetSelectedElements().Where(t => ((MeTLTextBox)t).tag().author == Globals.me).ToList();
            if(rawElements.Count == 0) return;
            var selectedElements = rawElements.Select(b => Clone((MeTLTextBox)b)).ToList();
            if (selectedElements.Count == 0) return;
            Action undo = () =>
                              {
                                  var selection = new List<UIElement>();
                                  foreach (var box in selectedElements)
                                  {
                                      myTextBox = box;
                                      selection.Add(box);
                                      if(Children.ToList().Where(c => ((MeTLTextBox)c).tag().id == box.tag().id).ToList().Count == 0)
                                          Children.Add(box);
                                      box.TextChanged += SendNewText;
                                      box.PreviewTextInput += box_PreviewTextInput;
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
                                  // set keyboard focus to the current canvas so the help button does not grey out
                                  Keyboard.Focus(this);
                                  ClearAdorners();
                              };
            redo();
            UndoHistory.Queue(undo, redo);
        }


        private void MoveTo(int _slide)
        {
            if (myTextBox != null)
            {
                var textBox = myTextBox;
                textBox.Focusable = false;
            }
            myTextBox = null;
        }
        private bool focusable = true;
        private void SetLayer(string layer)
        {
            focusable = layer == "Text";
            foreach (var box in Children)
            {
                if (box.GetType() == typeof(MeTLTextBox))
                {
                    var tag = ((MeTLTextBox)box).tag();
                    ((MeTLTextBox)box).Focusable = focusable && (tag.author == Globals.me);
                }
            }
        }
        private void keyPressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                Trace.TraceInformation("DeleteText Delete Key");
                deleteSelectedItems(null);
            }
        }
        protected new void ApplyPrivacyStylingToElement(FrameworkElement element, string privacy)
        {
            if (!Globals.conversationDetails.Permissions.studentCanPublish && !Globals.isAuthor)
            {
                RemovePrivacyStylingFromElement(element);
                return;
            }
            if (element.GetType() != typeof (MeTLTextBox)) return;
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
        private void dirtyTextBoxWithoutHistory(MeTLTextBox box)
        {
            RemovePrivacyStylingFromElement(box);
            if (Children.ToList().Where(c => ((MeTLTextBox)c).tag().id == box.tag().id).ToList().Count != 0)
                Children.Remove(Children.ToList().Where(b => ((MeTLTextBox)b).tag().id == box.tag().id).First());
            Commands.SendDirtyText.ExecuteAsync(new TargettedDirtyElement(currentSlide, box.tag().author, target, box.tag().privacy, box.tag().id));
        }

        private void receiveDirtyText(TargettedDirtyElement element)
        {
            if (!(element.target.Equals(target))) return;
            if (element.slide != currentSlide) return;
            Dispatcher.adoptAsync(delegate
            {
                if (myTextBox != null && element.identifier == myTextBox.tag().id) return;
                if (element.author == me) return;
                for (int i = 0; i < Children.Count; i++)
                {
                    var currentTextbox = (MeTLTextBox)Children[i];
                    if (element.identifier.Equals(currentTextbox.tag().id))
                        Children.Remove(currentTextbox);
                }
            });
        }

        public List<string> GetSelectedAuthors()
        {
            return GetSelectedElements().Where(s => ((MeTLTextBox)s).tag().author != me).Select(s => ((MeTLTextBox)s).tag().author).Distinct().ToList();

        }
        protected override void CanEditChanged()
        {
            canEdit = base.canEdit;
            if (privacy == "private") canEdit = true;
        }
        private new bool canEdit
        {
            get { return base.canEdit; }
            set
            {
                base.canEdit = value;
                requeryTextCommands(); 
            }
        }
        private void selectionChanged(object sender, EventArgs e)
        {
            ClearAdorners();
            addAdorners();
            if (GetSelectedElements().Count == 0 && myTextBox != null) return;
            myTextBox = null;
            if (GetSelectedElements().Count == 1 && GetSelectedElements()[0] is MeTLTextBox)
                myTextBox = ((MeTLTextBox)GetSelectedElements()[0]);
            updateTools();
        }
        private void updateSelectionAdorners()
        {
            addAdorners();
        }
        public void addAdorners()
        {
            var selectedElements = GetSelectedElements();
            if (selectedElements.Count == 0) return;
            var publicElements = selectedElements.Where(t => ((MeTLTextBox)t).tag().privacy.ToLower() == "public").ToList();
            var myElements = selectedElements.Where(t => ((MeTLTextBox)t).tag().author == Globals.me).Count();
            string privacyChoice;
            if (publicElements.Count == 0)
                privacyChoice = "show";
            else if (publicElements.Count == selectedElements.Count)
                privacyChoice = "hide";
            else
                privacyChoice = "both";
            foreach (MeTLTextBox box in GetSelectedElements().Where(e => e is MeTLTextBox).ToList())
            {
                if (box != null)
                    box.UpdateLayout();
            }
            Commands.AddPrivacyToggleButton.ExecuteAsync(new PrivacyToggleButton.PrivacyToggleButtonInfo(privacyChoice, myElements!= 0, GetSelectionBounds()));
        }
        private void selectingText(object sender, InkCanvasSelectionChangingEventArgs e)
        {
            e.SetSelectedElements(filterMyText(e.GetSelectedElements()));
           
        }
        private IEnumerable<UIElement> filterMyText(IEnumerable<UIElement> elements)
        {
            #if ENABLE_BANHAMMER
            if (inMeeting() || Globals.isAuthor) return elements;

            return elements.Cast<MeTLTextBox>().Where(text => text.tag().author == Globals.me || Globals.isAuthor).Cast<UIElement>().ToList();
            #else
            if (inMeeting()) return elements;

            return elements.Cast<MeTLTextBox>().Where(text => text.tag().author == Globals.me).Cast<UIElement>().ToList();
            #endif // ENABLE_BANHAMMER
        }
        private void SendTextBoxes(object sender, EventArgs e)
        {
            ClearAdorners();
            var clonedCollection = new List<MeTLTextBox>();
            foreach(MeTLTextBox box in GetSelectedElements())
                clonedCollection.Add(Clone(box));
            foreach (MeTLTextBox box in clonedCollection)
            {
                myTextBox = box;
                sendTextWithoutHistory(box, box.tag().privacy);
            }
            addAdorners();
        }
        private void dirtyText(object sender, InkCanvasSelectionEditingEventArgs e)
        {
            ClearAdorners();
            foreach (var box in GetSelectedElements())
            {
                myTextBox = (MeTLTextBox)box;
            }
        }
        List<MeTLTextBox> boxesAtTheStart = new List<MeTLTextBox>();
        private void textMovingorResizing(object sender, InkCanvasSelectionEditingEventArgs e)
        {
            boxesAtTheStart.Clear();
            boxesAtTheStart = GetSelectedElements().Select(tb => Clone((MeTLTextBox)tb)).ToList();
        }
        private void textMovedorResized(object sender, EventArgs e)
        {
            Trace.TraceInformation("MovedTextbox");
            var startingText = boxesAtTheStart.Select(Clone).ToList();
            List<UIElement> selectedElements =GetSelectedElements().Select(tb => (UIElement)Clone((MeTLTextBox)tb)).ToList();
            abosoluteizeElements(selectedElements);
            Action undo = () =>
              {
                  ClearAdorners();
                  var mySelectedElements = selectedElements.Select(element => Clone((MeTLTextBox)element));
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
                  Select(selection);
                  addAdorners();
              };
            Action redo = () =>
              {
                  ClearAdorners();
                  var mySelectedElements = selectedElements.Select(element => Clone((MeTLTextBox)element));
                  var selection = new List<UIElement>();
                  foreach (var box in startingText)
                      removeBox(box);
                  foreach (var box in mySelectedElements)
                  {
                      selection.Add(box);
                      sendBox(box); 
                  }
                  Select(selection);
                  addAdorners();
              };
            redo();
            UndoHistory.Queue(undo, redo);
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
            if(Children.ToList().Where(c => ((MeTLTextBox)c).tag().id == box.tag().id).ToList().Count == 0)
                Children.Add(box);
            box.TextChanged += SendNewText;
            box.PreviewTextInput += box_PreviewTextInput;
            sendTextWithoutHistory(box, box.tag().privacy);
        }
        private static void requeryTextCommands()
        {
            Commands.RequerySuggested(new []{
                                                Commands.UpdateTextStyling,
                                                Commands.RestoreTextDefaults
                                            });
        }

        private Color currentColor = Colors.Black;
        private MeTLTextBox myTextBox;
        private bool canFocus = true;
        private void setInkCanvasMode(string modeString)
        {
            if (!canEdit)
            {
                EditingMode = InkCanvasEditingMode.None;
            }
            else
                EditingMode = (InkCanvasEditingMode)Enum.Parse(typeof(InkCanvasEditingMode), modeString);
        }
        public void FlushText()
        {
            Dispatcher.adoptAsync(() => Children.Clear());
        }
        private void resetTextbox(object obj)
        {
            if (myTextBox == null && GetSelectedElements().Count != 1) return;
            if(myTextBox == null)
                myTextBox = (MeTLTextBox)GetSelectedElements().First();
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
                               Family = box.FontFamily,
                               Size = box.FontSize,
                           };
            Commands.TextboxFocused.ExecuteAsync(info);
            sendTextWithoutHistory(box, box.tag().privacy);
        }
        private bool canUseTextCommands(object arg)
        {
            return canUseTextCommands(1.0);
        }
        private bool canUseTextCommands(double arg)
        {
            return true;
        }
        
        private void placeCursor(object sender, MouseButtonEventArgs e)
        {
            if (EditingMode != InkCanvasEditingMode.None) return;
            if (!canEdit) return;
            var pos = e.GetPosition(this);
            var source = (InkCanvas)sender;
            MeTLTextBox box = createNewTextbox();
            Children.Add(box);
            SetLeft(box, pos.X);
            SetTop(box, pos.Y);
            myTextBox = box;
            box.Focus();
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

      
        private void textboxLostFocus(object sender, RoutedEventArgs e)
        {
            var box = (MeTLTextBox)sender;
            var currentTag = box.tag();
            ClearAdorners();
            if (currentTag.privacy != Globals.privacy)
            {
                Commands.SendDirtyText.ExecuteAsync(new TargettedDirtyElement(currentSlide, Globals.me, target, currentTag.privacy, currentTag.id));
                currentTag.privacy = privacy;
                box.tag(currentTag);
                Commands.SendTextBox.ExecuteAsync(new TargettedTextBox(currentSlide, Globals.me, target, currentTag.privacy, box));
            }
            myTextBox = null;
            requeryTextCommands();
            if (box.Text.Length == 0)
                Children.Remove(box);
            else
                setAppropriatePrivacyHalo(box);
        }
        private void textboxGotFocus(object sender, RoutedEventArgs e)
        {
            if (((MeTLTextBox)sender).tag().author != Globals.me) return; //cannot edit other peoples textboxes
            myTextBox = (MeTLTextBox)sender;
            updateTools();
            requeryTextCommands();
            Select(new List<UIElement>());
            originalText = myTextBox.Text;
            Commands.ChangeTextMode.Execute("None");
        }
        private void updateTools()
        {
            var info = new TextInformation
            {
                Family = defaultFamily,
                Size = defaultSize,
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
                info.IsPrivate = myTextBox.tag().privacy.ToLower() == "private" ? true : false; 
            }
            Commands.TextboxFocused.ExecuteAsync(info);

        }
        private void box_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            originalText = ((MeTLTextBox)sender).Text;
            e.Handled = false;
        }
        public static Timer typingTimer = null;
        private string originalText;
        private int thisSlide = -1;
        private void SendNewText(object sender, TextChangedEventArgs e)
        {
            if (originalText == null) return; 


            var box = (MeTLTextBox)sender;
            var undoText = originalText.Clone().ToString();
            var redoText = box.Text.Clone().ToString();
            ApplyPrivacyStylingToElement(box, box.tag().privacy);
            box.Height = Double.NaN;
            var mybox = Clone(box);
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
                if (thisSlide == -1) thisSlide = currentSlide;
                typingTimer = new Timer(delegate
                {
                    Dispatcher.adoptAsync(delegate
                                                    {
                                                        sendTextWithoutHistory((MeTLTextBox)sender, privacy, thisSlide);
                                                        typingTimer = null;
                                                    });
                }, null, 600, Timeout.Infinite);
            }
            else
            {
                thisSlide = currentSlide;
                GlobalTimers.resetSyncTimer();
                typingTimer.Change(600, Timeout.Infinite);
            }
        }
        public void sendTextWithoutHistory(MeTLTextBox box, string thisPrivacy)
        {
            sendTextWithoutHistory(box, thisPrivacy, currentSlide);
        }
        public void sendTextWithoutHistory(MeTLTextBox box, string thisPrivacy, int slide)
        {
            RemovePrivacyStylingFromElement(box);
            if (box.tag().privacy != Globals.privacy)
                dirtyTextBoxWithoutHistory(box);
            var oldTextTag = box.tag();
            var newTextTag = new MeTLLib.DataTypes.TextTag(oldTextTag.author, thisPrivacy, oldTextTag.id);
            box.tag(newTextTag);
            var privateRoom = string.Format("{0}{1}", currentSlide, box.tag().author);
            if(thisPrivacy.ToLower() == "private" && Globals.isAuthor && Globals.me != box.tag().author)
                Commands.SneakInto.Execute(privateRoom);
            Commands.SendTextBox.ExecuteAsync(new TargettedTextBox(slide, box.tag().author, target, thisPrivacy, box));
            if(thisPrivacy.ToLower() == "private" && Globals.isAuthor && Globals.me != box.tag().author)
                Commands.SneakOutOf.Execute(privateRoom);
        }
        private void setAppropriatePrivacyHalo(MeTLTextBox box)
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
            if (targettedBox.author == Globals.me && alreadyHaveThisTextBox(targettedBox.box.toMeTLTextBox()) && me != "projector")
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
                if (child is MeTLTextBox)
                    if (((MeTLTextBox)child).tag().id.Equals(box.tag().id))
                        doomedChildren.Add((FrameworkElement)child);
            }
            foreach (var child in doomedChildren)
                Children.Remove(child);
        }

        private bool alreadyHaveThisTextBox(MeTLTextBox box)
        {
            bool result = false;
            Dispatcher.adopt(() =>
            {
                var boxId = box.tag().id;
                var privacy = box.tag().privacy;
                foreach (var text in Children)
                    if (text is MeTLTextBox)
                        if (((MeTLTextBox)text).tag().id == boxId && ((MeTLTextBox)text).tag().privacy == privacy) result = true;
            });
            return result;
        }
        private MeTLTextBox textBoxFromId(string boxId)
        {
            MeTLTextBox result = null;
            Dispatcher.adopt(() =>
            {
                foreach (var text in Children)
                    if (text.GetType() == typeof(MeTLTextBox))
                        if (((MeTLTextBox)text).tag().id == boxId) result = (MeTLTextBox)text;
            });
            return result;
        }
        public void doText(MeTLLib.DataTypes.TargettedTextBox targettedBox)
        {
            Dispatcher.adoptAsync(delegate
                                      {
                                          if (targettedBox.target != target) return;
                                          if (targettedBox.slide == currentSlide &&
                                              (targettedBox.privacy == "public" || targettedBox.author == Globals.me))
                                          {

                                              var box = targettedBox.box.toMeTLTextBox();
                                              removeDoomedTextBoxes(targettedBox);
                                              Children.Add(applyDefaultAttributes(box));
                                              if (!(targettedBox.author == Globals.me && focusable))
                                                  box.Focusable = false;
                                              ApplyPrivacyStylingToElement(box, targettedBox.privacy);
                                          }
                                      });
        }
        public MeTLTextBox Clone(MeTLTextBox OldBox)
        {
            var box = new MeTLTextBox();
            box.AcceptsReturn = true;
            box.TextWrapping = TextWrapping.WrapWithOverflow;
            box.GotFocus += textboxGotFocus;
            box.LostFocus += textboxLostFocus;
            box.BorderThickness = new Thickness(0);
            box.BorderBrush = new SolidColorBrush(Colors.Transparent);
            box.Background = new SolidColorBrush(Colors.Transparent);
            box.Focusable = canEdit && canFocus;
            box.tag(OldBox.tag());
            box.Height = OldBox.Height;
            box.Width = OldBox.Width;
            box.FontFamily = OldBox.FontFamily;
            box.FontWeight = OldBox.FontWeight;
            box.FontStyle = OldBox.FontStyle;
            box.TextDecorations = OldBox.TextDecorations;
            box.FontSize = OldBox.FontSize;
            box.Foreground = OldBox.Foreground;
            box.Text = OldBox.Text;
            SetLeft(box, GetLeft(OldBox));
            SetTop(box, GetTop(OldBox));
            return box;
        }
        public static IEnumerable<Point> getTextPoints(MeTLTextBox text)
        {
            if (text == null) return null;
            var y = GetTop(text);
            var x = GetLeft(text);
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
                        var box = ((MeTLTextBox)Children.ToList().Where(c => ((MeTLTextBox)c).tag().id ==  currentTextBox.tag().id).FirstOrDefault());
                        box.TextChanged -= SendNewText;
                        box.Text = undoText;
                        box.CaretIndex = caret;
                        sendTextWithoutHistory(box, box.tag().privacy);
                        box.TextChanged += SendNewText;
                    };
                    Action redo = () =>
                    {
                        ClearAdorners();
                        var box = ((MeTLTextBox)Children.ToList().Where(c => ((MeTLTextBox)c).tag().id ==  currentTextBox.tag().id).FirstOrDefault());
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
                            box.Width = (this.ActualWidth - GetLeft(box)) * 0.95; // Decrease by a further 5%
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
                    Children.Add(box);
                    SetLeft(box, 15);
                    SetTop(box, 15);
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

        public void OnClipboardCopy()
        {

        }

        public void OnClipboardCut()
        {

        }

        protected override void HandlePaste()
        {
            Commands.ClipboardManager.Execute(ClipboardAction.Paste);
        }
        protected override void HandleCopy()
        {
            try
            {
                if (myTextBox != null && myTextBox.SelectionLength > 0)
                {
                    Action undo = () => Clipboard.GetText();
                    Action redo = () => Clipboard.SetText(myTextBox.SelectedText);
                    redo();
                    UndoHistory.Queue(undo, redo);

                }
                else
                {
                    myTextBox = null;
                    var elements =  GetSelectedElements().Where(e => e is MeTLTextBox);
                    Action undo = () => {
                        foreach(var box in elements)
                            Clipboard.GetText();
                    };
                    Action redo = () =>
                                      {
                                          foreach (var box in elements)
                                              Clipboard.SetText(((MeTLTextBox)box).Text);
                                      };
                    UndoHistory.Queue(undo, redo);
                    redo();
                    
                }
            }
            catch (Exception)
            {
                
            }
        }
        protected override void HandleCut()
        {
            try
            {
                if (myTextBox != null && myTextBox.SelectionLength > 0)
                {
                    var selection = myTextBox.SelectedText;
                    var text = myTextBox.Text;
                    var start = myTextBox.SelectionStart;
                    var length = myTextBox.SelectionLength;
                    var currentTextBox = myTextBox.clone();
                    Action undo = () =>
                                      {
                                          ClearAdorners();
                                          var activeTextbox = ((MeTLTextBox)Children.ToList().Where(c => ((MeTLTextBox)c).tag().id ==  currentTextBox.tag().id).FirstOrDefault());
                                          activeTextbox.Text = text;
                                          activeTextbox.CaretIndex = start + length;
                                          if (!alreadyHaveThisTextBox(activeTextbox))
                                              sendTextWithoutHistory(currentTextBox, currentTextBox.tag().privacy);
                                          Clipboard.GetText();
                                          addAdorners();
                                      };
                    Action redo = () =>
                                      {
                                          ClearAdorners();
                                          Clipboard.SetText(selection);
                                          var activeTextbox = ((MeTLTextBox)Children.ToList().Where(c => ((MeTLTextBox)c).tag().id ==  currentTextBox.tag().id).FirstOrDefault());
                                          if (activeTextbox == null) return;
                                          activeTextbox.Text = text.Remove(start, length);
                                          activeTextbox.CaretIndex = start;
                                          if (activeTextbox.Text.Length == 0)
                                          {
                                              ClearAdorners();
                                              myTextBox = null;
                                              dirtyTextBoxWithoutHistory(currentTextBox);
                                          }
                                          addAdorners();
                                      };
                    redo();
                    UndoHistory.Queue(undo, redo);

                }
                else
                {
                    var listToCut = new List<MeTLTextBox>();
                    var selectedElements = GetSelectedElements().Select(tb => Clone((MeTLTextBox) tb)).ToList().Select(Clone);
                    foreach (MeTLTextBox box in GetSelectedElements().Where(e => e is MeTLTextBox))
                    {
                        Clipboard.SetText(box.Text);
                        listToCut.Add(box);
                    }
                    ClearAdorners();
                    Action redo = () =>
                                      {
                                          ClearAdorners();
                                          myTextBox = null;
                                          foreach (var element in listToCut)
                                              dirtyTextBoxWithoutHistory(element);
                                          addAdorners();
                                      };
                    Action undo = () =>
                                      {

                                          ClearAdorners();
                                          var mySelectedElements = selectedElements.Select(t => t.clone());
                                          List<UIElement> selection = new List<UIElement>();
                                          foreach (var box in mySelectedElements)
                                              Clipboard.GetText();
                                          foreach (var box in mySelectedElements)
                                          {
                                              sendBox(box.toMeTLTextBox());
                                              selection.Add(box);
                                          }
                                          Select(selection);
                                          addAdorners();
                                      };
                    UndoHistory.Queue(undo, redo);
                    redo();
                }


            }
            catch (Exception)
            {
            }
        }
        public override void showPrivateContent()
        {
            foreach (UIElement child in Children)
                if (child.GetType() == typeof(MeTLTextBox) && ((MeTLTextBox)child).tag().privacy == "private")
                    child.Visibility = Visibility.Visible;
        }
        public override void hidePrivateContent()
        {
            foreach (UIElement child in Children)
                if (child.GetType() == typeof(MeTLTextBox) && ((MeTLTextBox)child).tag().privacy == "private")
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
                
                var selectedElements = GetSelectedElements().ToList();
                
                Action redo = () => Dispatcher.adopt(delegate
                     {
                          var mySelectedElements = selectedElements.Select(t => Clone((MeTLTextBox)t));
                         foreach (MeTLTextBox textBox in mySelectedElements.Where(i => i.tag(). privacy != newPrivacy))
                         {
                             var oldTag = ((MeTLTextBox)textBox).tag();
                             oldTag.privacy = newPrivacy;
                             dirtyTextBoxWithoutHistory(textBox);
                             ((MeTLTextBox)textBox).tag(oldTag);
                             sendTextWithoutHistory(textBox, newPrivacy);
                         }
                     });
                Action undo = () =>
                      {
                          var mySelectedElements = selectedElements.Select(t => Clone((MeTLTextBox)t));
                          foreach (MeTLTextBox box in mySelectedElements)
                          {
                                if(Children.ToList().Where(tb => ((MeTLTextBox)tb).tag().id == box.tag().id).ToList().Count != 0)
                                    dirtyTextBoxWithoutHistory((MeTLTextBox)Children.ToList().Where(tb => ((MeTLTextBox)tb).tag().id == box.tag().id).ToList().First());
                                sendTextWithoutHistory(box, box.tag().privacy);

                          }
                      };
                redo();
                UndoHistory.Queue(undo, redo);
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
            var rand = new Random();
            box.Text = value;
            box.FontSize = rand.Next(14, 48);
            InkCanvas.SetLeft(box, rand.Next(15, 2000));
            InkCanvas.SetTop(box, rand.Next(15, 2000));
            Text.sendTextWithoutHistory(box, "public");
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