﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Automation;
using UITestFramework;

namespace Functional
{
    [TestClass]
    public class TextModeTests
    {
        private AutomationElement metlWindow;
        private HomeTabScreen homeTab;
        
        [TestInitialize]
        public void Setup()
        {
            var control = new UITestHelper();
            var success = control.WaitForControlEnabled(Constants.ID_METL_MAIN_WINDOW);
            Assert.IsTrue(success, ErrorMessages.EXPECTED_MAIN_WINDOW);

            if (metlWindow == null)
                metlWindow = MeTL.GetMainWindow();

            Assert.IsNotNull(metlWindow, ErrorMessages.EXPECTED_MAIN_WINDOW); 
            metlWindow = MeTL.GetMainWindow();

            homeTab = new HomeTabScreen(metlWindow).OpenTab();
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
