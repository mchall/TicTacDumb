using System;
using System.Linq;

namespace TicTacDumb
{
    public static class BoardHelpers
    {
        public static void PrintBoard(char[,] board)
        {
            Console.Write(board[0, 0]);
            Console.Write(board[0, 1]);
            Console.WriteLine(board[0, 2]);

            Console.Write(board[1, 0]);
            Console.Write(board[1, 1]);
            Console.WriteLine(board[1, 2]);

            Console.Write(board[2, 0]);
            Console.Write(board[2, 1]);
            Console.WriteLine(board[2, 2]);
        }

        public static int MovesLeft(char[,] board)
        {
            var tmp = ToString(board);
            return tmp.Count(c => c == '-');
        }

        public static bool HasVictory(char[,] board, char player)
        {
            var tmp = ToString(board);

            var tmpBoard = FlipBoard(board);
            for (int f = 0; f < 4; f++)
            {
                if (tmpBoard[0, 0] == player && tmpBoard[1, 1] == player && tmpBoard[2, 2] == player)
                {
                    return true;
                }

                for (int y = 0; y < 3; y++)
                {
                    if (tmpBoard[y, 0] == player && tmpBoard[y, 1] == player && tmpBoard[y, 2] == player)
                    {
                        return true;
                    }
                }

                tmpBoard = FlipBoard(tmpBoard);
            }

            return false;
        }

        public static char[,] NewBoard()
        {
            var board = new char[3, 3];
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    board[y, x] = '-';
                }
            }
            return board;
        }

        public static char[,] FlipBoard(char[,] input)
        {
            var newBoard = new char[3, 3];

            newBoard[0, 0] = input[2, 0];
            newBoard[0, 1] = input[1, 0];
            newBoard[0, 2] = input[0, 0];
            newBoard[1, 0] = input[2, 1];
            newBoard[1, 1] = input[1, 1];
            newBoard[1, 2] = input[0, 1];
            newBoard[2, 0] = input[2, 2];
            newBoard[2, 1] = input[1, 2];
            newBoard[2, 2] = input[0, 2];

            return newBoard;
        }

        public static Move FlipMove(Move input, int numFlips = 0)
        {
            if (numFlips == 0)
                return input;

            var newMove = new Move();

            newMove.X = input.Y;
            switch (input.X)
            {
                case 0: newMove.Y = 2; break;
                case 1: newMove.Y = 1; break;
                case 2: newMove.Y = 0; break;
            }
            newMove.Score = input.Score;
            newMove.Board = !string.IsNullOrEmpty(input.Board) ? FlipBoard(input.Board) : null;

            return FlipMove(newMove, numFlips - 1);
        }

        public static string FlipBoard(string input)
        {
            string newString = "";
            newString += $"{ input[2] }{ input[5] }{ input[8] }";
            newString += $"{ input[1] }{ input[4] }{ input[7] }";
            newString += $"{ input[0] }{ input[3] }{ input[6] }";
            return newString;
        }

        public static bool AreEqual(char[,] left, char[,] right)
        {
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if (left[y, x] != right[y, x])
                        return false;
                }
            }
            return true;
        }

        public static string ToString(char[,] board)
        {
            string result = "";
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    result += board[y, x];
                }
            }
            return result;
        }
    }
}