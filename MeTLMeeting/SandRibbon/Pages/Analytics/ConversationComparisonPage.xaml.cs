﻿using MeTLLib.DataTypes;
using SandRibbon.Pages.Collaboration.Models;
using SandRibbon.Pages.Conversations.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System;
using System.Globalization;
using SandRibbon.Pages.Collaboration;
using SandRibbon.Components;
using System.Windows.Navigation;

namespace SandRibbon.Pages.Analytics
{
    public class ParticipantsEnumerator : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var participants = value as ILookup<string, LocatedActivity>;
            return String.Join(",", participants.Select(p => p.Key));
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public partial class ConversationComparisonPage : Page
    {
        public ConversationComparisonPage(IEnumerable<SearchConversationDetails> cs)
        {           
            InitializeComponent();
            var root = DataContext as DataContextRoot;
            DataContext = new ConversationComparableCorpus(root.NetworkController,cs);
        }
        private void SlideSelected(object sender, RoutedEventArgs e)
        {
            var context = DataContext as ConversationComparableCorpus;
            var source = sender as FrameworkElement;
            var slide = source.DataContext as VmSlide;
            if (!(context.SlideContexts.Any(s => s.context.Slide == slide.Slide.id)))
            {
                context.SlideContexts.Add(new ToolableSpaceModel
                {
                    context = new VisibleSpaceModel
                    {
                        Slide = slide.Slide.id
                    }
                });
                Commands.WatchRoom.Execute(slide.Slide.id.ToString());
            }
        }        
    }
    public class ConversationComparableCorpus : DependencyObject
    {
        public ObservableCollection<ReticulatedConversation> Conversations
        {
            get { return (ObservableCollection<ReticulatedConversation>)GetValue(ConversationsProperty); }
            set { SetValue(ConversationsProperty, value); }
        }
        public static readonly DependencyProperty ConversationsProperty =
            DependencyProperty.Register("Conversations", typeof(ObservableCollection<ReticulatedConversation>), typeof(ConversationComparableCorpus), new PropertyMetadata(new ObservableCollection<ReticulatedConversation>()));

        public ObservableCollection<ToolableSpaceModel> SlideContexts
        {
            get { return (ObservableCollection<ToolableSpaceModel>)GetValue(SlideContextsProperty); }
            set { SetValue(SlideContextsProperty, value); }
        }
        public static readonly DependencyProperty SlideContextsProperty =
            DependencyProperty.Register("SlideContexts", typeof(ObservableCollection<ToolableSpaceModel>), typeof(ConversationComparableCorpus), new PropertyMetadata(new ObservableCollection<ToolableSpaceModel>()));

        public NetworkController NetworkController {get;set;}
        public ConversationComparableCorpus(NetworkController networkController, IEnumerable<SearchConversationDetails> cds)
        {
            NetworkController = NetworkController;
            foreach (var c in cds)
            {
                var conversation = new ReticulatedConversation{
                    networkController = networkController,
                    PresentationPath = c
                };
                Conversations.Add(conversation);                
            }
        }
    }
}
