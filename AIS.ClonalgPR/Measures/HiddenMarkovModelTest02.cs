using AIS.ClonalgPR.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIS.ClonalgPR.Measures
{
    public class HiddenMarkovModelTest02
    {
        private List<string> Sequences { get; set; }
        private List<Dictionary<char, int>> Aminoacids { get; set; }
        private List<Dictionary<char, double>> Probabilities { get; set; }
        private List<Dictionary<StatesEnum, double>> Transitions { get; set; }
        private List<int> InsertionRegion { get; set; }

        private int GetStatesCount
        {
            get
            {
                return Transitions.Count;
            }
        }

        public HiddenMarkovModelTest02(List<string> sequences)
        {
            Sequences = sequences;
            Aminoacids = new List<Dictionary<char, int>>();
            Probabilities = new List<Dictionary<char, double>>();
            Transitions = new List<Dictionary<StatesEnum, double>>();
            InsertionRegion = new List<int>();
        }

        public void Train()
        {
            CreateStates();
            CreateProbabilities();
        }

        private void CreateProbabilitiesInsertionState(ref int index) 
        {
            var aminoacids = new Dictionary<char, int>();
            var probabilities = new Dictionary<char, double>();
            var qtdSequences = Sequences.Count;
            var sequenceSize = Sequences[0].Length;
            int i = 0;

            for (i = index; i < sequenceSize; i++)
            {
                if (!Sequences.Where(w => w.Where((s, y) => y == i && s == '-').Any()).Any())
                    break;

                for (int j = 0; j < qtdSequences; j++)
                {
                    var sequence = Sequences[j].ToCharArray();
                    var aminoacid = sequence[i];

                    if (Constants.Gaps.Contains(aminoacid))
                        continue;

                        if (!aminoacids.ContainsKey(aminoacid))
                    {
                        aminoacids.Add(aminoacid, 1);
                        probabilities.Add(aminoacid, (double)1 / qtdSequences);
                    }
                    else
                    {
                        aminoacids[aminoacid] += 1;
                        probabilities[aminoacid] = (double)aminoacids[aminoacid] / qtdSequences;
                    }
                }
            }
            index = i;
            Probabilities.Add(probabilities);
        }

        private void CreateProbabilities()
        {
            var qtdSequences = Sequences.Count;
            var sequenceSize = Sequences[0].Length;

            for (int i = 0; i < sequenceSize; i++)
            {
                var aminoacids = new Dictionary<char, int>();
                var probabilities = new Dictionary<char, double>();
                for (int j = 0; j < qtdSequences; j++)
                {
                    var sequence = Sequences[j].ToCharArray();
                    var aminoacid = sequence[i];
                    if (Constants.Gaps.Contains(aminoacid)) 
                    {
                        CreateProbabilitiesInsertionState(ref i);
                    } else if (!aminoacids.ContainsKey(aminoacid))
                    {
                        aminoacids.Add(aminoacid, 1);
                        probabilities.Add(aminoacid, (double)1 / qtdSequences);
                    }
                    else
                    {
                        aminoacids[aminoacid] += 1;
                        probabilities[aminoacid] = (double)aminoacids[aminoacid] / qtdSequences;
                    }
                }
                Aminoacids.Add(aminoacids);
                Probabilities.Add(probabilities);
            }
        }

        private void CreateInsertionState(int index, ref Dictionary<StatesEnum, double> states)
        {
            var qtdSequences = Sequences.Count;
            var sequenceSize = Sequences[0].Length;
            var gapCount = 0;
            var matchCount = 0;

            for (int i = index; i < sequenceSize; i++)
            {
                for (int j = 0; j < qtdSequences; j++)
                {
                    var sequence = Sequences[j].ToCharArray();
                    var aminoacid = sequence[i];
                    if (Constants.Gaps.Contains(aminoacid))
                        gapCount++;
                    else
                        matchCount++;
                }
                states[StatesEnum.Insert] = Convert.ToDouble(matchCount) / Convert.ToDouble(qtdSequences);
            }
        } 

        private void CreateStates()
        {
            var qtdSequences = Sequences.Count;
            var sequenceSize = Sequences[0].Length;

            for (int i = 0; i < sequenceSize; i++)
            {
                var states = new Dictionary<StatesEnum, double>()
                {
                    { StatesEnum.Match, 0.0 },
                    { StatesEnum.Insert, 0.0 },
                    { StatesEnum.Delete, 0.0 }
                };

                var matchCount = 0;
                var deleteCount = 0;
                for (int j = 0; j < qtdSequences; j++)
                {
                    var sequence = Sequences[j].ToCharArray();
                    var aminoacid = sequence[i];
                    if (!Constants.Gaps.Contains(aminoacid))
                        matchCount++;
                    else
                        deleteCount++;
                }

                states[StatesEnum.Match] = Convert.ToDouble(matchCount) / Convert.ToDouble(qtdSequences);
                if (deleteCount > 1)
                    CreateInsertionState(i, ref states);
                //    states[StatesEnum.Insert] = Convert.ToDouble(matchCount) / Convert.ToDouble(qtdSequences);
                //else if (deleteCount == 1)
                //    states[StatesEnum.Delete] = Convert.ToDouble(deleteCount) / Convert.ToDouble(qtdSequences);

                Transitions.Add(states);
            }
        }

        private static double NullModelValue(int alphabetSize, int sequenceSize)
        {
            return Math.Pow((double)1 / alphabetSize, sequenceSize);
        }

        public double CalculateTotalProbability(char[] sequence)
        {
            var probability = 1.0;
            for (int i = 0; i < sequence.Length; i++)
            {
                var aminoacid = sequence[i];
                var probabilities = Probabilities[i];
                var transitions = Transitions[i];
                probability *= probabilities[aminoacid];

                var matchTransition = transitions.GetValueOrDefault(StatesEnum.Match);
                var insertTransition = transitions.GetValueOrDefault(StatesEnum.Insert);
                var deleteTransition = transitions.GetValueOrDefault(StatesEnum.Delete);
                probability *= matchTransition > 0 ? matchTransition : 1;
                probability *= insertTransition > 0 ? insertTransition : 1;
                probability *= deleteTransition > 0 ? deleteTransition : 1;
            }

            return probability;
        }

        public double CalculateLogOdds(char[] sequence)
        {
            var probability = CalculateTotalProbability(sequence);
            return Math.Log(probability) / NullModelValue(Constants.Aminoacids.Length, sequence.Length);
        }
    }
}