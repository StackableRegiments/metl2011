﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;
using SandRibbon.Providers;
using System.Collections.ObjectModel;
using MeTLLib.DataTypes;
using System.Globalization;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Automation.Peers;
using MeTLLib;
using System.ComponentModel;
using System.Collections.Generic;
using SandRibbon.Components.Utility;
using System.Windows.Automation;
using SandRibbon.Pages.Collaboration;

namespace SandRibbon.Pages.Conversations
{
    public class VisibleToAuthor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (Globals.me == ((ConversationDetails)value).Author) ? Visibility.Visible : Visibility.Hidden;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return false;
        }
    }
    public partial class ConversationSearchPage : Page
    {
        private ObservableCollection<ConversationDetails> searchResultsObserver = new ObservableCollection<MeTLLib.DataTypes.ConversationDetails>();

        private System.Threading.Timer refreshTimer;
        private ListCollectionView sortedConversations;

        public ConversationSearchPage()
        {
            InitializeComponent();
            sortedConversations = CollectionViewSource.GetDefaultView(this.searchResultsObserver) as ListCollectionView;
            sortedConversations.Filter = isWhatWeWereLookingFor;
            sortedConversations.CustomSort = new ConversationComparator();
            SearchResults.ItemsSource = searchResultsObserver;
            refreshTimer = new Timer(delegate { FillSearchResultsFromInput(); });
            this.PreviewKeyUp += OnPreviewKeyUp;
        }

        private void OnPreviewKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            Dispatcher.adopt(() =>
            {
                if (keyEventArgs.Key == Key.Enter)
                {
                    RefreshSortedConversationsList();
                    FillSearchResultsFromInput();
                }
            });
        }

        private void FillSearchResultsFromInput()
        {
            Dispatcher.Invoke((Action)delegate
            {
                var trimmedSearchInput = SearchInput.Text.Trim();
                if (String.IsNullOrEmpty(trimmedSearchInput))
                {

                    searchResultsObserver.Clear();                    
                    RefreshSortedConversationsList();
                    return;
                }
                if (!String.IsNullOrEmpty(trimmedSearchInput))
                {
                    FillSearchResults(trimmedSearchInput);
                }
                else
                    searchResultsObserver.Clear();
            });
        }

        private void FillSearchResults(string searchString)
        {
            var search = new BackgroundWorker();
            search.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                Commands.BlockSearch.ExecuteAsync(null);
                var bw = sender as BackgroundWorker;
                e.Result = ClientFactory.Connection().ConversationsFor(searchString, SearchConversationDetails.DEFAULT_MAX_SEARCH_RESULTS);
            };

            search.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                Commands.UnblockSearch.ExecuteAsync(null);
                if (e.Error == null)
                {
                    var conversations = e.Result as List<SearchConversationDetails>;
                    if (conversations != null)
                    {
                        searchResultsObserver.Clear();
                        conversations.ForEach(cd => searchResultsObserver.Add(cd));
                    }
                }

                #region Automation events
                if (AutomationPeer.ListenerExists(AutomationEvents.AsyncContentLoaded))
                {
                    var peer = UIElementAutomationPeer.FromElement(this) as UIElementAutomationPeer;
                    if (peer != null)
                    {
                        peer.RaiseAsyncContentLoadedEvent(new AsyncContentLoadedEventArgs(AsyncContentLoadedState.Completed, 100));
                    }
                }
                #endregion
            };

            search.RunWorkerAsync();
        }

        private void clearState()
        {
            SearchInput.Clear();
            RefreshSortedConversationsList();
            SearchInput.SelectionStart = 0;
        }

        private void PauseRefreshTimer()
        {
            refreshTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void RestartRefreshTimer()
        {
            refreshTimer.Change(500, Timeout.Infinite);
        }

        private void UpdateAllConversations(ConversationDetails details)
        {
            if (details.IsEmpty) return;
            // can I use the following test to determine if we're in a conversation?
            if (String.IsNullOrEmpty(Globals.location.activeConversation))
                return;
            RefreshSortedConversationsList();
        }
        private static bool shouldShowConversation(ConversationDetails conversation)
        {
            return conversation.UserHasPermission(Globals.credentials);
        }
        private void RefreshSortedConversationsList()
        {
            if (sortedConversations != null)
                sortedConversations.Refresh();
        }
        private bool isWhatWeWereLookingFor(object sender)
        {
            var conversation = sender as ConversationDetails;
            if (conversation == null) return false;
            if (!shouldShowConversation(conversation))
                return false;
            if (conversation.isDeleted)
                return false;
            var author = conversation.Author;
            if (author != Globals.me && onlyMyConversations.IsChecked.Value)
                return false;
            var title = conversation.Title.ToLower();
            var searchField = new[] { author.ToLower(), title };
            var searchQuery = SearchInput.Text.ToLower().Trim();
            if (searchQuery.Length == 0 && author == Globals.me)
            {//All my conversations show up in an empty search
                return true;
            }
            return searchQuery.Split(' ').All(token => searchField.Any(field => field.Contains(token)));
        }
        private void searchConversations_Click(object sender, RoutedEventArgs e)
        {
            FillSearchResultsFromInput();
        }
        private void SearchInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            RestartRefreshTimer();
        }
        private void ChooseConversationToEnter(object sender, RoutedEventArgs e)
        {
            var requestedJid = ((FrameworkElement)sender).Tag as string;
            // Check that the permissions have not changed since user searched for the conversation
            var conversation = ClientFactory.Connection().DetailsOf(requestedJid);
            if (conversation.UserHasPermission(Globals.credentials))
            {
                Commands.JoinConversation.ExecuteAsync(requestedJid);
                NavigationService.Navigate(new ConversationOverviewPage(conversation));
            }
            else
                MeTLMessage.Information("You no longer have permission to view this conversation.");
        }


        private ContentPresenter view(object backedByConversation)
        {
            var conversation = (ConversationDetails)((FrameworkElement)backedByConversation).DataContext;
            var item = SearchResults.ItemContainerGenerator.ContainerFromItem(conversation);
            var view = (ContentPresenter)item;
            return view;
        }

        private void EditConversation(object sender, RoutedEventArgs e)
        {
            var conversation = (ConversationDetails)((FrameworkElement)sender).DataContext;
            NavigationService.Navigate(new ConversationEditPage(conversation));
        }

        private void onlyMyConversations_Checked(object sender, RoutedEventArgs e)
        {
            RefreshSortedConversationsList();
        }
    }
    public class ConversationComparator : System.Collections.IComparer
    {
        private SearchConversationDetails ConvertToSearchConversationDetails(object obj)
        {
            if (obj is MeTLLib.DataTypes.SearchConversationDetails)
                return obj as SearchConversationDetails;

            if (obj is MeTLLib.DataTypes.ConversationDetails)
                return new SearchConversationDetails(obj as ConversationDetails);

            throw new ArgumentException("obj is of invalid type: " + obj.GetType().Name);
        }

        public int Compare(object x, object y)
        {
            var dis = ConvertToSearchConversationDetails(x);
            var dat = ConvertToSearchConversationDetails(y);
            return -1 * dis.Created.CompareTo(dat.Created);
        }
    }


    /// <summary>
    /// The ItemsView is the same as the ItemsControl but provides an Automation ID.
    /// </summary>
    public class ItemsView : ItemsControl
    {
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ItemsViewAutomationPeer(this);
        }
    }

    public class ItemsViewAutomationPeer : FrameworkElementAutomationPeer
    {
        private readonly ItemsView itemsView;

        public ItemsViewAutomationPeer(ItemsView itemsView) : base(itemsView)
        {
            this.itemsView = itemsView;
        }

        protected override string GetAutomationIdCore()
        {
            return itemsView.Name;
        }
    }
}
