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
        // ---------------------- Functions needed for deciding action of proposer ---------------------- 
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
        private double[] ProbabilityDistributionResponder(List<int> DealsAccepted)
        {
            double[] probabilityDistribution = new double[101];
            
            if (DealsAccepted.Count == 0)
                for (int i = 0; i < 51; i++)
                    probabilityDistribution[i] = 100;
                for (int i = 51; i < 101; i++)
                    probabilityDistribution[i] = 100 - ((i - 50) * 0.5);
            else
            {
                int lowest = DealsAccepted.Min();
                for (int i = lowest; i <= 101; i++)
                    probabilityDistribution[i] = 100;
                for (int i = lowest; i >= 0; i--)
                    probabilityDistribution[i] = 100 - i / lowest;
            }

            return probabilityDistribution;
        }
        public int DecideBestOffer(Agent responder)
        {
            double[] rewards = new double[101];
            double futureRewards = 0;
            double[] probabilityDistribution = ProbabilityDistributionResponder(responder.GetDealsAccepted());

            for (int dealVal = 0; dealVal < 101; dealVal++)
            {
                futureRewards = FutureRewardProposer(responder, dealVal);
                rewards[dealVal] = probabilityDistribution[dealVal] * (dealVal - 100 + futureRewards);
            }

            return Array.IndexOf(rewards, rewards.Max());
        }
        // ---------------------- Functions needed for deciding action of responder ---------------------- 
        public double FutureRewardAcceptResponder(Agent FutureAgent, int DealOffered)
        {
            double[] Rewards = new double[101];

            List<int> agent1Prospects = FutureAgent.GetDealsProposed();
            agent1Prospects.Add(DealOffered);
            double[] probabilityDistribution = ProbabilityDistributionProposer(agent1Prospects);

            for (int deal = 0; deal < 101; deal++)
            {
                Rewards[deal] = probabilityDistribution.ElementAt(deal) * deal;
            }

            return Array.IndexOf(Rewards, Rewards.Max());
        }
        public double FutureRewardRejectResponder(Agent FutureAgent, int DealOffered)
        {
            double[] Rewards = new double[101];

            List<int> agent1Prospects = FutureAgent.GetDealsProposed();
            agent1Prospects.Add(DealOffered);
            double[] probabilityDistribution = ProbabilityDistributionProposer(agent1Prospects);

            for (int deal = 0; deal < 101; deal++)
            {
                Rewards[deal] = probabilityDistribution.ElementAt(deal) * deal;
            }

            return Array.IndexOf(Rewards, Rewards.Max());
        }
        public double[] ProbabilityDistributionProposer(List<int> DealsProposed)
        {
            double[] probabilityDistribution = new double[101];
            // It will be difficult to come up with a good function for this
            if (DealsProposed.Count == 0)
                for (int i = 0; i < 50; i++)
                    probabilityDistribution[i] = i*2;
                for (int i = 50; i < 101; i++)
                    probabilityDistribution[i] = 100;
            else
            {
                int highest = DealsProposed.Max();
                for (int i = highest; i > 0; i--)
                    probabilityDistribution[i] = i/(highest/100);
                for (int i = highest+1; i < 101; i++)
                    probabilityDistribution[i] = 0;
            }
            return probabilityDistribution;
        }
        public int DecideRejectAccept(Agent Proposer, int DealOffered)
        {
            double futureRewardsAccept = FutureRewardAcceptResponder(responder, DealOffered);
            double futureRewardsReject = FutureRewardRejectResponder(responder, DealOffered);

            double AcceptOffer = DealOffered + futureRewardsAccept;
            double RejectOffer = futureRewardsReject;

            if (AcceptOffer > RejectOffer)
            // Accept the offer
            {
                return 1;
            }
            else
            // Reject the offer
            {
                return 0;
            }
        }

        public void AddDealAccepted()
        {

        }

        public void AddDealRejected()
        {

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
