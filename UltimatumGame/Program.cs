using System;

namespace UltimatumGame
{
    class Program
    {
        static void Main(string[] args)
        {
            // Make the game run
            UltimatumGame UG = new UltimatumGame(0,2 , 0.6, 0.6, 1000);
            UG.PlaySingle();
        }
    }
}
