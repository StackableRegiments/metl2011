﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Ink;
using System.Windows.Media;
using MeTLLib;
using MeTLLib.Providers.Connection;
using Microsoft.Practices.Composite.Presentation.Commands;
using SandRibbon.Providers;
using System.Collections.Generic;
using MeTLLib.DataTypes;
using SandRibbon.Pages.Collaboration.Models;

namespace SandRibbon.Components
{
    public partial class Projector : UserControl
    {
        public ToolableSpaceModel ToolableSpaceModel
        {
            get { return (ToolableSpaceModel)GetValue(toolableSpaceModelProperty); }
            set { SetValue(toolableSpaceModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for backend.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty toolableSpaceModelProperty =
            DependencyProperty.Register("toolableSpaceModel_projector", typeof(ToolableSpaceModel), typeof(Projector), new PropertyMetadata(new ToolableSpaceModel(MetlConfiguration.empty)));

        public MetlConfiguration backend {  get { return ToolableSpaceModel.backend; } }
        public static WidthCorrector WidthCorrector = new WidthCorrector();
        public static HeightCorrector HeightCorrector = new HeightCorrector();
        public ScrollViewer viewConstraint
        {
            set 
            {
                DataContext = value;
                value.ScrollChanged += new ScrollChangedEventHandler(value_ScrollChanged);
            }
        }
        private static Window windowProperty;
        public static Window Window
        {
            get { return windowProperty; }
            set 
            {
                windowProperty = value;
                value.Closed += (_sender, _args) =>
                {
                    windowProperty = null;
                    AppCommands.RequerySuggested(AppCommands.MirrorPresentationSpace);
                };
            }
        }
        private void value_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            scroll.ScrollToHorizontalOffset(e.HorizontalOffset);
            scroll.ScrollToVerticalOffset(e.VerticalOffset);
        }

        public Projector()
        {
            InitializeComponent();
            Loaded += Projector_Loaded;
            AppCommands.SetDrawingAttributes.RegisterCommand(new DelegateCommand<DrawingAttributes>(SetDrawingAttributes));
            App.getContextFor(backend).controller.commands.UpdateConversationDetails.RegisterCommandToDispatcher(new DelegateCommand<ConversationDetails>(UpdateConversationDetails));
            App.getContextFor(backend).controller.commands.PreParserAvailable.RegisterCommand(new DelegateCommand<MeTLLib.Providers.Connection.PreParser>(PreParserAvailable));
            AppCommands.SetPedagogyLevel.RegisterCommand(new DelegateCommand<object>(setPedagogy));
            App.getContextFor(backend).controller.commands.LeaveAllRooms.RegisterCommand(new DelegateCommand<object>(shutdown));
            App.getContextFor(backend).controller.commands.MoveToCollaborationPage.RegisterCommandToDispatcher(new DelegateCommand<object>(moveTo));
        }
        private string generateTitle(ConversationDetails details)
        {
            var possibleIndex = details.Slides.Where(s => s.id == Globals.location.currentSlide);
            int slideIndex = 1;
            if(possibleIndex.Count() != 0)
                slideIndex = possibleIndex.First().index + 1;
            return string.Format("{0} Page:{1}", details.Title, slideIndex);
        }
        private void UpdateConversationDetails(ConversationDetails details)
        {
            if (details.IsEmpty) return;
            conversationLabel.Text = generateTitle(details);
            
            if (((details.isDeleted || App.getContextFor(backend).controller.creds.authorizedGroups.Where(g=>g.groupKey.ToLower() == details.Subject.ToLower()).Count() == 0) && details.Jid.GetHashCode() == Globals.location.activeConversation.GetHashCode()) || String.IsNullOrEmpty(Globals.location.activeConversation))
            {
                shutdown(null);
            }
        }

        private void shutdown(object obj)
        {
            if(Window != null)
                Window.Close();
        }

        private void setPedagogy(object obj)
        {
            //when you change pedagogy all the commands are deregistered this will restart the projector
            if(Window != null)
                Window.Close();
            AppCommands.CheckExtendedDesktop.Execute(null);
        }

        private void moveTo(object obj)
        {
            conversationLabel.Text = generateTitle(Globals.conversationDetails);
            
            stack.Flush();
        }
        private void Projector_Loaded(object sender, RoutedEventArgs e)
        {
            startProjector(null);
        }
        private void startProjector(object obj)
        {
            try
            {
                App.getContextFor(ToolableSpaceModel.backend).controller.client.historyProvider.Retrieve<PreParser>(null, null, PreParserAvailable, Globals.location.currentSlide.ToString());
            }
            catch (Exception)
            {
            }
            stack.me = "projector";
            stack.Work.EditingMode = InkCanvasEditingMode.None;
            conversationLabel.Text = generateTitle(Globals.conversationDetails);
        }
        private static DrawingAttributes currentAttributes = new DrawingAttributes();
        private static DrawingAttributes deleteAttributes = new DrawingAttributes();
        private static Color deleteColor = Colors.Red;
        public void PreParserAvailable(MeTLLib.Providers.Connection.PreParser parser)
        {
            //if (!isPrivate(parser))
            //{
                BeginInit();
                stack.ReceiveStrokes(parser.ink);
                stack.ReceiveImages(parser.images.Values);
                foreach (var text in parser.text.Values)
                    stack.DoText(text);
                stack.RefreshCanvas();
                /*foreach (var moveDelta in parser.moveDeltas)
                    stack.ReceiveMoveDelta(moveDelta, processHistory: true);*/
                EndInit();
           //}
        }

        private bool isPrivate(MeTLLib.Providers.Connection.PreParser parser)
        {
            if (parser.ink.Where(s => s.privacy == Privacy.Private).Count() > 0)
                return true;
            if (parser.text.Where(s => s.Value.privacy == Privacy.Private).Count() > 0)
                return true;
            if (parser.images.Where(s => s.Value.privacy == Privacy.Private).Count() > 0)
                return true;
            return false;
        }

        private void SetDrawingAttributes(DrawingAttributes attributes)
        {
            currentAttributes = attributes;
            deleteAttributes = currentAttributes.Clone();
            deleteAttributes.Color = deleteColor;
        }
    }
    public class WidthCorrector : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var sourceWidth = (double)values[0];
            var sourceHeight = (double)values[1];
            var targetWidth = (double)values[2];
            var targetHeight = (double)values[3];
            var sourceAspect = sourceWidth / sourceHeight;
            var destinationAspect = targetWidth / targetHeight;
            if(Math.Abs(destinationAspect - sourceAspect) < 0.01) return sourceWidth;
            if (destinationAspect < sourceAspect) return sourceWidth;
            return sourceHeight * destinationAspect;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class HeightCorrector : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var sourceWidth = (double)values[0];
            var sourceHeight = (double)values[1];
            var targetWidth = (double)values[2];
            var targetHeight = (double)values[3];
            var sourceAspect = sourceWidth / sourceHeight;
            var destinationAspect = targetWidth / targetHeight;
            if(Math.Abs(destinationAspect - sourceAspect) < 0.01) return sourceHeight;
            if (destinationAspect > sourceAspect) return sourceHeight;
            return sourceWidth / destinationAspect;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}