﻿using MeTLLib.DataTypes;
using Microsoft.Practices.Composite.Presentation.Commands;
using SandRibbon.Components;
using SandRibbon.Components.Utility;
using SandRibbon.Pages.Analytics;
using SandRibbon.Pages.Conversations;
using SandRibbon.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace SandRibbon.Pages.Collaboration
{
    public class ImageSources
    {
        public ImageSource eraserImage { get; protected set; }
        public ImageSource penImage { get; protected set; }
        public ImageSource highlighterImage { get; protected set; }
        public Brush selectedBrush { get; protected set; }
        public ImageSources(ImageSource _eraserImage, ImageSource _penImage, ImageSource _highlighterImage, Brush _selectedBrush)
        {
            eraserImage = _eraserImage;
            penImage = _penImage;
            highlighterImage = _highlighterImage;
            selectedBrush = _selectedBrush;
        }
    }
    public class PenAttributes : DependencyObject
    {
        public DrawingAttributes attributes { get; protected set; }
        protected DrawingAttributes originalAttributes;
        protected InkCanvasEditingMode originalMode;
        public int id { get; protected set; }
        protected bool ready = false;
        protected ImageSources images;
        public PenAttributes(int _id, InkCanvasEditingMode _mode, DrawingAttributes _attributes, ImageSources _images)
        {
            id = _id;
            images = _images;
            attributes = _attributes;
            mode = _mode;
            attributes.StylusTip = StylusTip.Ellipse;
            width = attributes.Width;
            color = attributes.Color;
            originalAttributes = _attributes.Clone();
            originalMode = _mode;
            isSelectedPen = false;
            ready = true;
            icon = generateImageSource();
        }
        public void replaceAttributes(PenAttributes newAttributes)
        {
            mode = newAttributes.mode;
            color = newAttributes.color;
            width = newAttributes.width;
            isHighlighter = newAttributes.isHighlighter;
        }
        public void resetAttributes()
        {
            replaceAttributes(new PenAttributes(id, originalMode, originalAttributes, images));
        }
        protected static readonly Point centerBottom = new Point(128, 256);
        protected static readonly Point centerTop = new Point(128, 0);
        protected void regenerateVisual()
        {
            backgroundBrush = generateBackgroundBrush();
            icon = generateImageSource();
            description = generateDescription();
        }
        protected string generateDescription()
        {
            return ColorHelpers.describe(this);
        }
        protected Brush generateBackgroundBrush()
        {
            return isSelectedPen ? images.selectedBrush : Brushes.Transparent;
        }
        protected ImageSource generateImageSource()
        {
            DrawingVisual visual = new DrawingVisual();
            DrawingContext dc = visual.RenderOpen();
            /*
            if (isSelected)
            {
                dc.DrawRectangle(images.selectedBrush, new Pen(images.selectedBrush, 0), new Rect(new Point(0, 0), new Point(256, 256)));
                dc.DrawEllipse(new SolidColorBrush(Colors.White), new Pen(new SolidColorBrush(Colors.White), 0.0), centerTop, 128, 128);
            }
            */
            if (mode == InkCanvasEditingMode.EraseByStroke || mode == InkCanvasEditingMode.EraseByPoint)
            {
                dc.DrawImage(images.eraserImage, new Rect(0, 0, 256, 256));
            }
            else
            {
                var colorBrush = new SolidColorBrush(attributes.Color);
                //                dc.DrawEllipse(colorBrush, new Pen(colorBrush, 0), center, attributes.Width / 2, attributes.Width / 2);
                //dc.DrawEllipse(colorBrush, new Pen(colorBrush, 0), centerBottom, attributes.Width * 1.25, attributes.Width * 1.25);
                dc.DrawEllipse(colorBrush, new Pen(colorBrush, 0), centerBottom, attributes.Width + 25, attributes.Width + 25); //adjusting size to scale them and make them more visible, so that the radius will be between 25 and 125 out of a maximum diameter of 256
                if (attributes.IsHighlighter)
                {
                    dc.DrawImage(images.highlighterImage, new Rect(0, 0, 256, 256));
                }
                else
                {
                    dc.DrawImage(images.penImage, new Rect(0, 0, 256, 256));
                }
            }
            dc.Close();
            var bmp = new RenderTargetBitmap(256, 256, 96.0, 96.0, PixelFormats.Pbgra32);
            bmp.Render(visual);
            return bmp;
        }
        public double width
        {
            get { return (double)GetValue(widthProperty); }
            set
            {
                attributes.Width = value;
                attributes.Height = value;
                var changed = ((double)GetValue(widthProperty)) != value;
                if (ready && changed)
                    regenerateVisual();
                SetValue(widthProperty, value);
            }
        }

        public static readonly DependencyProperty widthProperty = DependencyProperty.Register("width", typeof(double), typeof(PenAttributes), new PropertyMetadata(1.0));
        public bool isHighlighter
        {
            get { return (bool)GetValue(isHighlighterProperty); }
            set
            {
                attributes.IsHighlighter = value;
                var changed = ((bool)GetValue(isHighlighterProperty)) != value;
                if (ready && changed)
                    regenerateVisual();
                SetValue(isHighlighterProperty, value);
            }
        }

        public static readonly DependencyProperty isHighlighterProperty = DependencyProperty.Register("isHighlighter", typeof(bool), typeof(PenAttributes), new PropertyMetadata(false));
        public Color color
        {
            get { return (Color)GetValue(colorProperty); }
            set
            {
                attributes.Color = value;
                var changed = ((Color)GetValue(colorProperty)) != value;
                if (ready && changed)
                    regenerateVisual();
                SetValue(colorProperty, value);
            }
        }

        public static readonly DependencyProperty colorProperty = DependencyProperty.Register("color", typeof(Color), typeof(PenAttributes), new PropertyMetadata(Colors.Black));
        public InkCanvasEditingMode mode
        {
            get { return (InkCanvasEditingMode)GetValue(modeProperty); }
            set
            {
                var changed = ((InkCanvasEditingMode)GetValue(modeProperty)) != value;
                if (ready && changed)
                    regenerateVisual();
                SetValue(modeProperty, value);
            }
        }

        public static readonly DependencyProperty modeProperty = DependencyProperty.Register("mode", typeof(InkCanvasEditingMode), typeof(PenAttributes), new PropertyMetadata(InkCanvasEditingMode.None));
        public Brush backgroundBrush
        {
            get { return (Brush)GetValue(backgroundBrushProperty); }
            set
            {
                SetValue(backgroundBrushProperty, value);
            }
        }

        public static readonly DependencyProperty backgroundBrushProperty = DependencyProperty.Register("backgroundBrush", typeof(Brush), typeof(PenAttributes), new PropertyMetadata(Brushes.Transparent));
        public string description
        {
            get { return (string)GetValue(descriptionProperty); }
            set
            {
                SetValue(descriptionProperty, value);
            }
        }

        public static readonly DependencyProperty descriptionProperty = DependencyProperty.Register("description", typeof(string), typeof(PenAttributes), new PropertyMetadata(""));

        public ImageSource icon
        {
            get { return (ImageSource)GetValue(iconProperty); }
            protected set { SetValue(iconProperty, value); }
        }

        public static ImageSource emptyImage = new BitmapImage();
        public static readonly DependencyProperty iconProperty = DependencyProperty.Register("icon", typeof(ImageSource), typeof(PenAttributes), new PropertyMetadata(emptyImage));
        public bool isSelectedPen
        {
            get { return (bool)GetValue(isSelectedPenProperty); }
            set
            {
                if (ready)
                    regenerateVisual();
                SetValue(isSelectedPenProperty, value);
            }
        }

        public static readonly DependencyProperty isSelectedPenProperty = DependencyProperty.Register("isSelectedPen", typeof(bool), typeof(PenAttributes), new PropertyMetadata(false));
    }
    public partial class RibbonCollaborationPage : SlideAwarePage
    {
        protected System.Collections.ObjectModel.ObservableCollection<PenAttributes> penCollection;
        public RibbonCollaborationPage(UserGlobalState _userGlobal, UserServerState _userServer, UserConversationState _userConv, ConversationState _convState, UserSlideState _userSlide, NetworkController _networkController, ConversationDetails _details, Slide _slide)
        {
            NetworkController = _networkController;
            ConversationDetails = _details;
            Slide = _slide;
            UserGlobalState = _userGlobal;
            UserServerState = _userServer;
            UserConversationState = _userConv;
            ConversationState = _convState;
            UserSlideState = _userSlide;
            InitializeComponent();
            DataContext = this;
            var ic = new ImageSourceConverter();
            var images = new ImageSources(
                ic.ConvertFromString("pack://application:,,,/MeTL;component/Resources/ShinyEraser.png") as ImageSource,
                ic.ConvertFromString("pack://application:,,,/MeTL;component/Resources/appbar.draw.pen.png") as ImageSource,
                ic.ConvertFromString("pack://application:,,,/MeTL;component/Resources/Highlighter.png") as ImageSource,
                (Brush)FindResource("CheckedGradient")
            );
            penCollection = new System.Collections.ObjectModel.ObservableCollection<PenAttributes> {
                new PenAttributes(1,InkCanvasEditingMode.EraseByStroke,new System.Windows.Ink.DrawingAttributes {Color=Colors.White,IsHighlighter=false, Width=1 },images),
                new PenAttributes(2,InkCanvasEditingMode.Ink,new System.Windows.Ink.DrawingAttributes {Color=Colors.Black,IsHighlighter=false, Width=1 },images),
                new PenAttributes(3,InkCanvasEditingMode.Ink,new System.Windows.Ink.DrawingAttributes {Color=Colors.Red,IsHighlighter=false, Width=3 },images),
                new PenAttributes(4,InkCanvasEditingMode.Ink,new System.Windows.Ink.DrawingAttributes {Color=Colors.Blue,IsHighlighter=false, Width=3 },images),
                new PenAttributes(5,InkCanvasEditingMode.Ink,new System.Windows.Ink.DrawingAttributes {Color=Colors.Green,IsHighlighter=false, Width=5 },images),
                new PenAttributes(6,InkCanvasEditingMode.Ink,new System.Windows.Ink.DrawingAttributes {Color=Colors.Yellow,IsHighlighter=true, Width=15},images),
                new PenAttributes(7,InkCanvasEditingMode.Ink,new System.Windows.Ink.DrawingAttributes {Color=Colors.Cyan,IsHighlighter=true, Width=25},images)
            };

            pens.ItemsSource = penCollection;

            InitializeComponent();            
            var setLayerCommand = new DelegateCommand<string>(SetLayer);
            var duplicateSlideCommand = new DelegateCommand<SlideAwarePage>(duplicateSlide, canDuplicateSlide);
            var duplicateConversationCommand = new DelegateCommand<ConversationDetails>(duplicateConversation, userMayAdministerConversation);
            var fitToViewCommand = new DelegateCommand<object>(fitToView, canFitToView);
            var originalViewCommand = new DelegateCommand<object>(originalView, canOriginalView);
            var zoomInCommand = new DelegateCommand<object>(doZoomIn, canZoomIn);
            var zoomOutCommand = new DelegateCommand<object>(doZoomOut, canZoomOut);
            var setZoomRectCommand = new DelegateCommand<Rect>(SetZoomRect);

            var currentPenId = 0;
            var setPenAttributesCommand = new DelegateCommand<PenAttributes>(pa =>
            {
                currentPenId = pa.id;
                foreach (var p in penCollection)
                {
                    p.isSelectedPen = false;                    
                };
                pa.isSelectedPen = true;
            });
            var requestReplacePenAttributesCommand = new DelegateCommand<PenAttributes>(pa =>
            {
                new PenCustomizationDialog(pa).ShowDialog();
            }, pa => pa.mode != InkCanvasEditingMode.EraseByPoint && pa.mode != InkCanvasEditingMode.EraseByStroke);
            var replacePenAttributesCommand = new DelegateCommand<PenAttributes>(pa =>
            {
                penCollection.First(p => p.id == pa.id).replaceAttributes(pa);
                if (pa.id == currentPenId)
                {
                    Commands.SetPenAttributes.Execute(pa);
                }
            });
            var requestResetPenAttributesCommand = new DelegateCommand<PenAttributes>(pa =>
            {
                var foundPen = penCollection.First(p => p.id == pa.id);
                foundPen.resetAttributes();
                if (pa.id == currentPenId)
                {
                    Commands.SetPenAttributes.Execute(foundPen);
                }
            }, pa => pa.mode != InkCanvasEditingMode.EraseByPoint && pa.mode != InkCanvasEditingMode.EraseByStroke);
            var updateConversationDetailsCommand = new DelegateCommand<ConversationDetails>(UpdateConversationDetails);
            var proxyMirrorPresentationSpaceCommand = new DelegateCommand<MainWindow>(openProjectorWindow);
            var wordCloudCommand = new DelegateCommand<object>(openWordCloud);
            var uploadFileCommand = new DelegateCommand<object>(uploadFile, canUploadFile);
            
            Loaded += (cs, ce) =>
            {
                UserConversationState.ContentVisibility = ContentFilterVisibility.isGroupSlide(Slide) ? ContentFilterVisibility.defaultGroupVisibilities : ContentFilterVisibility.defaultVisibilities;             
                Commands.SetLayer.RegisterCommand(setLayerCommand);
                Commands.DuplicateSlide.RegisterCommand(duplicateSlideCommand);
                Commands.DuplicateConversation.RegisterCommand(duplicateConversationCommand);
                Commands.FitToView.RegisterCommand(fitToViewCommand);
                Commands.OriginalView.RegisterCommand(originalViewCommand);
                Commands.ZoomIn.RegisterCommand(zoomInCommand);
                Commands.ZoomOut.RegisterCommand(zoomOutCommand);
                Commands.SetZoomRect.RegisterCommand(setZoomRectCommand);
                Commands.SetPenAttributes.RegisterCommand(setPenAttributesCommand);
                Commands.RequestReplacePenAttributes.RegisterCommand(requestReplacePenAttributesCommand);
                Commands.ReplacePenAttributes.RegisterCommand(replacePenAttributesCommand);
                Commands.RequestResetPenAttributes.RegisterCommand(requestResetPenAttributesCommand);
                Commands.UpdateConversationDetails.RegisterCommand(updateConversationDetailsCommand);
                Commands.ProxyMirrorPresentationSpace.RegisterCommand(proxyMirrorPresentationSpaceCommand);
                Commands.WordCloud.RegisterCommand(wordCloudCommand);
                Commands.FileUpload.RegisterCommand(uploadFileCommand);
                scroll.ScrollChanged += (s, e) =>
                    {
                        Commands.RequerySuggested(Commands.ZoomIn, Commands.ZoomOut, Commands.OriginalView, Commands.FitToView, Commands.FitToPageWidth);
                    };
                notesAdornerScroll.ScrollViewer = notesScroll;
                Commands.SetLayer.Execute("Sketch");
                Commands.SetPenAttributes.Execute(penCollection[1]);
                Commands.ShowProjector.Execute(null);

                SetWindowTitle(ConversationDetails);   
                NetworkController.client.SendAttendance("global", new Attendance(NetworkController.credentials.name, ConversationDetails.Jid.ToString(), true, -1));
                NetworkController.client.SendAttendance(ConversationDetails.Jid.ToString(), new Attendance(NetworkController.credentials.name, Slide.id.ToString(), true, -1));                
            };
            this.Unloaded += (ps, pe) =>
            {
                Commands.FileUpload.UnregisterCommand(uploadFileCommand);
                Commands.HideProjector.Execute(null);
                Commands.SetLayer.UnregisterCommand(setLayerCommand);
                Commands.DuplicateSlide.UnregisterCommand(duplicateSlideCommand);
                Commands.DuplicateConversation.UnregisterCommand(duplicateConversationCommand);
                Commands.FitToView.UnregisterCommand(fitToViewCommand);
                Commands.OriginalView.UnregisterCommand(originalViewCommand);
                Commands.ZoomIn.UnregisterCommand(zoomInCommand);
                Commands.ZoomOut.UnregisterCommand(zoomOutCommand);
                Commands.SetZoomRect.UnregisterCommand(setZoomRectCommand);
                Commands.SetPenAttributes.UnregisterCommand(setPenAttributesCommand);
                Commands.RequestReplacePenAttributes.UnregisterCommand(requestReplacePenAttributesCommand);
                Commands.ReplacePenAttributes.UnregisterCommand(replacePenAttributesCommand);
                Commands.RequestResetPenAttributes.UnregisterCommand(requestResetPenAttributesCommand);
                Commands.UpdateConversationDetails.UnregisterCommand(updateConversationDetailsCommand);
                Commands.ProxyMirrorPresentationSpace.UnregisterCommand(proxyMirrorPresentationSpaceCommand);
                Commands.WordCloud.UnregisterCommand(wordCloudCommand);
                UserConversationState.ContentVisibility = ContentFilterVisibility.defaultVisibilities;

                NetworkController.client.LeaveRoom(Slide.id.ToString() + NetworkController.credentials.name);
                NetworkController.client.LeaveRoom(Slide.id.ToString());

                NetworkController.client.SendAttendance(ConversationDetails.Jid.ToString(), new Attendance(NetworkController.credentials.name, Slide.id.ToString(), false, -1));                
                NetworkController.client.SendAttendance("global", new Attendance(NetworkController.credentials.name, ConversationDetails.Jid.ToString(), false, -1));
            };
        }

        private bool canDuplicateSlide(SlideAwarePage context)
        {
            return context.IsAuthor || context.ConversationDetails.Permissions.studentCanAddPage;
        }

        private bool canUploadFile(object arg)
        {
            var isAuthor = ConversationDetails.isAuthor(NetworkController.credentials.name);
            return isAuthor || ConversationDetails.Permissions.studentCanUploadAttachment;
        }

        private void uploadFile(object o)
        {
            var upload = new OpenFileForUpload(Window.GetWindow(this), NetworkController, ConversationDetails, Slide);
            upload.AddResourceFromDisk();
        }
        private void openWordCloud(object obj)
        {
            NavigationService.Navigate(new TagCloudPage(NetworkController, ConversationDetails, UserGlobalState, UserServerState));
        }

        private void UpdateConversationDetails(ConversationDetails cd)
        {
            if (ConversationDetails.Jid == cd.Jid)
            {
                ConversationDetails = cd;
            }
            SetWindowTitle(cd);
        }

        private void SetWindowTitle(ConversationDetails cd)
        {
            Commands.SetWindowTitle.Execute(
                            string.Format("{0} is working in {1}'s '{2}' with collaboration {3}",
                            NetworkController.credentials.name,
                            cd.Author,
                            cd.Title,
                            cd.Permissions.studentCanWorkPublicly ? "ENABLED" : "DISABLED"));
        }

        protected void openProjectorWindow(MainWindow window)
        {
            Commands.MirrorPresentationSpace.Execute(new KeyValuePair<MainWindow, ScrollViewer>(window, scroll));
        }        
        private bool userMayAdministerConversation(ConversationDetails _conversation)
        {
            return ConversationDetails.UserHasPermission(NetworkController.credentials);
        }

        private void SetLayer(string layer)
        {
            Dispatcher.adopt(delegate
            {

                foreach (var group in new UIElement[] { inkGroup, textGroup, imageGroup })
                {
                    group.Visibility = Visibility.Collapsed;
                }
                switch (layer)
                {
                    case "Sketch": inkGroup.Visibility = Visibility.Visible; break;
                    case "Text": textGroup.Visibility = Visibility.Visible; break;
                    case "Image": imageGroup.Visibility = Visibility.Visible; break;
                    case "Html": imageGroup.Visibility = Visibility.Visible; break;
                }
            });
        }

        private bool LessThan(double val1, double val2, double tolerance)
        {
            var difference = val2 * tolerance;
            return val1 < (val2 - difference) && val1 < (val2 + difference);
        }
        private bool GreaterThan(double val1, double val2, double tolerance)
        {
            var difference = val2 * tolerance;
            return val1 > (val2 - difference) && val1 > (val2 + difference);
        }

        private Adorner[] GetPrivacyAdorners(Viewbox viewbox, out AdornerLayer adornerLayer)
        {
            adornerLayer = AdornerLayer.GetAdornerLayer(viewbox);
            if (adornerLayer == null)
                return null;

            return adornerLayer.GetAdorners(viewbox);
        }

        private void GetViewboxAndCanvasFromTarget(string targetName, out Viewbox viewbox, out UIElement container)
        {
            if (targetName == "presentationSpace")
            {
                viewbox = canvasViewBox;
                container = canvas;
                return;
            }
            if (targetName == "notepad")
            {
                viewbox = notesViewBox;
                container = privateNotes;
                return;
            }
            if (targetName == null) //default
            {
                viewbox = canvasViewBox;
                container = canvas;
                return;
            }
            throw new ArgumentException(string.Format("Specified target {0} does not match a declared ViewBox", targetName));
        }

        private bool RemovePrivacyAdorners(string targetName)
        {
            Viewbox viewbox;
            UIElement container;
            GetViewboxAndCanvasFromTarget(targetName, out viewbox, out container);

            bool hasAdorners = false;
            AdornerLayer adornerLayer;
            var adorners = GetPrivacyAdorners(viewbox, out adornerLayer);
            Dispatcher.adopt(() =>
            {
                if (adorners != null && adorners.Count() > 0)
                {
                    hasAdorners = true;
                    foreach (var adorner in adorners)
                        adornerLayer.Remove(adorner);
                }
            });

            return hasAdorners;
        }

        private void zoomConcernedControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            BroadcastZoom();
        }

        private void scroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            BroadcastZoom();
        }

        private void BroadcastZoom()
        {
            var currentZoomHeight = scroll.ActualHeight / canvasViewBox.ActualHeight;
            var currentZoomWidth = scroll.ActualWidth / canvasViewBox.ActualWidth;
            var currentZoom = Math.Max(currentZoomHeight, currentZoomWidth);
            Commands.ZoomChanged.Execute(currentZoom);
        }

        private void notepadSizeChanged(object sender, SizeChangedEventArgs e)
        {
            BroadcastZoom();
        }

        private void notepadScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            BroadcastZoom();
        }
        private void RibbonApplicationMenuItem_SearchConversations_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ConversationSearchPage(UserGlobalState, UserServerState, NetworkController, ""));
        }
        private void RibbonApplicationMenuItem_ConversationOverview_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ConversationOverviewPage(UserGlobalState, UserServerState, UserConversationState, NetworkController, ConversationDetails));
        }
        private bool canZoomIn(object sender)
        {
            return !(scroll == null) && ConversationDetails != ConversationDetails.Empty;
        }
        private bool canZoomOut(object sender)
        {
            var result = false;
            if (scroll == null)
                result = false;
            else
            {
                var cvHeight = adornerGrid.ActualHeight;
                var cvWidth = adornerGrid.ActualWidth;
                var cvRatio = cvWidth / cvHeight;
                bool hTrue = scroll.ViewportWidth < scroll.ExtentWidth;
                bool vTrue = scroll.ViewportHeight < scroll.ExtentHeight;
                var scrollRatio = scroll.ActualWidth / scroll.ActualHeight;
                if (scrollRatio > cvRatio)
                {
                    result = hTrue;
                }
                if (scrollRatio < cvRatio)
                {
                    result = vTrue;
                }
                result = (hTrue || vTrue) && ConversationDetails != ConversationDetails.Empty;
            }
            return result;
        }

        private void doZoomIn(object sender)
        {
            var ZoomValue = 0.9;
            var scrollHOffset = scroll.HorizontalOffset;
            var scrollVOffset = scroll.VerticalOffset;
            var cvHeight = adornerGrid.ActualHeight;
            var cvWidth = adornerGrid.ActualWidth;
            var cvRatio = cvWidth / cvHeight;
            double newWidth = 0;
            double newHeight = 0;
            double oldWidth = scroll.ActualWidth;
            double oldHeight = scroll.ActualHeight;
            var scrollRatio = oldWidth / oldHeight;
            if (scrollRatio > cvRatio)
            {
                newWidth = scroll.ActualWidth * ZoomValue;
                if (newWidth > scroll.ExtentWidth)
                    newWidth = scroll.ExtentWidth;
                scroll.Width = newWidth;
                newHeight = newWidth / cvRatio;
                if (newHeight > scroll.ExtentHeight)
                    newHeight = scroll.ExtentHeight;
                scroll.Height = newHeight;
            }
            if (scrollRatio < cvRatio)
            {
                newHeight = scroll.ActualHeight * ZoomValue;
                if (newHeight > scroll.ExtentHeight)
                    newHeight = scroll.ExtentHeight;
                scroll.Height = newHeight;
                newWidth = newHeight * cvRatio;
                if (newWidth > scroll.ExtentWidth)
                    newWidth = scroll.ExtentWidth;
                scroll.Width = newWidth;
            }
            if (scrollRatio == cvRatio)
            {
                newHeight = scroll.ActualHeight * ZoomValue;
                if (newHeight > scroll.ExtentHeight)
                    newHeight = scroll.ExtentHeight;
                scroll.Height = newHeight;
                newWidth = scroll.ActualWidth * ZoomValue;
                if (newWidth > scroll.ExtentWidth)
                    newWidth = scroll.ExtentWidth;
                scroll.Width = newWidth;
            }
            scroll.ScrollToHorizontalOffset(scrollHOffset + ((oldWidth - newWidth) / 2));
            scroll.ScrollToVerticalOffset(scrollVOffset + ((oldHeight - newHeight) / 2));
        }
        private void doZoomOut(object sender)
        {
            var ZoomValue = 1.1;
            var scrollHOffset = scroll.HorizontalOffset;
            var scrollVOffset = scroll.VerticalOffset;
            var cvHeight = adornerGrid.ActualHeight;
            var cvWidth = adornerGrid.ActualWidth;
            var cvRatio = cvWidth / cvHeight;
            var scrollRatio = scroll.ActualWidth / scroll.ActualHeight;
            double newWidth = 0;
            double newHeight = 0;
            double oldWidth = scroll.ActualWidth;
            double oldHeight = scroll.ActualHeight;
            if (scrollRatio > cvRatio)
            {
                newWidth = scroll.ActualWidth * ZoomValue;
                if (newWidth > scroll.ExtentWidth)
                    newWidth = scroll.ExtentWidth;
                scroll.Width = newWidth;
                newHeight = newWidth / cvRatio;
                if (newHeight > scroll.ExtentHeight)
                    newHeight = scroll.ExtentHeight;
                scroll.Height = newHeight;
            }
            if (scrollRatio < cvRatio)
            {
                newHeight = scroll.ActualHeight * ZoomValue;
                if (newHeight > scroll.ExtentHeight)
                    newHeight = scroll.ExtentHeight;
                scroll.Height = newHeight;
                newWidth = newHeight * cvRatio;
                if (newWidth > scroll.ExtentWidth)
                    newWidth = scroll.ExtentWidth;
                scroll.Width = newWidth;
            }
            if (scrollRatio == cvRatio)
            {
                newHeight = scroll.ActualHeight * ZoomValue;
                if (newHeight > scroll.ExtentHeight)
                    newHeight = scroll.ExtentHeight;
                scroll.Height = newHeight;
                newWidth = scroll.ActualWidth * ZoomValue;
                if (newWidth > scroll.ExtentWidth)
                    newWidth = scroll.ExtentWidth;
                scroll.Width = newWidth;
            }
            scroll.ScrollToHorizontalOffset(scrollHOffset + ((oldWidth - newWidth) / 2));
            scroll.ScrollToVerticalOffset(scrollVOffset + ((oldHeight - newHeight) / 2));
        }
        private void SetZoomRect(Rect viewbox)
        {
            Dispatcher.adopt(delegate
            {
                scroll.Width = viewbox.Width;
                scroll.Height = viewbox.Height;
                scroll.UpdateLayout();
                scroll.ScrollToHorizontalOffset(viewbox.X);
                scroll.ScrollToVerticalOffset(viewbox.Y);
            });
        }
        protected bool canFitToView(object _unused)
        {
            return scroll != null && !(double.IsNaN(scroll.Height) && double.IsNaN(scroll.Width) && double.IsNaN(canvas.Height) && double.IsNaN(canvas.Width));
        }
        protected void fitToView(object _unused)
        {
            if (scroll != null)
            {
                scroll.Height = double.NaN;
                scroll.Width = double.NaN;
                canvas.Height = double.NaN;
                canvas.Width = double.NaN;
            }
        }
        protected bool canOriginalView(object _unused)
        {
            return
                scroll != null &&
                ConversationDetails != null &&
                ConversationDetails != ConversationDetails.Empty &&
                Slide != null &&
                Slide != Slide.Empty &&
                scroll.Height != Slide.defaultHeight &&
                scroll.Width != Slide.defaultWidth;
        }
        protected void originalView(object _unused)
        {

            if (scroll != null &&
            ConversationDetails != null &&
            ConversationDetails != ConversationDetails.Empty &&
            Slide != null &&
            Slide != Slide.Empty)
            {
                var currentSlide = Slide;
                if (currentSlide == null || currentSlide.defaultHeight == 0 || currentSlide.defaultWidth == 0) return;
                
                scroll.Width = currentSlide.defaultWidth;
                scroll.Height = currentSlide.defaultHeight;
                if (canvas != null && canvas.stack != null && !Double.IsNaN(canvas.stack.offsetX) && !Double.IsNaN(canvas.stack.offsetY))
                {
                    scroll.ScrollToHorizontalOffset(Math.Min(scroll.ExtentWidth, Math.Max(0, -canvas.stack.offsetX)));
                    scroll.ScrollToVerticalOffset(Math.Min(scroll.ExtentHeight, Math.Max(0, -canvas.stack.offsetY)));
                }
                else
                {
                    scroll.ScrollToLeftEnd();
                    scroll.ScrollToTop();
                }
            }
        }
        private void TextColor_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Commands.SetTextColor.Execute((Color)((System.Windows.Controls.Ribbon.RibbonGalleryItem)e.NewValue).Content);
        }
        private void duplicateSlide(SlideAwarePage context)
        {   if (context.ConversationDetails.UserHasPermission(context.NetworkController.credentials) && 
                context.ConversationDetails.Slides.Exists(s => s.id == context.Slide.id))
            {
                NetworkController.client.DuplicateSlide(context.ConversationDetails, context.Slide);
            }
        }
        private void duplicateConversation(ConversationDetails _conversationToDuplicate)
        {
            var conversationToDuplicate = ConversationDetails;
            if (conversationToDuplicate.UserHasPermission(NetworkController.credentials))
            {
                NetworkController.client.DuplicateConversation(conversationToDuplicate);
            }
        }

        private void Ribbon_Loaded(object sender, RoutedEventArgs e)
        {
            Grid child = VisualTreeHelper.GetChild((DependencyObject)sender, 0) as Grid;
            if (child != null)
            {
                child.RowDefinitions[0].Height = new GridLength(0);
            }
        }
    }
}
