using System;
using System.Windows;
using System.Windows.Controls;

namespace TicTacDumb.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public char _player;
        public char _opponent;
        public char[,] _board;
        public bool _isPlayable;

        public MainWindow()
        {
            InitializeComponent();
            Memory.LoadMemory();
        }

        private void NewGame()
        {
            lblResult.Text = "";
            _isPlayable = true;
            _board = BoardHelpers.NewBoard();
            RegenerateBoard();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!_isPlayable)
                return;

            var btn = sender as Button;
            if (btn != null)
            {
                switch (btn.Name)
                {
                    case "btn00":
                        DoMove(0, 0);
                        break;
                    case "btn01":
                        DoMove(0, 1);
                        break;
                    case "btn02":
                        DoMove(0, 2);
                        break;

                    case "btn10":
                        DoMove(1, 0);
                        break;
                    case "btn11":
                        DoMove(1, 1);
                        break;
                    case "btn12":
                        DoMove(1, 2);
                        break;

                    case "btn20":
                        DoMove(2, 0);
                        break;
                    case "btn21":
                        DoMove(2, 1);
                        break;
                    case "btn22":
                        DoMove(2, 2);
                        break;
                }
            }
        }

        private void DoMove(int y, int x)
        {
            if (_board[y, x] == '-')
            {
                _board[y, x] = _player;

                if (BoardHelpers.HasVictory(_board, _player))
                {
                    lblResult.Text = "-- PLAYER 1 WON --";
                    RegenerateBoard();
                    _isPlayable = false;
                    return;
                }

                Learning.Think(_board, _opponent);

                if (BoardHelpers.HasVictory(_board, _player))
                {
                    lblResult.Text = "-- PLAYER 2 WON --";
                    RegenerateBoard();
                    _isPlayable = false;
                    return;
                }

                if (BoardHelpers.MovesLeft(_board) == 0)
                {
                    lblResult.Text = "-- TIE --";
                    RegenerateBoard();
                    _isPlayable = false;
                    return;
                }

                RegenerateBoard();
            }
        }

        private void RegenerateBoard()
        {
            btn00.Content = _board[0, 0];
            btn01.Content = _board[0, 1];
            btn02.Content = _board[0, 2];

            btn10.Content = _board[1, 0];
            btn11.Content = _board[1, 1];
            btn12.Content = _board[1, 2];

            btn20.Content = _board[2, 0];
            btn21.Content = _board[2, 1];
            btn22.Content = _board[2, 2];
        }

        private void Player1_Click(object sender, RoutedEventArgs e)
        {
            NewGame();
            _player = 'x';
            _opponent = 'o';
            GameGrid.Visibility = Visibility.Visible;
            SelectionGrid.Visibility = Visibility.Collapsed;
        }

        private void Player2_Click(object sender, RoutedEventArgs e)
        {
            NewGame();
            _player = 'o';
            _opponent = 'x';
            GameGrid.Visibility = Visibility.Visible;
            SelectionGrid.Visibility = Visibility.Collapsed;

            Learning.Think(_board, _opponent);
            RegenerateBoard();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            GameGrid.Visibility = Visibility.Collapsed;
            SelectionGrid.Visibility = Visibility.Visible;
        }
    }
}