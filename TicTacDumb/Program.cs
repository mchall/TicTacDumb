using System;

namespace TicTacDumb
{
    class Program
    {
        static void Main(string[] args)
        {
            Memory.LoadMemory();
            //Learning.Train();
            Learning.PlayFromMemory();
        }
    }
}