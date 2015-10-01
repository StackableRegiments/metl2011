﻿using Itschwabing.Libraries.ResourceChangeEvent;
using MeTLLib.DataTypes;
using SandRibbon.Providers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Threading;

namespace SandRibbon.Pages.Collaboration.Palettes
{
    public partial class CommandBarConfigurationPage : Page
    {
        public CommandBarConfigurationPage()
        {
            InitializeComponent();
             var rangeProperties = new[] {
                new SlotConfigurer("How wide are your buttons?","ButtonWidth"),
                new SlotConfigurer("How tall are your buttons?","ButtonHeight"),
                new SlotConfigurer("How wide are your graphs?", "SensorWidth")
            };            
            sliders.ItemsSource = rangeProperties;            
            
            Bars.ItemsSource = Globals.currentProfile.castBars;
            ToolSets.ItemsSource = new[] {
                new MacroGroup {
                    Label="Freehand inking",
                    Row=1,
                    Macros=new[] {
                        new Macro("pen_red"),
                        new Macro("pen_blue"),
                        new Macro("pen_black"),
                        new Macro("pen_green")
                    }
                },
                new MacroGroup {
                    Label="Highlighters",
                    Row=0,
                    Macros=new[] {
                        new Macro("pen_yellow_highlighter"),                        
                        new Macro("pen_orange_highlighter")
                    }
                },
                new MacroGroup {
                    Label="Immediate teaching feedback",
                    Row=2,
                    Macros=new[] {
                        new Macro("worm")                        
                    }
                },
                new MacroGroup {
                    Label="Social controls",
                    Row=3,
                    Macros=new[] {
                        new Macro("participants_toggle")
                    }
                }
            };            
            SimulateFeedback();            
        }

        private void SimulateFeedback() {
            var t = new DispatcherTimer();
            t.Interval = new System.TimeSpan(0, 0, 5);
            t.Tick += delegate {
                Commands.ReceiveStrokes.Execute(Enumerable.Range(0,new Random().Next(50)).Select(i => new TargettedStroke(
                    0,"","",Privacy.NotSet,"",0,null,0.0
                    )).ToList());
            };
            t.Start();
        }

        private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {//Package the macro you picked up
            var thumb = sender as Thumb;
            var macro = thumb.DataContext as Macro;
            var dataObject = new DataObject();
            dataObject.SetData("Macro", macro.ResourceKey);
            DragDrop.DoDragDrop(thumb, dataObject, DragDropEffects.Copy);
        }

        private void ContentControl_Drop(object sender, DragEventArgs e)
        {//Land the macro on the right slot
            var resourceKey = e.Data.GetData("Macro") as string;
            var slot = sender as ContentControl;
            var macro = slot.DataContext as Macro;
            macro.ResourceKey = resourceKey;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var element = sender as FrameworkElement;
            var context = element.DataContext as SlotConfigurer;
            Application.Current.Resources[context.Property] = e.NewValue;
        }

        private void ButtonWidthChanged(object sender, Itschwabing.Libraries.ResourceChangeEvent.ResourceChangeEventArgs e)
        {
            var behaviour = sender as ResourceChangeEventBehavior;
            var element = behaviour.GetAssociatedObject();
            if (element == null) return;
            var context = element.DataContext as Bar;
            if (context.Orientation == Orientation.Vertical) {
                var width = (Double) e.NewValue;
                element.Width = width;
            }
        }

        private void ButtonHeightChanged(object sender, Itschwabing.Libraries.ResourceChangeEvent.ResourceChangeEventArgs e)
        {
            var behaviour = sender as ResourceChangeEventBehavior;
            var element = behaviour.GetAssociatedObject();
            if (element == null) return;
            var context = element.DataContext as Bar;
            if (context.Orientation == Orientation.Horizontal)
            {
                var height = (Double)e.NewValue;
                element.Height = height;
            }
        }

        private void SetGridRows(object sender, RoutedEventArgs e)
        {
            var grid = sender as Grid;
            var itemsSource = ToolSets.ItemsSource;
            foreach (var element in itemsSource) {
                grid.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            }
        }
    }
    public class MacroGroup {
        public string Label { get; set; }
        public int Row { get; set; }
        public IEnumerable<Macro> Macros { get; set; }
    }
    public class Macro : DependencyObject
    {        
        public string ResourceKey
        {
            get { return (string)GetValue(ResourceKeyProperty); }
            set { SetValue(ResourceKeyProperty, value); }
        }

        public static readonly DependencyProperty ResourceKeyProperty =
            DependencyProperty.Register("ResourceKey", typeof(string), typeof(Macro), new PropertyMetadata("slot"));


        public Macro(string resourceKey) {
            ResourceKey = resourceKey;
        }
    }
    public class Bar
    {              
        public Orientation Orientation { get; set; }
        public HorizontalAlignment HorizontalAlignment { get; set; }
        public VerticalAlignment VerticalAlignment { get; set; }
        public ObservableCollection<Macro> Macros { get; set; }
        public double ScaleFactor { get; set; }
        public int Rows { get; internal set; }
        public int Columns { get; internal set; }

        public Bar(int size)
        {
            Macros = new ObservableCollection<Macro>(Enumerable.Range(0, size).Select(i => new Macro("slot")));            
        }
    }
    public class SlotConfigurer
    {
        public string DisplayLabel { get; set; }
        public string Property { get; set; }
        public SlotConfigurer(string label, string property)
        {
            this.DisplayLabel = label;
            this.Property = property;
        }
    }

    class FactorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {            
            var containerMeasure = (Double)values[0];
            var dataContext = values[1] as Bar;
            var orientation = (Orientation) Enum.Parse(typeof(Orientation),(string) parameter);
            if (dataContext.Orientation == orientation)
            {
                return containerMeasure * dataContext.ScaleFactor;
            }
            switch (dataContext.Orientation) {
                case Orientation.Horizontal: return App.Current.TryFindResource("ButtonHeight");
                case Orientation.Vertical: return (Double)App.Current.TryFindResource("ButtonWidth") + (Double)App.Current.TryFindResource("SensorWidth");
                default:return DependencyProperty.UnsetValue;
            }
        }
        
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    class MacroAppearanceConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            var resourceKey = value as string;
            var functionalControl = Application.Current.FindResource(resourceKey) as FrameworkElement;
            foreach (var c in LogicalTreeHelper.GetChildren(functionalControl)) {                
                if (c is Appearance)
                {
                    var a = c as Appearance;
                    return a.Clone() ;
                }
            }
            return null;         
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
    class DynamicResourceConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            var resourceKey = value as string;
            return Application.Current.FindResource(resourceKey);
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}