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

            for (int i = 0; i < sequenceSize; i++)
            {
                var gap = 0;

                for (int j = 0; j < qtdSequences; j++)
                {
                    var sequence = Sequences[j].ToCharArray();
                    var symbol = sequence[i];

                    if (Constants.Gaps.Contains(symbol))
                        gap++;
                }

                Regions.Add(
                    new Dictionary<TransitionEnum, int>()
                    {
                        { TransitionEnum.Delete, gap == 1 ? 1 : 0 },
                        { TransitionEnum.Match, gap == 0 ? 1 : 0 },
                        { TransitionEnum.Insert, gap > 1 ? 1 : 0 }
                    });
            }
        }

        private void CreateProbabilities()
        {
            var qtdSequences = Sequences.Count;
            var sequenceSize = Sequences[0].Length;

            for (int i = 0; i < sequenceSize; i++)
            {
                var symbols = new Dictionary<char, int>()
                {
                    { 'A', 0 },
                    { 'C', 0 },
                    { 'G', 0 },
                    { 'T', 0 },
                };
                var probabilities = new Dictionary<char, double>()
                {
                    { 'A', 0 },
                    { 'C', 0 },
                    { 'G', 0 },
                    { 'T', 0 },
                };
                var state = GetState(Regions[i]);

                for (int j = 0; j < qtdSequences; j++)
                {
                    var sequence = Sequences[j].ToCharArray();
                    var symbol = sequence[i];

                    if (state == TransitionEnum.Match)
                        CreateProbabilityMatchState(symbol, qtdSequences, ref symbols, ref probabilities);
                    else if (state == TransitionEnum.Delete)
                        continue;
                    else if (state == TransitionEnum.Insert)
                    {
                        var length = ReturnsLengthInsertTransitionState(i);
                        CreateProbabilityInsertState(length, qtdSequences, ref i, ref symbols, ref probabilities);
                        break;
                    }
                }
                Probabilities.Add(probabilities);
            }
        }

        private void CreateTransitions()
        {
            for (int i = 1; i < Regions.Count - 1; i++)
            {
                var state = GetState(Regions[i]);

                if (state == TransitionEnum.Match || state == TransitionEnum.Insert)
                    CreateInsertionOrMatchState(ref i);
                else if (state == TransitionEnum.Delete)
                    continue;
            }
        }

        private void CreateInsertionOrMatchState(ref int index)
        {
            var qtdSequences = Sequences.Count;
            var previousIndex = index;
            var length = ReturnsLengthInsertTransitionState(index);
            var quantity = QuantitySequencesInsertState(previousIndex, length, qtdSequences);
            var insertionPercentage = Convert.ToDouble(quantity) / Convert.ToDouble(qtdSequences);
            var matchPercentage = 1.0 - insertionPercentage;

            Transitions.Add(new Dictionary<TransitionEnum, double>()
                        {
                            { TransitionEnum.Match, matchPercentage },
                            { TransitionEnum.Insert, insertionPercentage },
                            { TransitionEnum.Delete, 0.0 }
                        });

            if (insertionPercentage > 0)
                CreateInsertionState(length, ref index);
        }

        private void CreateInsertionState(int length, ref int index)
        {
            var count = 0;
            for (int i = index + 1; i < index + length; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    var sequence = Sequences[j].ToCharArray();
                    var symbol = sequence[i];
                    if (!Constants.Gaps.Contains(symbol))
                    {
                        count++;
                        break;
                    }
                }
            }

            var qtdSequences = Sequences.Count;
            var insertionPercentage = Convert.ToDouble(count) / Convert.ToDouble(qtdSequences);
            var matchPercentage = 1.0 - insertionPercentage;

            Transitions.Add(new Dictionary<TransitionEnum, double>()
                        {
                            { TransitionEnum.Match, matchPercentage },
                            { TransitionEnum.Insert, insertionPercentage },
                            { TransitionEnum.Delete, 0.0 }
                        });

            index += length - 1;
        }

        private int QuantitySequencesInsertState(int index, int length, int qtdSequences)
        {
            var indexes = new List<int>();

            for (int i = index; i < index + length; i++)
            {
                for (int j = 0; j < qtdSequences; j++)
                {
                    var sequence = Sequences[j].ToCharArray();
                    var symbol = sequence[i];

                    if (!Constants.Gaps.Contains(symbol) && !indexes.Contains(j))
                        indexes.Add(j);
                }
            }

            return indexes.Count;
        }

        private int ReturnsLengthInsertTransitionState(int index)
        {
            var count = 0;
            for (int i = index; i < Regions.Count; i++)
            {
                var state = GetState(Regions[i]);
                if (state != TransitionEnum.Insert)
                    break;
                count++;
            }

            return count;
        }

        private void CreateStates()
        {
            var qtdStates = GetQtdStates();

            for (int i = 0; i < qtdStates; i++)
            {
                States.Add(new State()
                {
                    Probabilities = Probabilities[i],
                    Transitions = (i != qtdStates -1) ? Transitions[i] : null
                });
            }
        }

        private int GetQtdStates()
        {
            var count = 0;
            var previousStateIsInsert = false;

            for (int i = 0; i < Regions.Count; i++)
            {
                var state = GetState(Regions[i]);

                if (state == TransitionEnum.Match)
                {
                    count++;
                    previousStateIsInsert = false;
                }
                else if (state == TransitionEnum.Insert)
                {
                    if (!previousStateIsInsert)
                        count++;
                    previousStateIsInsert = true;
                }
                else if (state == TransitionEnum.Delete)
                {
                    count++;
                    previousStateIsInsert = false;
                }
            }
            return count;
        }

        private void CreateProbabilityMatchState(char symbol, int qtdSequences, ref Dictionary<char, int> symbols, ref Dictionary<char, double> probabilities)
        {
            if (Constants.Gaps.Contains(symbol))
                return;

            symbols[symbol] += 1;
            probabilities[symbol] = (double)symbols[symbol] / qtdSequences;
        }

        private void CreateProbabilityInsertState(int length, int qtdSequences, ref int index, ref Dictionary<char, int> symbols, ref Dictionary<char, double> probabilities)
        {
            int i = 0;
            for (i = index; i < index + length; i++)
            {
                for (int j = 0; j < qtdSequences; j++)
                {
                    var sequence = Sequences[j].ToCharArray();
                    var symbol = sequence[i];
                    CreateProbabilityMatchState(symbol, qtdSequences, ref symbols, ref probabilities);
                }
            }
            index = i - 1;
        }

        private TransitionEnum GetState(Dictionary<TransitionEnum, int> region)
        {
            return region.GetValueOrDefault(TransitionEnum.Delete) == 1 ? TransitionEnum.Delete :
                region.GetValueOrDefault(TransitionEnum.Insert) == 1 ? TransitionEnum.Insert : TransitionEnum.Match;
        }

        private static double NullModelValue(int alphabetSize, int sequenceSize)
        {
            return Math.Pow((double)1 / alphabetSize, sequenceSize);
        }

        public double CalculateTotalProbability(char[] sequence)
        {
            var probability = 1.0;
            for (int i = 0; i < States.Count; i++)
            {
                var symbol = sequence[i];
                if (Constants.Gaps.Contains(symbol))
                    continue;

                var state = States[i];
                probability *= GetProbabilityValue(state.Probabilities, symbol) * GetTransitionValue(state.Transitions);
            }

            return probability;
        }

        public double CalculateLogOdds(char[] sequence)
        {
            var probability = CalculateTotalProbability(sequence);
            return Math.Log(probability) / NullModelValue(Constants.Aminoacids.Length, sequence.Length);
        }

        private double GetProbabilityValue(Dictionary<char, double> probabilities, char symbol)
        {
            var probability = probabilities[symbol];
            return probability > 0 ? probability : 1;
        }

        private double GetTransitionValue(Dictionary<TransitionEnum, double> transitions)
        {
            if (transitions == null)
                return 1;

            var matchTransition = transitions.GetValueOrDefault(TransitionEnum.Match);
            var deleteTransition = transitions.GetValueOrDefault(TransitionEnum.Delete);
            var insertTransition = transitions.GetValueOrDefault(TransitionEnum.Insert);

            return ((matchTransition > 0) ? matchTransition : 1) * ((deleteTransition > 0) ? deleteTransition : 1) * ((insertTransition > 0) ? insertTransition : 1);
        }
    }
}