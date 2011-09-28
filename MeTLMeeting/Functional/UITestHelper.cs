﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using System.Threading;

namespace Functional
{
    public class UITestHelper
    {
        private const int sleepIncrement = 100;
        private const int defaultTimeout = 30 * 1000;
        private AutomationElement desktop;

        public UITestHelper()
        {
            desktop = AutomationElement.RootElement;
        }

        private AutomationElement FindFirstChildUsingAutomationId(string controlAutomationId)
        {
            return desktop.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, controlAutomationId));
        }

        /// returns true if control is enabled before time-out; otherwise, false.
        public bool WaitForControlEnabled(string controlAutomationId)
        {
            int totalTime = 0;
            AutomationElement uiControl = null;

            do
            {
                uiControl = FindFirstChildUsingAutomationId(controlAutomationId);
                totalTime += sleepIncrement;
                Thread.Sleep(sleepIncrement);
            }
            while (uiControl == null && totalTime < defaultTimeout);

            return uiControl != null;
        }

        /// returns true if control is not found before time-out; otherwise, false.
        public bool WaitForControlNotExist(string controlAutomationId)
        {
            int totalTime = 0;
            AutomationElement uiControl = null;

            do
            {
                uiControl = FindFirstChildUsingAutomationId(controlAutomationId);
                totalTime += sleepIncrement;
                Thread.Sleep(sleepIncrement);
            }
            while (uiControl != null && totalTime < defaultTimeout);

            return uiControl == null;
        }
    }
}
