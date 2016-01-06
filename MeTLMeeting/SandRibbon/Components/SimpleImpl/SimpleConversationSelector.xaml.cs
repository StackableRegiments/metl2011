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

namespace SandRibbon.Components
{
    public partial class SimpleConversationSelector : UserControl, IConversationSelector, IConversationListing
    {
        public static IEnumerable<ConversationDetails> rawConversationList = new List<ConversationDetails>();
        public static IEnumerable<ConversationDetails> recentConversations = new List<ConversationDetails>();
        public SimpleConversationSelector()
        {
            InitializeComponent();
            this.conversations.ItemsSource = new List<ConversationDetails>();
            Commands.JoinConversation.RegisterCommand(new DelegateCommand<ConversationDetails>(joinConversation));
            Commands.UpdateForeignConversationDetails.RegisterCommand(new DelegateCommand<ConversationDetails>(UpdateConversationDetails));
            Commands.UpdateConversationDetails.RegisterCommand(new DelegateCommand<ConversationDetails>(UpdateConversationDetails));
            Commands.SetIdentity.RegisterCommand(new DelegateCommand<object>(SetIdentity));
            Commands.ProxyJoinConversation.RegisterCommand(new DelegateCommand<ConversationDetails>(doJoinConversation, canJoinConversation));
        }
        private void SetIdentity(object obj)
        {
            Dispatcher.adopt(delegate
            {
                RedrawList(null);
            });
        }
        private void joinConversation(ConversationDetails details)
        {
            Dispatcher.adopt(delegate
            {
                details.LastAccessed = DateTime.Now;
                if (recentConversations.Where(c => c.Jid == details.Jid).Count() > 0)
                    recentConversations.Where(c => c.Jid == details.Jid).First().LastAccessed = details.LastAccessed;
                else
                {
                    recentConversations = recentConversations.Concat(new[] { details });
                }
                RecentConversationProvider.addRecentConversation(details, Globals.me);
                conversations.ItemsSource = recentConversations.OrderByDescending(c => c.LastAccessed).Take(6);
            });
        }
        private void UpdateConversationDetails(ConversationDetails details)
        {
            if (ConversationDetails.Empty.Equals(details)) return;
            if (recentConversations.Where(c => c.IsJidEqual(details.Jid)).Count() == 0) return;
            Dispatcher.adopt(delegate
            {
                if (details.isDeleted)
                {
                    recentConversations = recentConversations.Where(c => c.Jid != details.Jid);
                    RecentConversationProvider.removeRecentConversation(details.Jid);
                }
                else
                    recentConversations.Where(c => c.Jid == details.Jid).First().Title = details.Title;
                conversations.ItemsSource = recentConversations.OrderByDescending(c => c.LastAccessed).Take(6);
            });
        }
        private void RedrawList(object _unused)
        {
            Dispatcher.adopt(delegate 
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
                var myConversations = conversations.Where(c => c.Author == Globals.me).OrderBy(c => c.LastAccessed.Date).Reverse().Take(2).ToList();
                if (myConversations.Count() > 0)
                {
                    list.Add(new SeparatorConversation("My Conversations"));
                    list.AddRange(myConversations);
                }
                list.Add(new SeparatorConversation("Conversations I've worked in"));
                var recentConversations = RecentConversationProvider.loadRecentConversations().Where(c => c.IsValid && conversations.Contains(c)).Reverse().Take(2);
                list.AddRange(recentConversations);
                var recentAuthors = list.Select(c => c.Author).Where(c => c != Globals.me).Distinct().ToList();
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
        private void doJoinConversation(ConversationDetails partialDetails)
        {
            var details = App.controller.client.DetailsOf(partialDetails.Jid);
            if (details.isDeleted || !details.UserHasPermission(Globals.credentials))
            {
                // remove the conversation from the menu list
                UpdateConversationDetails(details);
                MeTLMessage.Warning(String.Format("Conversation \"{0}\" is no longer available.", details.Title));
            }
            else
            {
                Commands.JoinConversation.ExecuteAsync(details);
            }
        }
        private bool canJoinConversation(ConversationDetails details)
        {
            if (details.isDeleted || !details.UserHasPermission(Globals.credentials))
            {
                UpdateConversationDetails(details);
                return false;
            }
            return true;
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