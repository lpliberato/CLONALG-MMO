using System.Collections.Generic;

namespace AIS.ClonalgPR.Models
{
    public class Result
    {
        public List<int> MaximumIterations { get; set; } = new List<int>();
        public List<double> Average { get; set; } = new List<double>();
        public List<double> StandardDeviation { get; set; } = new List<double>();
        public List<double> Variance { get; set; } = new List<double>();
        public List<double> GreaterAffinity { get; set; } = new List<double>();
        public List<double> PercentHighAffinity { get; set; } = new List<double>();
        public List<double> PercentLowAffinity { get; set; } = new List<double>();
        public List<double> Time { get; set; } = new List<double>();
    }
}
