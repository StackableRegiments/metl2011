﻿using System;
using System.Collections.Generic;
using System.Linq;
using MeTLLib.DataTypes;
using SandRibbon.Components;
using SandRibbon.Components.Pedagogicometry;
using System.Drawing;
using TextInformation = SandRibbon.Components.TextInformation;
using SandRibbonObjects;
using SandRibbon.Components.Utility;
using SandRibbon.Pages.Conversations.Models;
using SandRibbon.Profiles;
using SandRibbon.Pages.Collaboration.Palettes;
using Awesomium.Core;

namespace SandRibbon.Providers
{
    enum DefaultPageDimensions
    {
        Width = 720,
        Height = 540
    }

    public static class GlobalConstants
    {
        public const string METLCOLLABORATOR = "MeTL Collaborator";
        public const string METLDEMONSTRATOR = "MeTL Demonstrator";
        public const string METL = "MeTL";
        public const string METLPRESENTER = "MeTL Presenter";
        public static readonly string PUBLIC = "public";
        public static readonly string PRIVATE = "private";
        public static readonly string PROJECTOR = "projector";
        public static readonly string PRESENTATIONSPACE = "presentationSpace";
    }
    public class Globals
    {

        private static Size canvasSize = new Size();

        //private static QuizData quizData = new QuizData();
        public static bool AuthorOnline(string author)
        {
            return PresenceListing.Keys.Where(k => k.Contains(author)).Count() > 0;
        }
        public static bool AuthorInRoom(string author, string jid)
        {

            var authorStatus = PresenceListing.Keys.Where(k => k.Contains(author));
            if (authorStatus.Count() == 0)
                return false;
            var rooms = authorStatus.Aggregate(new List<string>(), (acc, item) =>
                                                                       {
                                                                           acc.AddRange(PresenceListing[item]);
                                                                           return acc;
                                                                       });
            return rooms.Contains(jid);
        }
        public static void UpdatePresenceListing(MeTLPresence presence)
        {
            if (!PresenceListing.ContainsKey(presence.Who))
            {
                if (presence.Joining)
                    PresenceListing.Add(presence.Who, new List<string> { presence.Where });
            }
            else
            {
                if (presence.Joining)
                {
                    var list = PresenceListing[presence.Who];
                    list.Add(presence.Where);
                    PresenceListing[presence.Who] = list.Distinct().ToList();
                }
                else
                {
                    PresenceListing[presence.Who].Remove(presence.Where);
                }
            }
        }
        public static Dictionary<string, List<string>> PresenceListing = new Dictionary<string, List<string>>();

        public static string generateId(string me)
        {
            return string.Format("{0}:{1}", me, DateTimeFactory.Now().Ticks);
        }
        public static string generateId(string me, string seed)
        {
            return string.Format("{0}:{1}:{2}", me, DateTimeFactory.Now().Ticks, seed);
        }
        /*
        public static bool isAuthor
        {
            get
            {
                if (me == null || conversationDetails.ValueEquals(ConversationDetails.Empty)) return false;
                return me.ToLower() == conversationDetails.Author.ToLower();
            }
        }
        */
        public static UserOptions UserOptions
        {
            get
            {
                return SandRibbon.Commands.SetUserOptions.IsInitialised ? (UserOptions)SandRibbon.Commands.SetUserOptions.LastValue() : UserOptions.DEFAULT;
            }
        }
        public static string MeTLType
        {
            get
            {
                try
                {
                    return Commands.MeTLType.LastValue().ToString();
                }
                catch (Exception)
                {
                    return GlobalConstants.METL;
                }
            }

        }
        public static int QuizMargin { get { return 30; } }
        public static Size DefaultCanvasSize
        {
            get
            {
                return new Size((int)DefaultPageDimensions.Width, (int)DefaultPageDimensions.Height);
            }
        }
        public static Size CanvasSize
        {
            get
            {
                return canvasSize;
            }
            set
            {
                canvasSize = value;
            }
        }
        /*
        public static OneNoteConfiguration OneNoteConfiguration { get; set; } = new OneNoteConfiguration
        {
            apiKey = "exampleApiKey",
            apiSecret = "exampleApiSecret"
        };
        */
        /*
        public static PedagogyLevel pedagogy
        {
            get
            {
                return (PedagogyLevel)Commands.SetPedagogyLevel.LastValue();
            }
        }
        public static Location location
        {
            get
            {
                var conversationDetails = Globals.conversationDetails;
                return new Location(conversationDetails.Jid, slide, conversationDetails.Slides.Select(s => s.id).ToList());
            }
        }
        public static List<Slide> slides
        {
            get
            {
                var value = ((ConversationDetails)Commands.UpdateConversationDetails.LastValue());
                if (value != null)
                    return value.Slides;
                else throw new NotSetException("Slides not set");
            }
        }
        public static Slide slideDetails
        {
            get
            {
                try
                {
                    return conversationDetails.Slides.Find(s => s.id == slide);
                }
                catch
                {
                    return Slide.Empty;
                }
            }
        }
        public static ConversationDetails conversationDetails
        {
            get
            {
                return Commands.UpdateConversationDetails.IsInitialised ? (ConversationDetails)Commands.UpdateConversationDetails.LastValue() : ConversationDetails.Empty;
            }
        }
        public static KeyValuePair<ConversationDetails,Slide> slideDetailsInConversationDetails
        {
            get
            {
                return new KeyValuePair<ConversationDetails, Slide>(conversationDetails, slideDetails);
            }
        }
        */
        /*
        public static List<ContentVisibilityDefinition> contentVisibility
        {
            get
            {
                return Commands.SetContentVisibility.IsInitialised ? (List<ContentVisibilityDefinition>)Commands.SetContentVisibility.LastValue() : new List<ContentVisibilityDefinition>();
            }
        }
        */
        /*
        public static MeTLLib.DataTypes.QuizData quiz
        {
            get
            {
                return quizData;
            }
        }
        */
        /*
        public static MeTLLib.DataTypes.Credentials credentials
        {
            get
            {
                return (Credentials)Commands.SetIdentity.LastValue();
            }
        }
        public static List<MeTLLib.DataTypes.AuthorizedGroup> authorizedGroups
        {
            get
            {
                return credentials.authorizedGroups;
            }
        }
        public static List<string> authorizedGroupNames
        {
            get
            {
                return authorizedGroups.Select(g => g.groupKey).ToList();
            }
        }
        */
        /*
        public static bool synched
        {
            get
            {
                return (bool)Commands.SetSync.LastValue();
            }
        }
        */
        /*
        public static int teacherSlide
        {
            get
            {
                return (int)Commands.SyncedMoveRequested.LastValue();
            }
        }
        public static int slide
        {
            get
            {
                return Commands.MoveToCollaborationPage.IsInitialised ? (int)Commands.MoveToCollaborationPage.LastValue() : -1;
            }
        }
        
        public static string me
        {
            get
            {
                return credentials.name;
            }
        }
        public static string privacy
        {
            get
            {
                return (Commands.SetPrivacy.IsInitialised ? (string)Commands.SetPrivacy.LastValue() : GlobalConstants.PUBLIC);
            }
        }
        */
        /*
        public static Policy policy
        {
            get { return new Policy(isAuthor, false); }
        }
        public static UserInformation userInformation
        {
            get { return new UserInformation(credentials, location, policy); }
        }
        */
        /*
        public static bool IsBanhammerActive
        {
            get
            {
                return (bool)(Commands.BanhammerActive.IsInitialised ? Commands.BanhammerActive.LastValue() : false);
            }
        }
        */
        private static StoredUIState _storedUIState;
        public static StoredUIState StoredUIState
        {
            get
            {
                if (_storedUIState == null)
                {
                    _storedUIState = new StoredUIState();
                }

                return _storedUIState;
            }
        }


