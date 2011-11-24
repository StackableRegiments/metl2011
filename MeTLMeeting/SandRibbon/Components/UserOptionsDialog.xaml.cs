﻿using System;
using System.Windows;
using SandRibbon.Providers;
using MeTLLib.DataTypes;
using SandRibbon.Components.Sandpit;
using System.Diagnostics;
using Divelements.SandRibbon;

namespace SandRibbon.Components
{
    public partial class UserOptionsDialog : Window
    {
        public UserOptionsDialog()
        {
            InitializeComponent();
            DataContext = Globals.UserOptions;
        }
        private void CycleRibbonAppearance(object sender, RoutedEventArgs e)
        {
            App.colorScheme = (RibbonAppearance)((int)App.colorScheme + 1);
            if (!Enum.IsDefined(typeof(RibbonAppearance), App.colorScheme))
                App.colorScheme = (RibbonAppearance)0;

            Commands.SetRibbonAppearance.Execute(App.colorScheme);
        }
        private void Apply(object sender, RoutedEventArgs e)
        {
            Commands.SetUserOptions.Execute(DataContext);
            //this should be wired to a new command - SaveUserOptions, which is commented out in SandRibbonInterop.Commands
            //Commands.SaveUserOptions.Execute(DataContext);
            var level = Pedagogicometer.level((Pedagogicometry.PedagogyCode)((UserOptions)DataContext).pedagogyLevel);
            Trace.TraceInformation("SetPedagogy {0}",level.label);
            Commands.SetPedagogyLevel.Execute(level);
            // ChangeLanguage commented out for 182 staging release. Causing a crash.
            //Commands.ChangeLanguage.Execute(System.Windows.Markup.XmlLanguage.GetLanguage(((UserOptions)DataContext).language));
            try
            {
                if (!String.IsNullOrEmpty(Globals.location.activeConversation))
                {
                    Commands.JoinConversation.Execute(Globals.location.activeConversation);
                }
            }
            catch (NotSetException)
            {
            }

            Close();
        }
        private void Cancel(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void Reset(object sender, RoutedEventArgs e)
        {
            Commands.SetUserOptions.Execute(UserOptions.DEFAULT);
            Close();
        }
    }
}
