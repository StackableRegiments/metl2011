﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Automation;
using UITestFramework;

namespace Functional
{
    [TestClass]
    public class TextModeTests
    {
        private UITestHelper metlWindow;
        private HomeTabScreen homeTab;
        
        [TestInitialize]
        public void Setup()
        {
            metlWindow = new UITestHelper();
            metlWindow.SearchProperties.Add(new PropertyExpression(AutomationElement.AutomationIdProperty, Constants.ID_METL_MAIN_WINDOW));

            var success = metlWindow.WaitForControlExist();
            Assert.IsTrue(success, ErrorMessages.EXPECTED_MAIN_WINDOW);

            homeTab = new HomeTabScreen(metlWindow.AutomationElement).OpenTab();
        }

        [TestMethod]
        public void ActivateTextMode()
        {
            homeTab.ActivateTextMode();
        }

        [TestMethod]
        public void InsertText()
        {

        }
    }
}
