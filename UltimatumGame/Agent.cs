using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimatumGame
{
    /// <summary>
    /// Enumerator that each agent will have that will represent their TOM level
    /// </summary>
    public enum ToMLevel
    {
        ToM0,
        ToM1,
        ToM2,
        ToM3,
        ToMn // Might be interesting to see how an nth level ToM goes but can be removed
    };

    class Agent
    {

        #region Agent Variables
        private Random random;
        private double Score { get; set; }
        private int Offer { get; set; }
        private List<int> DealsProposed { get; set; }
        private int AcceptanceThreshold { get; set; }
        private List<int> DealsAccepted { get; set; }
        private double UpdateWeight { get; set; }
        private ToMLevel ToMLevel { get; set; }
        // Get the Enum Values
        private ToMLevel[] levels = (ToMLevel[])Enum.GetValues(typeof(ToMLevel));
        #endregion

        #region Constructor Method(s)
        public Agent(int TomLevel=0)
        {
            // Set values
            random = new Random();
            UpdateWeight = 0.5;
            DealsProposed = new List<int>();
            DealsAccepted = new List<int>();

            // Set Thresholds randomly
            Offer = random.Next(0, 101);
            AcceptanceThreshold = random.Next(0, 101);

            // Set ToM Level
            try
            {
                // Account for possibility of someone messing with number / picking number higher than levels established
                ToMLevel = levels[TomLevel];
            }
            catch
            {
                // Set ToM level randomly
                ToMLevel = (ToMLevel)random.Next(0, 5);
            }
        }
        #endregion

        #region Functional Methods
        public void AdjustScore(double val)
        {
            Score += val;
        }
        public void CompareScores(List<Agent> neighbours)
        {
            Agent highestNeighbour = this;

            foreach (Agent a in neighbours){
                if (a.Score > highestNeighbour.Score)
                    highestNeighbour = a;
            }

            if (highestNeighbour != this)
            {
                this.AcceptanceThreshold =  Convert.ToInt32((1 - this.UpdateWeight) * this.AcceptanceThreshold + (this.UpdateWeight * highestNeighbour.AcceptanceThreshold));

                this.Offer = Convert.ToInt32((1 - this.UpdateWeight) * this.Offer + (this.UpdateWeight * highestNeighbour.Offer));

            }
        }
        public double FutureRewardProposer(Agent FutureAgent, int DealOffered)
        {
            double[] acceptanceRewards = new double[101];
            double[] rejectionRewards = new double[101];

            List<int> agent2RejectProspects = FutureAgent.GetDealsAccepted();
            List<int> agent2AcceptProspects = new List<int>();
            foreach (int i in agent2RejectProspects)
                agent2AcceptProspects.Add(i);

            agent2AcceptProspects.Add(DealOffered);
            double[] probabilityDistributionAcceptance = ProbabilityDistributionResponder(agent2RejectProspects);
            double[] probabilityDistributionRejection = ProbabilityDistributionResponder(agent2AcceptProspects);

            for (int deal = 0; deal < 101; deal++)
            {
                acceptanceRewards[deal] = probabilityDistributionAcceptance.ElementAt(deal) * (deal - 100);
                rejectionRewards[deal] = probabilityDistributionRejection.ElementAt(deal) * (deal - 100);
            }

            if (acceptanceRewards.Max() > rejectionRewards.Min())
                return acceptanceRewards.Max();
            else
                return rejectionRewards.Max();
        }
        public double[] ProbabilityDistributionResponder(List<int> DealsAccepted)
        {
            double[] probabilityDistribution = new double[101];
            
            if (DealsAccepted.Count == 0)
                for (int i = 51; i < 101; i++)
                    probabilityDistribution[i] = 100 - ((i - 50) * 0.5);
            else
            {
                int lowest = DealsAccepted.Min();
                for (int i = lowest; i >= 0; i--)
                    probabilityDistribution[i] = 100 - i / lowest;
            }

            return probabilityDistribution;
        }

        // Implement these
        public int MakeOffer(Agent responder)
        {
            double[] rewards = new double[101];
            double futureRewards = 0;
            double[] probabilityDistribution = ProbabilityDistributionResponder(responder.GetDealsAccepted());

            for (int dealVal = 0.; dealVal < 101; dealVal++)
            {
                futureRewards = FutureRewardProposer(responder, dealVal);
                rewards[dealVal] = probabilityDistribution[dealVal] * (dealVal - 100 + futureRewards);
            }

            return Array.IndexOf(rewards, rewards.Max());
        }

        public bool AcceptOrReject(Agent agent, int offer)
        {
            return true;
        }
        #endregion

        #region Getter Methods
        public string toString()
        {
            return "My ToM level is: "+ToMLevel;
        }
        
        // Represents the value that will be offered to the responder
        public int GetOffer()
        {
            return Offer;
        }

        public int GetAcceptanceThreshold()
        {
            return AcceptanceThreshold;
        }

        public List<int> GetDealsProposed()
        {
            return DealsProposed;
        }

        public List<int> GetDealsAccepted()
        {
            return DealsAccepted;
        }

        public double GetScore()
        {
            return Score;
        }
        #endregion
    }
}
