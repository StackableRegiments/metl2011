﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Ink;
using System.IO;

namespace SandRibbon.Utils
{
    class CursorExtensions
    {
        public static Cursor ConvertToCursor(FrameworkElement fe, Point hotSpot)
        {
            int width = (int)fe.Width;
            int height = (int)fe.Height;
            fe.Measure(new Size(fe.Width, fe.Height));
            fe.Arrange(new Rect(0, 0, fe.Width, fe.Height));
            fe.UpdateLayout();
            if (width < 1) width = 1;
            if (height < 1) height = 1;

            var bitmapSource = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmapSource.Render(fe);
            
            var pixels = new int[width * height];
            try
            {
                bitmapSource.CopyPixels(pixels, width * 4, 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            var bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(pixels[y * width + x]));

            var stream = new System.IO.MemoryStream();

            var handle = bitmap.GetHicon();
            System.Drawing.Icon.FromHandle(handle).Save(stream);
            
            var streamBuff = stream.ToArray();
            var resultStream = new MemoryStream();

            System.Drawing.Icon.FromHandle(handle).Save(resultStream);

            var hsY = (byte)(int)(hotSpot.Y * height);
            var hsX = (byte)(int)(hotSpot.X * width);
            resultStream.Seek(2, SeekOrigin.Begin); 
            resultStream.Write(streamBuff, 2, 1);
            resultStream.Seek(8, SeekOrigin.Begin);
            resultStream.WriteByte(0);
            resultStream.Seek(10, SeekOrigin.Begin);
            resultStream.Seek(10, System.IO.SeekOrigin.Begin);
            resultStream.WriteByte(hsX);
            resultStream.Seek(12, System.IO.SeekOrigin.Begin);
            resultStream.WriteByte(hsY);
            resultStream.Seek(0, SeekOrigin.Begin);

            try
            {
                var cursor = new System.Windows.Input.Cursor(resultStream);
                return cursor;
            }
            catch (Exception) { }
            return Cursors.Cross;
        }
        public static Cursor generateCursorFromPen(DrawingAttributes pen)
        {
            var colour = new SolidColorBrush(pen.Color);
            var poly = new System.Windows.Shapes.Ellipse
            {
                Height = pen.Height,
                Width = pen.Width,
                Fill = colour,
                Stroke = colour
            };
            return CursorExtensions.ConvertToCursor(poly, new System.Windows.Point(0.5, 0.5));
        }

    }
}