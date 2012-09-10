﻿namespace SandRibbon.Components.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using MeTLLib.DataTypes;
    using SandRibbon.Providers;
    using System.Windows.Media;
    using System.Windows.Ink;
    using System.Windows.Controls;

    public class PrinterMoveDeltaProcessor : MoveDeltaProcessor
    {
        public PrinterMoveDeltaProcessor(InkCanvas canvas, string target) : base(canvas, target)
        {
        }

        protected override void AddStroke(Stroke stroke)
        {
            Canvas.Strokes.Add(stroke);
        }

        protected override void RemoveStroke(Stroke stroke)
        {
            Canvas.Strokes.Remove(stroke);
        }

        protected override void RemoveImage(Image image)
        {
            Canvas.Children.Remove(image);
        }

        protected override void RemoveText(TextBox text)
        {
            Canvas.Children.Remove(text);
        }

        protected override void ChangeImagePrivacy(Image image, Privacy newPrivacy)
        {
            image.ApplyPrivacyStyling(Target, newPrivacy);
        }

        protected override void ChangeTextPrivacy(TextBox text, Privacy newPrivacy)
        {
            text.ApplyPrivacyStyling(Target, newPrivacy);
        }
    }
}
