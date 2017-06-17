using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
            public long Score { get; set; }

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

        private static ConcurrentDictionary<string, List<Move>> _memory = new ConcurrentDictionary<string, List<Move>>();
        private static Random _rand = new Random();

        static void Main(string[] args)
        {
            LoadMemory();
            //Train();
            PlayFromMemory();
        }

        private static void PlayFromMemory()
        {
            int totalGames = 1000000;

            int tied = 0;
            int p1 = 0;
            int p2 = 0;

            Parallel.For(0, totalGames, i =>
            {
                var board = NewBoard();

                var history = new List<Move>();

                while (true)
                {
                    NextMove(board, 'x', history, 1);

                    if (HasVictory(board, 'x'))
                    {
                        Interlocked.Increment(ref p1);
                        break;
                    }

                    NextMove(board, 'o', history, 0);

                    if (HasVictory(board, 'o'))
                    {
                        Interlocked.Increment(ref p2);
                        break;
                    }

                    if (MovesLeft(board) == 0)
                    {
                        Interlocked.Increment(ref tied);
                        break;
                    }
                }
            });

            Console.WriteLine($"Player 1 Won: { p1 } times");
            Console.WriteLine($"Player 2 Won: { p2 } times");
            Console.WriteLine($"Tied: { tied } times");
            Console.Read();
        }

        private static void Train()
        {
            int totalGames = 10000000;

            //for(int i=0;i<totalGames;i++)
            Parallel.For(0, totalGames, i =>
            {
                var board = NewBoard();

                var history = new List<Move>();
                var outcome = Outcome.Tied;

                while (true)
                {
                    NextMove(board, 'x', history, 1);

                    if (HasVictory(board, 'x'))
                    {
                        outcome = Outcome.Player1Won;
                        break;
                    }

                    NextMove(board, 'o', history, 1);

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

                RememberMoves(history, outcome);
            });

            SaveMemory();
        }

        private static void FlattenMemory()
        {
            ConcurrentDictionary<string, List<Move>> flat = new ConcurrentDictionary<string, List<Move>>();

            foreach (var kvp in _memory)
            {
                int numFlips = 0;
                string board = kvp.Key;
                for (int f = 0; f < 3; f++)
                {
                    if (flat.ContainsKey(board))
                        break;
                    board = FlipBoard(board);
                    numFlips++;
                }

                var flippedMoves = kvp.Value.ConvertAll(m => FlipMove(m, numFlips));

                if (!flat.ContainsKey(board))
                    flat[board] = new List<Move>();

                foreach (var move in flippedMoves)
                {
                    var found = flat[board].FirstOrDefault(m => m.Equals(move));
                    if (found != null)
                        found.Score += move.Score;
                    else
                        flat[board].Add(move);
                }
            }

            _memory = flat;
        }

        private static void LoadMemory()
        {
            var memoryText = File.ReadAllText("memory.json");
            _memory = JsonConvert.DeserializeObject<ConcurrentDictionary<string, List<Move>>>(memoryText);
        }

        private static void SaveMemory()
        {
            FlattenMemory();
            var str = JsonConvert.SerializeObject(_memory);
            File.WriteAllText("memory.json", str);
        }

        private static void RememberMoves(List<Move> history, Outcome outcome)
        {
            Move prevMove = null;
            bool isPlayer1 = false;
            foreach (var move in history)
            {
                isPlayer1 = !isPlayer1;

                if (!_memory.ContainsKey(move.Board))
                    _memory[move.Board] = new List<Move>();

                var isLastMove = move == history.Last();

                int scoreAlteration = 0;
                if (isLastMove && (outcome == Outcome.Player1Won || outcome == Outcome.Player2Won))
                {
                    scoreAlteration = 1000;

                    if (prevMove != null)
                    {
                        _memory[prevMove.Board].Find(m => m.Equals(prevMove)).Score = -100000;
                        if (_memory[prevMove.Board].Contains(move))
                        {
                            _memory[prevMove.Board].Find(m => m.Equals(move)).Score += scoreAlteration;
                        }
                        else
                        {
                            _memory[prevMove.Board].Add(new Move() { Score = scoreAlteration, X = move.X, Y = move.Y });
                        }
                    }
                }
                else if ((outcome == Outcome.Player1Won && isPlayer1) || (outcome == Outcome.Player2Won && !isPlayer1))
                {
                    scoreAlteration = 2;
                }
                else if (isLastMove && outcome == Outcome.Tied)
                {
                    scoreAlteration = 1;
                }
                else
                {
                    scoreAlteration = 0;
                }

                if (_memory[move.Board].Contains(move))
                {
                    _memory[move.Board].Find(m => m.Equals(move)).Score += scoreAlteration;
                }
                else
                {
                    move.Score = scoreAlteration;
                    _memory[move.Board].Add(move);
                }

                prevMove = move;
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

                for (int x = 0; x < 3; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        if (board[y, x] == '-')
                        {
                            moves.Add(new Move() { X = x, Y = y });
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
            int numFlips = 0;
            var key = ToString(board);
            for (int f = 0; f < 4; f++)
            {
                if (_memory.ContainsKey(key))
                    break;
                key = FlipBoard(key);
                numFlips++;
            }

            var currentMemory = _memory.ContainsKey(key) ? _memory[key] : new List<Move>();

            List<Move> memoryMoves = new List<Move>();
            foreach (var move in validMoves)
            {
                var flip = FlipMove(move, numFlips);

                if (currentMemory.Contains(flip))
                {
                    memoryMoves.Add(currentMemory.Find(m => m.Equals(flip)));
                }
            }

            var maxScore = memoryMoves.Max(m => m.Score);
            var bestMove = memoryMoves.First(m => m.Score == maxScore);

            if (numFlips > 0)
            {
                bestMove = FlipMove(bestMove, 4 - numFlips);
            }

            return bestMove;
        }

        private static int MovesLeft(char[,] board)
        {
            var tmp = ToString(board);
            return tmp.Count(c => c == '-');
        }

        private static bool HasVictory(char[,] board, char player)
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

        private static char[,] NewBoard()
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

        private static Move FlipMove(Move input, int numFlips = 0)
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

        private static string ToString(char[,] board)
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