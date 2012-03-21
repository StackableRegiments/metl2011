﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows;
using SandRibbon.Providers;
using MeTLLib.DataTypes;
using System.Diagnostics;

namespace SandRibbon.Components.Utility
{
    public class ContentBuffer
    {
        private List<UIElement> uiCollection;
        private StrokeCollection strokeCollection;

        public ContentBuffer()
        {
            uiCollection = new List<UIElement>();
            strokeCollection = new StrokeCollection();
        }

        private void ClearStrokes()
        {
            strokeCollection.Clear();
        }

        private void ClearElements()
        {
            uiCollection.Clear();
        }

        private void AddStrokes(StrokeCollection strokes)
        {
            strokeCollection.Add(strokes);
        }

        private void AddStrokes(Stroke stroke)
        {
            strokeCollection.Add(stroke);
        }

        private void RemoveStrokes(StrokeCollection strokes)
        {
            try
            {
                strokeCollection.Remove(strokes);
            }
            catch (ArgumentException) { }
        }
        
        private void RemoveStroke(Stroke stroke)
        {
            try
            {
                strokeCollection.Remove(stroke);
            }
            catch (ArgumentException) { }
        }

        private void AddElements(UIElement element)
        {
            uiCollection.Add(element);
        }

        private void AddElements(UIElementCollection elements)
        {
            foreach (UIElement element in elements)
            {
                uiCollection.Add(element);
            }
        }

        private void RemoveElement(UIElement element)
        {
            try
            {
                uiCollection.Remove(element);
            }
            catch (ArgumentException) { }
        }

        private ContentVisibilityEnum CurrentContentVisibility
        {
            get
            {
                var currentContent = Commands.SetContentVisibility.IsInitialised ? (ContentVisibilityEnum)Commands.SetContentVisibility.LastValue() : ContentVisibilityEnum.AllVisible;
                return currentContent;
            }
        }

        #region Collections

        public StrokeCollection Strokes
        {
            get
            {
                return strokeCollection;
            }
        }

        public List<UIElement> CanvasChildren
        {
            get
            {
                return uiCollection;
            }
        }

        public StrokeCollection FilteredStrokes(ContentVisibilityEnum contentVisibility)
        {
            return FilterStrokes(Strokes, contentVisibility);
        }

        public IEnumerable<UIElement> FilteredElements(ContentVisibilityEnum contentVisibility)
        {
            return FilterElements(CanvasChildren, contentVisibility);
        }

        #endregion

        public void UpdateChild<TypeOfChild>(TypeOfChild childToFind, Action<TypeOfChild> updateChild) where TypeOfChild : UIElement
        {
            var child = uiCollection.Find((elem) => elem == childToFind);
            if (child != null)
            {
                updateChild(child as TypeOfChild);
            }
        }
        public void UpdateChildren<TypeOfChildren>(Action<TypeOfChildren> updateChild) 
        {
            foreach (var uiElement in uiCollection.OfType<TypeOfChildren>())
            {
                updateChild(uiElement);
            }
        }

        public void UpdateStrokes(Action<Stroke> updateChild)
        {
            foreach (Stroke uiElement in strokeCollection)
            {
                updateChild(uiElement);
            }
        }

        public void Clear()
        {
            ClearStrokes();
            ClearElements();
        }

        #region Handle strokes

        public void ClearStrokes(Action modifyVisibleContainer)
        {
            ClearStrokes();
            modifyVisibleContainer();
        }

        public void AddStrokes(StrokeCollection strokes, Action<StrokeCollection> modifyVisibleContainer)
        {
            AddStrokes(strokes);
#if TOGGLE_CONTENT
            modifyVisibleContainer(FilterStrokes(strokes, CurrentContentVisibility));
#else
            modifyVisibleContainer(strokes);
#endif
        }

        public void AddStrokes(Stroke stroke, Action<StrokeCollection> modifyVisibleContainer)
        {
            var strokes = new StrokeCollection();
            strokes.Add(stroke);

            AddStrokes(stroke);
#if TOGGLE_CONTENT
            modifyVisibleContainer(FilterStrokes(strokes, CurrentContentVisibility));
#else
            modifyVisibleContainer(strokes);
#endif
        }

