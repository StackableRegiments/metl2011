﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using MeTLLib;
using Microsoft.Practices.Composite.Presentation.Commands;
using SandRibbon.Automation.AutomationPeers;
using SandRibbon.Providers;
using SandRibbonInterop.Interfaces;
using MeTLLib.DataTypes;
using SandRibbon.Components.Utility;
using SandRibbon.Pages;

namespace SandRibbon.Components
{
    public partial class SimpleConversationSelector : UserControl, IConversationSelector, IConversationListing
    {
        public static IEnumerable<ConversationDetails> rawConversationList = new List<ConversationDetails>();
        public static IEnumerable<ConversationDetails> recentConversations = new List<ConversationDetails>();

        public ServerAwarePage rootPage { get; protected set; }
        public SimpleConversationSelector()
        {
            InitializeComponent();
            var updateConversationDetailsCommand = new DelegateCommand<ConversationDetails>(UpdateConversationDetails);
            Loaded += (s, e) =>
            {
                this.conversations.ItemsSource = new List<ConversationDetails>();
                Commands.UpdateForeignConversationDetails.RegisterCommand(updateConversationDetailsCommand);
                Commands.UpdateConversationDetails.RegisterCommandToDispatcher(updateConversationDetailsCommand);
            };
            Unloaded += (s, e) => {
                Commands.UpdateForeignConversationDetails.UnregisterCommand(updateConversationDetailsCommand);
                Commands.UpdateConversationDetails.UnregisterCommand(updateConversationDetailsCommand);
            };
        }
        private void UpdateConversationDetails(ConversationDetails details)
        {
            if (ConversationDetails.Empty.Equals(details)) return;
            if (recentConversations.Where(c => c.IsJidEqual(details.Jid)).Count() == 0) return;
            if (details.isDeleted)
            {
                recentConversations = recentConversations.Where(c => c.Jid != details.Jid);
                RecentConversationProvider.removeRecentConversation(details.Jid);
            }
            else
                recentConversations.Where(c => c.Jid == details.Jid).First().Title = details.Title;
            conversations.ItemsSource = recentConversations.OrderByDescending(c => c.LastAccessed).Take(6);
        }
        private void RedrawList(object _unused)
        {
            Dispatcher.adopt(() =>
            {
                var potentialConversations = RecentConversationProvider.loadRecentConversations();
                if (potentialConversations != null && potentialConversations.Count() > 0)
                {
                    recentConversations = potentialConversations.Where(c => 
                        c.IsValid && !c.isDeleted);
                    conversations.ItemsSource = recentConversations.Take(6);
                }
            });
        }
        public void List(IEnumerable<ConversationDetails> conversations)
        {
            Dispatcher.adopt((Action)delegate
            {
                rawConversationList = conversations.ToList();
                var list = new List<ConversationDetails>();
                var myConversations = conversations.Where(c => c.Author == rootPage.getNetworkController().credentials.name).OrderBy(c => c.LastAccessed.Date).Reverse().Take(2).ToList();
                if (myConversations.Count() > 0)
                {
                    list.Add(new SeparatorConversation("My Conversations"));
                    list.AddRange(myConversations);
                }
                list.Add(new SeparatorConversation("Conversations I've worked in"));
                var recentConversations = RecentConversationProvider.loadRecentConversations().Where(c => c.IsValid && conversations.Contains(c)).Reverse().Take(2);
                list.AddRange(recentConversations);
                var recentAuthors = list.Select(c => c.Author).Where(c => c != rootPage.getNetworkController().credentials.name).Distinct().ToList();
                foreach (var author in recentAuthors)
                {
                    var otherConversationsByThisAuthor = conversations.Where(c => c.IsValid && !list.Contains(c) && c.Author == author).Reverse();
                    if (otherConversationsByThisAuthor.Count() > 0)
                    {
                        list.Add(new SeparatorConversation(string.Format("{0}'s other conversations:", author)));
                        list.AddRange(otherConversationsByThisAuthor.Take(2));
                    }
                }
                this.conversations.ItemsSource = list;
            });
        }
        public IEnumerable<string> List()
        {
            return rawConversationList.Select(c => c.Title).ToList();
        }
        private void doJoinConversation(object sender, ExecutedRoutedEventArgs e)
        {
            var conversationJid = e.Parameter as string;
            var details = rootPage.getNetworkController().client.DetailsOf(conversationJid);
            if (details.isDeleted || !details.UserHasPermission(rootPage.getNetworkController().credentials))
            {
                // remove the conversation from the menu list
                UpdateConversationDetails(details);
                MeTLMessage.Warning(String.Format("Conversation \"{0}\" is no longer available.", details.Title));
            }
            else
            {
                Commands.JoinConversation.Execute(conversationJid);
            }
        }
        private void canJoinConversation(object sender, CanExecuteRoutedEventArgs e)
        {//CommandParameter is conversation title
            e.CanExecute = Commands.JoinConversation.CanExecute((string)e.Parameter);
        }
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ConversationListingAutomationPeer(this, "selector");
        }
    }
    public class SeparatorStyleSelector : StyleSelector
    {
        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is SeparatorConversation)
            {
                return (Style)((FrameworkElement)container).FindResource("separatorStyle");
            }
            return null;
        }
    }
    public class SeparatorConversation : ConversationDetails
    {
        public SeparatorConversation(string label)
            : base(label == null ? "" : label, "", "", new List<Slide>(), new Permissions("", false, false, false), "")
        {
            if (label == null) label = String.Empty;
            Title = label;
        }
    }
}