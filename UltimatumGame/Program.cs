using System;

namespace UltimatumGame
{
    class Program
    {
        static void Main(string[] args)
        {
            // Make the game run
            UltimatumGame UG = new UltimatumGame(0, 0, 0.6, 0.6, 10);
            UG.PlaySingle();
        }
    }
}