        public void RemoveStrokes(Stroke stroke, Action<StrokeCollection> modifyVisibleContainer)
        {
            var strokes = new StrokeCollection();
            strokes.Add(stroke);

            RemoveStroke(stroke);
#if TOGGLE_CONTENT
            modifyVisibleContainer(FilterStrokes(strokes, CurrentContentVisibility));
#else
            modifyVisibleContainer(strokes);
#endif
        }

        public void RemoveStrokes(StrokeCollection strokes, Action<StrokeCollection> modifyVisibleContainer)
        {
            RemoveStrokes(strokes);
#if TOGGLE_CONTENT
            modifyVisibleContainer(FilterStrokes(strokes, CurrentContentVisibility));
#else
            modifyVisibleContainer(strokes);
#endif
        }

        private StrokeCollection FilterStrokes(StrokeCollection strokes, ContentVisibilityEnum contentVisibility)
        {
            var comparer = BuildComparer(contentVisibility);
            return new StrokeCollection(strokes.Where(s => comparer.Any((comp) => comp(s.tag().author))));
        }

        #endregion

        #region Handle images and text

        public void ClearElements(Action modifyVisibleContainer)
        {
            ClearElements();
            modifyVisibleContainer();
        }

        public void AddElement(UIElement element, Action<UIElement> modifyVisibleContainer)
        {
            AddElements(element);
#if TOGGLE_CONTENT
            var filteredElement = FilterElement(element, CurrentContentVisibility);
            if (filteredElement != null)
            { 
                modifyVisibleContainer(filteredElement);
            }
#else
            modifyVisibleContainer(element);
#endif
        }

        public void RemoveElement(UIElement element, Action<UIElement> modifyVisibleContainer)
        {
            RemoveElement(element);
#if TOGGLE_CONTENT
            var filteredElement = FilterElement(element, CurrentContentVisibility);
            if (filteredElement != null)
            { 
                modifyVisibleContainer(filteredElement);
            }
#else
            modifyVisibleContainer(element);
#endif
        }

        private UIElement FilterElement(UIElement element, ContentVisibilityEnum contentVisibility)
        {
            var tempList = new List<UIElement>();
            tempList.Add(element);

            return FilterElements(tempList, contentVisibility).FirstOrDefault();
        }

        private string AuthorFromElementTag(UIElement element)
        {
            if (element is Image)
                return ((Image)element).tag().author;

            if (element is MeTLTextBox)
                return ((MeTLTextBox)element).tag().author;

            return string.Empty;
        }

        private IEnumerable<UIElement> FilterElements(List<UIElement> elements, ContentVisibilityEnum contentVisibility)
        {
            var comparer = BuildComparer(contentVisibility);
            return elements.Where(elem => comparer.Any((comp) => comp(AuthorFromElementTag(elem))));
        }

        private List<Func<string, bool>> BuildComparer(ContentVisibilityEnum contentVisibility)
        {
            var comparer = new List<Func<string,bool>>();

            if (IsVisibilityFlagSet(contentVisibility, ContentVisibilityEnum.OwnerVisible))
                comparer.Add((elementAuthor) => elementAuthor == Globals.conversationDetails.Author);

            if (IsVisibilityFlagSet(contentVisibility, ContentVisibilityEnum.TheirsVisible))
                comparer.Add((elementAuthor) => elementAuthor != Globals.me);

            if (IsVisibilityFlagSet(contentVisibility, ContentVisibilityEnum.MineVisible))
                comparer.Add((elementAuthor) => elementAuthor == Globals.me);

            return comparer;
        }

        #endregion

        private bool IsVisibilityFlagSet(ContentVisibilityEnum contentVisible, ContentVisibilityEnum flag)
        {
            return (contentVisible & flag) != ContentVisibilityEnum.NoneVisible;
        }
    }

}
