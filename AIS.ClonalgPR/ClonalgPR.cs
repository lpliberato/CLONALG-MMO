using AIS.ClonalgPR.Measures;
using AIS.ClonalgPR.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace AIS.ClonalgPR
{
    public class ClonalgPR
    {
        private List<Antibody> _memoryCells = new List<Antibody>();
        private Result _results = new Result();
        private IDistance _distance = null;
        private List<Antigen> _antigens = new List<Antigen>();
        private Stopwatch _watch = new Stopwatch();
        private TypeBioSequence _typeBioSequence;

        public Result Results
        {
            get { return _results; }
        }

        public ClonalgPR(IDistance distance, List<Antigen> antigens, TypeBioSequence typeBioSequence)
        {
            _distance = distance;
            _antigens = antigens;
            _typeBioSequence = typeBioSequence;
        }


        private void Affinity(Antigen antigen, List<Antibody> antibodies)
        {
            for (int i = 0; i < antibodies.Count(); i++)
            {
                // var sequenceA = antigen.Sequence.ToCharArray();
                var sequenceB = antibodies[i].Sequence;
                antibodies[i].Affinity = _distance.Calculate(sequenceB: sequenceB);
            }
        }

        private List<Antibody> Clone(List<Antibody> antibodies)
        {
            var clones = new List<Antibody>();
            foreach (var antibody in antibodies)
            {
                var rate = _distance.CalculateCloneRate(antibody.Affinity, antibody.Length);
                var clonesAmount = Math.Round(rate * antibody.Length);
                for (int j = 0; j < clonesAmount; j++)
                    clones.Add(antibody.Clone());
            }

            return clones;
        }

        private List<Antibody> Initialize(int antibodyAmount = 0, int sequenceSize = 0)
        {
            var antibodies = new List<Antibody>();

            if (antibodyAmount == 0)
                antibodyAmount = Constants.Random.Next(Constants.minimalAmountOfAntibodies, Constants.maximumAmountOfAntibodies);

            if (sequenceSize == 0)
                sequenceSize = _distance.SequenceSize();

            for (int i = 0; i < antibodyAmount; i++)
            {
                var sequence = new char[sequenceSize];
                for (int j = 0; j < sequenceSize; j++)
                    sequence[j] = GenerateSequences();

                //sequence = "ACACATC";
                antibodies.Add(new Antibody
                {
                    Sequence = sequence,
                    Length = sequence.Where(w => !Constants.Gaps.Contains(w)).Count()
                });
            }

            return antibodies;
        }

        private char GenerateSequences()
        {
            switch (_typeBioSequence)
            {
                case TypeBioSequence.DNA:
                    return Constants.DNA[Constants.Random.Next(0, Constants.DNA.Length)];
                case TypeBioSequence.RNA:
                    return Constants.RNA[Constants.Random.Next(0, Constants.RNA.Length)];
                case TypeBioSequence.PROTEIN:
                    return Constants.Aminoacids[Constants.Random.Next(0, Constants.Aminoacids.Length)];
                default:
                    return Constants.DNA[Constants.Random.Next(0, Constants.DNA.Length)];
            }
        }

        private void Insert(Antigen antigen, List<Antibody> antibodies, int index)
        {
            if (antibodies == null || antibodies.Count() == 0) return;

            var antigenName = antigen.Name;
            var memoryCell = _memoryCells.Where((a,i) => i == index).FirstOrDefault();
            var maxAffinity = antibodies.Max(_antibody => _antibody.Affinity);
            var antibody = antibodies.Where(_antibody => _antibody.Affinity == maxAffinity).FirstOrDefault();

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
                var mutateAmount = (int)(antibody.Length * rate);
                var sequenceIndex = Constants.Random.Next(0, antibody.Length);
                var sequence = antibody.Sequence;

                for (int j = 0; j < mutateAmount; j++)
                {
                    var nucleotideOrAminoacid = GenerateSequences();
                    sequence[sequenceIndex] = nucleotideOrAminoacid;
                    antibody.Sequence = sequence;
                    sequenceIndex = Constants.Random.Next(0, antibody.Length);
                }
            }

            return antibodies;
        }

        private List<Antibody> Replace(List<Antibody> antibodies, int inferiorLimit)
        {
            if (antibodies == null || antibodies.Count() == 0) return antibodies;

            var sequenceSize = _distance.SequenceSize();
            var antibodiesReplaced = antibodies.OrderBy(o => o.Affinity).Take(inferiorLimit).ToList();
            antibodiesReplaced.ForEach(antibody => antibodies.Remove(antibody));

            var amountOfAntibodiesReplaced = antibodiesReplaced.Count();
            if (amountOfAntibodiesReplaced > 0)
            {
                var _antibodies = Initialize(antibodyAmount: amountOfAntibodiesReplaced, sequenceSize: sequenceSize);
                _antibodies.ForEach(p => antibodies.Add(p));
            }
            return antibodies;
        }

        private List<Antibody> Select(List<Antibody> population, int numberHighAffinity)
        {
            return _distance.Order(population).Take(numberHighAffinity).ToList();
        }

        public void Execute(int maximumIterations, double percentHighAffinity, double percentLowAffinity)
        {
            StartTimer();

            var i = 1;
            while (i < maximumIterations)
            {
                var antibodies = Initialize(antibodyAmount: 100);
                var numberHighAffinity = (int)Math.Round(percentHighAffinity * antibodies.Count());
                var numberLowAffinity = (int)Math.Round(percentLowAffinity * antibodies.Count());

                for (int j = 0; j < _antigens.Count(); j++)
                {
                    var antigen = _antigens[j];
                    Affinity(antigen, antibodies);
                    var selectedPopulation = Select(antibodies, numberHighAffinity);
                    var clonedPopulation = Clone(selectedPopulation);
                    var mutatedPopulation = Mutation(clonedPopulation);
                    Affinity(antigen, mutatedPopulation);
                    selectedPopulation = Select(mutatedPopulation, numberHighAffinity);
                    Insert(antigen, selectedPopulation, j);
                    antibodies = Replace(selectedPopulation, numberLowAffinity);
                }
                i++;
            }
            StopTimer();
            SetStatistics(maximumIterations, percentHighAffinity, percentLowAffinity);
            PrintAntibodies();
        }

        private void PrintAntibodies()
        {
            _memoryCells.ForEach(antibody =>
            {
                if (antibody.Sequence.Length > 0)
                    Console.WriteLine(new string(antibody.Sequence));
            });
        }

        private void SetStatistics(int maximumIterations, double percentHighAffinity, double percentLowAffinity)
        {
            if (_memoryCells == null || _memoryCells.Count() == 0) return;

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
            var affinities = _memoryCells.Select(s => s.Affinity).ToList();
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
            return _memoryCells.Select(s => s.Affinity).Max();
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

        private void PrintResults(int maximumIterations, double average, double variance, double standardDeviation, double greaterAffinity, double seconds, double percentHighAffinity, double percentLowAffinity)
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

        private void SetResult(double average, double variance, double standardDeviation, int maximumIterations, double percentHighAffinity, double percentLowAffinity, double greaterAffinity, double seconds)
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
