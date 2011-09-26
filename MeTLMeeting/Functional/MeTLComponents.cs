﻿using System;
using System.Linq;
using System.Windows.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Controls.Primitives;
using Keys = System.Windows.Forms.SendKeys;
using System.Windows.Automation;
using Button=System.Windows.Forms.Button;
using System.Threading;
using System.Windows;

namespace Functional
{
    public class PresentationSpace
    {
        private AutomationElement _parent;
        private AutomationElement _canvas;
        public PresentationSpace(AutomationElement parent)
        {
            _parent = parent;
            _canvas = _parent.Descendant("canvas"); 
        }
        public string Style()
        {
            return _canvas.Value();
        }
    }
    public class QuickLaunch
    {
        private AutomationElement _parent;
        private AutomationElement _tute;
        private AutomationElement _lecture;
        public QuickLaunch(AutomationElement parent)
        {
            _parent = parent;
            _tute = _parent.Descendant("quickLaunchTutorial");
            _lecture = _parent.Descendant("quickLaunchLecture");
        }
        public void BecomeTutorial()
        {
            _tute.Invoke();
        }
        public void BecomeLecture()
        {
            _lecture.Invoke();
        }
    }
    public class ApplicationPopup
    {
        private AutomationElement _parent;
        public ApplicationPopup(AutomationElement parent)
        {
            _parent = parent;
        }
        public void open()
        {
            var buttons = _parent.Children(typeof(Button));
            var appButton = buttons[3];
            var rect = appButton.Current.BoundingRectangle;
            Assert.AreEqual(46,rect.Width);
            Assert.AreEqual(46,rect.Height);
            appButton.Invoke();
        }
        public ConversationPicker RecommendedConversations()
        {
            open();
            var popup = _parent.Descendant(typeof(Popup));
            var menuItems = popup.Descendants(typeof(Divelements.SandRibbon.MenuItem));
            var prev = popup.Descendants();
            menuItems[0].Invoke();
            var post = popup.Descendants().Except(prev);
            var children = post.Aggregate("",(acc,item)=>acc+" "+item.Current.ClassName);
            return new ConversationPicker(post.Where(e => e.Current.ClassName == "SimpleConversationSelector").First());
        }
        public ConversationPicker AllConversations()
        {    
            open();
            var popup = _parent.Descendant(typeof(Popup));
            var menuItems = popup.Descendants(typeof(Divelements.SandRibbon.MenuItem));
            var prev = popup.Descendants();
            menuItems[1].Invoke();
            var post = popup.Descendants().Except(prev);
            var children = post.Aggregate("",(acc,item)=>acc+" "+item.Current.ClassName+":"+item.Current.AutomationId);
            return new ConversationPicker(post.Where(e => e.Current.ClassName == "SimpleConversationFilterer").First());
        }

        public void ImportPowerpoint(string filename)
        {
            open();
            var popup = _parent.Descendant(typeof(Popup));
            var menuItems = popup.Descendants(typeof(Divelements.SandRibbon.MenuItem));
            try
            {
                // Invoke the import powerpoint menuitem
                var menu = menuItems[1];
                menu.Invoke();

                //Thread.Sleep(3000);
                Keys.SendWait(filename);
            }
            catch (Exception)
            {
            }
        }
        public void CreateConversation()
        {
            open();
            var popup = _parent.Descendant(typeof(Popup));
            var menuItems = popup.Descendants(typeof(Divelements.SandRibbon.MenuItem));
            try
            {
                // invoke create new conversation
                var menu = menuItems[0];
                menu.Invoke();
            }
            catch (Exception) 
            {
            }
        }
        public void SearchConversation()
        {
            open();
            var popup = _parent.Descendant(typeof(Popup));
            var menuItems = popup.Descendants(typeof(Divelements.SandRibbon.MenuItem));
            try
            {
                // invoke search conversation
                var menu = menuItems[2];
                menu.Invoke();
            }
            catch (Exception) 
            {
            }
        }
    }
    public class ConversationPicker
    {
        private AutomationElement _title;
        private AutomationElement _restriction;
        private AutomationElement _creationType;
        private AutomationElement _file;
        private AutomationElement _importType;
        private AutomationElement _create;

