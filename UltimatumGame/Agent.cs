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
        ToM4,
        ToM5,
        ToM6
    };

    /// <summary>
    /// Class for the Agent that will be playing the UG - all UG methods and ToM Implementation details in this class
    /// </summary>
    class Agent
    {
        private int NumToMLevels = 7;
        private Random random;
        #region Agent Variables
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
        private double ChangeValue = 0.0;
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

        #region Proposer Methods
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

            double[] ResponderRejectPropsects = FutureAdjustProbabilityDistribution(Responder.GetResponderProbabilityDistribution(), DealOffered, false);
            double[] ResponderAcceptProspects = FutureAdjustProbabilityDistribution(Responder.GetResponderProbabilityDistribution(), DealOffered, true);

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

            double ChangeDirection = DetermineChange((int)this.ToMLevel, responder);

            this.AttitudeValue += ChangeDirection;

            if (this.AttitudeValue > 0)
                AdjustDistribution(probabilityDistribution, true, responder);
            else if (this.AttitudeValue < 0)
                AdjustDistribution(probabilityDistribution, false, responder); 

            for (int dealVal = 0; dealVal < 101; dealVal++)
            {
                futureRewards = FutureRewardProposer(responder, dealVal);
                rewards[dealVal] = probabilityDistribution[dealVal] * (100-dealVal + futureRewards);
            }

            return Array.IndexOf(rewards, rewards.Max());
        }
        #endregion

        #region Responder Methods
        /// <summary>
        /// Calculate the value to be gained by Responder for accepting
        /// </summary>
        /// <param name="Proposer">The proposing agent</param>
        /// <param name="DealOffered">the value to be gained for the responder</param>
        /// <returns>a double representing the value to be gained</returns>
        public double FutureRewardAcceptResponder(Agent Proposer, int DealOffered)
        {
            double[] Rewards = new double[101];

            double[] Agent1Prospects = Proposer.GetProposerProbabilityDistribution();

            Agent1Prospects = FutureAdjustProbabilityDistribution(Agent1Prospects, DealOffered, true);
            

            for (int deal = 0; deal < 101; deal++)
            {
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

            double[] Agent1Prospects = Proposer.GetProposerProbabilityDistribution();

            Agent1Prospects = FutureAdjustProbabilityDistribution(Agent1Prospects, DealOffered, false);

            for (int deal = 0; deal < 101; deal++)
            {
                Rewards[deal] = Agent1Prospects[deal] * deal;
            }

            return Array.IndexOf(Rewards, Rewards.Max());
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
        #endregion
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
        /// Method used to change a probability distribution after deals have been made
        /// </summary>
        /// <param name="ResponderOrProposer">boolean representing whether it is the responder or proposer distribution of the agent</param>
        /// <param name="index">The index to change - and centre point of bell curve change</param>
        /// <param name="PosOrNeg">Whether the change is positive or negative</param>
        public void AdjustProbabilityDistribution(bool ResponderOrProposer, int index,bool PosOrNeg, double ToMScore=1.0)
        {
            // Edit value at index by a factor adjusted for the learning speeed
            if (ResponderOrProposer)
            {
                // this.ResponderProbabilityDistribution[index] = (double) this.ResponderProbabilityDistribution[index] * (1 - this.LearningSpeed);
                if (PosOrNeg)
                    this.ResponderProbabilityDistribution[index] = (double) this.ResponderProbabilityDistribution[index] * (1 + (this.LearningSpeed * Math.Abs(ToMScore)));
                else
                    this.ResponderProbabilityDistribution[index] = (double) this.ResponderProbabilityDistribution[index] * (1 - (this.LearningSpeed * Math.Abs(ToMScore)));
            }
            else
            {
                if (PosOrNeg)
                   this.ProposerProbabilityDistribution[index] = (double) this.ProposerProbabilityDistribution[index] * (1 + (this.LearningSpeed * Math.Abs(ToMScore)));
                else
                    this.ProposerProbabilityDistribution[index] = (double) this.ProposerProbabilityDistribution[index] * (1 - (this.LearningSpeed * Math.Abs(ToMScore)));
            }

            // calculate a fractional learning speed for the 20 values surrounding the index
            //double fractionalLearningSpeed = this.LearningSpeed / 10;
            double fractionalLearningSpeed = (this.LearningSpeed * Math.Abs(ToMScore)) / 10;
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

            if (ResponderOrProposer)
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

        /// <summary>
        /// Method used to normalize distributions for when values get above 1 or below 0
        /// </summary>
        /// <param name="distribution">The distribution ot change</param>
        private void NormalizeDistribution(double[] distribution)
        {
            distribution.Select(x => (double)((x - distribution.Min()) / (distribution.Max() - distribution.Min())));
        }

        /// <summary>
        /// Method used to change a probability distribution for future rewards tracking
        /// </summary>
        /// <param name="distribution">the distribution to change</param>
        /// <param name="PosOrNeg">Whether the change is positive or negative</param>
        /// <param name="Responder">The responding agent</param>
        private void AdjustDistribution(double[] distribution, bool PosOrNeg, Agent Responder)
        {
            // Adjust above and down from last accepted offer
            int val = Responder.GetLastAccepted();

            if (PosOrNeg)
            {
                for (int i = val; i < 101; i++)
                    distribution[i] += (this.AttitudeValue * this.LearningSpeed);
            }
            else
            {
                for (int i = 0; i < val; i++)
                    distribution[i] -= (Math.Abs(this.AttitudeValue) * this.LearningSpeed);
            }

            if (distribution.Max() > 1.0 || distribution.Min() < 0.0)
            {
                NormalizeDistribution(distribution);
            }    
        }
        #endregion

        #region Theory of Mind Methods
        /// <summary>
        /// Method that takes in the ToM level of the proposer and the responder and returns a double representing the change to attitude of proposer
        /// </summary>
        /// <param name="ToMLevelProposer">ToM level of the proposer</param>
        /// <param name="Responder">The responding agent</param>
        /// <returns>a double that is the overall change made to attitude</returns>
        private double DetermineChange(int ToMLevelProposer, Agent Responder)
        {
            int ans = DetermineChangeRecursive(ToMLevelProposer, (this, Responder));

            int SumToZero = 0;
            for (int i = ToMLevelProposer; i > 0; i--)
                SumToZero += i;

            double scaledAns = ans;
            if (SumToZero != 0)
                scaledAns = (double) ans / (double) SumToZero;
            
            this.ChangeValue = scaledAns;

            return scaledAns;
        }

        /// <summary>
        /// The recursive method that actually generates the change value based on ToM level and moods of both agents
        /// </summary>
        /// <param name="ToMLevelProposer">The ToM level of the proposer</param>
        /// <param name="AgentTuple">A tuple with proposer and responder</param>
        /// <returns>an int that represents the overall attitude value change</returns>
        private int DetermineChangeRecursive(int ToMLevelProposer, (Agent, Agent) AgentTuple)
        {
            bool Agent1Attitude = AgentTuple.Item1.GetAttitude();
            bool Agent2Attitude = AgentTuple.Item2.GetAttitude();

            // Switch values
            AgentTuple = (AgentTuple.Item2, AgentTuple.Item1);

            if (ToMLevelProposer == 0)
            {
                if (Agent1Attitude) return 1;
                else return -1;
            }
            else
            {
                if (ToMLevelProposer % 2 == 0)
                {
                    if (Agent1Attitude) return ToMLevelProposer + DetermineChangeRecursive(ToMLevelProposer - 1, AgentTuple);
                    else return -ToMLevelProposer + DetermineChangeRecursive(ToMLevelProposer - 1, AgentTuple);
                }
                else
                {
                    if (Agent2Attitude) return -ToMLevelProposer + DetermineChangeRecursive(ToMLevelProposer - 1, AgentTuple);
                    else return ToMLevelProposer + DetermineChangeRecursive(ToMLevelProposer - 1, AgentTuple);
                }
            }
        }
        #endregion

        #region Setter Methods
        public void SetAttitude(bool Attitude)
        {
            this.Attitude = Attitude;
        }

        public void AdjustScore(double val)
        {
            Score += val;
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

        public double GetChangeVal()
        {
            return ChangeValue;
        }

        public double GetAttitudeValue()
        {
            return AttitudeValue;
        }
        #endregion

    }
}
