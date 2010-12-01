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

namespace SandRibbon.Utils
{
    class LogMessage : CouchDocument{
        public string content;
        public long timestamp;
        public string user;
        public string server;
        public int slide;
        public override void WriteJson(JsonWriter writer)
        {
            base.WriteJson(writer);
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
        public static void Log(string appendThis)
        {/*Interesting quirk about the formatting: \n is the windows line ending but ruby assumes
          *nix endings, which are \r.  Safest to use both, I guess.*/
            var now = SandRibbonObjects.DateTimeFactory.Now();
            putCouch(appendThis, now);
        }
        public static void Log(string format, params object[] args) {
            var s = string.Format(format, args);
            Log(s);
        }
        private static void putCouch(string message, DateTime now) {
            if (String.IsNullOrEmpty(Globals.me)) return;
            var msg = new LogMessage
            {
                content = message,
                timestamp = now.Ticks,
                user = Globals.me,
                slide = Globals.location.currentSlide,
                server = ClientFactory.Connection().server.host
            };
            try
            {

                db.SaveArbitraryDocument<LogMessage>(msg);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);    
            }
        }
        public static void query() {
            var time = DateTime.Now.Ticks - 1000;
            var tempView = db.NewTempView("e", "e", String.Format("if(doc.timestamp > {0})emit(null,doc)", time));
            var messages = tempView.LinqQuery<LogMessage>();
            var a = (from c in messages select c).ToList();
        }
    }
}
