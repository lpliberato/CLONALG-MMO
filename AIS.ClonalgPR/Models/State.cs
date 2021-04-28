using System.Collections.Generic;

namespace AIS.ClonalgPR.Models
{
    public class State
    {
        public TransitionEnum Type { get; set; }
        public Dictionary<char, double> Probabilities { get; set; }
        public Dictionary<TransitionEnum, double>Transitions { get; set; }
    }
}
