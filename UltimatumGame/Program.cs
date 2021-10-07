using System;

namespace UltimatumGame
{
    class Program
    {
        static void Main(string[] args)
        {
            // Make the game run
            UltimatumGame UG = new UltimatumGame(0, 0, 0.8, 0.4, 1000);
            UG.PlaySingle();
        }
    }
}
