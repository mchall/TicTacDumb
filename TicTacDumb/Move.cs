using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicTacDumb
{
    public class Move
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
}