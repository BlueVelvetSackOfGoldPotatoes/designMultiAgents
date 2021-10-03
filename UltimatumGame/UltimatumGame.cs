using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimatumGame
{
    class UltimatumGame
    {
        #region Game Attributes
        private int WorldX { get; set; }
        private int WorldY { get; set; }
        private int Iterations { get; set; }
        private Agent[][] World { get; set; }
        #endregion

        #region Constructor Methods
        public UltimatumGame(int Worldx, int Worldy, int iterations) // Other variables to determine how much ToM levels occur should go here
        {
            WorldX = Worldx;
            WorldY = Worldy;
            Iterations = iterations;
            World = new Agent[WorldX][];

            for (int i = 0; i < WorldX; i++)
                World[i] = new Agent[WorldY];

            Initialize();        
        }
        #endregion

        #region Functional Methods
        /// <summary>
        /// Method that iniitlizes all the plots as Agents
        /// </summary>
        private void Initialize()
        {
            for (int i = 0; i < WorldX; i++)
            {
                for (int j = 0; j < WorldY; j++)
                {
                    // This part will change once ToM is implemented
                    World[i][j] = new Agent();
                }
            }
        }

        private void UltimatumGameBasic(Agent agent1, Agent agent2)
        {
            int offer = agent1.GetOffer();
            int threshold = agent2.GetAcceptanceThreshold();

            if (offer >= threshold)
            {
                agent1.AdjustScore(100 - offer);
                agent2.AdjustScore(offer);
            }
        }

        public void Tick()
        {
            // Play UG Loop
            for (int i = 0; i < WorldX; i++)
            {
                for (int j = 0; j < WorldY; j++)
                {
                    //Play the game with 4 surrounding agents try/catch for all edge cases
                    List<Agent> Neighbours = new List<Agent>();

                    // Try add all neighbours to a list
                    try
                    {
                        Neighbours.Add(World[i-1][j]);
                    }
                    catch (Exception) { }

                    try
                    {
                        Neighbours.Add(World[i][j-1]);
                    }
                    catch (Exception) { }

                    try
                    {
                        Neighbours.Add(World[i+1][j]);
                    }
                    catch (Exception) { }

                    try
                    {
                        Neighbours.Add(World[i][j+1]);
                    }
                    catch (Exception) { }

                    // Chokepoint of Code - think of fixes - parallelise with lock on grid?
                    foreach (Agent a in Neighbours)
                    {
                        UltimatumGameBasic(World[i][j],a);
                    }
                }
            }

            // Compare Scores Loop
            for (int i = 0; i < WorldX; i++)
            {
                for (int j = 0; j < WorldY; j++)
                {
                    //Play the game with 4 surrounding agents try/catch for all edge cases
                    List<Agent> Neighbours = new List<Agent>();

                    // Try add all neighbours to a list
                    try
                    {
                        Neighbours.Add(World[i - 1][j]);
                    }
                    catch (Exception) { }

                    try
                    {
                        Neighbours.Add(World[i][j - 1]);
                    }
                    catch (Exception) { }

                    try
                    {
                        Neighbours.Add(World[i + 1][j]);
                    }
                    catch (Exception) { }

                    try
                    {
                        Neighbours.Add(World[i][j + 1]);
                    }
                    catch (Exception) { }

                    // Chokepoint of Code - think of fixes - parallelise with lock on grid?
                    World[i][j].CompareScores(Neighbours);
                }
            }

            // Get Average Values
            int num = WorldX * WorldY;
            int ThresholdAverage = 0;
            int OfferAverage = 0;
            double AverageScore = 0;

            for (int i = 0; i < WorldX; i++)
            {
                for (int j = 0; j < WorldY; j++)
                {
                    ThresholdAverage += World[i][j].GetAcceptanceThreshold();
                    OfferAverage += World[i][j].GetOffer();
                    AverageScore += World[i][j].GetScore();
                }
            }

            Console.WriteLine("Average Threshold: "+ThresholdAverage/num);
            Console.WriteLine("Average Offer: " + OfferAverage/num);
            Console.WriteLine("Average Score: " + AverageScore/num);
        }

        public void Play()
        {
            for (int i = 0; i < Iterations; i++)
            {
                Console.WriteLine("--------------------Iteration " + (i + 1) + "--------------------");
                Tick();
            }
        }
        #endregion
    }
}
