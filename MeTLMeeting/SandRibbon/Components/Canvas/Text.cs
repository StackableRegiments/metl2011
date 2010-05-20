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
using Microsoft.Practices.Composite.Presentation.Commands;
using SandRibbon.Components.Utility;
using SandRibbon.Utils;
using SandRibbonInterop;
using SandRibbonInterop.MeTLStanzas;

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
        private FontFamily currentFamily = new FontFamily("Arial");
        public Text()
        {
            EditingMode = InkCanvasEditingMode.None;
            Background = Brushes.Transparent;
            SelectionMoved += SendTextBoxes;
            Loaded += (a, b) =>
            {
                MouseUp += (c, args) => placeCursor(this, args);
            };
            PreviewKeyDown += keyPressed;
            SelectionMoving += dirtyText;
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
            Commands.ReceiveTextBox.RegisterCommand(new DelegateCommand<TargettedTextBox>(ReceiveTextBox));
            Commands.SetTextCanvasMode.RegisterCommand(new DelegateCommand<string>(setInkCanvasMode));
            Commands.ReceiveDirtyText.RegisterCommand(new DelegateCommand<TargettedDirtyElement>(receiveDirtyText));
            Commands.SetLayer.RegisterCommand(new DelegateCommand<String>(setupText));
            Commands.MoveTo.RegisterCommand(new DelegateCommand<int>(MoveTo));
        }
        private void MoveTo(int _slide)
        {
            myTextBox = null;
        }
        private bool focusable = true;
        private void setupText(string layer)
        {
            focusable = layer == "Text";
            foreach(var box in Children)
            {
                if (box.GetType() == typeof(TextBox))
                {
                    TextTag tag = ((TextBox) box).tag();
                    ((TextBox) box).Focusable = focusable && (tag.author == me);
                }
            }
        }
        private void keyPressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                foreach (TextBox box in GetSelectedElements())
                {
                    UndoHistory.Queue(() => {
                        sendTextWithoutHistory(box, box.tag().privacy);
                    }, 
                    () => {
                        doDirtyText(box);
                    });
                    dirtyTextBoxWithoutHistory(box);
                }
            }
        }
        private void dirtyTextBoxWithoutHistory(TextBox box)
        {
            removePrivateRegion(box);
            Commands.SendDirtyText.Execute(new TargettedDirtyElement
            {
                identifier = box.tag().id,
                target = target,
                privacy = box.tag().privacy,
                author = me,
                slide = currentSlideId
            });
        }
        
        private void receiveDirtyText(TargettedDirtyElement element)
        {
            if (!(element.target.Equals(target))) return;
            if (!(element.slide == currentSlideId)) return;
            var doDirtyText = (Action)delegate
            {
                for (int i = 0; i < Children.Count; i++)
                {
                    var currentTextbox = (TextBox)Children[i];
                    if (element.identifier.Equals(currentTextbox.tag().id))
                        Children.Remove(currentTextbox);
                }
            };
            if (Thread.CurrentThread != Dispatcher.Thread)
                Dispatcher.BeginInvoke(doDirtyText);
            else
                doDirtyText();
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
                if (text.tag().author == me)
                    myText.Add(text);
            }
            return myText;
        }
        private void dirtyText(object sender, InkCanvasSelectionEditingEventArgs e)
        {
            foreach (var box in GetSelectedElements())
            {
                myTextBox = (TextBox) box;
                doDirtyText((TextBox) box);
            }
        }
        private void doDirtyText(TextBox box)
        {
            UndoHistory.Queue(() =>
            {
                dirtyTextBoxWithoutHistory(box);
                sendText(box);
                myTextBox = null;
            },
            () =>
            {
                doDirtyText(box);
            });
            dirtyTextBoxWithoutHistory(box);
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
            Dispatcher.BeginInvoke((Action)(() => Children.Clear()));
        }
        private void resetTextbox(object obj)
        {
            if (myTextBox == null) return;
            resetText(myTextBox);
        }
        private void resetText(TextBox box)
        {
            removePrivateRegion(box);
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
            Commands.TextboxFocused.Execute(info); 
            sendText(box);
        }
        private void setTextSize(double size)
        {
            removePrivateRegion(myTextBox);
            currentSize = size;
            if (myTextBox == null) return;
            removePrivateRegion(myTextBox);
            myTextBox.FontSize = size;
            myTextBox.Focus();
            sendText(myTextBox);
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
            //return canEdit && textBoxSelected;            
        }
        private void toggleStrikethrough(object obj)
        {
            if(myTextBox == null) return;
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
            if(myTextBox == null) return;
            var currentTextbox = myTextBox; 
            currentTextbox.FontStyle = currentTextbox.FontStyle == FontStyles.Italic ? FontStyles.Normal : FontStyles.Italic;
            sendText(currentTextbox);
            updateTools();
        }
        private void toggleUnderline(object obj)
        {
            if(myTextBox == null) return;
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
            if(myTextBox == null) return; 
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
        private TextBox createNewTextbox()
        {
            var box = new TextBox();
            box.tag(new TextTag
                        {
                            author = me, 
                            privacy = privacy,
                            id =string.Format("{0}:{1}", me, SandRibbonObjects.DateTimeFactory.Now())
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
            if (currentTag.privacy != privacy)
            {
                Commands.SendDirtyText.Execute(new TargettedDirtyElement
                                                   {
                                                       identifier = currentTag.id,
                                                       target = target,
                                                       privacy = currentTag.privacy,
                                                       author = me,
                                                       slide = currentSlideId
                                                   });
                currentTag.privacy = privacy;
                box.tag(currentTag);
                Commands.SendTextBox.Execute(new TargettedTextBox
                                                 {
                                                     box = box,
                                                     author = me,
                                                     privacy = currentTag.privacy,
                                                     slide = currentSlideId,
                                                     target = target,
                                                 });
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
            if(myTextBox.TextDecorations.Count > 0)
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
            Commands.TextboxFocused.Execute(info);
        }

        public static Timer typingTimer = null;
        private void SendNewText(object sender, TextChangedEventArgs e)
        {
            if (typingTimer == null)
            {
                typingTimer = new Timer(delegate
                {
                    Dispatcher.BeginInvoke((Action)(() =>
                                                    {
                                                        sendText((TextBox)sender);
                                                        typingTimer = null;
                                                    }));
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
            UndoHistory.Queue(
            () => {
                dirtyTextBoxWithoutHistory(box);
            }, 
            () => {
                sendText(box);
            });
            GlobalTimers.resetSyncTimer();
            sendTextWithoutHistory(box, box.tag().privacy);
        }
        private void sendTextWithoutHistory(TextBox box, string thisPrivacy)
        {
            setAppropriatePrivacyHalo(box);
            Commands.SendTextBox.Execute(new TargettedTextBox
            {
                box = box,
                author = me,
                privacy = thisPrivacy,
                slide = currentSlideId,
                target = target,
            });
        }

        private void setAppropriatePrivacyHalo(TextBox box)
        {
            if(!Children.Contains(box)) return;
            if(privacy == "private")
                addPrivateRegion(box);
            else
                removePrivateRegion(box);
        }

        public void RemoveTextboxWithTag(string tag)
        {
            for (var i = 0; i < Children.Count; i++)
            {
                if (((TextBox)Children[i]).Tag.ToString() == tag)
                    Children.Remove(Children[i]);
            }
        }
        private void SendTextBoxes(object sender, EventArgs e)
        {
            foreach (TextBox box in GetSelectedElements())
            {
                myTextBox = box;
                sendText(box);
            }
        }
        public void ReceiveTextBox(TargettedTextBox targettedBox)
        {
            if(targettedBox.target != target) return;
            if(targettedBox.author == me && alreadyHaveThisTextBox(targettedBox.box)) return;//I never want my live text to collide with me.
            if (targettedBox.slide == currentSlideId && (targettedBox.privacy == "private" && me == "Projector"))
            {
                var doProjector = (Action) delegate
                                               {
                                                   removeDoomedTextBoxes(targettedBox);
                                               };
                if (Thread.CurrentThread == Dispatcher.Thread)
                    Dispatcher.BeginInvoke(doProjector);
                else
                    doProjector();
            }

            if (targettedBox.slide == currentSlideId &&(targettedBox.privacy == "public" || targettedBox.author == me))
            {
                if (Thread.CurrentThread != Dispatcher.Thread)
                    Dispatcher.BeginInvoke((Action)delegate { doText(targettedBox); });
                else
                    doText(targettedBox);
            }
        }

        private void removeDoomedTextBoxes(TargettedTextBox targettedBox)
        {
            var box = targettedBox.box;
            var doomedChildren = new List<FrameworkElement>();
            foreach (var child in Children)
            {
                if(child is TextBox)
                    if(((TextBox)child).tag().id.Equals(box.tag().id))
                        doomedChildren.Add((FrameworkElement) child);
            }
            foreach (var child in doomedChildren)
                Children.Remove(child);
        }

        private bool alreadyHaveThisTextBox(TextBox box)
        {
            var boxId = box.tag().id;
            var privacy = box.tag().privacy;
            foreach (var text in Children)
                if (text is TextBox)
                    if (((TextBox)text).tag().id == boxId && ((TextBox)text).tag().privacy == privacy) return true;
            return false;
        }
        public void doText(TargettedTextBox targettedBox)
        {
            if(targettedBox.target != target) return;
            if(targettedBox.author == me && alreadyHaveThisTextBox(targettedBox.box)) return;//I never want my live text to collide with me.
            if (targettedBox.slide == currentSlideId && (targettedBox.privacy == "public" || targettedBox.author == me))
            {

                Console.WriteLine("Received textbox");
                var box = targettedBox.box;
                removeDoomedTextBoxes(targettedBox);
                Children.Add(applyDefaultAttributes(box));
                if (!(targettedBox.author == me && focusable))
                    box.Focusable = false;
                if (targettedBox.privacy == "private" && targettedBox.target == target)
                    addPrivateRegion(box);
            }
        }
        private void addPrivateRegion(TextBox text)
        {
            addPrivateRegion(getTextPoints(text));
        }
        private void removePrivateRegion(TextBox text)
        {
            removePrivateRegion(getTextPoints(text));
        }
        public static IEnumerable<Point> getTextPoints(TextBox text)
        {
            if(text == null) return null;
            var y = InkCanvas.GetTop(text);
            var x = InkCanvas.GetLeft(text);
            var width = text.FontSize*text.Text.Count();
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
            var listToCut = new List<TargettedDirtyElement>();
            foreach (TextBox box in GetSelectedElements().Where(e => e is TextBox))
            {

                Clipboard.SetText(box.Text);
                listToCut.Add(new TargettedDirtyElement
                                  {
                                      identifier = box.tag().id,
                                      target = target,
                                      privacy = box.tag().privacy,
                                      author = me,
                                      slide = currentSlideId
                                  });
            }
            foreach(var element in listToCut)
                Commands.SendDirtyText.Execute(element);
        }
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new TextAutomationPeer(this);
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
            : base(owner) {}
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
            Text.ParseInjectedStream(value, element =>{
                Text.Dispatcher.Invoke((Action)delegate
                {
                    foreach (var text in element.SelectElements<MeTLStanzas.TextBox>(true))
                    {
                        Text.sendText(text.Box.box);
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
                var text = Text;
                var sb = new StringBuilder("<text>");
                foreach(var toString in from UIElement box in text.Children
                    select new MeTLStanzas.TextBox(new TargettedTextBox
                    {
                        author = text.me,
                        privacy = text.privacy,
                        slide = text.currentSlideId,
                        box = (TextBox) box, 
                        target = text.target
                    }).ToString())
                sb.Append(toString);
                sb.Append("</text>");
                return sb.ToString();
            }
        }
    }
}