﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Divelements.SandRibbon;
using Microsoft.Practices.Composite.Presentation.Commands;
using SandRibbon.Components.Canvas;
using SandRibbon.Providers;
using SandRibbonInterop.Interfaces;

namespace SandRibbon.Tabs.Groups
{
    public partial class TextTools : UserControl, ITextTools
    {
        private List<double> fontSizes = new List<double> { 8.0, 10.0, 12.0, 14.0, 16.0, 18.0, 20.0, 24.0, 28.0, 32.0, 36.0, 40.0, 48.0, 56.0, 64.0, 72.0, 96.0, 128.0, 144.0, 196.0, 240.0 };
        private List<string> fontList = new List<string> { "Arial", "Times New Roman", "Lucida", "Palatino Linotype", "Verdana", "Wingdings" };

        public TextTools()
        {
            InitializeComponent();
            fontFamily.ItemsSource = fontList;
            fontSize.ItemsSource = fontSizes;
            fontSize.SelectedIndex = 0;
            Commands.SetLayer.RegisterCommandToDispatcher<string>(new DelegateCommand<string>(SetLayer));
            Commands.TextboxFocused.RegisterCommandToDispatcher(new DelegateCommand<TextInformation>(update));
            Commands.RestoreTextDefaults.RegisterCommand(new DelegateCommand<object>(restoreTextDefaults));
            Commands.MoveTo.RegisterCommandToDispatcher<object>(new DelegateCommand<object>(MoveTo));
            Commands.ToggleBold.RegisterCommand(new DelegateCommand<object>(togglebold ));
            Commands.ToggleItalic.RegisterCommand(new DelegateCommand<object>(toggleItalic));
            Commands.ToggleUnderline.RegisterCommand(new DelegateCommand<object>(toggleUnderline));
        }
        private void togglebold(object obj)
        {
            TextBoldButton.IsChecked = !TextBoldButton.IsChecked;
            sendValues();
        }
        private void toggleItalic(object obj)
        {
            TextItalicButton.IsChecked = !TextItalicButton.IsChecked;
            sendValues();
        }
        private void toggleUnderline(object obj)
        {
            TextUnderlineButton.IsChecked = !TextUnderlineButton.IsChecked;
            sendValues();
        }

        private void MoveTo(object obj)
        {
            fontSize.SelectedItem = generateDefaultFontSize();
        }

        private void restoreTextDefaults(object obj)
        {
            ColourPickerBorder.BorderBrush = Brushes.Black;
            fontSize.SelectedItem = 10;
            fontFamily.SelectedItem = "Arial";
        }

        private void update(TextInformation info)
        {
            fontSize.SelectedItem = info.size;
            fontFamily.SelectedItem = info.family.ToString();
            TextBoldButton.IsChecked = info.bold;
            TextItalicButton.IsChecked = info.italics;
            TextUnderlineButton.IsChecked = info.underline;
            TextStrikethroughButton.IsChecked = info.strikethrough;
            ColourPickerBorder.BorderBrush = new SolidColorBrush(info.color);
        }
        private void sendValues()
        {
            if (fontSize == null || fontFamily == null || fontFamily.SelectedItem == null || ColourPickerBorder == null || ColourPickerBorder.BorderBrush == null || TextBoldButton == null || TextItalicButton == null || TextUnderlineButton == null || TextStrikethroughButton == null) return;
            var info = new TextInformation
            {
                size = (double)fontSize.SelectedItem,
                family = new FontFamily(fontFamily.SelectedItem.ToString()),
                bold =  TextBoldButton.IsChecked == true,
                italics = TextItalicButton.IsChecked == true,
                underline = TextUnderlineButton.IsChecked == true,
                strikethrough = TextStrikethroughButton.IsChecked == true,
                color = ((SolidColorBrush) ColourPickerBorder.BorderBrush).Color
            };
            Commands.UpdateTextStyling.Execute(info);
        }

        private void SetLayer(string layer)
        {
            if (layer == "Text")
                Visibility = Visibility.Visible;
            else
                Visibility = Visibility.Collapsed;
        }
        private void setUpTools(object sender, RoutedEventArgs e)
        {
            fontFamily.SelectedItem = "Arial";
        }
        private const double defaultWidth = 720;
        private const double defaultFontSize = 24.0;
        private static double generateDefaultFontSize()
        {
            try
            {
                MeTLLib.DataTypes.Slide currentSlide;
                if (Globals.slides.Count > 0)
                    currentSlide = Globals.slides.Where(s => s.id == Globals.slide).FirstOrDefault();
                else currentSlide = new MeTLLib.DataTypes.Slide(0, "", MeTLLib.DataTypes.Slide.TYPE.SLIDE, 0, 720, 540);
                var multiply = (currentSlide.defaultWidth / defaultWidth) > 0
                                     ? (int)(currentSlide.defaultWidth / defaultWidth) : 1;
                return defaultFontSize * multiply;
            }
            catch (NotSetException e)
            {
                return defaultFontSize;
            }
        }
        private void decreaseFont(object sender, RoutedEventArgs e)
        {
            if (fontSize.ItemsSource == null) return;
            int currentItem = fontSize.SelectedIndex;
            if (currentItem - 1 >= 0)
            {
                fontSize.SelectedIndex = currentItem - 1;
                sendValues();
            }
        }
        private void increaseFont(object sender, RoutedEventArgs e)
        {
            if (fontSize.ItemsSource == null) return;
            int currentItem = fontSize.SelectedIndex;
            if (currentItem + 1 < fontSizes.Count())
            {
                fontSize.SelectedIndex = currentItem + 1;
                sendValues();
            }
        }
        private void fontSizeSelected(object sender, SelectionChangedEventArgs e)
        {
            if (fontSize.SelectedIndex == -1) return;
            if (e.AddedItems.Count == 0) return;
            sendValues();
        }
        private void fontFamilySelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            sendValues();
        }
        private void textColorSelected(object sender, ColorEventArgs e)
        {
            ColourPickerBorder.BorderBrush = new SolidColorBrush(e.Color);
            ((System.Windows.Controls.Primitives.Popup)ColourSelection.Parent).IsOpen = false;
            sendValues();
        }
        private void ShowColourSelector(object sender, RoutedEventArgs e)
        {
            ((System.Windows.Controls.Primitives.Popup)ColourSelection.Parent).IsOpen = true;
        }
        private void valuesUpdated(object sender, RoutedEventArgs e)
        {
            var clickedButton = (CheckBox)sender;
            if (clickedButton == TextStrikethroughButton)
                if (clickedButton.IsChecked == true)
                    TextUnderlineButton.IsChecked = false;
            if (clickedButton == TextUnderlineButton)
                if (clickedButton.IsChecked == true)
                    TextStrikethroughButton.IsChecked = false;
            sendValues();
        }

        private void restoreDefaults(object sender, RoutedEventArgs e)
        {
            fontSize.SelectedIndex = 0;
            fontFamily.SelectedIndex = 0;    
            TextBoldButton.IsChecked = false;
            TextItalicButton.IsChecked = false;
            TextUnderlineButton.IsChecked = false;
            TextStrikethroughButton.IsChecked = false;
            ColourPickerBorder.BorderBrush = new SolidColorBrush(Colors.Black);
            sendValues();
        }
    }
}
