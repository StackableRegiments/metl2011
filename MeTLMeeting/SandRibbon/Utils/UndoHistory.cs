﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Practices.Composite.Presentation.Commands;
using SandRibbon.Components;
using SandRibbon.Providers;
using MeTLLib;

namespace SandRibbon.Utils
{
    public partial class UndoHistory
    {
        public class HistoricalAction
        {
            public Action undo;
            public Action redo;
            public long time;
            public string description;

            public HistoricalAction(Action undo, Action redo, long time, string description)
            {
                this.undo = undo;
                this.redo = redo;
                this.time = time;
                this.description = description;
            }
        }
        private Dictionary<int, Stack<HistoricalAction>> undoQueue = new Dictionary<int,Stack<HistoricalAction>>();
        private Dictionary<int, Stack<HistoricalAction>> redoQueue = new Dictionary<int,Stack<HistoricalAction>>();
        private int currentSlide;
        private UndoHistoryVisualiser visualiser;
        protected MeTLLib.MetlConfiguration backend;

        public UndoHistory(MetlConfiguration _backend)
        {
            backend = _backend;
            Commands.Undo.RegisterCommand(new DelegateCommand<object>(Undo, CanUndo));
            Commands.Redo.RegisterCommand(new DelegateCommand<object>(Redo, CanRedo));
            App.getContextFor(backend).controller.commands.MoveToCollaborationPage.RegisterCommand(new DelegateCommand<int>(
                i =>
                {
                    currentSlide = i;
                    RaiseQueryHistoryChanged();
                    visualiser.ClearViews();
                }
            ));

            visualiser = new UndoHistoryVisualiser();
        }
        public void Queue(Action undo, Action redo, String description)
        {
            ReenableMyContent();
            foreach(var queue in new[]{undoQueue, redoQueue})
                if(!queue.ContainsKey(currentSlide)) 
                    queue.Add(currentSlide, new Stack<HistoricalAction>());
            
            var newAction = new HistoricalAction(undo,redo, DateTime.Now.Ticks, description); 
            undoQueue[currentSlide].Push(newAction);
            visualiser.UpdateUndoView(undoQueue[currentSlide]);

            RaiseQueryHistoryChanged();
        }
        private void RaiseQueryHistoryChanged()
        {
            Commands.RequerySuggested(Commands.Undo, Commands.Redo);
        }
        private bool CanUndo(object _param) 
        { 
            return undoQueue.ContainsKey(currentSlide) && undoQueue[currentSlide].Count() > 0; 
        }

        private void ReenableMyContent()
        {
            #if TOGGLE_CONTENT
            try
            {
                // content has been modified, so make sure "my" content is visible
                Commands.SetContentVisibility.Execute(Globals.contentVisibility | ContentVisibilityEnum.MyPublicVisible | ContentVisibilityEnum.MyPrivateVisible);
            }
            catch (Exception)
            {
            }
            #endif
        }

        internal void Undo(object param)
        {
            if (CanUndo(param))
            {
                ReenableMyContent();
                var head = undoQueue[currentSlide].Pop();
                visualiser.UpdateUndoView(undoQueue[currentSlide]);
                head.undo.Invoke();
                redoQueue[currentSlide].Push(head);
                visualiser.UpdateRedoView(redoQueue[currentSlide]);
                RaiseQueryHistoryChanged();

            }
        }
        private bool CanRedo(object _param)
        {
            return redoQueue.ContainsKey(currentSlide) && redoQueue[currentSlide].Count() > 0;
        }
        private void Redo(object param)
        {
            if (CanRedo(param))
            {
                ReenableMyContent();
                var head = redoQueue[currentSlide].Pop();
                visualiser.UpdateRedoView(redoQueue[currentSlide]);
                head.redo.Invoke();
                undoQueue[currentSlide].Push(head);
                visualiser.UpdateUndoView(undoQueue[currentSlide]);
                RaiseQueryHistoryChanged();

            }
        }

    }
}
