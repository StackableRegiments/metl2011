﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Practices.Composite.Presentation.Commands;
using System.Windows.Controls.Primitives;
using SandRibbonObjects;

namespace SandRibbon.Components.Sandpit
{
    public partial class S15Boards : UserControl
    {
        public static BoardPlacementConverter BOARD_PLACEMENT_CONVERTER = new BoardPlacementConverter();
        public static OnlineColorConverter ONLINE_COLOR_CONVERTER = new OnlineColorConverter();
        public S15Boards()
        {
            InitializeComponent();
            Commands.SendWakeUp.RegisterCommand(new DelegateCommand<object>(_nothing=>{
                BoardManager.ClearBoards("S15");
                var boards = BoardManager.boards["S15"].ToList();
                boardDisplay.ItemsSource = boards;
                Commands.ToggleFriendsVisibility.Execute(null);
                for (int i = 0; i < BoardManager.DEFAULT_CONVERSATION.Slides.Count;i++)
                {
                    var user = boards[i].name;
                    Commands.SendPing.Execute(user);
                    Commands.SendMoveBoardToSlide.Execute(
                        new SandRibbon.Utils.Connection.JabberWire.BoardMove{
                            boardUsername=user,
                            roomJid = BoardManager.DEFAULT_CONVERSATION.Slides[i].id
                    });
                }
            }));
            Commands.CloseBoardManager.RegisterCommand(new DelegateCommand<object>(
                _obj => Commands.ToggleFriendsVisibility.Execute(null)
            ));
        }
        public void boardClicked(object sender, RoutedEventArgs e) {
            var board = (Board)((FrameworkElement)sender).DataContext;
            if (board.online)
            {
                System.Windows.Controls.Canvas.SetTop(avatar, (board.y - BoardManager.AVATAR_HEIGHT / 2)+40);
                System.Windows.Controls.Canvas.SetLeft(avatar, (board.x - BoardManager.AVATAR_WIDTH / 2)+60);
                Commands.MoveTo.Execute(
                    BoardManager.DEFAULT_CONVERSATION.Slides[((List<Board>)BoardManager.boards["S15"]).IndexOf(board)].id);
            }
        }
    }
    public class OnlineColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new SolidColorBrush((bool)value ? Colors.White : Colors.Black);
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
    public class BoardPlacementConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (int)value - BoardManager.DISPLAY_WIDTH / 2;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}
