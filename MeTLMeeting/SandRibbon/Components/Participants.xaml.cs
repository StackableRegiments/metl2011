﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MeTLLib.DataTypes;
using Microsoft.Practices.Composite.Presentation.Commands;
using MeTLLib.Providers.Connection;
using Iveonik.Stemmers;
using System.Windows.Data;
using System.Globalization;
using System.Collections.ObjectModel;

namespace SandRibbon.Components
{
    public class SlideCollectionDescriber : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is List<string>)
            {
                return (value as List<string>).Aggregate("", (acc, item) => (acc == "" ? item : acc + ", " + item));
            }
            else
            {
                return "";
            }
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class MeTLUser : DependencyObject
    {
        public string username { private set; get; }
        public Dictionary<string, int> usages = new Dictionary<string, int>();
        public HashSet<String> words
        {
            get
            {
                return (HashSet<string>)GetValue(themeProperty);
            }
            set
            {
                SetValue(themeProperty, value);
            }
        }
        public static readonly DependencyProperty themeProperty = DependencyProperty.Register("themes", typeof(HashSet<String>), typeof(MeTLUser), new UIPropertyMetadata(new HashSet<String>()));
        public int activityCount
        {
            get
            {
                return (int)GetValue(activityCountProperty);
            }
            set
            {
                SetValue(activityCountProperty, value);
            }
        }
        public static readonly DependencyProperty activityCountProperty = DependencyProperty.Register("activityCount", typeof(int), typeof(MeTLUser), new UIPropertyMetadata(0));
        public int submissionCount
        {
            get
            {
                return (int)GetValue(submissionCountProperty);
            }
            set
            {
                SetValue(submissionCountProperty, value);
            }
        }

        public object Words { get; internal set; }

        public static readonly DependencyProperty submissionCountProperty = DependencyProperty.Register("submissionCount", typeof(int), typeof(MeTLUser), new UIPropertyMetadata(0));

        public List<string> slideLocation
        {
            get
            {
                return (List<string>)GetValue(slideLocationProperty);
            }
            set
            {
                SetValue(slideLocationProperty, value);
            }
        }

        public static readonly DependencyProperty slideLocationProperty = DependencyProperty.Register("slideLocation", typeof(List<string>), typeof(MeTLUser), new UIPropertyMetadata(new List<string>()));

        public ObservableCollection<AttributedGroup> Membership
        {
            get; set;
        } = new ObservableCollection<AttributedGroup>();

        public MeTLUser(string user)
        {
            username = user;
            slideLocation = new List<string>();
            activityCount = 0;
            submissionCount = 0;
        }
    }

    public class AttributedGroup
    {
        public string Person { get; set; }
        public GroupSet GroupSet { get; set; }
        public Group Group { get; set; }
        public bool IsMember { get; set; }
    }

    public partial class Participants : UserControl
    {
        public static SlideCollectionDescriber slideCollectionDescriber = new SlideCollectionDescriber();
        public Dictionary<string, MeTLUser> people = new Dictionary<string, MeTLUser>();
        public HashSet<String> seen = new HashSet<string>();
        public IStemmer stemmer = new EnglishStemmer();
        public Participants()
        {
            InitializeComponent();
            Commands.ReceiveStrokes.RegisterCommand(new DelegateCommand<List<TargettedStroke>>(ReceiveStrokes));
            Commands.ReceiveStroke.RegisterCommand(new DelegateCommand<TargettedStroke>(ReceiveStroke));
            Commands.ReceiveTextBox.RegisterCommand(new DelegateCommand<TargettedTextBox>(ReceiveTextbox));
            Commands.ReceiveImage.RegisterCommand(new DelegateCommand<TargettedImage>(ReceiveImage));
            Commands.PreParserAvailable.RegisterCommand(new DelegateCommand<PreParser>(ReceivePreParser));
            Commands.JoinConversation.RegisterCommand(new DelegateCommand<ConversationDetails>(JoinConversation));
            Commands.UpdateConversationDetails.RegisterCommand(new DelegateCommand<ConversationDetails>(ReceiveConversationDetails));
            Commands.MoveTo.RegisterCommand(new DelegateCommand<Location>(MoveTo));
            Commands.ReceiveScreenshotSubmission.RegisterCommand(new DelegateCommand<TargettedSubmission>(ReceiveSubmission));
            Commands.ReceiveAttendance.RegisterCommand(new DelegateCommand<Attendance>(ReceivePresence));
        }

        private void MoveTo(Location loc)
        {
            UpdateGroups(loc);
        }
        private void UpdateGroups(Location loc)
        {
            foreach (var p in people.Values)
            {
                p.Membership.Clear();
                foreach (var gs in loc.currentSlide.GroupSets)
                {
                    foreach (var g in gs.Groups)
                    {
                        p.Membership.Add(new AttributedGroup
                        {
                            Group = g,
                            GroupSet = gs,
                            Person = p.username,
                            IsMember = g.GroupMembers.Contains(p.username)
                        });
                    }
                }
            }
            participantListBox.ItemsSource = people.Values.ToList();
        }
        private void JoinConversation(ConversationDetails newJid)
        {
            ClearList();
        }
        private void ClearList()
        {
            seen.Clear();
            Dispatcher.adopt(() =>
            {
                people.Clear();
            });
        }
        private void ReceiveSubmission(TargettedSubmission sub)
        {
            if (sub.target == "bannedcontent")
            {
                foreach (var historicallyBannedUser in sub.blacklisted)
                {
                    constructPersonFromUsername(historicallyBannedUser.UserName);
                }
            }
            RegisterAction(sub.author);
            RegisterSubmission(sub.author);
        }
        private void ReceiveConversationDetails(ConversationDetails details)
        {
            if (details.Jid == SandRibbon.Providers.Globals.conversationDetails.Jid)
            {
                Dispatcher.adopt(() =>
                {
                    var slide = details.Slides.Where(s => s.id == Providers.Globals.slide).First();
                    UpdateGroups(new Location(
                        details,
                        slide,
                        new List<Slide>()
                    ));
                    foreach (var bannedUsername in details.blacklist)
                    {
                        constructPersonFromUsername(bannedUsername);
                    }
                });
            }
        }
        protected void ReceivePresence(Attendance presence)
        {
            if (Providers.Globals.conversationDetails.Slides.Select(s => s.id.ToString()).Contains(presence.location))
            {
                Dispatcher.adopt(delegate
                {
                    constructPersonFromUsername(presence.author);
                    var user = people[presence.author];
                    if (presence.present)
                    {
                        user.slideLocation = user.slideLocation.Union(new List<string> { presence.location }).Distinct().ToList();
                    }
                    else
                    {
                        user.slideLocation = user.slideLocation.Where(sl => sl != presence.location).ToList();
                    }
                });
            }
        }

        private void ReceiveStrokes(List<TargettedStroke> strokes)
        {
            foreach (var s in strokes)
            {
                ReceiveStroke(s);
            }
        }
        private void RegisterAction(string username)
        {
            Dispatcher.adopt(() =>
            {
                constructPersonFromUsername(username);
                people[username].activityCount++;
            });
        }
        private void RegisterSubmission(string username)
        {
            Dispatcher.adopt(() =>
            {
                constructPersonFromUsername(username);
                people[username].submissionCount++;
            });
        }
        private void ReceiveStroke(TargettedStroke s)
        {
            if (seen.Contains(s.identity)) return;
            seen.Add(s.identity);
            RegisterAction(s.author);
        }
        private void ReceiveTextbox(TargettedTextBox t)
        {
            if (seen.Contains(t.identity)) return;
            seen.Add(t.identity);
            RegisterAction(t.author);
        }
        private void ReceiveImage(TargettedImage i)
        {
            if (seen.Contains(i.identity)) return;
            seen.Add(i.identity);
            RegisterAction(i.author);
        }
        private void ReceivePreParser(PreParser p)
        {
            ReceiveStrokes(p.ink);
            foreach (var t in p.text.Values.ToList())
                ReceiveTextbox(t);
            foreach (var i in p.images.Values.ToList())
                ReceiveImage(i);
            foreach (var s in p.submissions)
                ReceiveSubmission(s);
            foreach (var a in p.attendances)
                ReceivePresence(a);
        }
        private void Ensure(string key, Dictionary<String, List<String>> dict)
        {
            if (!dict.ContainsKey(key))
            {
                dict[key] = new List<String>();
            }
        }
        private List<string> Filter(List<string> words)
        {
            return words.Where(w => w.Count() > 3).Select(w => stemmer.Stem(w)).ToList();
        }

        private Object l = new Object();
        private void constructPersonFromUsername(string username)
        {
            if (!people.ContainsKey(username))
            {
                people[username] = new MeTLUser(username);
                participantListBox.ItemsSource = people.Values.ToList();
            }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var source = (FrameworkElement)sender;
            var group = (AttributedGroup)source.DataContext;
            foreach (var g in group.GroupSet.Groups)
            {
                g.GroupMembers.Remove(group.Person);
            }
            group.Group.GroupMembers.Add(group.Person);
            App.controller.client.conversationDetailsProvider.Update(Providers.Globals.conversationDetails);
        }

        private void AddGroup(object sender, RoutedEventArgs e)
        {
            var s = Providers.Globals.slideDetails;
            if (s.GroupSets.Count == 0)
            {
                s.GroupSets.Add(new GroupSet(System.Guid.NewGuid().ToString(), Providers.Globals.slide.ToString(), 5, new List<Group> { }));
            }
            var gs = s.GroupSets[0];
            gs.Groups.Add(new Group(System.Guid.NewGuid().ToString(), Providers.Globals.slide.ToString(), new List<String>()));
            App.controller.client.conversationDetailsProvider.Update(Providers.Globals.conversationDetails);
        }
    }
}
