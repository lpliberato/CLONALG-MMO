using System.Collections.Generic;

namespace AIS.ClonalgPR.Models
{
    public class State
    {
        public List<Dictionary<char, double>> Probabilities { get; set; }
        public List<Dictionary<TransitionEnum, double>> Transitions { get; set; }
    }
}
