﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using SandRibbonObjects;
using MeTLLib.DataTypes;
using System.Windows.Ink;
using SandRibbon.Components.Sandpit;
using SandRibbon.Components.Pedagogicometry;

namespace SandRibbon.Providers
{
    public class Globals
    {
        public const string METLCOLLABORATOR = "MeTL Collaborator";
        public const string METLDEMONSTRATOR = "MeTL Demonstrator";
        public const string METL = "MeTL";
        public const string METLPRESENTER = "MeTL Presenter";
        public static bool isAuthor
        {
            get
            {
                return me == conversationDetails.Author;
            }
        }
        public static string MeTLType
        {
            get
            {
                try
                {
                    return Commands.MeTLType.lastValue().ToString();
                }
                catch (Exception)
                {
                    return METLCOLLABORATOR;
                }
            }

        }
        public static PedagogyLevel pedagogy
        {
            get
            {
                try
                {
                    Commands.SetPedagogyLevel.lastValue();
                }
                catch (NotSetException)
                {
                    Pedagogicometer.SetDefaultPedagogyLevel();
                }
                return (PedagogyLevel)Commands.SetPedagogyLevel.lastValue();
            }
        }
        public static Utils.Connection.JabberWire.Location location
        {
            get
            {
                try
                {
                    var conversationDetails = Globals.conversationDetails;
                    return new SandRibbon.Utils.Connection.JabberWire.Location
                    {
                        activeConversation = conversationDetails.Jid,
                        currentSlide = slide,
                        availableSlides = conversationDetails.Slides.Select(s => s.id).ToList()
                    };
                }
                catch (NotSetException e)
                {
                    throw e;
                }
            }
        }
        public static DrawingAttributes drawingAttributes
        {
            get
            {
                try { return ((DrawingAttributes)Commands.ReportDrawingAttributes.lastValue()); }
                catch (NotSetException)
                {
                }
                return new DrawingAttributes();
            }
        }
        public static List<Slide> slides
        {
            get
            {
                return ((ConversationDetails)Commands.UpdateConversationDetails.lastValue()).Slides;
            }
        }
        public static ConversationDetails conversationDetails
        {
            get
            {
                return (ConversationDetails)Commands.UpdateConversationDetails.lastValue();
            }
        }
        public static MeTLLib.DataTypes.Credentials credentials
        {
            get
            {
                var credentials = Commands.ConnectWithAuthenticatedCredentials.lastValue();
                return (MeTLLib.DataTypes.Credentials)credentials;
            }
        }
        public static List<MeTLLib.DataTypes.AuthorizedGroup> authorizedGroups
        {
            get
            {
                return credentials.authorizedGroups;
            }
        }
        public static bool synched
        {
            get
            {
                try
                {
                    return (bool)Commands.SetSync.lastValue();
                }
                catch (NotSetException)
                {
                    return true;
                }
            }
        }
        public static int teacherSlide
        {
            get
            {
                return (int)Commands.SyncedMoveRequested.lastValue();
            }
        }
        public static int slide
        {
            get
            {
                return (int)Commands.MoveTo.lastValue();
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
                return (string)Commands.SetPrivacy.lastValue();
            }
        }
    }
}