﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Practices.Composite.Presentation.Commands;
using SandRibbon.Providers;
using SandRibbonInterop;

namespace SandRibbon.Quizzing
{
    public partial class AnswerAQuiz : Window
    {
        public static TitleConverter TitleConverter = new TitleConverter();
        public static QuestionConverter QuestionConverter = new QuestionConverter();
        private QuizQuestion question {
            get {
                return (QuizQuestion)DataContext;
            }
        }
        public AnswerAQuiz()
        {
            InitializeComponent();
            Commands.JoinConversation.RegisterCommand(new DelegateCommand<object>(closeMe));
            Commands.PreEditConversation.RegisterCommand(new DelegateCommand<object>(closeMe));
            Commands.PreCreateConversation.RegisterCommand(new DelegateCommand<object>(closeMe));
            Commands.ShowConversationSearchBox.RegisterCommand(new DelegateCommand<object>(closeMe));
        }

        private void closeMe(object obj)
        {
            Close();
        }
        public AnswerAQuiz(QuizQuestion thisQuiz):this()
        {
            DataContext = thisQuiz; 
        }
        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selection = ((Option)e.AddedItems[0]);
            if(selection.correct)
                MessageBox.Show("Nice Shooting Tex");
            Commands.SendQuizAnswer.Execute(new QuizAnswer
                                               {
                                                    answerer = Globals.me,
                                                    answer = selection.name,
                                                    id = question.id
                                               });
            this.Close();
        }
    }
    public class TitleConverter : IValueConverter 
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return string.Format("Title: {0}", value);
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
    public class QuestionConverter : IValueConverter 
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var question = (string)value;
            return string.IsNullOrEmpty(question)? "" :string.Format("Question: {0}", question);
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}
