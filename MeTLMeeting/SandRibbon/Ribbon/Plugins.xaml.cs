﻿using MeTLLib.DataTypes;
using Microsoft.Practices.Composite.Presentation.Commands;
using System.Windows.Controls.Ribbon;
using System.Windows;
using SandRibbon.Providers;
using SandRibbon.Pages.Collaboration;
using SandRibbon.Pages;

namespace SandRibbon.Tabs
{
    public partial class Plugins : RibbonTab
    {
        public SlideAwarePage rootPage { get; protected set; }
        public Plugins()
        {
            InitializeComponent();
            var updateCommand = new DelegateCommand<ConversationDetails>(Update);
            Loaded += (s, e) => {
                if (rootPage == null)
                {
                    rootPage = DataContext as SlideAwarePage;
                }
                Commands.UpdateConversationDetails.RegisterCommand(updateCommand);
            };
            Unloaded += (s, e) =>
            {
                Commands.UpdateConversationDetails.UnregisterCommand(updateCommand);
            };
        }

        public object Visibiity { get; private set; }

        private void Update(ConversationDetails obj)
        {
            Dispatcher.adopt(delegate {
                teacherPlugins.Visibility = (obj.Author == rootPage.getNetworkController().credentials.name) ? Visibility.Visible : Visibility.Collapsed;
            });
        }
    }
}
