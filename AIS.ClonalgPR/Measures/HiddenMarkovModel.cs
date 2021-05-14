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
            var symbolsAmount = GetAmountOfSymbolsInTheState(index);

            if (symbolsAmount == observationsAmount)
                States.Add(new State() { Name = StateEnum.Match, EmissionProbabilities = InitEmissionProbabilities(), TransitionProbabilities = InitTransitionProbabilities() });
            else
            {
                symbolsAmount = GetAmountOfSymbolsInTheState(index + 1);
                if (symbolsAmount == observationsAmount)
                    States.Add(new State() { Name = StateEnum.Insert, EmissionProbabilities = InitEmissionProbabilities(), TransitionProbabilities = InitTransitionProbabilities() });
            }

            CreateStates(index + 1);
        }

        private void CreateProbabilities(int index = 0, int insertStateCounter = 0)
        {
            var observationSize = Observations[0].Length;
            if (index == observationSize)
                return;

            var observationsAmount = Observations.Count;
            var symbols = InitSymbols();
            var probabilities = InitEmissionProbabilities();
            var symbolsAmount = GetAmountOfSymbolsInTheState(index);
            State state;

            state = States[index - insertStateCounter];
            if (symbolsAmount != observationsAmount)
            {
                insertStateCounter++;
                var nextSymbolsAmount = GetAmountOfSymbolsInTheState(index + 1);
                if (nextSymbolsAmount == observationsAmount)
                    insertStateCounter--;
            }

            for (int j = 0; j < observationsAmount; j++)
            {
                var sequence = Observations[j].ToCharArray();
                var symbol = sequence[index];
                CreateProbabilityPerState(symbol, observationsAmount, ref symbols, ref probabilities);
            }

            SetProbabilities(state.EmissionProbabilities, probabilities);
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
                    { 'A', 0.0 },
                    { 'C', 0.0 },
                    { 'G', 0.0 },
                    { 'T', 0.0 },
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

        private int GetAmountOfSymbolsInTheState(int index)
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

        private void CreateTransitions()
        {
            for (int i = 0; i < States.Count - 1; i++)
            {
                State state = States[i];

                if (state.Name == StateEnum.Match || state.Name == StateEnum.Insert)
                    CreateTransitionsStates(i, state);
                else if (state.Name == StateEnum.Delete)
                    continue;
            }
        }

        private void CreateTransitionsStates(int index, State state)
        {
            var symbolsAmount = GetAmountOfSymbolsInTheState(index + 1);
            var percentageOfMachState = Convert.ToDouble(symbolsAmount) / Convert.ToDouble(Observations.Count);
            var percentageOfInsertState = 1d - percentageOfMachState;

            state.TransitionProbabilities = new Dictionary<StateEnum, double>()
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

        private List<int> GetIndexesInsertStates()
        {
            var indexes = new List<int>();
            var observationSize = Observations[0].Length;
            for (int i = 0; i < observationSize; i++)
            {
                var symbolsAmount = GetAmountOfSymbolsInTheState(i);
                if (symbolsAmount != Observations.Count)
                    indexes.Add(i);
            }
            return indexes;
        }

        private double ForwardViterbi(List<char> observations)
        {
            var prob = 1d;
            var insertStateCounter = 0;
            var indexesInsertState = GetIndexesInsertStates();

            for (int i = 0; i < observations.Count; i++)
            {
                if (indexesInsertState != null && indexesInsertState.Contains(i))
                    insertStateCounter = indexesInsertState.Count - 1;

                var symbol = observations[i];
                var state = States[i - insertStateCounter];
                var emissionProbability = 0d;
                var transitionProbabilities = 0d;

                if (state.TransitionProbabilities != null)
                    transitionProbabilities = state.TransitionProbabilities[state.Name];

                if (!Constants.Gaps.Contains(symbol))
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