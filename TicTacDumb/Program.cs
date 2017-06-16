using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicTacDumb
{
    class Program
    {
        enum Outcome
        {
            Tied,
            Player1Won,
            Player2Won
        }

        class Move
        {
            [JsonIgnore]
            public string Board { get; set; }

            public int X { get; set; }
            public int Y { get; set; }
            public int Score { get; set; }

            public override string ToString()
            {
                return $"[{ X }, { Y }] - { Score }";
            }

            public override bool Equals(object obj)
            {
                var other = obj as Move;
                if (other != null && other.X == X && other.Y == Y)
                    return true;
                return false;
            }

            public override int GetHashCode()
            {
                return X ^ Y;
            }
        }

        private static Dictionary<string, List<Move>> _memory = new Dictionary<string, List<Move>>();
        private static Random _rand = new Random();

        static void Main(string[] args)
        {
            //LoadMemory();
            Train();
            //PlayFromMemory();
        }

        private static void LoadMemory()
        {
            var memoryText = File.ReadAllText("memory.json");
            _memory = JsonConvert.DeserializeObject<Dictionary<string, List<Move>>>(memoryText);
        }

        private static void PlayFromMemory()
        {
            int totalGames = 1000000;

            int tied = 0;
            int p1 = 0;
            int p2 = 0;


            for (int i = 0; i < totalGames; i++)
            {
                var board = NewBoard();

                while (true)
                {
                    NextMove(board, 'x', trainingPercentage: 1);

                    if (HasVictory(board, 'x'))
                    {
                        p1++;
                        break;
                    }

                    NextMove(board, 'o', trainingPercentage: 0);

                    if (HasVictory(board, 'o'))
                    {
                        p2++;
                        break;
                    }

                    if (MovesLeft(board) == 0)
                    {
                        tied++;
                        break;
                    }
                }
            }

            Console.WriteLine($"Player 1 Won: { p1 } times");
            Console.WriteLine($"Player 2 Won: { p2 } times");
            Console.WriteLine($"Tied: { tied } times");
            Console.Read();
        }

        private static void Train()
        {
            int totalGames = 10000000;

            for (int i = 0; i < totalGames; i++)
            {
                var board = NewBoard();

                var player1History = new List<Move>();
                var player2History = new List<Move>();
                var outcome = Outcome.Tied;

                while (true)
                {
                    NextMove(board, 'x', player1History, 1);

                    if (HasVictory(board, 'x'))
                    {
                        outcome = Outcome.Player1Won;
                        break;
                    }

                    NextMove(board, 'o', player2History, 1);

                    if (HasVictory(board, 'o'))
                    {
                        outcome = Outcome.Player2Won;
                        break;
                    }

                    if (MovesLeft(board) == 0)
                    {
                        outcome = Outcome.Tied;
                        break;
                    }
                }

                RememberMoves(player1History, outcome, true);
                RememberMoves(player2History, outcome, false);
            }

            SaveMemory();
        }

        private static void SaveMemory()
        {
            var str = JsonConvert.SerializeObject(_memory);
            File.WriteAllText("memory.json", str);
        }

        private static void RememberMoves(List<Move> history, Outcome outcome, bool isPlayer1)
        {
            foreach (var move in history)
            {
                var board = move.Board;
                for (int i = 0; i < 4; i++)
                {
                    if (_memory.ContainsKey(board))
                        break;
                    board = FlipBoard(board);
                }

                if (!_memory.ContainsKey(board))
                    _memory[board] = new List<Move>();

                var isLastMove = move == history.Last();

                int scoreAlteration = 0;
                switch (outcome)
                {
                    case Outcome.Player1Won:
                        scoreAlteration = isPlayer1 ? (isLastMove ? 100 : 2) : (isLastMove ? -100 : 0);
                        break;
                    case Outcome.Player2Won:
                        scoreAlteration = isPlayer1 ? (isLastMove ? -100 : 0) : (isLastMove ? 100 : 2);
                        break;
                    case Outcome.Tied:
                        scoreAlteration = 0;
                        break;
                }

                if (_memory[board].Contains(move))
                {
                    _memory[board].Find(m => m.Equals(move)).Score += scoreAlteration;
                }
                else
                {
                    move.Score = scoreAlteration;
                    _memory[board].Add(move);
                }
            }
        }

        private static void PrintBoard(char[,] board)
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

        private static void NextMove(char[,] board, char player, List<Move> history = null, double trainingPercentage = 0)
        {
            if (MovesLeft(board) > 0)
            {
                List<Move> moves = new List<Move>();

                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (board[i, j] == '-')
                        {
                            moves.Add(new Move() { X = j, Y = i });
                        }
                    }
                }

                Move nextMove;

                var coin = _rand.NextDouble();
                if (coin < trainingPercentage)
                {
                    nextMove = moves[_rand.Next(moves.Count)];
                }
                else
                {
                    nextMove = BestMove(board, moves);
                }


                if (history != null)
                {
                    nextMove.Board = ToString(board);
                    history.Add(nextMove);
                }
                board[nextMove.Y, nextMove.X] = player;
            }
        }

        private static Move BestMove(char[,] board, List<Move> validMoves)
        {
            var key = ToString(board);
            for (int i = 0; i < 4; i++)
            {
                if (_memory.ContainsKey(key))
                    break;
                key = FlipBoard(key);
            }

            var currentMemory = _memory.ContainsKey(key) ? _memory[key] : new List<Move>();

            List<Move> memoryMoves = new List<Move>();
            foreach (var move in validMoves)
            {
                if (currentMemory.Contains(move))
                {
                    memoryMoves.Add(currentMemory.Find(m => m.Equals(move)));
                }
            }

            var maxScore = memoryMoves.Max(m => m.Score);
            return memoryMoves.First(m => m.Score == maxScore);
        }

        private static int MovesLeft(char[,] board)
        {
            var tmp = ToString(board);
            return tmp.Count(c => c == '-');
        }

        private static bool HasVictory(char[,] board, char player)
        {
            var tmpBoard = FlipBoard(board);
            for (int j = 0; j < 4; j++)
            {
                if (tmpBoard[0, 0] == player && tmpBoard[1, 1] == player && tmpBoard[2, 2] == player)
                {
                    return true;
                }

                for (int i = 0; i < 3; i++)
                {
                    if (tmpBoard[i, 0] == player && tmpBoard[i, 1] == player && tmpBoard[i, 2] == player)
                    {
                        return true;
                    }
                }

                tmpBoard = FlipBoard(tmpBoard);
            }

            return false;
        }

        private static char[,] NewBoard()
        {
            var board = new char[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    board[i, j] = '-';
                }
            }
            return board;
        }

        private static char[,] FlipBoard(char[,] input)
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

        private static string FlipBoard(string input)
        {
            string newString = "";
            newString += $"{ input[2] }{ input[5] }{ input[8] }";
            newString += $"{ input[1] }{ input[4] }{ input[7] }";
            newString += $"{ input[0] }{ input[3] }{ input[6] }";
            return newString;
        }

        private static bool AreEqual(char[,] left, char[,] right)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (left[i, j] != right[i, j])
                        return false;
                }
            }
            return true;
        }

        private static string ToString(char[,] board)
        {
            string result = "";
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    result += board[j, i];
                }
            }
            return result;
        }
    }
}