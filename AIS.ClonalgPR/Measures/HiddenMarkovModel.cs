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
        private List<Dictionary<StateEnum, double>> Transitions { get; set; }
        private List<Dictionary<StateEnum, int>> Regions { get; set; }
        private List<State> States { get; set; }

        public HiddenMarkovModel(List<string> sequences)
        {
            Sequences = sequences;
            Probabilities = new List<Dictionary<char, double>>();
            Transitions = new List<Dictionary<StateEnum, double>>();
            Regions = new List<Dictionary<StateEnum, int>>();
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
            var sequencesAmount = Sequences.Count;
            var sequenceSize = Sequences[0].Length;

            for (int i = 0; i < sequenceSize; i++)
            {
                var gap = 0;

                for (int j = 0; j < sequencesAmount; j++)
                {
                    var sequence = Sequences[j].ToCharArray();
                    var symbol = sequence[i];

                    if (Constants.Gaps.Contains(symbol))
                        gap++;
                }

                Regions.Add(
                    new Dictionary<StateEnum, int>()
                    {
                        { StateEnum.Delete, gap == 1 ? 1 : 0 },
                        { StateEnum.Match, gap == 0 ? 1 : 0 },
                        { StateEnum.Insert, gap > 1 ? 1 : 0 }
                    });
            }
        }

        private void CreateProbabilities()
        {
            var sequencesAmount = Sequences.Count;
            var sequenceSize = Sequences[0].Length;

            for (int i = 0; i < sequenceSize; i++)
            {
                var symbols = InitSymbols();
                var probabilities = InitProbabilities();
                var state = GetState(Regions[i]);

                for (int j = 0; j < sequencesAmount; j++)
                {
                    var sequence = Sequences[j].ToCharArray();
                    var symbol = sequence[i];

                    if (state == StateEnum.Match)
                        CreateProbabilityMatchState(symbol, sequencesAmount, ref symbols, ref probabilities);
                    else if (state == StateEnum.Delete)
                        continue;
                    else if (state == StateEnum.Insert)
                    {
                        var length = ReturnsLengthInsertTransitionState(i);
                        CreateProbabilityInsertState(length, sequencesAmount, ref i, ref symbols, ref probabilities);
                        break;
                    }
                }
                Probabilities.Add(probabilities);
            }
        }

        private Dictionary<char, int> InitSymbols()
        {
            return new Dictionary<char, int>()
                {
                    { 'A', 0 },
                    { 'C', 0 },
                    { 'G', 0 },
                    { 'T', 0 },
                };
        }

        private Dictionary<char, double> InitProbabilities()
        {
            return new Dictionary<char, double>()
                {
                    { 'A', 0.0 },
                    { 'C', 0.0 },
                    { 'G', 0.0 },
                    { 'T', 0.0 },
                };
        }

        private void CreateTransitions()
        {
            for (int i = 1; i < Regions.Count - 1; i++)
            {
                var state = GetState(Regions[i]);

                if (state == StateEnum.Match || state == StateEnum.Insert)
                    CreateInsertionOrMatchState(ref i);
                else if (state == StateEnum.Delete)
                    continue;
            }
        }

        private void CreateInsertionOrMatchState(ref int index)
        {
            var sequencesAmount = Sequences.Count;
            var previousIndex = index;
            var length = ReturnsLengthInsertTransitionState(index);
            var amountSequencesInsertState = AmountSequencesInsertState(previousIndex, length, sequencesAmount);
            var insertionPercentage = Convert.ToDouble(amountSequencesInsertState) / Convert.ToDouble(sequencesAmount);
            var matchPercentage = 1.0 - insertionPercentage;

            Transitions.Add(new Dictionary<StateEnum, double>()
                        {
                            { StateEnum.Match, matchPercentage },
                            { StateEnum.Insert, insertionPercentage },
                            { StateEnum.Delete, 0.0 }
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

            var sequencesAmount = Sequences.Count;
            var insertionPercentage = Convert.ToDouble(count) / Convert.ToDouble(sequencesAmount);
            var matchPercentage = 1.0 - insertionPercentage;

            Transitions.Add(new Dictionary<StateEnum, double>()
                        {
                            { StateEnum.Match, matchPercentage },
                            { StateEnum.Insert, insertionPercentage },
                            { StateEnum.Delete, 0.0 }
                        });

            index += length - 1;
        }

        private int AmountSequencesInsertState(int index, int length, int sequencesAmount)
        {
            var indexes = new List<int>();

            for (int i = index; i < index + length; i++)
            {
                for (int j = 0; j < sequencesAmount; j++)
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
                if (state != StateEnum.Insert)
                    break;
                count++;
            }

            return count;
        }

        private void CreateStates()
        {
            var qtdStates = GetAmountOfStates();

            for (int i = 0; i < qtdStates; i++)
            {
                States.Add(new State()
                {
                    Probabilities = Probabilities[i],
                    Transitions = (i != qtdStates - 1) ? Transitions[i] : null
                });
            }
        }

        private int GetAmountOfStates()
        {
            var count = 0;
            var previousStateIsInsert = false;

            for (int i = 0; i < Regions.Count; i++)
            {
                var state = GetState(Regions[i]);

                if (state == StateEnum.Match || state == StateEnum.Delete)
                {
                    count++;
                    previousStateIsInsert = false;
                }
                else if (state == StateEnum.Insert)
                {
                    if (!previousStateIsInsert)
                        count++;
                    previousStateIsInsert = true;
                }
            }
            return count;
        }

        private void CreateProbabilityMatchState(char symbol, int sequencesAmount, ref Dictionary<char, int> symbols, ref Dictionary<char, double> probabilities)
        {
            if (Constants.Gaps.Contains(symbol))
                return;

            symbols[symbol] += 1;
            probabilities[symbol] = (double)symbols[symbol] / sequencesAmount;
        }

        private void CreateProbabilityInsertState(int length, int sequencesAmount, ref int index, ref Dictionary<char, int> symbols, ref Dictionary<char, double> probabilities)
        {
            int i = 0;
            for (i = index; i < index + length; i++)
            {
                for (int j = 0; j < sequencesAmount; j++)
                {
                    var sequence = Sequences[j].ToCharArray();
                    var symbol = sequence[i];
                    CreateProbabilityMatchState(symbol, sequencesAmount, ref symbols, ref probabilities);
                }
            }
            index = i - 1;
        }

        private StateEnum GetState(Dictionary<StateEnum, int> region)
        {
            return region.GetValueOrDefault(StateEnum.Delete) == 1 ? StateEnum.Delete :
                region.GetValueOrDefault(StateEnum.Insert) == 1 ? StateEnum.Insert : StateEnum.Match;
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

        private double GetTransitionValue(Dictionary<StateEnum, double> transitions)
        {
            if (transitions == null)
                return 1;

            var matchTransition = transitions.GetValueOrDefault(StateEnum.Match);
            var deleteTransition = transitions.GetValueOrDefault(StateEnum.Delete);
            var insertTransition = transitions.GetValueOrDefault(StateEnum.Insert);

            return ((matchTransition > 0) ? matchTransition : 1) * ((deleteTransition > 0) ? deleteTransition : 1) * ((insertTransition > 0) ? insertTransition : 1);
        }
    }
}