        public ConversationPicker(AutomationElement parent)
        {
            _title = parent.Descendant("conversationNameTextBox");
            _restriction = parent.Descendant("conversationSubjectListBox");
            _creationType = parent.Descendant("startingContentSelector");
            _file = parent.Descendant("importFileTextBox");
            _importType = parent.Descendant("importSelector");
            _create = parent.Descendant("CommitButton");
        }
        public ConversationPicker title (string value)
        {
            _title.Value("");
            _title.SetFocus();
            _title.Value(value);
            return this;
        }
        public ConversationPicker file(string value)
        {
            _file.Value("");
            _file.SetFocus();
            _file.Value(value);
            return this;
        }
        public ConversationPicker createType(int position)
        {
            ((SelectionItemPattern)_creationType.Children(typeof(ListBoxItem))[position]
                .GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
            return this;
        }
        public ConversationPicker powerpointType(int position)
        {
            ((SelectionItemPattern)_importType.Children(typeof(ListBoxItem))[position]
                .GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
            return this;
        }
        public void enter(string title)
        {
        }
        public void update()
        {
            _title.SetFocus();
            ((SelectionItemPattern)_restriction.Children(typeof(ListBoxItem))[0]
                .GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
            _title.SetFocus();
            _create.Invoke();
        }
        public void create()
        {
            _title.SetFocus();
            _file.SetFocus();
            _title.SetFocus();
            _create.Invoke();
        }
    }
    public class UserCanvasStack
    {
        private AutomationElement _stack;
        private AutomationElement _handwriting;
        private AutomationElement _text;
        private AutomationElement _images;
        private AutomationElement _privacyTools;
        public string Ink
        {
            get 
            {
                return _handwriting.Value();
            }
            set
            {
                _handwriting.Value(value);
            }
        }
        public string Text
        {
            get
            {
                return _text.Value();
            }
            set
            {
                _text.Value(value);
            }
        }
        public string Images
        {
            get
            {
                return _images.Value();
            }
            set
            {
                _images.Value(value);
            }
        }
        public string Privacy
        {
            get
            {
                return _privacyTools.Value();
            }
            set
            {
                _privacyTools.Value(value);
            }
        }
      
        public UserCanvasStack(AutomationElement parent, string target)
        {
            _stack = parent.Descendant(target);
            _handwriting = _stack.Descendant("handwriting");
            _text = _stack.Descendant("text");
            _images = _stack.Descendant("images");
        }
    }

    public class Quiz
    {
        private AutomationElement _open;
        private AutomationElement _quiz;
        private AutomationElement _parent;
        public Quiz(AutomationElement parent)
        {
            _parent = parent;
        }
        public Quiz openTab()
        {
            Keys.SendWait("%");
            Thread.Sleep(100);
            Keys.SendWait("Q");
            return this;
        }
        public void open()
        {
            var buttons = _parent.Descendants(typeof(Button));
            foreach (AutomationElement button in buttons)
            {
                if (button.Current.AutomationId.ToLower().Contains("createquiz"))
                {
                    button.Invoke();
                    return;
                }
            }
        }

        public void openQuiz()
        {
            var allButtons = _parent.Descendants(typeof(Button));
            foreach(AutomationElement button in allButtons)
            {
                // this will only find the first quiz
                if (button.Current.AutomationId.ToLower().Equals("quiz"))
                {
                    button.Invoke();
                    return;
                }
            }
        }
    }
    public class QuizAnswer
    {
        private AutomationElement _buttons;
        public QuizAnswer(AutomationElement parent)
        {
             var _dialog = AutomationElement.RootElement
                                        .FindFirst(TreeScope.Children, 
                                                    new PropertyCondition(AutomationElement.AutomationIdProperty, 
                                                    "answerAQuiz"));
            _buttons = _dialog.Descendant("quizOptions"); 
        }
        public void answer()
        {
            ((SelectionItemPattern)_buttons.Children(typeof(ListBoxItem))[0]
                .GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
        }
    }
    public class QuizCreate
    {
        private AutomationElement _create;
        private AutomationElement _title;
        private AutomationElement _question;
        private AutomationElement _parent;
        private AutomationElement _dialog;
        private AutomationElementCollection _options;
        public QuizCreate(AutomationElement parent)
        {
            _parent = parent;
            _dialog = _parent.Descendant("createAQuiz");
            _create = _dialog.Descendant("quizCommitButton");
            _question = _dialog.Descendant("question");
            _title = _dialog.Descendant("quizTitle");
            _options = _dialog.Descendants(typeof(TextBox));
        }
        public QuizCreate question(string value)
        {
            _question.Value(value);
            return this; 
        }
        public QuizCreate options()
        {
            var count = 0;
            foreach (AutomationElement element in _options)
            {
                if (element.Current.AutomationId.ToLower().Equals("quizanswer"))
                {
                    element.Value("Answer " + count.ToString());
                    count++;
                }
            }
            return this;
        }
        public void create()
        {
            _create.Invoke();
        }
    }
    public class SubmissionViewer
    {
        private AutomationElement _submissionList;
        private AutomationElement _import;
        public SubmissionViewer(AutomationElement _obj)
        {
              var parent = AutomationElement
                                                .RootElement
                                                .FindFirst(TreeScope.Children, 
                                                            new PropertyCondition(AutomationElement.AutomationIdProperty, 
                                                            "viewSubmissions"));
            _submissionList = parent.Descendant("submissions");
            _import = parent.Descendant("importSelectedSubmission");
        }
        public void import()
        {
            ((SelectionItemPattern)_submissionList.Children(typeof(ListBoxItem))[0]
                .GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
            _import.Invoke();
        }
    }

    public class Submission
    {
        private AutomationElement _parent;
        private AutomationElement _view;

        public Submission(AutomationElement parent)
        {
            _parent = parent;
            _view= parent.Descendant("viewSubmission");

        }
        public Submission openTab()
        {
            Keys.SendWait("%");
            Thread.Sleep(100);
            Keys.SendWait("S");
            return this;
        }
        public void open()
        {
        }
        public void view()
        {
            _view.Invoke();
        }
        public void submit()
        {
            var buttons = _parent.Descendants(typeof(Button));
            foreach (AutomationElement button in buttons)
            {
                if (button.Current.AutomationId.ToLower().Contains("submitSubmission"))
                {
                    button.Invoke();
                    return;
                }
            }
        }
    }
    public  class ConversationSearcher
    {
        private AutomationElement _searchField;
        private AutomationElement _searchButton;
        private AutomationElement _searchResults;
        private AutomationElement _parent;

        public ConversationSearcher(AutomationElement parent)
        {
            _parent = parent;
            _searchField = parent.Descendant("SearchInput");
            _searchButton = parent.Descendant("searchConversations");
        }
        public ConversationSearcher searchField(string value)
        {
            _searchField.Value("");
            _searchField.SetFocus();
            _searchField.Value(value);

            return this;
        }
        public ConversationSearcher Search()
        {
            _searchButton.Invoke();
            return this;
        }
        public ConversationSearcher GetResults()
        {
            _searchResults = _parent.Descendant("SearchResults");
            Assert.AreNotEqual(Rect.Empty, _searchResults.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty));
            return this;
        }
    }
    public class SlideNavigation 
    {
        private AutomationElement _forward;
        private AutomationElement _back;
        private AutomationElement _add;
        private AutomationElement _sync;

        public SlideNavigation(AutomationElement parent)
        {
            _forward = parent.Descendant("moveToNext");
            _back = parent.Descendant("moveToPrevious");
            _add = parent.Descendant("addSlideButton");
            _sync = parent.Descendant("syncButton");
        }
        public void Add()
        {
            _add.Invoke();
        }
        public void Back()
        {
            _back.Invoke();
        }
        public void Forward()
        {
            _forward.Invoke();
        }
        public void Sync()
        {
            _sync.Invoke();
        }
    }
    public class Login
    {
        private AutomationElement _login;
        private AutomationElement _username;
        private AutomationElement _password;
        private AutomationElement _submit;
        private AutomationElement _remember;
        public Login(AutomationElement parent)
        {
            _login = parent.Descendant("login");
            _username = _login.Descendant("username");
            _password = _login.Descendant("password");
            _submit = _login.Descendant("submit");
            _remember = _login.Descendant("rememberMe");
        }
        private void moveWindowToTestingDatabase()
        {
            _login.Value(Constants.TEST_DB);
            Assert.AreEqual(_login.Value(), Constants.TEST_DB, "MeTL has not been pointed at the testing DB.  Testing cannot continue.");
        }
        public Login username(string value)
        {
            _username.Value("");
            _username.SetFocus();
            _username.Value(value);
            return this;
        }
        public Login password(string value)
        {
            _password.Value("");
            _password.SetFocus();
            Keys.SendWait(value);
            return this;
        }
        public Login remember()
        {
            _remember.Toggle();
            return this;
        }
        public Login submit()
        {
            _submit.Invoke();
            return this;
        }
    }
}
