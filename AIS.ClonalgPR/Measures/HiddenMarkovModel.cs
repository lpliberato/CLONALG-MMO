using AIS.ClonalgPR.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIS.ClonalgPR.Measures
{
    public class HiddenMarkovModel
    {
        private List<string> Observations { get; set; }
        private List<State> States { get; set; }

        public HiddenMarkovModel(List<string> observations)
        {
            Observations = observations;
            States = new List<State>();
        }

        public void Train()
        {
            CreateStates();
            CreateProbabilities();
            CreateTransitions();
        }

        private void CreateStates()
        {
            var observationsAmount = Observations.Count;
            var observationSize = Observations[0].Length;

            for (int i = 0; i < observationSize; i++)
            {
                var gap = 0;
                var match = 0;

                for (int j = 0; j < observationsAmount; j++)
                {
                    var sequence = Observations[j].ToCharArray();
                    var symbol = sequence[i];

                    if (Constants.Gaps.Contains(symbol))
                        gap++;
                    else
                        match++;
                }

                if (match > 0 && gap == 0)
                    States.Add(new State() { Name = StateEnum.Match });
                else if (match > 0 && gap > 0 && match > gap)
                    States.Add(new State() { Name = StateEnum.Delete });
                else if (match > 0 && gap > 0 && match < gap)
                    States.Add(new State() { Name = StateEnum.Insert });
            }
        }

        private void CreateProbabilities()
        {
            var observationSize = Observations.Count;

            for (int i = 0; i < States.Count; i++)
            {
                var symbols = InitSymbols();
                var probabilities = InitProbabilities();
                var state = States[i];

                for (int j = 0; j < observationSize; j++)
                {
                    var sequence = Observations[j].ToCharArray();
                    var symbol = sequence[i];

                    if (state.Name == StateEnum.Match || state.Name == StateEnum.Insert)
                        CreateProbabilityPerState(symbol, observationSize, ref symbols, ref probabilities);
                    else if (state.Name == StateEnum.Delete)
                        continue;
                }
                state.EmissionProbabilities = probabilities;
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
            for (int i = 0; i < States.Count - 1; i++)
            {
                State state = States[i];

                if (state.Name == StateEnum.Match)
                    CreateMatchState(state);
                else if (state.Name == StateEnum.Insert)
                    CreateInsertState(i, state);
                else if (state.Name == StateEnum.Delete)
                    CreateDeleteState(state);
            }
        }

        private void CreateMatchState(State state)
        {
            state.TransitionProbabilities = new Dictionary<StateEnum, double>()
                        {
                            { StateEnum.Match, 1.0 },
                            { StateEnum.Insert, 0.0 },
                            { StateEnum.Delete, 0.0 }
                        };
        }

        private void CreateInsertState(int index, State state)
        {
            var count = AmountSymbolsInsertState(index);
            var observationsAmount = Observations.Count;
            var insertionPercentage = Convert.ToDouble(count) / Convert.ToDouble(observationsAmount);
            var matchPercentage = 1.0 - insertionPercentage;

            state.TransitionProbabilities = new Dictionary<StateEnum, double>()
                        {
                            { StateEnum.Match, matchPercentage },
                            { StateEnum.Insert, insertionPercentage },
                            { StateEnum.Delete, 0.0 }
                        };
        }

        private void CreateDeleteState(State state)
        {
            state.TransitionProbabilities = new Dictionary<StateEnum, double>()
                        {
                            { StateEnum.Match, 0.0 },
                            { StateEnum.Insert, 0.0 },
                            { StateEnum.Delete, 0.0 }
                        };
        }

        private int AmountSymbolsInsertState(int index)
        {
            var count = 0;
            for (int i = 0; i < Observations.Count; i++)
            {
                var observation = Observations[i].ToCharArray();
                var symbol = observation[index];
                if (!Constants.Gaps.Contains(symbol))
                    count++;
            }

            return count;
        }

        private void CreateProbabilityPerState(char symbol, int observationsAmount, ref Dictionary<char, int> symbols, ref Dictionary<char, double> probabilities)
        {
            if (Constants.Gaps.Contains(symbol))
                return;

            symbols[symbol] += 1;
            probabilities[symbol] = (double)symbols[symbol] / observationsAmount;
        }

        private static double NullModelValue(int alphabetSize, int observationsSize)
        {
            return Math.Pow((double)1 / alphabetSize, observationsSize);
        }

        public double CalculateTotalProbability(List<char> observations)
        {
            return ForwardViterbi(observations);
        }

        public double CalculateLogOdds(List<char> observations)
        {
            var probability = CalculateTotalProbability(observations);
            return Math.Log(probability) / NullModelValue(Constants.DNA.Length, observations.Count);
        }

        private double ForwardViterbi(List<char> observations)
        {
            var prob = 1d;

            for (int i = 0; i < observations.Count; i++)
            {
                var symbol = observations[i];
                var state = States[i];
                var emissionProbability = Constants.Gaps.Contains(symbol) ? 1 : state.EmissionProbabilities[symbol];
                var transitionProbabilities = state.TransitionProbabilities != null ? state.TransitionProbabilities[state.Name] : 1;
                var p = emissionProbability > 0 ? emissionProbability : 1 * transitionProbabilities > 0 ? transitionProbabilities : 1;

                prob *= p;
            }

            return prob;
        }
    }
}