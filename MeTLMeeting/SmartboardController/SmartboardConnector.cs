﻿using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Input;
using SBSDKComWrapperLib;
using MessageBox = System.Windows.MessageBox;
using System.Diagnostics;

namespace SmartboardController
{
    class SmartboardConnector
    {
        private ISBSDKBaseClass2 Sbsdk;
        private _ISBSDKBaseClass2Events_Event SbsdkEvents;

        [DllImport("user32.dll")]
        private static extern int RegisterWindowMessageA([MarshalAs(UnmanagedType.LPStr)] string lpString);
        private int SBSDKMessageID = RegisterWindowMessageA("SBSDK_NEW_MESSAGE");
        public bool isConnected = false;
        private IntPtr mainMeTLWindowPtr;
        private HwndSource mainMeTLWindowSrc;
        private DependencyObject dpObj;
        public SmartboardConnector(DependencyObject windowsObject)
        {
            dpObj = windowsObject;
        }
        private void SMARTboardConsole(string message)
        {
            Trace.TraceInformation("SmartboardConnector Message at (" + DateTime.Now + "): " + message);
        }
        public void connectToSmartboard(object _unused)
        {
            try
            {
                Main_Loaded();
                Sbsdk = new SBSDKBaseClass2();
            }
            catch (Exception e)
            {
                Trace.TraceInformation("SmartboardConnector::connectToSmartboard Exception: " + e.Message);
                return;
            }
            if (Sbsdk != null)
            {
                SbsdkEvents = (_ISBSDKBaseClass2Events_Event)Sbsdk;
            }
            if (SbsdkEvents != null)
            {
                SbsdkEvents.OnEraser += new SBSDKComWrapperLib._ISBSDKBaseClass2Events_OnEraserEventHandler(this.OnEraser);
                SbsdkEvents.OnNoTool += new SBSDKComWrapperLib._ISBSDKBaseClass2Events_OnNoToolEventHandler(this.OnNoTool);
                SbsdkEvents.OnPen += new SBSDKComWrapperLib._ISBSDKBaseClass2Events_OnPenEventHandler(this.OnPen);
                SbsdkEvents.OnBoardStatusChange += new SBSDKComWrapperLib._ISBSDKBaseClass2Events_OnBoardStatusChangeEventHandler(this.OnBoardStatusChange);
            }
            if (Sbsdk != null)
            {
                Sbsdk.SBSDKAttachWithMsgWnd(mainMeTLWindowPtr.ToInt32(), false, mainMeTLWindowPtr.ToInt32());
                Sbsdk.SBSDKSetSendMouseEvents(mainMeTLWindowPtr.ToInt32(), _SBCSDK_MOUSE_EVENT_FLAG.SBCME_ALWAYS, -1);
            }
            isConnected = true;
            SMARTboardConsole("Connected to SMARTboard");
        }
        public void disconnectFromSmartboard(object _unused)
        {
            if (Sbsdk != null)
                Sbsdk.SBSDKDetach(mainMeTLWindowPtr.ToInt32());
            if (SbsdkEvents != null)
            {
                SbsdkEvents.OnEraser -= new SBSDKComWrapperLib._ISBSDKBaseClass2Events_OnEraserEventHandler(this.OnEraser);
                SbsdkEvents.OnNoTool -= new SBSDKComWrapperLib._ISBSDKBaseClass2Events_OnNoToolEventHandler(this.OnNoTool);
                SbsdkEvents.OnPen -= new SBSDKComWrapperLib._ISBSDKBaseClass2Events_OnPenEventHandler(this.OnPen);
                SbsdkEvents.OnBoardStatusChange -= new SBSDKComWrapperLib._ISBSDKBaseClass2Events_OnBoardStatusChangeEventHandler(this.OnBoardStatusChange);
            }
            Sbsdk = null;
            isConnected = false;
            SMARTboardConsole("Disconnected from SMARTboard");
        }
        private void Main_Loaded()
        {
            if (dpObj == null) return;
            mainMeTLWindowPtr = new WindowInteropHelper(Window.GetWindow(dpObj)).Handle;
            mainMeTLWindowSrc = HwndSource.FromHwnd(mainMeTLWindowPtr);
            if (mainMeTLWindowSrc != null)
                mainMeTLWindowSrc.AddHook(new HwndSourceHook(WndProc));
        }
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            OnNotifyMessage(msg);
            return IntPtr.Zero;
        }
        private void OnNotifyMessage(int m)
        {
            if (m.Equals(SBSDKMessageID) && Sbsdk != null)
            {
                Sbsdk.SBSDKProcessData();
            }
        }
        private void OnPen(int iPointerID)
        {
            var penRed = new int();
            var penGreen = new int();
            var penBlue = new int();
            int penAlpha = 255;
            Sbsdk.SBSDKGetToolColor(iPointerID, out penRed, out penGreen, out penBlue);
            Color SMARTboardPenColor = new Color();
            SMARTboardPenColor.R = Convert.ToByte(penRed);
            SMARTboardPenColor.G = Convert.ToByte(penGreen);
            SMARTboardPenColor.B = Convert.ToByte(penBlue);
            SMARTboardPenColor.A = Convert.ToByte(penAlpha);
            SMARTboardConsole("SMARTboard pen: R" + penRed + ", G" + penGreen + ", B" + penBlue + ", A" + penAlpha);
            var color = new Color { A = SMARTboardPenColor.A, R = SMARTboardPenColor.R, G = SMARTboardPenColor.G, B = SMARTboardPenColor.B };
            var drawingAttributes = new DrawingAttributes() { Color = color, IsHighlighter = false, Height = 3, Width = 3 };
            App.SetLayer("Sketch");
            App.SetDrawingAttributes(drawingAttributes);
            App.SetInkCanvasMode("Ink");
        }
        private void OnNoTool(int iPointerID)
        {
            SMARTboardConsole("SMARTboard No Active Tool");
        }
        private void OnEraser(int iPointerID)
        {
            App.SetLayer("Sketch");
            App.SetInkCanvasMode("EraseByStroke");
            SMARTboardConsole("SMARTboard Eraser");
        }
        private void OnBoardStatusChange()
        {
            SMARTboardConsole("SMARTboard boardStatusChange");
        }
    }
}