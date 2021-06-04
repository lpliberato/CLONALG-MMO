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

        private void CreateStates(int index = 0)
        {
            var observationSize = Observations[0].Length;
            if (index == observationSize)
                return;

            var observationsAmount = Observations.Count;
            var symbolsAmount = GetAmountOfMatches(index);

            if (symbolsAmount == observationsAmount)
                States.Add(new State() 
                { 
                    Name = StateEnum.Match, 
                    EmissionProbabilities = InitEmissionProbabilities(), 
                    TransitionProbabilities = InitTransitionProbabilities() 
                });
            else
            {
                symbolsAmount = GetAmountOfMatches(index + 1);
                if (symbolsAmount == observationsAmount)
                    States.Add(new State() 
                    { 
                        Name = StateEnum.Insert, 
                        EmissionProbabilities = InitEmissionProbabilities(), 
                        TransitionProbabilities = InitTransitionProbabilities() 
                    });
            }

            CreateStates(index + 1);
        }

        private void SetInsertStateCounterForProbabilities(ref int insertStateCounter, int index, int observationsAmount)
        {
            var symbolsAmount = GetAmountOfMatches(index);
            if (symbolsAmount != observationsAmount)
            {
                insertStateCounter++;
                var nextSymbolsAmount = GetAmountOfMatches(index + 1);
                if (nextSymbolsAmount == observationsAmount)
                    insertStateCounter--;
            }
        }

        private void CreateProbabilities(int index = 0, int insertStateCounter = 0)
        {
            var observationSize = Observations[0].Length;
            if (index == observationSize)
                return;

            var observationsAmount = Observations.Count;
            var symbols = InitSymbols();
            var probabilities = InitEmissionProbabilities();
            State state = States[index - insertStateCounter];

            for (int j = 0; j < observationsAmount; j++)
            {
                var sequence = Observations[j].ToCharArray();
                var symbol = sequence[index];
                CreateProbabilityPerState(symbol, observationsAmount, ref symbols, ref probabilities);
            }

            SetProbabilities(state.EmissionProbabilities, probabilities);
            SetInsertStateCounterForProbabilities(ref insertStateCounter, index, observationsAmount);
            CreateProbabilities(index + 1, insertStateCounter);
        }

        private void SetProbabilities(Dictionary<char, double> emissionProbabilities, Dictionary<char, double> probabilities)
        {
            foreach (var item in probabilities)
                emissionProbabilities[item.Key] += item.Value;
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

        private Dictionary<char, double> InitEmissionProbabilities()
        {
            return new Dictionary<char, double>()
                {
                    { 'A', 0d },
                    { 'C', 0d },
                    { 'G', 0d },
                    { 'T', 0d },
                };
        }

        private Dictionary<StateEnum, double> InitTransitionProbabilities()
        {
            return new Dictionary<StateEnum, double>()
            {
                { StateEnum.Match, 0d },
                { StateEnum.Insert, 0d },
                { StateEnum.Delete, 0d }
            };
        }

        private void SetInsertStateCounterForTransitions(ref int insertStateCounter, int index, int observationsAmount)
        {
            var symbolsAmount = GetAmountOfMatches(index);
            var nextSymbolsAmount = GetAmountOfMatches(index + 1);

            if (symbolsAmount != observationsAmount && nextSymbolsAmount != observationsAmount)
                insertStateCounter++;
        }

        private void CreateTransitions(int index = 0, int insertStateCounter = 0)
        {
            var currentIndex = index - insertStateCounter;
            if (currentIndex == States.Count) return;

            State state = States[currentIndex];

            var indexForObservations = state.Name == StateEnum.Insert ? currentIndex : index;
            CreateTransitionsStates(index, currentIndex, state);

            var indexToTransition = index >= Observations[0].Length - 2 ? currentIndex : index;
            SetInsertStateCounterForTransitions(ref insertStateCounter, indexToTransition, Observations.Count);
            CreateTransitions(index + 1, insertStateCounter);
        }

        private int GetAmountOfMatches(int index)
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

        private int GetAmountOfInserts(int index)
        {
            var indexes = new List<int>();
            var observationSize = Observations[0].Length;
            var observationsAmount = Observations.Count;
            var matchCount = 0;
            var gapCount = 0;

            for (int i = index; i < observationSize; i++)
            {
                for (int j = 0; j < observationsAmount; j++)
                {
                    var observation = Observations[j];
                    var symbol = observation[i];

                    if (!Constants.Gaps.Contains(symbol))
                    {
                        matchCount++;
                        if (!indexes.Contains(j))
                            indexes.Add(j);
                    }
                    else
                        gapCount++;
                }
                if (matchCount == observationsAmount) break;
            }

            return indexes.Count;
        }

        private void CreateTransitionsStates(int index, int currentIndex, State currentState)
        {
            if (currentIndex >= States.Count) return;

            var nextState = currentIndex < States.Count - 1 ? States[currentIndex + 1] : null;
            int symbolsAmount;
            double percentageOfMachState = 0d;
            double percentageOfInsertState = 0d;

            if (currentState.Name == StateEnum.Match && (nextState == null || nextState.Name == StateEnum.Match))
            {
                symbolsAmount = GetAmountOfMatches(index);
                percentageOfMachState = Convert.ToDouble(symbolsAmount) / Convert.ToDouble(Observations.Count);
                percentageOfInsertState = 1d - percentageOfMachState;
            }
            else if (currentState.Name == StateEnum.Match && nextState.Name == StateEnum.Insert)
            {
                symbolsAmount = GetAmountOfInserts(currentIndex + 1);
                percentageOfInsertState = Convert.ToDouble(symbolsAmount) / Convert.ToDouble(Observations.Count);
                percentageOfMachState = 1d - percentageOfInsertState;
            }
            else if (currentState.Name == StateEnum.Insert && nextState.Name == StateEnum.Match)
            {
                symbolsAmount = GetAmountOfInserts(currentIndex);
                percentageOfMachState = Convert.ToDouble(symbolsAmount) / Convert.ToDouble(Observations.Count);
                percentageOfInsertState = 1d - percentageOfMachState;
            }

            currentState.TransitionProbabilities = new Dictionary<StateEnum, double>()
                        {
                            { StateEnum.Match, percentageOfMachState },
                            { StateEnum.Insert, percentageOfInsertState },
                            { StateEnum.Delete, 0.0 }
                        };
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
            return Math.Pow(1d / alphabetSize, observationsSize);
        }

        public double CalculateTotalProbability(List<char> observations)
        {
            return ForwardViterbi(observations);
        }

        public double CalculateLogOdds(List<char> observations)
        {
            var probability = CalculateTotalProbability(observations);
            return Math.Log(probability) - (observations.Count * Math.Log(1d / Constants.DNA.Length));
        }

        private double ForwardViterbi(List<char> observations)
        {
            var prob = 1d;
            for (int i = 0; i < observations.Count; i++)
            {
                var symbol = observations[i];
                var state = States[i];
                var emissionProbability = 0d;
                var transitionProbabilities = 0d;

                if (state.TransitionProbabilities != null)
                    transitionProbabilities = state.TransitionProbabilities.Max(m => m.Value);

                if (state.EmissionProbabilities != null)
                    emissionProbability = state.EmissionProbabilities[symbol];

                if (emissionProbability > 0)
                    prob *= emissionProbability;

                if (transitionProbabilities > 0)
                    prob *= transitionProbabilities;
            }

            return prob;
        }
    }
}