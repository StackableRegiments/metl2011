﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Functional
{
    class TestConstants
    {
        public const string OWNER_USERNAME = "eecrole";
        public const string OWNER_PASSWORD = "cleareight6";
        public const string OWNER_CONVERSATION_TITLE = "CITestSearchTestsOwner";
        public const string NONOWNER_CONVERSATION_TITLE = "CITestSearchTestsNonOwner";
        public const string DELETED_CONVERSATION_TITLE = "CITestSearchTestsDeleted";
    }
    class Constants
    {
        public const string ID_METL_MAIN_WINDOW = "ribbonWindow";
        public const string ID_METL_CONVERSATION_SEARCH_TEXTBOX = "SearchInput";
        public const string ID_METL_CONVERSATION_SEARCH_RESULTS = "SearchResults";
        public const string ID_METL_VERSION_LABEL = "VersionLabel";
        public const string ID_METL_LOGGING_IN_LABEL = "LoggingInLabel";
        public const string ID_METL_LOGIN_ERROR_LABEL = "loginErrors";
        public const string ID_METL_LOGOUT_AND_EXIT_BACKNAV_BUTTON = "LogoutAndExit";
        public const string ID_METL_USER_CANVAS_STACK = "canvas";
        public const string ID_METL_SEARCH_ALL_CONVERSATIONS_BUTTON = "searchConversations";
    }

    class ErrorMessages
    {
        public const string CONVERSATION_MISSING_FROM_RECENT = "Conversation missing from Recent Conversations menu.";
        public const string PROBLEM_SHUTTING_DOWN = "MeTL did not shutdown correctly.";
        public const string EXPECTED_MAIN_WINDOW = "Expected to find MeTL '" + Constants.ID_METL_MAIN_WINDOW + "'.";
        public const string UNABLE_TO_FIND_EXECUTABLE = "Unable to find the MeTL executable.";
        public const string EXPECTED_ONE_INSTANCE = "Expected only one instance of MeTL to be running.";
        public const string WAIT_FOR_CONTROL_FAILED = "WaitForControl function failed.";
        public const string VERSION_MISMATCH = "MeTL version does not match with provided.";
        public const string UNABLE_TO_FIND_CONVERSATION = "Unable to find searched conversation.";
        public const string EXPECTED_CONTENT = "Expected element to have specified content.";
        public const string EXPECTED_NO_RESULTS = "Expected to find no matching conversations.";
    }
}
