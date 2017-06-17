using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace TicTacDumb
{
    public static class Memory
    {
        private static ConcurrentDictionary<string, List<Move>> _memory = new ConcurrentDictionary<string, List<Move>>();

        public static ConcurrentDictionary<string, List<Move>> Instance
        {
            get { return _memory; }
        }

        public static void LoadMemory()
        {
            var memoryText = File.ReadAllText("memory.json");
            _memory = JsonConvert.DeserializeObject<ConcurrentDictionary<string, List<Move>>>(memoryText);
        }

        public static void SaveMemory()
        {
            FlattenMemory();
            var str = JsonConvert.SerializeObject(_memory);
            File.WriteAllText("memory.json", str);
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
                    board = BoardHelpers.FlipBoard(board);
                    numFlips++;
                }

                var flippedMoves = kvp.Value.ConvertAll(m => BoardHelpers.FlipMove(m, numFlips));

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
    }
}