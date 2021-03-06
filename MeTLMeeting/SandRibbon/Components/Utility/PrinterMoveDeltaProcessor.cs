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
        public PrinterMoveDeltaProcessor(InkCanvas canvas, string target, ContentBuffer contentBuffer) : base(canvas, target, contentBuffer)
        {
        }

        protected override void AddStroke(PrivateAwareStroke stroke)
        {
            Canvas.Strokes.Add(stroke);
        }

        protected override void RemoveStroke(PrivateAwareStroke stroke)
        {
            Canvas.Strokes.Remove(stroke);
        }

        protected override void RemoveImage(MeTLImage image)
        {
            Canvas.Children.Remove(image);
        }

        protected override void RemoveText(TextBox text)
        {
            Canvas.Children.Remove(text);
        }

        protected override void ChangeImagePrivacy(MeTLImage image, Privacy newPrivacy)
        {
            image.ApplyPrivacyStyling(Target, newPrivacy);
        }

        protected override void ChangeTextPrivacy(TextBox text, Privacy newPrivacy)
        {
            text.ApplyPrivacyStyling(Target, newPrivacy);
        }
    }
}
