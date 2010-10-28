﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Threading;
using System.Windows.Ink;
using System.Windows.Input;
using MeTLLib;
using System.Diagnostics;
using MeTLLib.Providers.Connection;
using System.Windows;
using System.Windows.Threading;
using System.Net;
using System.ServiceProcess;
using System.Net.Mime;

namespace ThumbService
{
    struct RequestInfo {//Structs hash against their first field
        public int slide;
        public int width;
        public int height;
        public string server;
    }
    public class ThumbService : ServiceBase
    {
        private Dictionary<RequestInfo, byte[]> cache = new Dictionary<RequestInfo, byte[]>();
        private ClientConnection client = ClientFactory.Connection(MeTLServerAddress.serverMode.STAGING);
        private ReaderWriterLockSlim locker = new ReaderWriterLockSlim();
        private HttpListener listener;
        public static void Main(string[] _args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
            ServiceBase.Run(new ServiceBase[]{new ThumbService()});
        }
        protected override void OnStart(string[] _args){
            client.events.StatusChanged += (sender, args) => Trace.TraceInformation("Status changed: {0}", args.isConnected);
            client.Connect("eecrole", "m0nash2008");
            listener = new HttpListener();
            listener.Prefixes.Add("http://*:8080/");
            listener.Start();
            listener.BeginGetContext(Route, listener);
        }
        public void Route(IAsyncResult result) {
            HttpListenerContext context = listener.EndGetContext(result);
            try
            {
                if (q(context, "invalidate") == "true")
                    Forget(context);//Takes write lock
                Thumb(context);//May take write lock
            }
            catch (Exception e)
            {
                try
                {
                    context.Response.StatusCode = 401;
                    var error = Encoding.UTF8.GetBytes(string.Format("Error: {0} {1}", e.Message, e.StackTrace));
                    context.Response.OutputStream.Write(error, 0, error.Count());
                }
                catch (HttpListenerException) { 
                    /*At this point the client has probably closed the connection.  We're more interested in protecting ourselves than him.
                     No further response*/
                }
            }
            finally {
                if (locker.IsReadLockHeld)
                    locker.ExitReadLock();
                if (locker.IsWriteLockHeld)
                    locker.ExitWriteLock();
                try {
                    context.Response.OutputStream.Close();
                }
                catch (Exception){//See above justification of ignoring this exception
                }
                listener.BeginGetContext(Route, listener);
            }
        }
        private string q(HttpListenerContext context, string key){
            return context.Request.QueryString[key];
        }
        public void Forget(HttpListenerContext context)
        {
            locker.EnterWriteLock();
            int slide = Int32.Parse(q(context, "slide"));
            var memoKeys = cache.Keys.Where(k => k.slide == slide).ToList();
            foreach (var key in memoKeys)
                cache.Remove(key);
        }
        public void Thumb(HttpListenerContext context){
            var requestInfo = new RequestInfo
            {
                slide = Int32.Parse(q(context, "slide")),
                width = Int32.Parse(q(context, "width")),
                height = Int32.Parse(q(context, "height")),
                server = q(context, "server")
            };
            byte[] image;
            if (cache.ContainsKey(requestInfo))
            {
                locker.EnterReadLock();
                image = cache[requestInfo];
            }
            else
            {
                if(!locker.IsWriteLockHeld)
                    locker.EnterWriteLock();
                image = createImage(requestInfo);
                cache[requestInfo] = image;
            }
            context.Response.ContentType = "image/png";
            context.Response.ContentLength64 = image.Count();
            context.Response.OutputStream.Write(image, 0, image.Count());
            context.Response.OutputStream.Close();
        }
        private byte[] parserToInkCanvas(PreParser parser, RequestInfo info) { 
            ManualResetEvent waitHandler = new ManualResetEvent(false);
            byte[] result = new byte[0];
            var staThread = new Thread(new ParameterizedThreadStart(delegate
            {
                try
                {
                    var canvas = new InkCanvas();
                    parser.Populate(canvas);
                    var viewBox = new Viewbox();
                    viewBox.Stretch = Stretch.Uniform;
                    viewBox.Child = canvas;
                    viewBox.Width = info.width;
                    viewBox.Height = info.height;
                    var size = new Size(info.width, info.height);
                    viewBox.Measure(size);
                    viewBox.Arrange(new Rect(size));
                    viewBox.UpdateLayout();
                    RenderTargetBitmap targetBitmap =
                       new RenderTargetBitmap(info.width, info.height, 96d, 96d, PixelFormats.Pbgra32);
                    targetBitmap.Render(viewBox);
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(targetBitmap));
                    using (var stream = new MemoryStream())
                    {
                        encoder.Save(stream);
                        result = stream.ToArray();
                    }
                }
                catch (Exception e) {
                }
                finally { 
                    waitHandler.Set();
                }
            }));
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            waitHandler.WaitOne();
            return result;
        }
        private byte[] createImage(RequestInfo info){
            Trace.TraceInformation(info.ToString());
            ManualResetEvent waitHandler = new ManualResetEvent(false);
            byte[] result = new byte[0];
            var synchrony = new Thread(new ThreadStart(delegate{
                client.getHistoryProvider().Retrieve<PreParser>(
                    null, null,
                    parser =>
                    {
                        result = parserToInkCanvas(parser, info);
                        waitHandler.Set();
                    }
                    , info.slide.ToString());
            }));
            synchrony.Start();
            waitHandler.WaitOne();
            cache[info] = result;
            return result;
        }
    }
}