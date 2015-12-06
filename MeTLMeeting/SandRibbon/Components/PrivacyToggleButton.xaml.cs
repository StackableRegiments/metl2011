﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SandRibbon.Providers;
using MeTLLib.DataTypes;
using SandRibbon.Pages.Collaboration;
using SandRibbon.Pages;
using SandRibbon.Pages.Collaboration.Models;

namespace SandRibbon.Components
{
    public partial class PrivacyToggleButton : UserControl
    {
        public PrivacyToggleButton(PrivacyToggleButtonInfo mode, Rect bounds)
        {
            InitializeComponent();
            System.Windows.Controls.Canvas.SetLeft(privacyButtons, bounds.Right);
            System.Windows.Controls.Canvas.SetTop(privacyButtons, bounds.Top);
            // no longer clip the bottom of the adorner buttons to the bottom of the element it is bound to
            //privacyButtons.Height = bounds.Height;

            Loaded += (s, e) =>
            {
                var rootPage = DataContext as DataContextRoot;
                if (mode.showDelete)
                    deleteButton.Visibility = Visibility.Visible;
                else
                    deleteButton.Visibility = Visibility.Collapsed;

                if (mode.AdornerTarget == "presentationSpace")
                {
                    if ((
                    !rootPage.ConversationState.StudentsCanPublish || 
                    rootPage.ConversationState.Blacklist.Contains(rootPage.NetworkController.credentials.name)) && !rootPage.ConversationState.IsAuthor)
                    {
                        showButton.Visibility = Visibility.Collapsed;
                        hideButton.Visibility = Visibility.Collapsed;
                    }
                    else if (mode.privacyChoice == "show")
                    {
                        showButton.Visibility = Visibility.Visible;
                        hideButton.Visibility = Visibility.Collapsed;

                    }
                    else if (mode.privacyChoice == "hide")
                    {
                        showButton.Visibility = Visibility.Collapsed;
                        hideButton.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        showButton.Visibility = Visibility.Visible;
                        hideButton.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    showButton.Visibility = Visibility.Collapsed;
                    hideButton.Visibility = Visibility.Collapsed;
                }

                if (rootPage.ConversationState.BanhammerActive)
                {
                    deleteButton.Visibility = Visibility.Collapsed;
                    showButton.Visibility = Visibility.Collapsed;
                    hideButton.Visibility = Visibility.Collapsed;
                }

                if (rootPage.ConversationState.BanhammerActive && rootPage.ConversationState.IsAuthor)
                    banhammerButton.Visibility = Visibility.Visible;
                else
                    banhammerButton.Visibility = Visibility.Collapsed;
            };

        }

        private void showContent(object sender, RoutedEventArgs e)
        {
            Commands.SetPrivacyOfItems.ExecuteAsync(Privacy.Public);
        }
        private void hideContent(object sender, RoutedEventArgs e)
        {
            Commands.SetPrivacyOfItems.ExecuteAsync(Privacy.Private);
        }
        private void deleteContent(object sender, RoutedEventArgs e)
        {
            Commands.DeleteSelectedItems.ExecuteAsync(null);
        }

        private void visualizeContent(object sender, RoutedEventArgs e)
        {
            Commands.VisualizeContent.Execute(null);
        }
        private void banhammerContent(object sender, RoutedEventArgs e)
        {
            Commands.BanhammerSelectedItems.Execute(null);
        }
        public class PrivacyToggleButtonInfo
        {
            public string privacyChoice;
            public Rect ElementBounds;
            public bool showDelete;
            public string AdornerTarget { get; private set; }

            public PrivacyToggleButtonInfo(string privacy, Boolean delete, Rect bounds, string target)
            {
                privacyChoice = privacy;
                showDelete = delete;
                ElementBounds = bounds;
                AdornerTarget = target;
            }
        }
    }
}
