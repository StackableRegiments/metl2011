﻿using System;
using System.Collections.Generic;
using System.Linq;
using MeTLLib.DataTypes;
using SandRibbon.Components.Pedagogicometry;
using SandRibbon.Components.Canvas;
using System.Drawing;

namespace SandRibbon.Providers
{
    public class Globals
    {
        public const string METLCOLLABORATOR = "MeTL Collaborator";
        public const string METLDEMONSTRATOR = "MeTL Demonstrator";
        public const string METL = "MeTL";
        public const string METLPRESENTER = "MeTL Presenter";

        private static Size canvasSize = new Size();
        private static QuizData quizData = new QuizData();
        public static bool isAuthor
        {
            get
            {
                if(me == null || conversationDetails.ValueEquals(ConversationDetails.Empty)) return false;
                return me == conversationDetails.Author;
            }
        }
        public static UserOptions UserOptions {
            get {
                try
                {
                    return (UserOptions)SandRibbon.Commands.SetUserOptions.LastValue();
                }
                catch (NotSetException)
                {
                    return UserOptions.DEFAULT;
                }
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
                    return METL;
                }
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
        public static ConversationDetails conversationDetails
        {
            get
            {
                return (ConversationDetails)Commands.UpdateConversationDetails.LastValue();
            }
        }
        public static MeTLLib.DataTypes.QuizData quiz
        {
            get
            {
                return quizData;                
            }
        }
        public static TextInformation currentTextInfo
        {
            get;
            set;
        }
        public static MeTLLib.DataTypes.Credentials credentials
        {
            get
            {
                return (MeTLLib.DataTypes.Credentials) Commands.SetIdentity.LastValue();
            }
        }
        public static List<MeTLLib.DataTypes.AuthorizedGroup> authorizedGroups
        {
            get
            {
                return credentials.authorizedGroups;
            }
        }
        public static List<string> authorizedGroupNames {
            get 
            {
                return authorizedGroups.Select(g => g.groupKey).ToList();
            }
        }
        public static bool synched
        {
            get
            {
                return (bool)Commands.SetSync.LastValue();
            }
        }
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
                return (int)Commands.MoveTo.LastValue();
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
                return (string)Commands.SetPrivacy.LastValue();
            }
        }
        public static Policy policy
        {
            get { return new Policy(isAuthor, false); }
        }
        public static UserInformation userInformation
        {
            get { return new UserInformation(credentials, location, policy); }
        }
        public static bool rememberMe {
            get
            {
                return (bool)Commands.RememberMe.LastValue();
            }
        }
    }
}