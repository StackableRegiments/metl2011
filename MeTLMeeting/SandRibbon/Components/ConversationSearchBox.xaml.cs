﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Practices.Composite.Presentation.Commands;
using SandRibbon.Providers;
using System.Windows.Media.Animation;
using System.Collections.ObjectModel;
using SandRibbon.Providers.Structure;
using SandRibbon.Utils;
using MeTLLib.DataTypes;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Windows.Threading;

namespace SandRibbon.Components
{
    public partial class ConversationSearchBox : UserControl
    {
        public class HideIfNotCurrentConversation : IValueConverter {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return activeConversation == ((MeTLLib.DataTypes.ConversationDetails)value).Jid ? Visibility.Visible : Visibility.Collapsed;
            }
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return false;
            }
        }
        public class HideErrorsIfEmptyConverter : IValueConverter { 
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
                return string.IsNullOrEmpty((string)value) ? Visibility.Collapsed : Visibility.Visible;
            }
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
                return value;
            }
        }
        public class IsMeConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return (me == ((MeTLLib.DataTypes.ConversationDetails)value).Author).ToString();
            }
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return false;
            }
        }
        public static HideErrorsIfEmptyConverter HideErrorsIfEmpty = new HideErrorsIfEmptyConverter();
        public static IsMeConverter isMe = new IsMeConverter();
        public static HideIfNotCurrentConversation hideIfNotCurrentConversation = new HideIfNotCurrentConversation();
        private ObservableCollection<MeTLLib.DataTypes.ConversationDetails> searchResults = new ObservableCollection<MeTLLib.DataTypes.ConversationDetails>();
        protected static string activeConversation;
        protected static string me;
        public string Errors
        {
            get { return (string)GetValue(ErrorsProperty); }
            set { SetValue(ErrorsProperty, value); }
        }
        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ErrorsProperty =
            DependencyProperty.Register("Errors", typeof(string), typeof(ConversationSearchBox), new UIPropertyMetadata(""));
        public string Version { get; set; }
        private System.Threading.Timer refreshTimer;
        public ConversationSearchBox()
        {
            InitializeComponent();
            Commands.SetIdentity.RegisterCommand(new DelegateCommand<object>(SetIdentity));
            Commands.UpdateConversationDetails.RegisterCommandToDispatcher(new DelegateCommand<ConversationDetails>(UpdateAllConversations));
            Commands.UpdateForeignConversationDetails.RegisterCommandToDispatcher(new DelegateCommand<ConversationDetails>(UpdateAllConversations));
            Commands.JoinConversation.RegisterCommand(new DelegateCommand<string>(JoinConversation));
            Commands.LeaveConversation.RegisterCommand(new DelegateCommand<string>(LeaveConversation));
            Commands.ShowConversationSearchBox.RegisterCommandToDispatcher(new DelegateCommand<object>(ShowConversationSearchBox));
            Commands.HideConversationSearchBox.RegisterCommandToDispatcher(new DelegateCommand<object>(HideConversationSearchBox));
            Commands.BackstageModeChanged.RegisterCommand(new DelegateCommand<string>(BackstageModeChanged));
            Version = ConfigurationProvider.instance.getMetlVersion();
            versionNumber.DataContext = Version;
            SearchResults.ItemsSource = searchResults;
            activeConversation = Globals.location.activeConversation;
            me = Globals.me;
            var view = GetListCollectionView();
            view.Filter = isWhatWeWereLookingFor;
            view.CustomSort = new ConversationComparator();
            refreshTimer = new Timer(delegate {
                Dispatcher.Invoke((Action)delegate
                {
                    GetListCollectionView().Refresh();
                });
            });
        }
        private void BackstageModeChanged(string mode)
        {
            Dispatcher.adoptAsync(() =>
            {
                updateLiveButton(mode);
                GetListCollectionView().Refresh();
            });
                string searchButtonText;
            switch (mode)
            {
                case "mine": searchButtonText = "Filter my Conversations"; break;
                default: searchButtonText = "Search all Conversations"; break;
            }
            Dispatcher.adoptAsync(() =>
            {
                if (searchConversations == null) return;
                searchConversations.Content = searchButtonText;
            });
        }
        private void updateLiveButton(string mode)
        {
            var elements = new[] {mine, find, currentConversation};
            foreach (var button in elements)
                if (button.Name == mode)
                    button.IsChecked = true;
        }
        private void SetIdentity(object _arg){
            var availableConversations = MeTLLib.ClientFactory.Connection().AvailableConversations;
            Dispatcher.adoptAsync(() =>
            {
                foreach (var conversation in availableConversations)
                    if(conversation.Subject.ToLower() != "deleted")
                        searchResults.Add(conversation);
            });
        }
        private void clearState(){
            SearchInput.Text = "";
            GetListCollectionView().Refresh();
            SearchInput.SelectionStart = 0;
        }
        private void ShowConversationSearchBox(object o)
        {
            activeConversation = Globals.location.activeConversation;
            me = Globals.me;
            if (String.IsNullOrEmpty(activeConversation))
                currentConversation.Visibility = Visibility.Collapsed;
            else {
                currentConversation.Visibility = Visibility.Visible;
            }
            Commands.RequerySuggested();
            this.Visibility = Visibility.Visible;
            clearState();
            Dispatcher.queueFocus(SearchInput);
        }
        private void slidePropertyOut(DependencyProperty property, double limit)
        {
            DoubleAnimation anim = new DoubleAnimation();
            anim.From = 150;
            anim.To = limit;
            anim.Duration = new Duration(TimeSpan.FromSeconds(0.8));
            anim.AutoReverse = false;
            BeginAnimation(property, anim);
        }
        private void HideConversationSearchBox(object o)
        {
            this.Visibility = Visibility.Collapsed;
            Commands.RequerySuggested();
        }
        private void JoinConversation(object o)
        {
            CloseConversationSearchBox();
        }
        private void CloseConversationSearchBox()
        {
            Commands.HideConversationSearchBox.Execute(null);
        }
        private void LeaveConversation(string jid)
        {
        }
        private void UpdateAllConversations(MeTLLib.DataTypes.ConversationDetails details)
        {
            if (details == null) return;
            foreach (var result in searchResults.Where(c => c.Jid == details.Jid).ToList())
                searchResults.Remove(result);
            if (details.Subject.ToLower() != "deleted")
                searchResults.Add(details);
            else
                if (details.Jid == Globals.location.activeConversation)
                    currentConversation.Visibility = Visibility.Collapsed;
            if (!(shouldShowConversation(details)) && details.Jid == Globals.conversationDetails.Jid)
            {
                Commands.RequerySuggested();
                this.Visibility = Visibility.Visible;
            }
            GetListCollectionView().Refresh();
        }
        private bool shouldShowConversation(ConversationDetails conversation)
        {
            if (!(Globals.credentials.authorizedGroups.Select(g => g.groupKey).Contains(conversation.Subject))
                && conversation.Subject != "Unrestricted"
                && !(String.IsNullOrEmpty(conversation.Subject))
                && !(Globals.credentials.authorizedGroups.Select(su => su.groupKey).Contains("Superuser"))) return false;
            return true;
        }
        private ListCollectionView GetListCollectionView()
        {
            return (ListCollectionView)CollectionViewSource.GetDefaultView(this.searchResults);
        }
        private bool isWhatWeWereLookingFor(object o)
        {
            var conversation = (MeTLLib.DataTypes.ConversationDetails)o;
            if (!shouldShowConversation(conversation)) 
                return false;
            if (backstageNav.currentMode == "currentConversation")
                if(conversation.Jid != activeConversation) 
                    return false;
            var author = conversation.Author.ToLower();
            var title = conversation.Title.ToLower();
            var searchField = new[] { author, title };
            var searchQuery = SearchInput.Text.ToLower().Trim();
            if (backstageNav.currentMode == "find" && searchQuery.Length == 0) return false;
            if (backstageNav.currentMode == "mine" && author != Globals.me) return false;
            var target = searchQuery.Split(' ').Aggregate(false, (acc, token) =>
            {
                if (acc) return true;
                if (String.IsNullOrEmpty(token)) return true;
                return searchField.Any(field => field.Contains(token));
            });
            return target;
        }
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
        private void searchConversations_Click(object sender, RoutedEventArgs e)
        {
            GetListCollectionView().Refresh();
        }
        private void SearchInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            refreshTimer.Change(250, Timeout.Infinite);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var jid = ((FrameworkElement)sender).Tag;
            if(jid.Equals(Globals.location.activeConversation))
                Commands.HideConversationSearchBox.Execute(null);
            else
                Commands.JoinConversation.ExecuteAsync(jid);
        }
        private void deleteConversation(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Really delete this conversation?", "Delete Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                var details = (MeTLLib.DataTypes.ConversationDetails)((FrameworkElement)sender).DataContext;
                details.Subject = "Deleted";
                MeTLLib.ClientFactory.Connection().UpdateConversationDetails(details);
            }
        }
        private void mode_Checked(object sender, RoutedEventArgs e)
        {
            var mode = ((FrameworkElement)sender).Name;
            backstageNav.currentMode = mode;
        }
        private ContentControl view(object backedByConversation) { 
            var conversation = (ConversationDetails)((FrameworkElement)backedByConversation).DataContext;
            var item = SearchResults.ItemContainerGenerator.ContainerFromItem(conversation);
            var view = (ContentControl)item;
            return view;
        }
        private ConversationDetails context(object sender) {
            return (ConversationDetails)((FrameworkElement)sender).DataContext;
        }
        ConversationDetails originalContext;
        private void assignTemplate(string dataTemplateResourceKey, object sender){
            var sentContext = context(sender);
            var presenter = view(sender);
            presenter.Content = sentContext;
            presenter.ContentTemplate = (DataTemplate)FindResource(dataTemplateResourceKey);
            originalContext = sentContext.Clone();
        }
        private void renameConversation(object sender, RoutedEventArgs e)
        {
            assignTemplate("rename", sender);
        }
        private void shareConversation(object sender, RoutedEventArgs e)
        {
            assignTemplate("share", sender);
        }
        private void cancelEdit(object sender, RoutedEventArgs e)
        {
            var source = (FrameworkElement)sender;
            source.DataContext = originalContext;
            assignTemplate("viewing", sender);
        }
        private string errorsFor(ConversationDetails proposedDetails)
        {
            proposedDetails.Title = proposedDetails.Title.Trim();
            var thisTitleIsASCII = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(proposedDetails.Title)).Equals(proposedDetails.Title);
            var thisIsAValidTitle = !String.IsNullOrEmpty(proposedDetails.Title.Trim());
            var titleAlreadyUsed = searchResults.Except(new[]{proposedDetails}).Any(c => c.Title.Equals(proposedDetails.Title, StringComparison.InvariantCultureIgnoreCase));
            var errorText = String.Empty;
            if (!thisTitleIsASCII)
                errorText += "Conversation title can only contain letters, numbers and punctuation marks. "; 
            if (!thisIsAValidTitle) { errorText += "Invalid conversation title.  "; }
            if (titleAlreadyUsed) { errorText += "Conversation title already used.  "; }
            return errorText;
        }
        private void focusRenameField(object sender, RoutedEventArgs e){
            var source = (TextBox)((FrameworkElement)sender).FindName("renameField");
            source.Focus();
        }
        private void saveEdit(object sender, RoutedEventArgs e)
        {
            var details = context(sender);
            var errors = errorsFor(details);
            if (string.IsNullOrEmpty(errors))
            {
                MeTLLib.ClientFactory.Connection().UpdateConversationDetails(details);
                assignTemplate("viewing", sender);
            }
            else {
                this.Errors = errors;
            }
        }
        private void TextBox_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke((Action)delegate {
                ((TextBox)sender).Focus();
            }, DispatcherPriority.Background);
        }
        private void EditTitleChanged(object sender, TextChangedEventArgs e) {
            //Be slow to complain and quick to forgive.  Remove the errors output as soon as the user starts editing.
            this.Errors = String.Empty;
        }
        private void KeyPressedInTitleRename(object sender, KeyEventArgs e) {
            if(e.Key == Key.Enter){
                var source = (TextBox)sender;
                var context = (ConversationDetails)source.DataContext;
                context.Title = source.Text;
                saveEdit(source, null);
            }
            else if (e.Key == Key.Escape) {
                cancelEdit(sender, null);
            }
        }
    }
    public class ConversationComparator : System.Collections.IComparer
    {
        public int Compare(object x, object y) {
            var dis = (MeTLLib.DataTypes.ConversationDetails)x;
            var dat = (MeTLLib.DataTypes.ConversationDetails)y;
            return -1 * dis.Created.CompareTo(dat.Created);
        }
    }
}
