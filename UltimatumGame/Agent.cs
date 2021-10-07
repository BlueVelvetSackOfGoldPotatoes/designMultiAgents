using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// 50 probability for everyone
// Just current reward for ToM0

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
        ToMn
    };

    class Agent
    {

        #region Agent Variables
        private Random random;
        private double LearningSpeed { get; set; }
        private double Score { get; set; }
        private int Offer { get; set; }
        private List<int> DealsProposed { get; set; }
        private int AcceptanceThreshold { get; set; }
        private bool Attitude { get; set; }
        private List<int> DealsAccepted { get; set; }
        private double UpdateWeight { get; set; }
        private ToMLevel ToMLevel { get; set; }
        private double[] ResponderProbabilityDistribution { get; set; }
        private double[] ProposerProbabilityDistribution { get; set; }
        private double AttitudeValue = 0.05;

        // Shift distribution
        // Attitude affects updates to probability dsitribution

        // ----------------------------------------------------------------------
        // Baseline - ToM 0
        //  If agent 1 is Trustful - Adjust higher offers to have higher likelihoods - willing to do worse for self

        // If agent 1 is Disgusted - Adjust lower offers to have higher likelihoods - not willing to do worse for self

        // -----------------------------------------------------------------------
        // ToM1 - update based on their feelings
        // Agent 1 making offer = trustful (pos) ~> previous deal worked ~ I have ToM1
        // Because im trustful im willing to offer a bit higher for agent 2

        // Agent 2 disgusted = ~> Agent 1 want to offer higher deal
        // Agent 1 see agent 2 attitude: would normally decrease deal but does nothing

        // Agent 2 happy ~~> Increas likelyhood of worst cut because I know he is happy and is more likely to accept worse deal 
        
        // ---------------------------------------------------------------------
        // ToM 2 - update based on both feelings - effectively ToM0
        // Im agent 1 (proposer) and I am (+) - I know agent 2 knows I am (+) therefore knows I am willing to do worse for myself => lower offer
        // 

        // Get the Enum Values
        private ToMLevel[] levels = (ToMLevel[])Enum.GetValues(typeof(ToMLevel));
        #endregion

        #region Constructor Method(s).
        /// <summary>
        /// Constructor Method for the Agent
        /// </summary>
        /// <param name="learningSpeed">The agents learning speed</param>
        /// <param name="TomLevel">The level of ToM the agent has/param>
        public Agent(double learningSpeed, int TomLevel=0)
        {
            // Set values
            random = new Random();
            UpdateWeight = 0.5;
            DealsProposed = new List<int>();
            DealsAccepted = new List<int>();
            LearningSpeed = learningSpeed;
            Attitude = true;

            // Set Thresholds randomly
            Offer = random.Next(0, 101);
            AcceptanceThreshold = random.Next(0, 101);

            // Set Probability Distributions
            ProposerProbabilityDistribution = new double[101];
            ResponderProbabilityDistribution = new double[101];

            Array.Fill(ProposerProbabilityDistribution, 0.5);
            Array.Fill(ResponderProbabilityDistribution, 0.5);


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

        /// <summary>
        /// Compare scores to the list of agents around you
        /// </summary>
        /// <param name="neighbours">a list of the neighbouring agents</param>
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
        /// <summary>
        /// A method that is used to calculate the future reward of offering any offer to the responder
        /// </summary>
        /// <param name="Responder">The responding agent</param>
        /// <param name="DealOffered">the value that the responder will get if the deal is accepted</param>
        /// <returns>a double representing the future rewards</returns>
        public double FutureRewardProposer(Agent Responder, int DealOffered)
        {
            double[] acceptanceRewards = new double[101];
            double[] rejectionRewards = new double[101];

            //List<int> agent2RejectProspects = Responder.GetDealsAccepted();
            //List<int> agent2AcceptProspects = new List<int>();
            //foreach (int i in agent2RejectProspects)
            //    agent2AcceptProspects.Add(i);
            //agent2AcceptProspects.Add(DealOffered);
            double[] ResponderRejectPropsects = FutureAdjustProbabilityDistribution(Responder.GetResponderProbabilityDistribution(), DealOffered, false);
            double[] ResponderAcceptProspects = FutureAdjustProbabilityDistribution(Responder.GetResponderProbabilityDistribution(), DealOffered, true);

            //double[] probabilityDistributionAcceptance = ProbabilityDistributionResponder(agent2AcceptProspects);
            //double[] probabilityDistributionRejection = ProbabilityDistributionResponder(agent2RejectProspects);

            for (int deal = 0; deal < 101; deal++)
            {
                acceptanceRewards[deal] = ResponderAcceptProspects.ElementAt(deal) * (100 - deal);
                rejectionRewards[deal] = ResponderRejectPropsects.ElementAt(deal) * (100 - deal);
            }

            if (acceptanceRewards.Max() > rejectionRewards.Max())
                return acceptanceRewards.Max();
            else
                return rejectionRewards.Max();
        }

        // --------------------------
        private double[] ProbabilityDistributionResponder(List<int> DealsAccepted)
        {
            double[] probabilityDistribution = new double[101];

            if (DealsAccepted.Count == 0)
            {
                for (int i = 0; i < 51; i++)
                    probabilityDistribution[i] = i * 2;
                for (int i = 51; i < 101; i++)
                    probabilityDistribution[i] = 100;
            }
            else
            {
                int lowest = DealsAccepted.Min();
                for (int i = lowest; i < 101; i++)
                    probabilityDistribution[i] = 100;
                for (int i = 0; i < lowest; i++)
                    probabilityDistribution[i] = (i / lowest) * 100;
            }

            return probabilityDistribution.Select(x => x / 100).ToArray();
        }

        /// <summary>
        /// A function that decides the best offer for the proposer to make based on the responder
        /// </summary>
        /// <param name="responder">The responder agent</param>
        /// <returns>the amount to give to the responder</returns>
        public int DecideBestOffer(Agent responder)
        {
            double[] rewards = new double[101];
            double futureRewards;
            double[] probabilityDistribution = responder.GetResponderProbabilityDistribution();
            // ProbabilityDistributionResponder(responder.GetDealsAccepted());

            for (int dealVal = 0; dealVal < 101; dealVal++)
            {
                futureRewards = FutureRewardProposer(responder, dealVal);
                rewards[dealVal] = probabilityDistribution[dealVal] * (100-dealVal + futureRewards);
            }

            return Array.IndexOf(rewards, rewards.Max());
        }

        // ---------------------- Functions needed for deciding action of responder ---------------------- 
        /// <summary>
        /// Calculate the value to be gained by Responder for accepting
        /// </summary>
        /// <param name="Proposer">The proposing agent</param>
        /// <param name="DealOffered">the value to be gained for the responder</param>
        /// <returns>a double representing the value to be gained</returns>
        public double FutureRewardAcceptResponder(Agent Proposer, int DealOffered)
        {
            double[] Rewards = new double[101];

            //List<int> agent1Prospects = Proposer.GetDealsProposed();
            double[] Agent1Prospects = Proposer.GetProposerProbabilityDistribution();

            //agent1Prospects.Add(DealOffered);
            Agent1Prospects = FutureAdjustProbabilityDistribution(Agent1Prospects, DealOffered, true);
            
            //double[] probabilityDistribution = ProbabilityDistributionProposer(agent1Prospects);

            for (int deal = 0; deal < 101; deal++)
            {
                //Rewards[deal] = probabilityDistribution.ElementAt(deal) * deal;
                Rewards[deal] = Agent1Prospects[deal] * deal;
            }

            return Array.IndexOf(Rewards, Rewards.Max());
        }

        /// <summary>
        /// Calculate the value to be gained by Responder for rejecting
        /// </summary>
        /// <param name="Proposer">The proposing agent</param>
        /// <param name="DealOffered">the value to be gained for the responder</param>
        /// <returns>a double representing the value to be gained</returns>
        public double FutureRewardRejectResponder(Agent Proposer, int DealOffered)
        {
            double[] Rewards = new double[101];

            //List<int> agent1Prospects = Proposer.GetDealsProposed();
            double[] Agent1Prospects = Proposer.GetProposerProbabilityDistribution();

            //agent1Prospects.Add(DealOffered);
            Agent1Prospects = FutureAdjustProbabilityDistribution(Agent1Prospects, DealOffered, false);

            //double[] probabilityDistribution = ProbabilityDistributionProposer(agent1Prospects);

            for (int deal = 0; deal < 101; deal++)
            {
                //Rewards[deal] = probabilityDistribution.ElementAt(deal) * deal;
                Rewards[deal] = Agent1Prospects[deal] * deal;
            }

            return Array.IndexOf(Rewards, Rewards.Max());
        }

        // --------------------------
        public double[] ProbabilityDistributionProposer(List<int> DealsProposed)
        {
            double[] probabilityDistribution = new double[101];
            // It will be difficult to come up with a good function for this
            if (DealsProposed.Count == 0)
            {
                for (int i = 0; i < 51; i++)
                    probabilityDistribution[i] = 100 - ((double)i * 2);
                for (int i = 51; i < 101; i++)
                    probabilityDistribution[i] = 0;
            }
            else
            {
                int dealValue = DealsProposed.Min();
                for (int i = 0; i < dealValue; i++)
                    probabilityDistribution[i] = 100 - ((double)i / dealValue) * 100;
                for (int i = dealValue + 1; i < 101; i++)
                    probabilityDistribution[i] = 0;
            }
            return probabilityDistribution.Select(x => x / 100).ToArray();
        }

        /// <summary>
        /// Calculate whether to accept or reject a deal based on the proposer
        /// </summary>
        /// <param name="Proposer">The proposing agent</param>
        /// <param name="DealOffered">the value the responder stands to gain</param>
        /// <returns>a boolean representing if the  offer has been accepted or rejected</returns>
        public bool DecideRejectAccept(Agent Proposer, int DealOffered)
        {
            Console.WriteLine("Offer: "+DealOffered);

            // Calculate the reward to be gained by accepting and rejecting
            double futureRewardsAccept = FutureRewardAcceptResponder(Proposer, DealOffered);
            double futureRewardsReject = FutureRewardRejectResponder(Proposer, DealOffered);

            double AcceptOffer = DealOffered + futureRewardsAccept;
            double RejectOffer = futureRewardsReject;

            if (AcceptOffer > RejectOffer)
            // Accept the offer
            {
                Console.WriteLine("I Accepted");
                return true;
            }
            else
            // Reject the offer
            {
                Console.WriteLine("I Rejected");
                return false;
            }
        }

        public void AddDealAccepted(int deal)
        {
            DealsAccepted.Add(deal);
        }

        public void AddDealProposed(int deal)
        {
            DealsProposed.Add(deal);
        }
        #endregion

        #region Auxilliary Methods
        /// <summary>
        /// Method used to model the change done to a probability distribution for future rewards tracking
        /// </summary>
        /// <param name="Distribution">The probability distribution to model</param>
        /// <param name="index">The index to change - and centre point of bell curve change</param>
        /// <param name="PosOrNeg">Whether the change is positive or negative</param>
        /// <returns>An adjusted distribution</returns>
        private double[] FutureAdjustProbabilityDistribution(double[] Distribution, int index, bool PosOrNeg)
        {
            // Make a copy of the distribution to not chnage the original one
            double[] tmp = Distribution.Select(a => a).ToArray();

            // Edit value at index by a factor adjusted for the learning speeed
            if (PosOrNeg)
                tmp[index] = tmp[index] * (1 + this.LearningSpeed);
            else
                tmp[index] = tmp[index] * (1 - this.LearningSpeed);

            // calculate a fractional learning speed for the 20 values surrounding the index
            double fractionalLearningSpeed = this.LearningSpeed / 10;
            double bellCurveUpdateVal = fractionalLearningSpeed;

            // implement the fractional updates to each value
            for (int i = (index - 10); i < index; i++)
            {
                try
                {
                    if (PosOrNeg)
                        tmp[i] = (double)tmp[i] * (1 + bellCurveUpdateVal);
                    else
                        tmp[i] = (double)tmp[i] * (1 - bellCurveUpdateVal);
                }
                catch { }

                try
                {
                    if (PosOrNeg)
                        tmp[index - i] = (double)tmp[index - i] * (1 + bellCurveUpdateVal);
                    else
                        tmp[index - i] = (double)tmp[index - i] * (1 - bellCurveUpdateVal);
                }
                catch { }

                bellCurveUpdateVal += fractionalLearningSpeed;
            }

            NormalizeDistribution(tmp);

            return tmp;
        }

        /// <summary>
        /// Method used to change a probability distribution for future rewards tracking
        /// </summary>
        /// <param name="ResponderOrProposer">boolean representing whether it is the responder or proposer distribution of the agent</param>
        /// <param name="index">The index to change - and centre point of bell curve change</param>
        /// <param name="PosOrNeg">Whether the change is positive or negative</param>
        public void AdjustProbabilityDistribution(bool ResponderOrProposer, int index, bool PosOrNeg)
        {
            // Edit value at index by a factor adjusted for the learning speeed
            if (ResponderOrProposer)
            {
                if (PosOrNeg)
                    this.ResponderProbabilityDistribution[index] = (double) this.ResponderProbabilityDistribution[index] * (1 + this.LearningSpeed);
                else
                    this.ResponderProbabilityDistribution[index] = (double) this.ResponderProbabilityDistribution[index] * (1 - this.LearningSpeed);
            }
            else
            {
                if (PosOrNeg)
                   this.ProposerProbabilityDistribution[index] = (double) this.ProposerProbabilityDistribution[index] * (1 + this.LearningSpeed);
                else
                    this.ProposerProbabilityDistribution[index] = (double) this.ProposerProbabilityDistribution[index] * (1 - this.LearningSpeed);
            }

            // calculate a fractional learning speed for the 20 values surrounding the index
            double fractionalLearningSpeed = this.LearningSpeed / 10;
            double bellCurveUpdateVal = fractionalLearningSpeed;

            // implement the fractional updates to each value
            for (int i = index-10; i < index; i++)
            {       
                try
                {
                    if (ResponderOrProposer)
                    {
                        if (PosOrNeg)
                            this.ResponderProbabilityDistribution[i] = (double)this.ResponderProbabilityDistribution[i] * (1 + bellCurveUpdateVal);
                        else
                            this.ResponderProbabilityDistribution[i] = (double)this.ResponderProbabilityDistribution[i] * (1 - bellCurveUpdateVal);
                    }
                    else
                    {
                        if (PosOrNeg)
                            this.ProposerProbabilityDistribution[i] = (double)this.ProposerProbabilityDistribution[i] * (1 + bellCurveUpdateVal);
                        else
                            this.ProposerProbabilityDistribution[i] = (double)this.ProposerProbabilityDistribution[i] * (1 - bellCurveUpdateVal);
                    }
                }
                catch { }

                try
                {
                    if (ResponderOrProposer)
                    {
                        if (PosOrNeg)
                            this.ResponderProbabilityDistribution[index - i] = (double)this.ResponderProbabilityDistribution[index - i] * (1 + bellCurveUpdateVal);
                        else
                            this.ResponderProbabilityDistribution[index - i] = (double)this.ResponderProbabilityDistribution[index - i] * (1 - bellCurveUpdateVal);
                    }
                    else
                    {
                        if (PosOrNeg)
                            this.ProposerProbabilityDistribution[index - i] = (double)this.ProposerProbabilityDistribution[index - i] * (1 + bellCurveUpdateVal);
                        else
                            this.ProposerProbabilityDistribution[index - i] = (double)this.ProposerProbabilityDistribution[index - i] * (1 - bellCurveUpdateVal);
                    }
                }
                catch { }

                bellCurveUpdateVal += fractionalLearningSpeed;
            }
            if (PosOrNeg)
                NormalizeDistribution(this.ProposerProbabilityDistribution);
            else
                NormalizeDistribution(this.ResponderProbabilityDistribution)
        }

        private void NormalizeDistribution(double[] distribution)
        {
            distribution.Select(x => (double)((x - distribution.Min()) / (distribution.Max() - distribution.Min())));
        }

        private void AdjustDistribution(double[] distribution, bool PosOrNeg)
        {
            if (PosOrNeg)
                distribution.Select(x => (double) (x + AttitudeValue));
            else
                distribution.Select(x => (double) (x - AttitudeValue));

            NormalizeDistribution(distribution);
        }
        #endregion

        #region Getter Methods
        public string toString()
        {
            return "My ToM level is: "+ToMLevel;
        }
        
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

        public double[] GetResponderProbabilityDistribution()
        {
            return ResponderProbabilityDistribution;
        }

        public double[] GetProposerProbabilityDistribution()
        {
            return ProposerProbabilityDistribution;
        }

        public bool GetAttitude()
        {
            return Attitude;
        }
        #endregion

    }
}