        public delegate void CanvasClipboardFocusChangedHandler(object sender, EventArgs e);

        private static object lockCurrentCanvasClipboardFocus = new object();
        private static string currentCanvasClipboardFocus = "";
        public static List<Profile> profiles = new List<Profile> {
            new Profile {
                logicalName = "Basic user",
                castBars = new[] {
                    new Bar(8,new[] {
                        new Macro("pen_red"),
                        new Macro("pen_blue"),
                        new Macro("pen_black"),
                        new Macro("pen_yellow_highlighter")
                    })
                {
                    VerticalAlignment = System.Windows.VerticalAlignment.Top,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Orientation=System.Windows.Controls.Orientation.Horizontal,
                    ScaleFactor=0.8,
                    Rows=1,
                    Columns=8
                }
            }
        }
        };
        public static void loadProfiles(Credentials credentials)
        {
            var name = credentials.name;
            profiles = new[] {
            new Profile {
                ownerName = name,
                logicalName = string.Format("{0} as an observer",name)
            },
            new Profile {
                ownerName = name,
                logicalName = string.Format("{0} with a keyboard",name),
                castBars = new[] {
                    new Bar(8,new[] {
                        new Macro("font_more_options"),
                        new Macro("font_size_increase"),
                        new Macro("font_size_decrease"),
                        new Macro("font_toggle_bold"),
                        new Macro("font_toggle_italic"),
                        new Macro("font_toggle_underline")
                    }) {
                        VerticalAlignment = System.Windows.VerticalAlignment.Top,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        Orientation=System.Windows.Controls.Orientation.Horizontal,
                        ScaleFactor=0.8,
                        Rows=1,
                        Columns=8
                    }
                }
            },
            new Profile
            {
                ownerName = name,
                logicalName = string.Format("{0} with a pen",name),
                castBars = new[] {
                    new Bar(8,new[] {
                        new Macro("pen_red"),
                        new Macro("pen_blue"),
                        new Macro("pen_black"),
                        new Macro("pen_yellow_highlighter"),
                        new Macro("select_mode"),
                        new Macro("wordcloud")
                    })
                {
                    VerticalAlignment = System.Windows.VerticalAlignment.Top,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Orientation=System.Windows.Controls.Orientation.Horizontal,
                    ScaleFactor=0.8,
                    Rows=1,
                    Columns=8
                },
                    new Bar(5)
                {
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                    Orientation=System.Windows.Controls.Orientation.Vertical,
                    ScaleFactor=0.8,
                    Rows=5,
                    Columns=1
                },
                    new Bar(5)
                {
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                    Orientation=System.Windows.Controls.Orientation.Vertical,
                    ScaleFactor=0.8,
                    Rows=5,
                    Columns=1
                }
                }
            }
        }.ToList();
        }
        public static Profile currentProfile = profiles[0];
        /*
        public  static WebSession authenticatedWebSession;
        public static string currentPage { get; set; }

        public static Slide slideObject() {
            return slides.Where(s => s.id == location.currentSlide).First();
        }
        */
        public static event CanvasClipboardFocusChangedHandler CanvasClipboardFocusChanged;
        public static string CurrentCanvasClipboardFocus
        {
            get
            {
                return currentCanvasClipboardFocus;
            }
            set
            {
                lock (lockCurrentCanvasClipboardFocus)
                {
                    currentCanvasClipboardFocus = value;
                    if (CanvasClipboardFocusChanged != null)
                    {
                        CanvasClipboardFocusChanged(currentCanvasClipboardFocus, EventArgs.Empty);
                    }
                }
            }
        }        
    }
}