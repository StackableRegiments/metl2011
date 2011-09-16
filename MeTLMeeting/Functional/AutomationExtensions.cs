﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Functional
{
    public static class AutomationExtensions
    {
        public static AutomationElement Descendant(this AutomationElement element, string name)
        {
            var result = element.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, name));
            Assert.IsNotNull(result, string.Format("{0}[{1}] unexpectedly null", element.GetCurrentPropertyValue(AutomationElement.AutomationIdProperty), name));
            return result;
        }
        public static AutomationElement Descendant(this AutomationElement element, Type type)
        {
            var result = element.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ClassNameProperty, type.Name));
            Assert.IsNotNull(result, string.Format("{0}[{1}] unexpectedly null", element.GetCurrentPropertyValue(AutomationElement.AutomationIdProperty), type.Name));
            return result;
        }
        public static AutomationElement FullDescendant(this AutomationElement element, Type type)
        {
            var result = element.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ClassNameProperty, type.FullName));
            Assert.IsNotNull(result, string.Format("{0}[{1}] unexpectedly null", element.GetCurrentPropertyValue(AutomationElement.AutomationIdProperty), type.FullName));
            return result;
        }
        public static AutomationElementCollection Descendants(this AutomationElement element, Type type)
        {
            var result = element.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ClassNameProperty, type.Name));
            Assert.IsNotNull(result, string.Format("{0}[{1}s] unexpectedly null", element.GetCurrentPropertyValue(AutomationElement.AutomationIdProperty), type.Name));
            return result;
        }
        public static IEnumerable<AutomationElement> Descendants(this AutomationElement element)
        {
            var result = new AutomationElement[1024];
            element.FindAll(TreeScope.Descendants, System.Windows.Automation.Condition.TrueCondition).CopyTo(result, 0);
            return result.TakeWhile(e => e != null).ToArray();
        }
        
        public static AutomationElementCollection Children(this AutomationElement element, Type type)
        {
            var result = element.FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.ClassNameProperty, type.Name));
            Assert.IsNotNull(result, string.Format("{0}[{1}s] unexpectedly null", element.GetCurrentPropertyValue(AutomationElement.AutomationIdProperty), type.Name));
            return result;
        }
        public static AutomationElement Child(this AutomationElement element, Type type)
        {
            var result = element.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.ClassNameProperty, type.Name));
            Assert.IsNotNull(result, string.Format("{0}[{1}] unexpectedly null", element.GetCurrentPropertyValue(AutomationElement.AutomationIdProperty), type.Name));
            return result;
        }
        public static string Value(this AutomationElement element)
        {
            return ((ValuePattern)element.GetCurrentPattern(ValuePattern.Pattern)).Current.Value;
        }
        public static AutomationElement Value(this AutomationElement element, string value)
        {
            ((ValuePattern)element.GetCurrentPattern(ValuePattern.Pattern)).SetValue(value);
            return element;
        }
        public static AutomationElement Invoke(this AutomationElement element)
        {
            ((InvokePattern)element.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
            return element;
        }
        public static AutomationElement Toggle(this AutomationElement element)
        {
            ((TogglePattern)element.GetCurrentPattern(TogglePattern.Pattern)).Toggle();
            return element;
        }
        public static string AutomationId(this AutomationElement element)
        {
            return element.GetCurrentPropertyValue(AutomationElement.AutomationIdProperty).ToString();
        }
        public static void SetPosition(this AutomationElement element, double width, double height, double x, double y)
        {
            ((WindowPattern)element.GetCurrentPattern(WindowPattern.Pattern)).SetWindowVisualState(WindowVisualState.Normal);
            ((TransformPattern)element.GetCurrentPattern(TransformPattern.Pattern)).Resize(width, height);
            ((TransformPattern)element.GetCurrentPattern(TransformPattern.Pattern)).Move(x, y);
        }
        public static AutomationElement pause(this AutomationElement element, int milis)
        {
            Thread.Sleep(milis);
            return element;
        }
    }
}
