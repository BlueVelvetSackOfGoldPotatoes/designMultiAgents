using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// Add some kind of variable that signals the annoyance/goodwill of the other agent. Based on this value worse offers will be rejected/accepted


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
        ToM4,
        ToM5,
        ToM6
    };

    class Agent
    {
        private int NumToMLevels = 7;
        #region Agent Variables
        private Random random;
        private double LearningSpeed { get; set; }
        private double Score { get; set; }
        private int Offer { get; set; }
        private List<int> DealsProposed { get; set; }
        private List<int> DealsProposedAccepted { get; set; }
        private List<int> DealsProposedRejected { get; set; }
        private int AcceptanceThreshold { get; set; }
        private bool Attitude { get; set; }
        private int LastDealAccepted { get; set; }
        private double UpdateWeight { get; set; }
        private ToMLevel ToMLevel { get; set; }
        private double[] ResponderProbabilityDistribution { get; set; }
        private double[] ProposerProbabilityDistribution { get; set; }
        private double AttitudeValue = 0.05;
        private ToMLevel[] levels = (ToMLevel[]) Enum.GetValues(typeof(ToMLevel));
        // Get the Enum Values
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
            DealsProposedAccepted = new List<int>();
            DealsProposedRejected = new List<int>();
            LastDealAccepted = 0;
            LearningSpeed = learningSpeed;
            Attitude = true;

            // Set Thresholds randomly
            Offer = random.Next(0, 101);
            AcceptanceThreshold = random.Next(0, 101);

            // Set Probability Distributions
            ProposerProbabilityDistribution = new double[101];
            ResponderProbabilityDistribution = new double[101];

            Array.Fill(ProposerProbabilityDistribution, 0.4);
            Array.Fill(ResponderProbabilityDistribution, 0.4);


            // Set ToM Level
            try
            {
                // Account for possibility of someone messing with number / picking number higher than levels established
                ToMLevel = levels[TomLevel];
            }
            catch
            {
                // Set ToM level randomly
                ToMLevel = (ToMLevel)random.Next(0, NumToMLevels);
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

            // ------------------------------------------------------ToM Stuff
            int ChangeDirection = DetermineChange((int)this.ToMLevel, responder);

            if (ChangeDirection > 0)
                AdjustDistribution(probabilityDistribution, true, responder);
            else if (ChangeDirection < 0)
                AdjustDistribution(probabilityDistribution, false, responder);
            // ---------------------------------------------------------------

            
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
                LastDealAccepted = DealOffered;
                return true;
            }
            else
            // Reject the offer
            {
                Console.WriteLine("I Rejected");

                return false;
            }
        }

        public void AddDealProposed(int deal)
        {
            DealsProposed.Add(deal);
        }

        public void AddDealProposedAccepted(int deal)
        {
            DealsProposedAccepted.Add(deal);
        }

        public void AddDealPropsedRejected(int deal)
        {
            DealsProposedRejected.Add(deal);
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

            if (tmp.Max() > 1.0 || tmp.Min() < 0.0)
            {
                NormalizeDistribution(tmp);
                //Console.WriteLine("Consider me normalized");
            }
                

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
            {
                if (this.ProposerProbabilityDistribution.Max() > 1.0 || this.ProposerProbabilityDistribution.Min() < 0.0)
                {
                    //Console.WriteLine("Consider me normalized");
                    NormalizeDistribution(this.ProposerProbabilityDistribution);
                }
                    
            }     
            else
                if (this.ResponderProbabilityDistribution.Max() > 1.0 || this.ResponderProbabilityDistribution.Min() < 0.0)
                {
                    //Console.WriteLine("Consider me normalized");
                    NormalizeDistribution(this.ResponderProbabilityDistribution);
                }
                
        }

        private void NormalizeDistribution(double[] distribution)
        {
            distribution.Select(x => (double)((x - distribution.Min()) / (distribution.Max() - distribution.Min())));
        }

        private void AdjustDistribution(double[] distribution, bool PosOrNeg, Agent Responder)
        {
            // Adjust above and down from last accepted offer
            int val = Responder.GetLastAccepted();

            if (PosOrNeg)
            {
                for (int i = val; i < 101; i++)
                    distribution[i] += AttitudeValue;
            }
            else
            {
                for (int i = 0; i < val; i++)
                    distribution[i] -= AttitudeValue;
            }

            if (distribution.Max() > 1.0 || distribution.Min() < 0.0)
            {
                //Console.WriteLine("Consider me normalized");
                NormalizeDistribution(distribution);
            }    
        }

        private int DetermineChange(int ToMLevelProposer, Agent Responder)
        {
            int ans = DetermineChangeRecursive(ToMLevelProposer, this, Responder);

            if (ans < 0) return -1; // increase pr of lower offers
            else if (ans > 0) return 1;
            else return 0;
        }

        private int DetermineChangeRecursive(int ToMLevelProposer, Agent Proposer, Agent Responder)
        {
            bool MyAttitude = Proposer.Attitude;
            bool ResponderAttitude = Responder.GetAttitude();

            if (ToMLevelProposer == 0){
                if (MyAttitude) return 1;
                else return -1;
            }else{
                if (ToMLevelProposer % 2 == 0) {
                    if (MyAttitude) return 1 + DetermineChangeRecursive(ToMLevelProposer - 1, Proposer, Responder);
                    else return -1 + DetermineChangeRecursive(ToMLevelProposer - 1, Proposer, Responder);
                } 
                else {
                    if (ResponderAttitude) return -1 + DetermineChangeRecursive(ToMLevelProposer - 1, Responder, Proposer);
                    else return 1 + DetermineChangeRecursive(ToMLevelProposer - 1, Responder, Proposer);
                }
            }
        }
        #endregion

        #region Setter Methods
        public void SetAttitude(bool Attitude)
        {
            this.Attitude = Attitude;
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

        public List<int> GetDealsProposedAccepted()
        {
            return DealsProposedAccepted;
        }

        public List<int> GetDealsProposedRejected()
        {
            return DealsProposedRejected;
        }

        public int GetLastAccepted()
        {
            return LastDealAccepted;
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
