﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using agsXMPP.Xml;
using SandRibbon.Utils;
using SandRibbon.Utils.Connection;
using Ionic.Zip;
using SandRibbonInterop.MeTLStanzas;
using agsXMPP.Xml.Dom;
using MeTLLib.DataTypes;
using MeTLLib.Providers.Connection;

namespace SandRibbon.Providers
{
    public class HistoryProviderFactory
    {
        private static object instanceLock = new object();
        private static IHistoryProvider instance;
        public static IHistoryProvider provider
        {
            get
            {
                lock (instanceLock)
                    if (instance == null)
                        instance = new CachedHistoryProvider();
                return instance;
            }
        }
    }
    public interface IHistoryProvider
    {
        void Retrieve<T>(
            Action retrievalBeginning,
            Action<int, int> retrievalProceeding,
            Action<T> retrievalComplete,
            string room
        ) where T : MeTLLib.Providers.Connection.PreParser;
        void RetrievePrivateContent<T>(
            Action retrievalBeginning,
            Action<int, int> retrievalProceeding,
            Action<T> retrievalComplete,
            string author,
            string room
        ) where T : MeTLLib.Providers.Connection.PreParser;
    }
    public abstract class BaseHistoryProvider : IHistoryProvider {
        public abstract void Retrieve<T>(
            Action retrievalBeginning,
            Action<int, int> retrievalProceeding,
            Action<T> retrievalComplete,
            string room
        ) where T : MeTLLib.Providers.Connection.PreParser;
        public void RetrievePrivateContent<T>(
            Action retrievalBeginning,
            Action<int, int> retrievalProceeding,
            Action<T> retrievalComplete,
            string author,
            string room
        ) where T : MeTLLib.Providers.Connection.PreParser{
            this.Retrieve(retrievalBeginning,retrievalProceeding,retrievalComplete,string.Format("{1}{0}", author,room));
            this.Retrieve(retrievalBeginning,retrievalProceeding,retrievalComplete,string.Format("{0}/{1}", author,room));
        }
    }
    public class CachedHistoryProvider : BaseHistoryProvider {
        private Dictionary<string, MeTLLib.Providers.Connection.PreParser> cache = new Dictionary<string, MeTLLib.Providers.Connection.PreParser>();
        private int measure<T>(int acc, T item){
            return acc + item.ToString().Length;
        }
        /*public static long cacheSize {
            get {
         //Warning: This does not calculate the size you would expect.  It's been left in here mostly as a breakpoint - we're
         //not storing XML at this point, but in memory structures
                return ((CachedHistoryProvider)HistoryProviderFactory.provider).cacheTotalSize;
            }
        }*/
        /*private long cacheTotalSize{
            get
            {
                return cache.Values.Aggregate(0, (acc, parser) => 
                    acc + 
                        parser.ink.Aggregate(0,measure<TargettedStroke>)+
                        parser.images.Values.Aggregate(0,measure<TargettedImage>)+
                        parser.text.Values.Aggregate(0,measure<TargettedTextBox>));
            }
        }*/
        public override void Retrieve<T>(Action retrievalBeginning, Action<int, int> retrievalProceeding, Action<T> retrievalComplete, string room)
        {
            /*if (!cache.ContainsKey(room) || isPrivateRoom(room))
            {
                
             */
            new HttpHistoryProvider().Retrieve<T>(
                    delegate { },
                    (_i, _j) => { },
                    history =>
                    {
                        /*if (cache.ContainsKey(room))
                            cache[room] = cache[room].merge<PreParser>(history);
                        else&*/
                            cache[room] = history;
                        //Commands.PreParserAvailable.ExecuteAsync(history);
                        retrievalComplete((T)cache[room]);
                    },
                    room);
            /*}
            else {
                retrievalComplete((T)cache[room]);
            }*/
        }

        private bool isPrivateRoom(string room)
        {
            var validChar = new[] {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'};
            if (!room.All(s=>(validChar.Contains(s)))) return true;
            return false;
            
        }

        public void HandleMessage(string to, Element message) {
            if(isPrivateRoom(to)) return;
            var room = Int32.Parse(to);
            if (!cache.ContainsKey(room.ToString()))
                cache[room.ToString()] = new MeTLLib.Providers.Connection.PreParser(null,room,null,null,null,null,null,null);
            cache[room.ToString()].ActOnUntypedMessage(message);
        }
    }
    public class HttpHistoryProvider : BaseHistoryProvider
    {
        public override void Retrieve<T>(Action retrievalBeginning, Action<int,int> retrievalProceeding, Action<T> retrievalComplete, string room)
        {
            Logger.Log(string.Format("HttpHistoryProvider.Retrieve: Beginning retrieve for {0}", room));
            var accumulatingParser = (T)Activator.CreateInstance(typeof(T), MeTLLib.Providers.Connection.PreParser.ParentRoom(room));
            if(retrievalBeginning != null)
                Application.Current.Dispatcher.adoptAsync(retrievalBeginning);
            var worker = new BackgroundWorker();
            worker.DoWork += (_sender, _args) =>
                                 {
                                     var zipUri = string.Format("https://{0}:1749/{1}/all.zip", Constants.JabberWire.SERVER, room);
                                     try
                                     {
                                         var zipData = HttpResourceProvider.secureGetData(zipUri);
                                         if (zipData.Count() == 0) return;
                                         var zip = ZipFile.Read(zipData);
                                         var days = (from e in zip.Entries where e.FileName.EndsWith(".xml") orderby e.FileName select e).ToArray();
                                         for (int i = 0; i < days.Count(); i++)
                                         {
                                             using(var stream = new MemoryStream())
                                             {
                                                 days[i].Extract(stream);
                                                 var historicalDay = Encoding.UTF8.GetString(stream.ToArray());
                                                 parseHistoryItem(historicalDay, accumulatingParser);
                                             }
                                             if (retrievalProceeding != null)
                                                 Application.Current.Dispatcher.BeginInvoke(retrievalProceeding, i, days.Count());
                                         }
                                     }
                                     catch (WebException e)
                                     {
                                         MessageBox.Show("WE: " + e.Message);
                                         //Nothing to do if it's a 404.  There is no history to obtain.
                                     }
                                 };
            if (retrievalComplete != null)
                worker.RunWorkerCompleted += (_sender, _args) =>
                {
                    Logger.Log(string.Format("{0} retrieval complete at historyProvider", room));
                    try
                    {
                        Application.Current.Dispatcher.Invoke(retrievalComplete, (T)accumulatingParser);
                    }
                    catch (Exception ex) {
                    //    Logger.Log("Exception on the retrievalComplete section: "+ex.Message.ToString()); 
                    }
                    };
            worker.RunWorkerAsync(null);
        }
        protected virtual void parseHistoryItem(string item, MeTLLib.Providers.Connection.JabberWire wire)
        {//This takes all the time
            Application.Current.Dispatcher.adoptAsync((Action)delegate
            {//Creating and event setting on the dispatcher thread?  Might be expensive, might not.  Needs bench.
                var history = item + "</logCollection>";
                var parser = new StreamParser();
                parser.OnStreamElement += ((_sender, node) =>
                                               {
                                                   wire.ReceivedMessage(node);
                                               });
                parser.Push(Encoding.UTF8.GetBytes(history), 0, history.Length);
            });
        }
    }
}
