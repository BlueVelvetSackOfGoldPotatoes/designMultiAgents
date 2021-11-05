using System;

namespace UltimatumGame
{
    class Program
    {
        static void Main(string[] args)
        {
            // Make the game run
            // Parameters in Order: Level of ToM of Agent1; Level of ToM of Agent2; Learning Rate of Agent 1; Learning Rate of Agent 2; # Iterations
            UltimatumGame UG = new UltimatumGame(5,4, 0.5, 0.5, 1000);
            UG.PlaySingle();
        }
    }
}
