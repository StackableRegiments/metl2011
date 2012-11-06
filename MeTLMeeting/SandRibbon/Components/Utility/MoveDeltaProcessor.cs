﻿namespace SandRibbon.Components.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Ink;
    using System.Windows.Media;
    using MeTLLib.DataTypes;

    public abstract class MoveDeltaProcessor
    {
        public InkCanvas Canvas { get; private set; }

        public string Target { get; private set; }

        protected MoveDeltaProcessor(InkCanvas canvas, string target)
        {
            this.Canvas = canvas;
            this.Target = target; 
        }

        protected abstract void AddStroke(Stroke stroke);

        protected abstract void RemoveStroke(Stroke stroke);
        protected abstract void RemoveImage(Image image);
        protected abstract void RemoveText(TextBox textbox);

        protected abstract void ChangeImagePrivacy(Image image, Privacy newPrivacy);
        protected abstract void ChangeTextPrivacy(TextBox textbox, Privacy newPrivacy);

        public void ReceiveMoveDelta(TargettedMoveDelta moveDelta, string recipient, bool processHistory)
        {
            if (!processHistory && moveDelta.HasSameAuthor(recipient))
            {
                return;
            }

            if (moveDelta.HasSameTarget(Target))
            {
                if (moveDelta.isDeleted)
                {
                    ContentDelete(moveDelta);
                    return;
                }

                if (moveDelta.newPrivacy != Privacy.NotSet)
                {
                    ContentPrivacyChange(moveDelta);
                    return;
                }

                ContentTranslateAndScale(moveDelta);
            }
        }

        protected void ContentTranslateAndScale(TargettedMoveDelta moveDelta)
        {
            var xTrans = moveDelta.xTranslate;
            var yTrans = moveDelta.yTranslate;
            var xScale = moveDelta.xScale;
            var yScale = moveDelta.yScale;
            
            /*var transformMatrix = new Matrix();
            transformMatrix.Scale(xScale, yScale);
            transformMatrix.Translate(xTrans, yTrans);*/

            foreach (var inkId in moveDelta.inkIds)
            {
                var deadStrokes = new List<Stroke>();
                foreach (var stroke in Canvas.Strokes.Where((s) => s.tag().id == inkId.Identity))
                {
                    if (dirtiesThis(moveDelta, stroke))
                    {

                        //Get bounds of the stroke, translate it to 0, scale it, add the move delta translate to the bounds and tranlate them back
                        Rect myRect = new Rect();
                        myRect = stroke.Clone().GetBounds();

                        var transformMatrix = new Matrix();
                        if (xScale != 1.0 || yScale != 1.0)
                        {
                            transformMatrix.Translate(-myRect.X, -myRect.Y);
                            stroke.Transform(transformMatrix, false);

                            transformMatrix = new Matrix();
                            transformMatrix.Scale(xScale, yScale);
                            if (double.IsNaN(xTrans))
                            {
                                xTrans = 0;
                            }
                            if (double.IsNaN(yTrans))
                            {
                                yTrans = 0;
                            }

                            xTrans = xTrans + myRect.X;
                            yTrans = yTrans + myRect.Y;
                        }

                        transformMatrix.Translate(xTrans, yTrans);
                        stroke.Transform(transformMatrix, false);
                    }
                }
            }

            foreach (var textId in moveDelta.textIds)
            {
                foreach (var textBox in Canvas.TextChildren().Where((t) => t.tag().id == textId.Identity))
                {
                    if (dirtiesThis(moveDelta, textBox))
                    {   
                        TranslateAndScale(textBox, xTrans, yTrans, xScale, yScale);
                    }
                }
            }

            foreach (var imageId in moveDelta.imageIds)
            {
                foreach (var image in Canvas.ImageChildren().Where((i) => i.tag().id == imageId.Identity))
                {
                    if (dirtiesThis(moveDelta, image))
                    {
                        TranslateAndScale(image, xTrans, yTrans, xScale, yScale);
                    }
                }
            }
        }

        private void TranslateAndScale(FrameworkElement element, double xTrans, double yTrans, double xScale, double yScale)
        {
            var left = InkCanvas.GetLeft(element) + xTrans;
            var top = InkCanvas.GetTop(element) + yTrans;

            InkCanvas.SetLeft(element, left);
            InkCanvas.SetTop(element, top);

            /* 
            // buggy

            // special case for images
            if (element is Image && !element.IsLoaded)
            {
                // don't like this at all... but how else are we going to scale an image that's still being retrieved over the network but we have the move delta now?
                ScaleImageAfterLoad(element, xScale, yScale);
                return;
            }
            */

            CorrectWidthAndHeight(element);

            element.Width *= xScale;
            element.Height *= yScale;
        }

        private void ScaleImageAfterLoad(FrameworkElement image, double xScale, double yScale)
        {
            RoutedEventHandler scaler = null; 
            scaler = (sender, args) =>
            {
                image.Loaded -= scaler;
                CorrectWidthAndHeight(image);

                image.Width *= xScale;
                image.Height *= yScale;
            };

            image.Loaded += scaler;
        }

        private void CorrectWidthAndHeight(FrameworkElement element)
        {
            if (double.IsNaN(element.Width) || double.IsNaN(element.Height))
            {
                // if we're trying to change the element's width and height before a measure pass the actual* aren't going to help
                if (element.IsMeasureValid)
                {
                    element.Height = element.ActualHeight;
                    element.Width = element.ActualWidth;
                }
            }
        }

        private bool dirtiesThis(TargettedMoveDelta moveDelta, Stroke elem)
        {
            return moveDelta.inkIds.Any(i => elem.tag().id == i && elem.tag().privacy == moveDelta.privacy && elem.tag().timestamp < moveDelta.timestamp);
        }

        private bool dirtiesThis(TargettedMoveDelta moveDelta, Image elem)
        {
            return moveDelta.imageIds.Any(i => elem.tag().id == i && elem.tag().privacy == moveDelta.privacy && elem.tag().timestamp < moveDelta.timestamp);
        }

        private bool dirtiesThis(TargettedMoveDelta moveDelta, TextBox elem)
        {
            return moveDelta.textIds.Any(i => elem.tag().id == i && elem.tag().privacy == moveDelta.privacy && elem.tag().timestamp < moveDelta.timestamp);
        }

        protected void ContentDelete(TargettedMoveDelta moveDelta)
        {
            var deadStrokes = new List<Stroke>();
            var deadTextboxes = new List<TextBox>();
            var deadImages = new List<Image>();

            foreach (var inkId in moveDelta.inkIds)
            {
                foreach (var stroke in Canvas.Strokes.Where((s) => s.tag().id == inkId.Identity))                
                {
                    if(dirtiesThis(moveDelta,stroke))
                    {
                        deadStrokes.Add(stroke);        
                    }                    
                }
            }

            foreach (var textId in moveDelta.textIds)
            {
                foreach (var textBox in Canvas.TextChildren().Where((t) => t.tag().id == textId.Identity))
                {
                    if(dirtiesThis(moveDelta,textBox))
                    {
                        deadTextboxes.Add(textBox);
                    }
                }
            }

            foreach (var imageId in moveDelta.imageIds)
            {
                foreach (var image in Canvas.ImageChildren().Where((i) => i.tag().id == imageId.Identity))
                {
                    if(dirtiesThis(moveDelta,image))
                    {
                        deadImages.Add(image);
                    }

                }
            }

            // improve the contentbuffer to remove items either:
            // - by using list.RemoveAll(predicate)
            // - iterating backwards and removing the item at the index
            // - find all the items to be removed then list.Except(listDeletions) on the list
            foreach (var text in deadTextboxes)
            {
                RemoveText(text);
            }

            foreach (var image in deadImages)
            {
                RemoveImage(image);
            }

            foreach (var stroke in deadStrokes)
            {
                RemoveStroke(stroke);
            }
        }

        protected void ContentPrivacyChange(TargettedMoveDelta moveDelta)
        {
            var privacyStrokes = new List<Stroke>();
            var privacyTextboxes = new List<TextBox>();
            var privacyImages = new List<Image>();

            foreach (var inkId in moveDelta.inkIds)
            {
                foreach (var stroke in Canvas.Strokes.Where((s) => s.tag().id == inkId.Identity))
                {
                    if (dirtiesThis(moveDelta, stroke))
                    {
                        privacyStrokes.Add(stroke);
                    }
                }
            }

            foreach (var textId in moveDelta.textIds)
            {
                foreach (var textBox in Canvas.TextChildren().Where((t) => t.tag().id == textId.Identity))
                {
                    if (dirtiesThis(moveDelta, textBox))
                    {
                        privacyTextboxes.Add(textBox);
                    }
                }
            }

            foreach (var imageId in moveDelta.imageIds)
            {
                foreach (var image in Canvas.ImageChildren().Where((i) => i.tag().id == imageId.Identity))
                {
                    if (dirtiesThis(moveDelta, image))
                    {
                        privacyImages.Add(image);
                    }
                }
            }

            foreach (var stroke in privacyStrokes)
            {
                var oldTag = stroke.tag();
                RemoveStroke(stroke);

                stroke.tag(new StrokeTag(oldTag, moveDelta.privacy));
                AddStroke(stroke);
            }

            foreach (var image in privacyImages)
            {
                var oldTag = image.tag();

                image.tag(new ImageTag(oldTag, moveDelta.privacy));
                ChangeImagePrivacy(image, moveDelta.privacy);
            }

            foreach (var text in privacyTextboxes)
            {
                var oldTag = text.tag();

                text.tag(new TextTag(oldTag, moveDelta.privacy));
                ChangeTextPrivacy(text, moveDelta.privacy);
            }
        }
    }
}