﻿using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MeTLLib;
using Microsoft.Practices.Composite.Presentation.Commands;
using SandRibbon.Components.Interfaces;
using SandRibbon.Components.Utility;
using SandRibbon.Providers;
using SandRibbon.Utils;
using SandRibbonInterop;
using MeTLLib.DataTypes;
using SandRibbon.Providers.Structure;
using Divelements.SandRibbon;
using SandRibbon.Utils.Connection;
using System.Collections.Generic;
using System.Windows.Ink;
using System.Drawing;
using MeTLLib.Providers.Connection;
using System.Windows.Data;

namespace SandRibbon.Components
{
    /*public class SlideThumbnail : DependencyObject
    {
        public SlideThumbnail(Slide slide)
        {
            this.slide = slide;
            Refresh();
        }
        public Slide slide { private set; get; }
        public int index { get { return slide.index; } }
        public int id { get { return slide.id; } }

        public System.Windows.Controls.Image image { private set; get; }
        public void Refresh()
        {
            if (image == null) image = new System.Windows.Controls.Image();
            ThumbnailProvider.thumbnail(image, id);
        }
    }*/
    /*public class SlideIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is int))
                return 0;
            else return (int)value + 1;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }*/
    public class SlideIndexConverter : IValueConverter
    {
        private ObservableCollection<Slide> collection;
        public SlideIndexConverter(ObservableCollection<Slide> collection)
        {
            this.collection = collection;
        }
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return collection.IndexOf((Slide)value) + 1;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
    public class SlideToThumbConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var source = (System.Windows.Controls.Image)values[0];
            var id = ((Slide)source.DataContext).id;
            ThumbnailProvider.thumbnail(source, id);
            return null;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public partial class SlideDisplay : UserControl, ISlideDisplay
    {
        public int currentSlideIndex = -1;
        public int currentSlideId = -1;
        public ObservableCollection<Slide> thumbnailList = new ObservableCollection<Slide>();
        public static Dictionary<int, PreParser> parsers = new Dictionary<int, PreParser>();
        public static Dictionary<int, PreParser> privateParsers = new Dictionary<int, PreParser>();
        //public static SlideIndexConverter SlideIndexConverter;
        public static SlideIndexConverter SlideIndex;
        public static SlideToThumbConverter SlideToThumb;
        private bool moveTo;
        public SlideDisplay()
        {
            //SlideIndexConverter = new SlideIndexConverter();
            SlideIndex = new SlideIndexConverter(thumbnailList);
            SlideToThumb = new SlideToThumbConverter();
            InitializeComponent();
            slides.ItemsSource = thumbnailList;
            Commands.SyncedMoveRequested.RegisterCommand(new DelegateCommand<int>(moveToTeacher));
            Commands.MoveTo.RegisterCommand(new DelegateCommand<int>(MoveTo, slideInConversation));
            Commands.UpdateConversationDetails.RegisterCommandToDispatcher(new DelegateCommand<ConversationDetails>(Display));
            Commands.JoinConversation.RegisterCommand(new DelegateCommand<string>(JoinConversation));
            Commands.AddSlide.RegisterCommand(new DelegateCommand<object>(addSlide, canAddSlide));
            Commands.MoveToNext.RegisterCommand(new DelegateCommand<object>(moveToNext, isNext));
            Commands.MoveToPrevious.RegisterCommand(new DelegateCommand<object>(moveToPrevious, isPrevious));
            Display(Globals.conversationDetails);
        }
        private void JoinConversation(string jid)
        {
            currentSlideIndex = 0;
        }
        private bool canAddSlide(object _slide)
        {
            try
            {
                var details = Globals.conversationDetails;
                if (String.IsNullOrEmpty(Globals.me) || details == null) return false;
                return (details.Permissions.studentCanPublish || details.Author == Globals.me);
            }
            catch (NotSetException e)
            {
                return false;
            }
        }
        private void addSlide(object _slide)
        {
            MeTLLib.ClientFactory.Connection().AppendSlideAfter(Globals.slide, Globals.conversationDetails.Jid);
            moveTo = true;
        }
        private bool isSlideInSlideDisplay(int slide)
        {
            return thumbnailList.Any(t => t.id == slide);
        }
        private void MoveTo(int slide)
        {
            Dispatcher.adopt(delegate
            {
                if (isSlideInSlideDisplay(slide))
                {
                    var currentSlide = (Slide)slides.SelectedItem;
                    if (currentSlide == null || currentSlide.id != slide)
                    {
                        slides.SelectedIndex =
                            thumbnailList.Select(s => s.id).ToList().IndexOf(slide);
                        slides.ScrollIntoView(slides.SelectedItem);
                    }
                }
            });
            Commands.RequerySuggested(Commands.MoveToNext);
            Commands.RequerySuggested(Commands.MoveToPrevious);
        }
        private void moveToTeacher(int where)
        {
            if (Globals.isAuthor) return;
            if (!Globals.synched) return;
            var action = (Action)(() => Dispatcher.adoptAsync((Action)delegate
                                         {
                                             if (thumbnailList.Where(t => t.id == where).Count() == 1)
                                                 Commands.InternalMoveTo.ExecuteAsync(where);
                                             Commands.MoveTo.ExecuteAsync(where);
                                         }));
            GlobalTimers.SetSyncTimer(action);
        }
        private bool slideInConversation(int slide)
        {
            return Globals.conversationDetails.Slides.Select(t => t.id).Contains(slide);
        }
        private bool isPrevious(object _object)
        {
            return slides != null && slides.SelectedIndex > 0;
        }
        private void moveToPrevious(object _object)
        {
            var previousIndex = slides.SelectedIndex - 1;
            if (previousIndex < 0) return;
            slides.SelectedIndex = previousIndex;
            slides.ScrollIntoView(slides.SelectedItem);
        }
        private bool isNext(object _object)
        {
            return (slides != null && slides.SelectedIndex < thumbnailList.Count() - 1);
        }
        private void moveToNext(object _object)
        {
            var nextIndex = slides.SelectedIndex + 1;
            slides.SelectedIndex = nextIndex;
            slides.ScrollIntoView(slides.SelectedItem);
        }
        public void Display(ConversationDetails details)
        {//We only display the details of our current conversation (or the one we're entering)
            if (details == null || details.Jid == "" || !(Globals.credentials.authorizedGroups.Select(s => s.groupKey).Contains(details.Subject)))
            {
                thumbnailList.Clear();
                return;
            }
            thumbnailList.Clear();
            foreach (var slide in details.Slides)
            {
                if (slide.type == Slide.TYPE.SLIDE)
                {
                    thumbnailList.Add(slide);
                }
            }
            if (moveTo)
            {
                currentSlideIndex++;
                moveTo = false;
            }
            slides.SelectedIndex = currentSlideIndex;
            if (slides.SelectedIndex == -1)
                slides.SelectedIndex = 0;
            slides.ScrollIntoView(slides.SelectedItem);
        }
        private void slides_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var source = (ListBox)sender;
            if (source.SelectedItem != null)
            {
                var proposedIndex = source.SelectedIndex;
                var proposedId =
                    ((Slide)source.SelectedItem).id;
                if (proposedId == currentSlideId) return;
                currentSlideIndex = proposedIndex;
                currentSlideId = proposedId;
                Action<Slide> refreshSlide = s => {
                    //s.Refresh();
                    var i = thumbnailList.IndexOf(s);
                    thumbnailList.RemoveAt(i);
                    thumbnailList.Insert(i,s);
                };
                foreach(var slide in e.RemovedItems) refreshSlide((Slide)slide);
                foreach(var slide in e.AddedItems) refreshSlide((Slide)slide);
                Commands.InternalMoveTo.ExecuteAsync(currentSlideId);
                Commands.MoveTo.ExecuteAsync(currentSlideId);
                if (Globals.isAuthor && Globals.synched)
                    Commands.SendSyncMove.ExecuteAsync(currentSlideId);
                slides.ScrollIntoView(slides.SelectedItem);
            }
        }
    }
}