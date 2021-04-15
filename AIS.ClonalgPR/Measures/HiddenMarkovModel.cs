using AIS.ClonalgPR.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIS.ClonalgPR.Measures
{
    public class HiddenMarkovModel
    {
        private List<string> Sequences { get; set; }
        private List<Dictionary<char, double>> Probabilities { get; set; }
        private List<Dictionary<TransitionEnum, double>> Transitions { get; set; }
        private List<Dictionary<TransitionEnum, int>> Regions { get; set; }
        private List<State> States { get; set; }

        public HiddenMarkovModel(List<string> sequences)
        {
            Sequences = sequences;
            Probabilities = new List<Dictionary<char, double>>();
            Transitions = new List<Dictionary<TransitionEnum, double>>();
            Regions = new List<Dictionary<TransitionEnum, int>>();
            States = new List<State>();
        }

        public void Train()
        {
            CreateRegions();
            CreateProbabilities();
            CreateTransitions();
            CreateStates();
        }

        private void CreateRegions()
        {
            var qtdSequences = Sequences.Count;
            var sequenceSize = Sequences[0].Length;
            var match = 0;
            var gap = 0;

            for (int i = 0; i < sequenceSize; i++)
            {
                for (int j = 0; j < qtdSequences; j++)
                {
                    var sequence = Sequences[j].ToCharArray();
                    var aminoacid = sequence[i];
                    if (!Constants.Gaps.Contains(aminoacid))
                        match++;
                    else
                        gap++;
                }

                Regions.Add(
                    new Dictionary<TransitionEnum, int>()
                    {
                        { TransitionEnum.Delete, gap == 1 ? 1 : 0 },
                        { TransitionEnum.Match, gap == 0 ? match : 0 },
                        { TransitionEnum.Insert, gap > 1 ? match : 0 }
                    });
            }
        }

        private void CreateProbabilities()
        {
            var qtdSequences = Sequences.Count;
            var sequenceSize = Sequences[0].Length;

            for (int i = 0; i < sequenceSize; i++)
            {
                var aminoacids = new Dictionary<char, int>();
                var probabilities = new Dictionary<char, double>();
                var region = Regions[i];
                var state = GetState(region);

                for (int j = 0; j < qtdSequences; j++)
                {
                    var sequence = Sequences[j].ToCharArray();
                    var aminoacid = sequence[i];

                    if (state == TransitionEnum.Match)
                        CreateProbabilityMatchState(aminoacid, qtdSequences, ref aminoacids, ref probabilities);
                    else if (state == TransitionEnum.Delete)
                        continue;
                    else if (state == TransitionEnum.Insert)
                        CreateProbabilityInsertState(ref i, sequenceSize, qtdSequences, ref aminoacids, ref probabilities);
                }
                Probabilities.Add(probabilities);
            }
        }

        private void CreateTransitions()
        {
            var qtdSequences = Sequences.Count;

            for (int i = 0; i < Regions.Count; i++)
            {
                if (i <= Regions.Count - 1)
                {
                    var region = Regions[i + 1];
                    var state = GetState(region);

                    if (state == TransitionEnum.Match)
                        Transitions.Add(new Dictionary<TransitionEnum, double>()
                        {
                            { TransitionEnum.Match, 1.0 },
                            { TransitionEnum.Insert, 0.0 },
                            { TransitionEnum.Delete, 0.0 }
                        });
                    else if (state == TransitionEnum.Delete)
                        continue;
                    else if (state == TransitionEnum.Insert)
                        continue;
                }
            }
        }

        private void CreateStates()
        {
            var qtdSequences = Sequences.Count;
            var sequenceSize = Sequences[0].Length;

            for (int i = 0; i < sequenceSize; i++)
            {
            }
        }

        private void CreateProbabilityMatchState(char aminoacid, int qtdSequences, ref Dictionary<char, int> aminoacids, ref Dictionary<char, double> probabilities)
        {
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

        private void CreateProbabilityDeleteState(char aminoacid, int qtdSequences, ref Dictionary<char, int> aminoacids, ref Dictionary<char, double> probabilities)
        {

        }

        private void CreateProbabilityInsertState(ref int index, int sequenceSize, int qtdSequences, ref Dictionary<char, int> aminoacids, ref Dictionary<char, double> probabilities)
        {
            int i = 0;
            for (i = index; i < sequenceSize; i++)
            {
                for (int j = 0; j < qtdSequences; j++)
                {
                    var sequence = Sequences[j].ToCharArray();
                    var aminoacid = sequence[i];
                    CreateProbabilityMatchState(aminoacid, qtdSequences, ref aminoacids, ref probabilities);
                }
            }
            index = i;
        }

        private TransitionEnum GetState(Dictionary<TransitionEnum, int> region)
        {
            return region.GetValueOrDefault(TransitionEnum.Delete) > 1 ? TransitionEnum.Delete :
                region.GetValueOrDefault(TransitionEnum.Insert) > 1 ? TransitionEnum.Insert : TransitionEnum.Match;
        }

        private void CreateInsertionState(int index, ref Dictionary<TransitionEnum, double> states, ref int matchCount)
        {
            var qtdSequences = Sequences.Count;
            var sequenceSize = Sequences[0].Length;
            var gapCount = 0;
            matchCount = 0;
            List<int> matchIndexes = new List<int>();
            int i = 0;

            for (i = index; i < sequenceSize; i++)
            {
                if (!Sequences.Where(sequence => sequence.Where((s, y) => y == i && s == '-').Any()).Any())
                    break;

                for (int j = 0; j < qtdSequences; j++)
                {
                    var sequence = Sequences[j].ToCharArray();
                    var aminoacid = sequence[index];
                    if (Constants.Gaps.Contains(aminoacid))
                        gapCount++;
                    else
                    {
                        matchCount++;
                        if (!matchIndexes.Contains(j))
                            matchIndexes.Add(j);
                    }
                }
            }
            states[TransitionEnum.Insert] = Convert.ToDouble(matchIndexes.Count) / Convert.ToDouble(qtdSequences);
            states[TransitionEnum.Match] = Convert.ToDouble(qtdSequences - matchIndexes.Count) / Convert.ToDouble(qtdSequences);
            index = i;
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

                var matchTransition = transitions.GetValueOrDefault(TransitionEnum.Match);
                var insertTransition = transitions.GetValueOrDefault(TransitionEnum.Insert);
                var deleteTransition = transitions.GetValueOrDefault(TransitionEnum.Delete);
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