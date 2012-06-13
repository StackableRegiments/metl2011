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
using SandRibbon.Utils;

namespace SandRibbon.Components.Utility
{
    public abstract class ContentFilter<C, T> where C : class, ICollection<T>, new() 
    {
        protected C contentCollection;

        public ContentFilter()
        {
            contentCollection = new C();
        }

        protected abstract bool Equals(T item1, T item2);
        protected abstract bool CollectionContains(T item);
        protected virtual string AuthorFromTag(T element)
        {
            return string.Empty;
        }

        public void Add(T element)
        {
            if (CollectionContains(element))
                return;

            contentCollection.Add(element);
        }

        private void Add(C elements)
        {
            foreach (T element in elements)
            {
                Add(element);
            }
        }

        private T Find(T element)
        {
            foreach (T elem in contentCollection)
            {
                if (Equals(elem, element))
                {
                    return elem;
                }
            }

            return default(T);
        }

        public void Remove(T element)
        {
            try
            {
                contentCollection.Remove(Find(element));
            }
            catch (ArgumentException) { }
        }

        private void Remove(C elements)
        {
            try
            {
                foreach (var elem in elements)
                {
                    var foundElem = Find(elem);
                    if (foundElem != null)
                    {
                        contentCollection.Remove(foundElem);
                    }
                }
            }
            catch (ArgumentException) { }
        }

        private ContentVisibilityEnum CurrentContentVisibility
        {
            get
            {
                return Commands.SetContentVisibility.IsInitialised ? (ContentVisibilityEnum)Commands.SetContentVisibility.LastValue() : ContentVisibilityEnum.AllVisible;
            }
        }

        public void Clear()
        {
            contentCollection.Clear();
        }
        
        public C FilteredContent(ContentVisibilityEnum contentVisibility)
        {
            return FilterContent(contentCollection, contentVisibility);
        }

        public void UpdateChild(T childToFind, Action<T> updateChild) 
        {
            var child = Find(childToFind); 
            if (child != null)
            {
                updateChild(child);
            }
        }

        public void UpdateChildren<V>(Action<V> updateChild) where V : UIElement
        {
            foreach (var uiElement in contentCollection.OfType<V>())
            {
                updateChild(uiElement);
            }
        }

        public void Clear(Action modifyVisibleContainer)
        {
            Clear();
            modifyVisibleContainer();
        }

        public void Add(T element, Action<T> modifyVisibleContainer)
        {
            Add(element);
            var filteredElement = FilterContent(element, CurrentContentVisibility);
            if (filteredElement != null)
            { 
                modifyVisibleContainer(filteredElement);
            }
        }

        public void Add(C elements, Action<C> modifyVisibleContainer)
        {
            Add(elements);
            var filteredElements = FilterContent(elements, CurrentContentVisibility);
            if (filteredElements != null)
            {
                modifyVisibleContainer(filteredElements);
            }
        }

        public void Remove(T element, Action<T> modifyVisibleContainer)
        {
            Remove(element);
            var filteredElement = FilterContent(element, CurrentContentVisibility);
            if (filteredElement != null)
            { 
                modifyVisibleContainer(filteredElement);
            }
        }

        public void Remove(C elements, Action<C> modifyVisibleContainer)
        {
            Remove(elements);
            modifyVisibleContainer(FilterContent(elements, CurrentContentVisibility));
        }

        public T FilterContent(T element, ContentVisibilityEnum contentVisibility)
        {
            var comparer = BuildComparer(contentVisibility);
            return comparer.Any((comp) => comp(AuthorFromTag(element))) ? element : default(T);
        }

        public C FilterContent(C elements, ContentVisibilityEnum contentVisibility)
        {
            var comparer = BuildComparer(contentVisibility);
            var tempList = new C();
            var matchedElements = elements.Where(elem => comparer.Any((comp) => comp(AuthorFromTag(elem))));

            foreach (var elem in matchedElements)
            {
                tempList.Add(elem);
            }
            
            return tempList;
        }

        #region Helpers

        private List<Func<string, bool>> BuildComparer(ContentVisibilityEnum contentVisibility)
        {
            var comparer = new List<Func<string,bool>>();
            var conversationAuthor = Globals.conversationDetails.Author;

            if (IsVisibilityFlagSet(contentVisibility, ContentVisibilityEnum.OwnerVisible))
                comparer.Add((elementAuthor) => elementAuthor == conversationAuthor);

            if (IsVisibilityFlagSet(contentVisibility, ContentVisibilityEnum.TheirsVisible))
                comparer.Add((elementAuthor) => (elementAuthor != Globals.me && elementAuthor != conversationAuthor));

            if (IsVisibilityFlagSet(contentVisibility, ContentVisibilityEnum.MineVisible))
                comparer.Add((elementAuthor) => elementAuthor == Globals.me);

            return comparer;
        }

        private bool IsVisibilityFlagSet(ContentVisibilityEnum contentVisible, ContentVisibilityEnum flag)
        {
            return (contentVisible & flag) != ContentVisibilityEnum.NoneVisible;
        }

        #endregion
    }
}