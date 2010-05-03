﻿#pragma checksum "..\..\..\..\Components\SimpleImpl\SimpleConversationCreator.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "7DEEC9F8C5D57DCD6800823BE8F1FEAD"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.4927
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using SandRibbon;
using SandRibbon.Components;
using SandRibbon.Components.SimpleImpl;
using SandRibbonInterop;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace SandRibbon.Components.SimpleImpl {
    
    
    /// <summary>
    /// SimpleConversationCreator
    /// </summary>
    public partial class SimpleConversationCreator : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 8 "..\..\..\..\Components\SimpleImpl\SimpleConversationCreator.xaml"
        internal SandRibbon.Components.SimpleImpl.SimpleConversationCreator parent;
        
        #line default
        #line hidden
        
        
        #line 24 "..\..\..\..\Components\SimpleImpl\SimpleConversationCreator.xaml"
        internal System.Windows.Controls.TextBox conversationName;
        
        #line default
        #line hidden
        
        
        #line 26 "..\..\..\..\Components\SimpleImpl\SimpleConversationCreator.xaml"
        internal System.Windows.Controls.TextBox conversationTag;
        
        #line default
        #line hidden
        
        
        #line 28 "..\..\..\..\Components\SimpleImpl\SimpleConversationCreator.xaml"
        internal System.Windows.Controls.ComboBox subjectList;
        
        #line default
        #line hidden
        
        
        #line 33 "..\..\..\..\Components\SimpleImpl\SimpleConversationCreator.xaml"
        internal SandRibbonInterop.NonRibbonButton CreateLectureButton;
        
        #line default
        #line hidden
        
        
        #line 48 "..\..\..\..\Components\SimpleImpl\SimpleConversationCreator.xaml"
        internal SandRibbonInterop.NonRibbonButton CreateTutorialButton;
        
        #line default
        #line hidden
        
        
        #line 64 "..\..\..\..\Components\SimpleImpl\SimpleConversationCreator.xaml"
        internal SandRibbonInterop.NonRibbonButton CreateMeetingButton;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/MeTL;component/components/simpleimpl/simpleconversationcreator.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\Components\SimpleImpl\SimpleConversationCreator.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.parent = ((SandRibbon.Components.SimpleImpl.SimpleConversationCreator)(target));
            return;
            case 2:
            this.conversationName = ((System.Windows.Controls.TextBox)(target));
            
            #line 24 "..\..\..\..\Components\SimpleImpl\SimpleConversationCreator.xaml"
            this.conversationName.TextChanged += new System.Windows.Controls.TextChangedEventHandler(this.checkCanSubmit);
            
            #line default
            #line hidden
            return;
            case 3:
            this.conversationTag = ((System.Windows.Controls.TextBox)(target));
            return;
            case 4:
            this.subjectList = ((System.Windows.Controls.ComboBox)(target));
            return;
            case 5:
            this.CreateLectureButton = ((SandRibbonInterop.NonRibbonButton)(target));
            return;
            case 6:
            this.CreateTutorialButton = ((SandRibbonInterop.NonRibbonButton)(target));
            return;
            case 7:
            this.CreateMeetingButton = ((SandRibbonInterop.NonRibbonButton)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}
