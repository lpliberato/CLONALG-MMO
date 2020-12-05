using AIS.ClonalgPR.Measures;
using AIS.ClonalgPR.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AIS.ClonalgPR
{
    public class ClonalgPR
    {
        private List<Antibody> _memoryCells = new List<Antibody>();
        private List<int> _antigensListSize = new List<int>();
        private Result _results = new Result();
        private IDistance _distance = null;
        private Stopwatch _watch = new Stopwatch();
        private char[] charactersEscape = new char[4] { '.', '-', 'x', 'X' };
        public Result Results
        {
            get { return _results; }
        }

        public ClonalgPR(IDistance distance)
        {
            this._distance = distance;
        }

        private void Affinity(Antigen antigen, List<Antibody> antibodies)
        {
            for (int i = 0; i < antibodies.Count(); i++)
            {
                var sequenceA = antigen.Sequence.ToCharArray();
                var sequenceB = antibodies[i].Sequence.ToCharArray();
                antibodies[i].Affinity = _distance.Calculate(sequenceA, sequenceB);
            }
        }

        private List<Antibody> Clone(List<Antibody> antibodies)
        {
            var clones = new List<Antibody>();
            foreach (var antibody in antibodies)
            {
                var rate = _distance.CalculateCloneRate(antibody.Affinity, antibody.Length);
                var clonesQtd = (int)(rate * antibody.Length);
                for (int j = 0; j < clonesQtd; j++)
                    clones.Add(antibody.Clone());
            }

            return clones;
        }

        private List<Antibody> Initialize(int qtdAntibody = 0, int sequenceSize = 0)
        {
            var antibodies = new List<Antibody>();

            if (qtdAntibody == 0)
                qtdAntibody = Constants.Random.Next(Constants.qtdMinAntibodies, Constants.qtdMaxAntibodies);

            if (sequenceSize == 0)
                sequenceSize = Constants.Random.Next(Constants.sequenceMinSize, Constants.sequenceMaxSize);

            for (int i = 0; i < qtdAntibody; i++)
            {
                var sequence = "";
                for (int j = 0; j < sequenceSize; j++)
                    sequence += Constants.Aminoacids[Constants.Random.Next(0, Constants.Aminoacids.Length)];

                antibodies.Add(new Antibody
                {
                    Sequence = sequence,
                    Length = sequence.Where(w => !charactersEscape.Contains(w)).Count()
                });
            }

            return antibodies;
        }

        private void Insert(Antigen antigen, List<Antibody> antibodies)
        {
            if (antibodies == null || antibodies.Count() == 0) return;

            var antibody = _distance.Order(antibodies, 1).FirstOrDefault();
            var antigenName = antigen.Name;
            var memoryCell = _memoryCells.Where(w => w.Antigen == antigenName).FirstOrDefault();

            if (memoryCell != null && _distance.IsBetterAffinity(antibody.Affinity, memoryCell.Affinity))
            {
                memoryCell.Affinity = antibody.Affinity;
                memoryCell.Antigen = antigenName;
                memoryCell.Length = antibody.Length;
                memoryCell.Sequence = antibody.Sequence;
            }
            else if (memoryCell == null)
            {
                antibody.Antigen = antigenName;
                _memoryCells.Add(antibody);
            }
        }

        private List<Antibody> Mutation(List<Antibody> antibodies)
        {
            foreach (var antibody in antibodies)
            {
                var rate = _distance.CalculateMutationRate(antibody.Affinity, antibody.Length);
                var mutateQtd = (int)(antibody.Length * rate);
                var sequenceIndex = Constants.Random.Next(0, antibody.Length);
                var sequence = antibody.Sequence.ToCharArray();

                for (int j = 0; j < mutateQtd; j++)
                {
                    var n = sequence[sequenceIndex];
                    var aminoacid = Constants.Aminoacids.Where(w => w != n).FirstOrDefault();
                    sequence[sequenceIndex] = aminoacid;
                    antibody.Sequence = new string(sequence);
                    sequenceIndex = Constants.Random.Next(0, antibody.Length);
                }
            }

            return antibodies;
        }

        private List<Antibody> Replace(List<Antibody> antibodies, int inferiorLimit)
        {
            if (antibodies == null || antibodies.Count() == 0) return antibodies;

            var sequenceSize = antibodies[0].Length;
            var antibodiesReplaced = antibodies.Take(inferiorLimit).ToList();
            antibodiesReplaced.ForEach(antibody => antibodies.Remove(antibody));

            var qtdAntibodiesReplaced = antibodiesReplaced.Count();
            if (qtdAntibodiesReplaced > 0)
            {
                var _antibodies = Initialize(qtdAntibody: qtdAntibodiesReplaced, sequenceSize: sequenceSize);
                _antibodies.ForEach(p => antibodies.Add(p));
            }
            return antibodies;
        }

        private List<Antibody> Select(List<Antibody> population, int numberHighAffinity)
        {
            return _distance.Order(population, numberHighAffinity);
        }

        public void Execute(List<Antigen> antigens, int maximumIterations, int percentHighAffinity, int percentLowAffinity)
        {
            StartTimer();
            var sequenceSize = Constants.Random.Next(10, 20);
            var antibodies = Initialize(sequenceSize: sequenceSize);
            var numberHighAffinity = (int)((double)percentHighAffinity / 100 * antibodies.Count());
            var numberLowAffinity = (int)((double)percentLowAffinity / 100 * antibodies.Count());

            var i = 1;
            while (i <= maximumIterations)
            {
                foreach (var antigen in antigens)
                {
                    Affinity(antigen, antibodies);
                    var selectedPopulation = Select(antibodies, numberHighAffinity);
                    var clonedPopulation = Clone(selectedPopulation);
                    var mutatedPopulation = Mutation(clonedPopulation);
                    Affinity(antigen, mutatedPopulation);
                    selectedPopulation = Select(mutatedPopulation, numberHighAffinity);
                    Insert(antigen, selectedPopulation);
                    antibodies = Replace(selectedPopulation, numberLowAffinity);
                }
                i++;
            }
            StopTimer();
            SetStatistics(maximumIterations, percentHighAffinity, percentLowAffinity);
        }

        private void SetStatistics(int maximumIterations, int percentHighAffinity, int percentLowAffinity)
        {
            var average = Average();
            var variance = Variance();
            var standardDeviation = StandardDeviation(variance);
            var greaterAffinity = GreaterAffinity();
            var time = GetTime();
            PrintResults(maximumIterations, average, variance, standardDeviation, greaterAffinity, time, percentHighAffinity, percentLowAffinity);
            SetResult(average, variance, standardDeviation, maximumIterations, percentHighAffinity, percentLowAffinity, greaterAffinity, time);
        }

        private double Average()
        {
            return _memoryCells.Where(w => !double.IsNaN(w.Affinity) && !double.IsInfinity(w.Affinity)).Select(s => s.Affinity).Average();
        }

        private double Variance()
        {
            var variances = new List<double>();
            var affinities = _memoryCells.Where(w => !double.IsNaN(w.Affinity) && !double.IsInfinity(w.Affinity)).Select(s => s.Affinity).ToList();
            var average = Average();
            var count = affinities.Count();
            affinities.ForEach(affinity => variances.Add(Math.Pow(affinity - average, 2)));
            return variances.Sum() / count;
        }

        private double StandardDeviation(double variance)
        {
            return Math.Sqrt(variance);
        }

        private double GreaterAffinity()
        {
            return _memoryCells.Where(w => !double.IsNaN(w.Affinity) && !double.IsInfinity(w.Affinity)).Select(s => s.Affinity).Max();
        }

        private void StartTimer()
        {
            _watch = Stopwatch.StartNew();
        }

        private void StopTimer()
        {
            _watch.Stop();
        }

        private double GetTime()
        {
            return _watch.Elapsed.TotalSeconds;
        }

        private void PrintResults(int maximumIterations, double average, double variance, double standardDeviation, double greaterAffinity, double seconds, int percentHighAffinity, int percentLowAffinity)
        {
            Console.WriteLine(string.Format("Iteração {0}", maximumIterations));
            Console.WriteLine(string.Format("Média: {0}", average));
            Console.WriteLine(string.Format("Variância: {0}", variance));
            Console.WriteLine(string.Format("Desvio padrão: {0}", standardDeviation));
            Console.WriteLine(string.Format("Maior afinidade: {0}", greaterAffinity));
            Console.WriteLine(string.Format("Tempo: {0} (s)", seconds));
            Console.WriteLine(string.Format("Limite baixa afinidade: {0}", percentLowAffinity));
            Console.WriteLine(string.Format("Limite de alta afinidade: {0}", percentHighAffinity));
            Console.WriteLine("");
        }

        private void SetResult(double average, double variance, double standardDeviation, int maximumIterations, int percentHighAffinity, int percentLowAffinity, double greaterAffinity, double seconds)
        {
            _results.Average.Add(average);
            _results.Variance.Add(variance);
            _results.StandardDeviation.Add(standardDeviation);
            _results.GreaterAffinity.Add(greaterAffinity);
            _results.MaximumIterations.Add(maximumIterations);
            _results.PercentHighAffinity.Add(percentHighAffinity);
            _results.PercentLowAffinity.Add(percentLowAffinity);
            _results.Time.Add(seconds);
        }
    }
}
