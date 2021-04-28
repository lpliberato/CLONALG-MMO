using System.Collections.Generic;

namespace AIS.ClonalgPR.Models
{
    public class State
    {
        public StateEnum Type { get; set; }
        public Dictionary<char, double> Probabilities { get; set; }
        public Dictionary<StateEnum, double>Transitions { get; set; }
    }
}
