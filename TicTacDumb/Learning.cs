using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TicTacDumb
{
    public static class Learning
    {
        private static Random _rand = new Random();

        public static void PlayFromMemory()
        {
            int totalGames = 1000000;

            int tied = 0;
            int p1 = 0;
            int p2 = 0;

            Parallel.For(0, totalGames, i =>
            {
                var board = BoardHelpers.NewBoard();

                var history = new List<Move>();

                while (true)
                {
                    NextMove(board, 'x', history, 1);

                    if (BoardHelpers.HasVictory(board, 'x'))
                    {
                        Interlocked.Increment(ref p1);
                        break;
                    }

                    NextMove(board, 'o', history, 0);

                    if (BoardHelpers.HasVictory(board, 'o'))
                    {
                        Interlocked.Increment(ref p2);
                        break;
                    }

                    if (BoardHelpers.MovesLeft(board) == 0)
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

        public static void Think(char[,] board, char player)
        {
            NextMove(board, player);
        }

        public static void Train()
        {
            int totalGames = 10000000;

            //for(int i=0;i<totalGames;i++)
            Parallel.For(0, totalGames, i =>
            {
                var board = BoardHelpers.NewBoard();

                var history = new List<Move>();
                var outcome = Outcome.Tied;

                while (true)
                {
                    NextMove(board, 'x', history, 0);

                    if (BoardHelpers.HasVictory(board, 'x'))
                    {
                        outcome = Outcome.Player1Won;
                        break;
                    }

                    NextMove(board, 'o', history, 1);

                    if (BoardHelpers.HasVictory(board, 'o'))
                    {
                        outcome = Outcome.Player2Won;
                        break;
                    }

                    if (BoardHelpers.MovesLeft(board) == 0)
                    {
                        outcome = Outcome.Tied;
                        break;
                    }
                }

                RememberMoves(history, outcome);
            });

            Memory.SaveMemory();
        }

        private static void RememberMoves(List<Move> history, Outcome outcome)
        {
            Move prevMove = null;
            bool isPlayer1 = false;
            foreach (var move in history)
            {
                isPlayer1 = !isPlayer1;

                if (!Memory.Instance.ContainsKey(move.Board))
                    Memory.Instance[move.Board] = new List<Move>();

                var isLastMove = move == history.Last();

                int scoreAlteration = 0;
                if (isLastMove && (outcome == Outcome.Player1Won || outcome == Outcome.Player2Won))
                {
                    scoreAlteration = 1000;

                    if (prevMove != null)
                    {
                        Memory.Instance[prevMove.Board].Find(m => m.Equals(prevMove)).Score = -100000;
                        if (Memory.Instance[prevMove.Board].Contains(move))
                        {
                            Memory.Instance[prevMove.Board].Find(m => m.Equals(move)).Score += scoreAlteration;
                        }
                        else
                        {
                            Memory.Instance[prevMove.Board].Add(new Move() { Score = scoreAlteration, X = move.X, Y = move.Y });
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

                if (Memory.Instance[move.Board].Contains(move))
                {
                    Memory.Instance[move.Board].Find(m => m.Equals(move)).Score += scoreAlteration;
                }
                else
                {
                    move.Score = scoreAlteration;
                    Memory.Instance[move.Board].Add(move);
                }

                prevMove = move;
            }
        }

        private static void NextMove(char[,] board, char player, List<Move> history = null, double trainingPercentage = 0)
        {
            if (BoardHelpers.MovesLeft(board) > 0)
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
                    nextMove.Board = BoardHelpers.ToString(board);
                    history.Add(nextMove);
                }
                board[nextMove.Y, nextMove.X] = player;
            }
        }

        private static Move BestMove(char[,] board, List<Move> validMoves)
        {
            int numFlips = 0;
            var key = BoardHelpers.ToString(board);
            for (int f = 0; f < 4; f++)
            {
                if (Memory.Instance.ContainsKey(key))
                    break;
                key = BoardHelpers.FlipBoard(key);
                numFlips++;
            }

            var currentMemory = Memory.Instance.ContainsKey(key) ? Memory.Instance[key] : new List<Move>();

            List<Move> memoryMoves = new List<Move>();
            foreach (var move in validMoves)
            {
                var flip = BoardHelpers.FlipMove(move, numFlips);

                if (currentMemory.Contains(flip))
                {
                    memoryMoves.Add(currentMemory.Find(m => m.Equals(flip)));
                }
            }

            var bestMove = memoryMoves.OrderByDescending(m => m.Score).First();

            if (numFlips > 0)
            {
                bestMove = BoardHelpers.FlipMove(bestMove, 4 - numFlips);
            }

            return bestMove;
        }
    }
}