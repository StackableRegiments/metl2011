﻿using System;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using SandRibbon.Providers;
using MeTLLib;
using Divan;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Windows.Forms;
using System.Threading;

namespace SandRibbon.Utils
{
    class LogMessage : CouchDocument{
        public string version;
        public string content;
        public long timestamp;
        public string user;
        public string server;
        public int slide;
        public override void WriteJson(JsonWriter writer)
        {
            base.WriteJson(writer);
            writer.WritePropertyName("version");
            writer.WriteValue(ConfigurationProvider.instance.getMetlVersion());
            writer.WritePropertyName("docType");
            writer.WriteValue("log");
            writer.WritePropertyName("content");
            writer.WriteValue(content);
            writer.WritePropertyName("timestamp");
            writer.WriteValue(timestamp);
            writer.WritePropertyName("user");
            writer.WriteValue(user);
            writer.WritePropertyName("server");
            writer.WriteValue(server);
            writer.WritePropertyName("slide");
            writer.WriteValue(slide);
        }
        public override void ReadJson(JObject obj)
        {
            base.ReadJson(obj);
            content = obj["message"].Value<string>();
            version = obj["version"].Value<string>();
            timestamp = obj["timestamp"].Value<long>();
            user = obj["user"].Value<string>();
            server = obj["server"].Value<string>();
            slide = obj["slide"].Value<int>();
        }
    }
    public class Logger
    {
        public static string log = "MeTL Log\r\n";
        public static readonly string POST_LOG = "http://madam.adm.monash.edu.au:5984/metl_log";
        private static CouchServer server = new CouchServer("madam.adm.monash.edu.au", 5984);
        private static readonly string DB_NAME = "metl_log";
        private static readonly ICouchDatabase db = server.GetDatabase(DB_NAME);
        public static void Crash(Exception e) {
            var crashMessage = string.Format("CRASH: {0} @ {1} INNER: {2}", 
                e.Message, 
                e.StackTrace, 
                e.InnerException == null? "NONE":e.InnerException.StackTrace);            
            Log(crashMessage);
        }
        public static void Fixed(string message) {
            try
            {
                Log(string.Format("CRASH: (fixed): {0} {1}", Globals.me, message));
            }
            catch (NotSetException e) { 
                Log(string.Format("CRASH: (fixed): {0} {1}", "USERNAME_NOT_SET", message));
            }
        }
        public static void Log(string appendThis)
        {/*Interesting quirk about the formatting: \n is the windows line ending but ruby assumes
          *nix endings, which are \r.  Safest to use both, I guess.*/
            var now = SandRibbonObjects.DateTimeFactory.Now();
            
            putCouch(appendThis, now);
        }
        private static void putCouch(string message, DateTime now) {
            if (String.IsNullOrEmpty(Globals.me)) return;
            if(String.IsNullOrEmpty(message)) return;
            if (message.Contains(POST_LOG)) return;
            if (new[] {
                "MeTL Presenter.exe ", 
                "MeTL Presenter.vshost.exe ", 
                "Failed to add item to relogin-queue.", 
                "MeTL Presenter.exe Warning: 0 :", 
                "MeTL Presenter.exe Info: 0 :", 
                "MeTL Presenter.exe Information: 0 :", 
                "Error loading thumbnail:"}.Any(prefix => message.StartsWith(prefix))) return;
            if (db != null)
                ThreadPool.QueueUserWorkItem(delegate
                {
                    try
                    {
                        string collaborationLevel;
                        string versionNumber;
                        try
                        {
                            collaborationLevel = Globals.conversationDetails.Permissions.studentCanPublish ? "Enabled" : "Disabled";
                        }
                        catch (Exception e)
                        {
                            collaborationLevel = "None";
                        }
                        try
                        {
                            versionNumber = ConfigurationProvider.instance.getMetlVersion();
                        }
                        catch (Exception e)
                        {
                            versionNumber = "Unknown";
                        }
                        var finalMessage = string.Format("{2} VERSION:{0}_CONVERSATIONCOLLABORATION:{1}", versionNumber, collaborationLevel, message);
                        var msg = new LogMessage
                        {
                            content = finalMessage,
                            timestamp = now.Ticks,
                            user = Globals.me,
                            slide = Globals.location.currentSlide,
                            server = ClientFactory.Connection().server.host
                        };
                        db.SaveArbitraryDocument<LogMessage>(msg);
                    }
                    catch (Exception e)
                    {
                        //what should we do if we cannot save to couch?
                        //ALL IS LOST
                    }
                });
        }
    }
}
