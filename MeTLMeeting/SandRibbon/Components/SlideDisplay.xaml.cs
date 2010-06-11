﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Practices.Composite.Presentation.Commands;
using SandRibbon.Components.Interfaces;
using SandRibbon.Components.Utility;
using SandRibbon.Providers;
using SandRibbon.Utils;
using SandRibbonInterop;
using SandRibbonObjects;
using SandRibbon.Providers.Structure;
using Divelements.SandRibbon;
using SandRibbon.Utils.Connection;

namespace SandRibbon.Components
{
    public partial class SlideDisplay : UserControl, ISlideDisplay
    {
        public int currentSlideIndex = -1;
        public int currentSlideId = -1;
        public ObservableCollection<ThumbnailInformation> thumbnailList = new ObservableCollection<ThumbnailInformation>();
        public bool isAuthor = false;
        private bool moveTo;
        private int realLocation;
        public SlideDisplay()
        {
            InitializeComponent();
            slides.ItemsSource = thumbnailList;
            Commands.SyncedMoveRequested.RegisterCommand(new DelegateCommand<int>(moveToTeacher));
            Commands.MoveTo.RegisterCommand(new DelegateCommand<int>(MoveTo, slideInConversation));
            Commands.JoinConversation.RegisterCommand(new DelegateCommand<string>(jid =>
            {
                currentSlideIndex = 0;
                slides.SelectedIndex = 0;
                slides.ScrollIntoView(slides.SelectedIndex);
            }));
            Commands.UpdateConversationDetails.RegisterCommand(new DelegateCommand<SandRibbonObjects.ConversationDetails>(Display));
            Commands.AddSlide.RegisterCommand(new DelegateCommand<object>(addSlide, canAddSlide));
            Commands.MoveToNext.RegisterCommand(new DelegateCommand<object>(moveToNext, isNext));
            Commands.MoveToPrevious.RegisterCommand(new DelegateCommand<object>(moveToPrevious, isPrevious));
            try
            {
                Display(Globals.conversationDetails);
            }
            catch (NotSetException)
            {
                    //YAAAAAY
            }
        }
        
        private bool canAddSlide(object _slide)
        {
            try
            {
                var details = Globals.conversationDetails;
                if (String.IsNullOrEmpty(Globals.me) || details == null) return false;
                return (details.Permissions.studentCanPublish || details.Author == Globals.me);
            }
            catch(NotSetException e)
            {
                return false;
            }
        }
        private void addSlide(object _slide)
        {
            ConversationDetailsProviderFactory.Provider.AppendSlideAfter(Globals.slide, Globals.conversationDetails.Jid);
            moveTo = true;
        }
        private void MoveTo(int slide)
        {
           Dispatcher.adoptAsync(delegate
                                     {
                                         realLocation = slide;
                                         var typeOfDestination =
                                             Globals.conversationDetails.Slides.Where(s => s.id == slide).Select(s => s.type).
                                                 FirstOrDefault();
                                         var currentSlide = (ThumbnailInformation) slides.SelectedItem;
                                         if (currentSlide == null || currentSlide.slideId != slide)
                                         {
                                             slides.SelectedIndex =
                                                 thumbnailList.Select(s => s.slideId).ToList().IndexOf(slide);
                                         }
                                         slides.ScrollIntoView(slides.SelectedItem);
                                     });
            Commands.RequerySuggested(Commands.MoveToNext);
            Commands.RequerySuggested(Commands.MoveToPrevious);
        }
        private void moveToTeacher(int where)
        {
            if(isAuthor) return;
            if (!Globals.synched) return;
            var action = (Action) (() => Dispatcher.BeginInvoke((Action) delegate
                                         {
                                             if (thumbnailList.Where( t => t.slideId == where).Count()==1)
                                               Commands.MoveTo.Execute(where);
                                         }));
            GlobalTimers.SetSyncTimer(action);
        }
        private bool slideInConversation(int slide)
        {
            var result = Globals.conversationDetails.Slides.Select(t => t.id).Contains(slide);
            return result;
        }
        private bool isPrevious(object _object)
        {
            return slides != null && slides.SelectedIndex > 0;
        }
        private void moveToPrevious(object _object)
        {
            var previousIndex = slides.SelectedIndex - 1;
            if(previousIndex < 0) return;
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
            Dispatcher.adoptAsync((Action)delegate
            {
                if (Globals.me == details.Author)
                    isAuthor = true;
                else
                    isAuthor = false;
                thumbnailList.Clear();
                foreach (var slide in details.Slides)
                {
                    if (slide.type == Slide.TYPE.SLIDE)
                    {
                        thumbnailList.Add(
                            new ThumbnailInformation
                                {
                                    slideId = slide.id,
                                    slideNumber = details.Slides.Where(s => s.type == Slide.TYPE.SLIDE).ToList().IndexOf(slide) + 1,
                                    Exposed = slide.exposed
                                });
                    }
                }
                if(moveTo)
                {
                    currentSlideIndex++;
                    moveTo = false;
                }
                slides.SelectedIndex = currentSlideIndex;
                if (slides.SelectedIndex == -1)
                    slides.SelectedIndex = 0;
            });
        }
        private void Viewbox_Loaded(object sender, RoutedEventArgs e)
        {
            var source = (Viewbox)sender;
            var thumb = (ThumbnailInformation)source.DataContext;
            var stack = (UserCanvasStack)source.Child;
            stack.handwriting.currentSlide = thumb.slideId;
            stack.images.currentSlide = thumb.slideId;
            stack.text.currentSlide = thumb.slideId;
            Commands.PreParserAvailable.RegisterCommand(new DelegateCommand<PreParser>(
               parser=>
               { 
                   if (parser.location.currentSlide == thumb.slideId){
                        stack.handwriting.ReceiveStrokes(parser.ink);
                        stack.images.ReceiveImages(parser.images.Values);
                        foreach (var text in parser.text.Values)
                            stack.text.ReceiveTextBox(text);
                    }
               }));
            Commands.SneakInto.Execute(thumb.slideId.ToString());
        }
        private bool isSlideExposed(ThumbnailInformation slide)
        {
            var isFirst = slide.slideNumber == 0;
            var isPedagogicallyAbleToSeeSlides = Globals.pedagogy.code >= 3;
            var isExposedIfNotCurrentSlide = isAuthor || isFirst || isPedagogicallyAbleToSeeSlides;
            try
            {
                return Globals.slide == slide.slideId || isExposedIfNotCurrentSlide;
            }
            catch (NotSetException)
            {//Don't have a current slide
                return isExposedIfNotCurrentSlide;
            }
        }
        private void slides_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var source = (ListBox)sender;
            if (source.SelectedItem != null)
            {
                var proposedIndex =source.SelectedIndex;
                var proposedId = ((ThumbnailInformation)source.SelectedItem).slideId;
                if (proposedId == currentSlideId) return;
                currentSlideIndex = proposedIndex;
                currentSlideId = proposedId;
                Commands.MoveTo.Execute(currentSlideId);
                slides.ScrollIntoView(slides.SelectedItem);
            }
        }
    }
}