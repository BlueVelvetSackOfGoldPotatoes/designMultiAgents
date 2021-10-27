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
        private Agent Agent1 { get; set; }
        private Agent Agent2 { get; set; }
        #endregion

        #region Constructor Methods
        public UltimatumGame(int WorldLength, int iterations) // Other variables to determine how much ToM levels occur should go here
        {
            WorldX = WorldLength;
            WorldY = WorldLength;
            Iterations = iterations;
            World = new Agent[WorldX][];

            for (int i = 0; i < WorldX; i++)
                World[i] = new Agent[WorldY];

            InitializeGridWorld();        
        }

        /// <summary>
        /// Basic ultimatum game implementation 
        /// </summary>
        /// <param name="ToMLevelAgent1">The ToM Level of agent 1</param>
        /// <param name="ToMLevelAgent2">The ToM Level of agent 2</param>
        /// <param name="Agent1LearningRate">The learning rate of agent 1</param>
        /// <param name="Agent2LearningRate">The learning rate of agent 1</param>
        /// <param name="iterations">The number of iterations</param>
        public UltimatumGame(int ToMLevelAgent1, int ToMLevelAgent2, double Agent1LearningRate, double Agent2LearningRate, int iterations)
        {
            Agent1 = new Agent(Agent1LearningRate,ToMLevelAgent1);
            Agent2 = new Agent(Agent2LearningRate,ToMLevelAgent2);
            Iterations = iterations;
        }
        #endregion

        #region Functional Methods
        /// <summary>
        /// Method that iniitlizes all the plots as Agents
        /// </summary>
        private void InitializeGridWorld()
        {
            for (int i = 0; i < WorldX; i++)
            {
                for (int j = 0; j < WorldY; j++)
                {
                    // This part will change once ToM is implemented
                    World[i][j] = new Agent(0.1);
                }
            }
        }

        /// <summary>
        /// Basic ToM Game to test functionality
        /// </summary>
        /// <param name="agent1">Agent 1</param>
        /// <param name="agent2">Agent 2</param>
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

        /// <summary>
        /// UG Game as played by TOM Agents
        /// </summary>
        /// <param name="agent1"></param>
        /// <param name="agent2"></param>
        private void UltimatumGameToM0(Agent agent1, Agent agent2)
        {
            int offer = agent1.DecideBestOffer(agent2);
            Console.WriteLine("Agent 1 Attitude:" + agent1.GetAttitudeValue());
            bool result = agent2.DecideRejectAccept(agent1, offer);
            Console.WriteLine("Agent 2 Attitude:" + agent2.GetAttitudeValue());
            double ToMScore;
            agent1.AddDealProposed(offer);
            if (result)
            {
                
                agent1.SetAttitude(true);
                agent1.AddDealProposedAccepted(offer);

                agent1.AdjustScore(100 - offer);
                agent2.AdjustScore(offer);

                ToMScore = agent1.GetChangeVal();

                if (ToMScore == 0.0)
                        ToMScore = 1.0;



                // Update Prob Dist for accepting or rejecting agent
                agent1.AdjustProbabilityDistribution(false, offer, true, ToMScore);
                agent2.AdjustProbabilityDistribution(true, offer, true);
            }
            else
            {
                ToMScore = agent1.GetChangeVal();
                if (ToMScore == 0.0)
                    ToMScore = 1.0;

                agent1.AddDealPropsedRejected(offer);
                agent1.AdjustProbabilityDistribution(false, offer, false, ToMScore);
                agent1.SetAttitude(false);
                agent2.AdjustProbabilityDistribution(true, offer, false);
            }
        }

        /// <summary>
        /// Run one iteration of the UG for all agents in the worldspace
        /// </summary>
        public void TickGridWorld()
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

        /// <summary>
        /// Play the grid world for the number of iterations
        /// </summary>
        public void Play()
        {
            for (int i = 0; i < Iterations; i++)
            {
                Console.WriteLine("--------------------Iteration " + (i + 1) + "--------------------");
                TickGridWorld();
            }
        }

        /// <summary>
        /// TIck for the single agent game of UG
        /// </summary>
        public void TickSingle()
        {
            UltimatumGameToM0(Agent1, Agent2);
            UltimatumGameToM0(Agent2, Agent1);

            Console.WriteLine("\t\t| Agent 1 \t| Agent 2");
            Console.WriteLine("Score\t\t| " + Agent1.GetScore() + " \t\t| " + Agent2.GetScore());

        }

        /// <summary>
        /// Play the single agent game
        /// </summary>
        public void PlaySingle()
        {
            for (int i = 0; i < Iterations; i++)
            {
                Console.WriteLine("--------------------Iteration " + (i + 1) + "--------------------");
                TickSingle();
            }
            Console.Write("----------------------------End of Game -----------------------------\n");
            Console.WriteLine("\t\t| Agent 1 \t| Agent 2");
            Console.WriteLine("Average Score\t\t| " + Agent1.GetScore()/Iterations + " \t\t| " + Agent2.GetScore()/Iterations);

            Console.WriteLine("Offers Proposed -----------------------------------------------------------------");
            Console.WriteLine("Max. Offer Proposed\t\t\t " + Agent1.GetDealsProposed().Max() + " \t\t| " + Agent2.GetDealsProposed().Max());
            Console.WriteLine("Av. Offer Proposed\t\t\t " + Agent1.GetDealsProposed().Sum()/Iterations + " \t\t| " + Agent2.GetDealsProposed().Sum()/Iterations);
            Console.WriteLine("Min. Offer Proposed\t\t\t " + Agent1.GetDealsProposed().Min() + " \t\t| " + Agent2.GetDealsProposed().Min());

            Console.WriteLine("Offers Proposed Accepted-----------------------------------------------------------------");
            Console.WriteLine("Max. Offer Proposed Acc.\t\t " + Agent1.GetDealsProposedAccepted().Max() + " \t\t| " + Agent2.GetDealsProposedAccepted().Max());
            Console.WriteLine("Av. Offer Proposed Acc.\t\t\t " + Agent1.GetDealsProposedAccepted().Sum() / Iterations + " \t\t| " + Agent2.GetDealsProposedAccepted().Sum() / Iterations);
            Console.WriteLine("Min. Offer Proposed Acc.\t\t " + Agent1.GetDealsProposedAccepted().Min() + " \t\t| " + Agent2.GetDealsProposedAccepted().Min());

            Console.WriteLine("Offers Proposed Rejected-----------------------------------------------------------------");
            Console.WriteLine("Max. Offer Proposed Rej.\t\t " + Agent1.GetDealsProposedRejected().Max() + " \t\t| " + Agent2.GetDealsProposedRejected().Max());
            Console.WriteLine("Av. Offer Proposed Rej.\t\t\t " + Agent1.GetDealsProposedRejected().Sum() / Iterations + " \t\t| " + Agent2.GetDealsProposedRejected().Sum() / Iterations);
            Console.WriteLine("Min. Offer Proposed Rej.\t\t " + Agent1.GetDealsProposedRejected().Min() + " \t\t| " + Agent2.GetDealsProposedRejected().Min());

        }
        #endregion
    }
}
