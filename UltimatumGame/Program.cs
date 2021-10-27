using System;

namespace UltimatumGame
{
    class Program
    {
        static void Main(string[] args)
        {
            // Make the game run
            UltimatumGame UG = new UltimatumGame(6, 2, 0.5, 0.5, 1000);
            UG.PlaySingle();
        }
    }
}
