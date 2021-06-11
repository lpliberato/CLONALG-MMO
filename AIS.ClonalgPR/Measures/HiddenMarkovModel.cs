using AIS.ClonalgPR.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIS.ClonalgPR.Measures
{
    public class HiddenMarkovModel : IDistance
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

        private int GetObservationsAmount()
        { 
          return Observations.Count;
        }

        private int GetObservationSize()
        {
            return Observations[0].Length;
        }

        private void AddMatchState()
        {
            States.Add(new State()
            {
                Name = StateEnum.Match,
                EmissionProbabilities = InitEmissionProbabilities(),
                TransitionProbabilities = InitTransitionProbabilities()
            });
        }

        private void AddInsertState(int index)
        {
            var observationsAmount = GetObservationsAmount();
            var symbolsAmount = GetAmountOfMatches(index + 1);

            if (symbolsAmount == observationsAmount)
                States.Add(new State()
                {
                    Name = StateEnum.Insert,
                    EmissionProbabilities = InitEmissionProbabilities(),
                    TransitionProbabilities = InitTransitionProbabilities()
                });
        }

        private bool IsLastIndexObservation(int index)
        {
            var observationSize = Observations[0].Length;
            return index == observationSize;
        }

        private void CreateStates(int index = 0)
        {
            if (IsLastIndexObservation(index)) return;

            var observationsAmount = GetObservationsAmount();
            var symbolsAmount = GetAmountOfMatches(index);

            if (symbolsAmount == observationsAmount)
                AddMatchState();
            else
                AddInsertState(index);
            CreateStates(index + 1);
        }

        private void UpdateInsertStateCounterForProbabilities(int index, ref int insertStateCounter)
        {
            var observationsAmount = GetObservationsAmount();
            var symbolsAmount = GetAmountOfMatches(index);

            if (symbolsAmount != observationsAmount)
            {
                insertStateCounter++;
                var nextSymbolsAmount = GetAmountOfMatches(index + 1);
                if (nextSymbolsAmount == observationsAmount)
                    insertStateCounter--;
            }
        }

        private char GetSymbol(int lineIndex, int columnIndex)
        {
            var sequence = Observations[lineIndex].ToCharArray();
            return sequence[columnIndex];
        }

        private State GetCurrentState(int index)
        {
            return States[index];
        }

        private bool IsGap(char symbol)
        {
            return Constants.Gaps.Contains(symbol);
        }

        private int IncreaseObservationAmount(char symbol, Dictionary<char, int> symbols)
        {
            return symbols[symbol] + 1;
        }

        private double CalculateProbabilityBySymbol(char symbol, Dictionary<char, int> symbols)
        {
            var observationsAmount = GetObservationsAmount();
            return (double)symbols[symbol] / observationsAmount;
        }

        private void UpdateTheAmountOfSymbols(char symbol, ref Dictionary<char, int> symbols)
        {
            symbols[symbol] = IncreaseObservationAmount(symbol, symbols);
        }

        private void UpdateSymbolProbability(char symbol, Dictionary<char, int> symbols, ref Dictionary<char, double> probabilities)
        {
            probabilities[symbol] = CalculateProbabilityBySymbol(symbol, symbols);
        }

        private void UpdateProbabilityAndAmountOfSymbols(int lineIndex, int columnIndex, ref Dictionary<char, int> symbols, ref Dictionary<char, double> probabilities)
        {
            var symbol = GetSymbol(lineIndex, columnIndex);
            if (!IsGap(symbol))
            {
                UpdateTheAmountOfSymbols(symbol, ref symbols);
                UpdateSymbolProbability(symbol, symbols, ref probabilities);
            }
        }

        private void UpdateAllProbabilityAndAmountOfSymbols(int columnIndex, ref Dictionary<char, int> symbols, ref Dictionary<char, double> probabilities)
        {
            var observationsAmount = GetObservationsAmount();
            for (int lineIndex = 0; lineIndex < observationsAmount; lineIndex++)
                UpdateProbabilityAndAmountOfSymbols(lineIndex, columnIndex, ref symbols, ref probabilities);
        }

        private void CreateProbabilities(int index = 0, int insertStateCounter = 0)
        {
            if (IsLastIndexObservation(index)) return;

            var symbols = InitSymbols();
            var probabilities = InitEmissionProbabilities();
            State currentState = GetCurrentState(index - insertStateCounter);

            UpdateAllProbabilityAndAmountOfSymbols(index, ref symbols, ref probabilities);
            UpdateEmissionProbabilities(currentState.EmissionProbabilities, probabilities);
            UpdateInsertStateCounterForProbabilities(index, ref insertStateCounter);
            CreateProbabilities(index + 1, insertStateCounter);
        }

        private void UpdateEmissionProbabilities(Dictionary<char, double> emissionProbabilities, Dictionary<char, double> probabilities)
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

        private void UpdateInsertStateCounterForTransitions(ref int insertStateCounter, int index, int observationsAmount)
        {
            var symbolsAmount = GetAmountOfMatches(index);
            var nextSymbolsAmount = GetAmountOfMatches(index + 1);

            if (symbolsAmount != observationsAmount && nextSymbolsAmount != observationsAmount)
                insertStateCounter++;
        }

        private bool IsLastIndexStates(int index)
        {
            return index == States.Count;
        }

        private int GetCurrentIndex(int index, int insertStateCounter)
        {
            return index - insertStateCounter;
        }

        private int GetIndexToTransition(int currentIndex, int previousIndex)
        {
            return previousIndex >= Observations[0].Length - 2 ? currentIndex : previousIndex;
        }

        private void CreateTransitions(int previousIndex = 0, int insertStateCounter = 0)
        {
            var currentIndex = GetCurrentIndex(previousIndex, insertStateCounter);
            if (IsLastIndexStates(currentIndex)) return;

            State currentState = GetCurrentState(currentIndex);
            CreateTransitionsStates(previousIndex, currentIndex, currentState);

            var indexToTransition = GetIndexToTransition(currentIndex, previousIndex);
            UpdateInsertStateCounterForTransitions(ref insertStateCounter, indexToTransition, Observations.Count);
            CreateTransitions(previousIndex + 1, insertStateCounter);
        }

        private int GetAmountOfMatches(int index)
        {
            var count = 0;
            for (int i = 0; i < Observations.Count; i++)
            {
                var symbol = GetSymbol(i, index);

                if (!IsGap(symbol))
                    count++;
            }

            return count;
        }

        private int GetAmountOfInserts(int index)
        {
            var indexes = new List<int>();
            var observationSize = GetObservationSize();
            var observationsAmount = GetObservationsAmount();
            var matchCount = 0;
            var gapCount = 0;

            for (int i = index; i < observationSize; i++)
            {
                for (int j = 0; j < observationsAmount; j++)
                {
                    var symbol = GetSymbol(j,i);

                    if (!IsGap(symbol))
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

        private State GetNextState(int currentIndex)
        {
            return currentIndex < States.Count - 1 ? States[currentIndex + 1] : null;
        }

        private double GetPercentageOfMachState(int symbolsAmount)
        { 
            return Convert.ToDouble(symbolsAmount) / Convert.ToDouble(Observations.Count);
        }

        private double GetPercentageOfInsertState(double percentageOfMachState)
        {
            return 1d - percentageOfMachState;
        }

        private double CalculatePercentageOfMachState(int index, int currentIndex, State currentState, State nextState)
        {
            double percentageOfMachState = 0d;
            var symbolsAmount = 0;

            if (currentState.Name == StateEnum.Match && (nextState == null || nextState.Name == StateEnum.Match))
            {
                symbolsAmount = GetAmountOfMatches(index);
                percentageOfMachState = GetPercentageOfMachState(symbolsAmount);
            }
            else if (currentState.Name == StateEnum.Match && nextState.Name == StateEnum.Insert)
            {
                symbolsAmount = GetAmountOfInserts(currentIndex + 1);
                var percentageOfInsertState = GetPercentageOfMachState(symbolsAmount);
                percentageOfMachState = GetPercentageOfInsertState(percentageOfInsertState);
            }
            else if (currentState.Name == StateEnum.Insert && nextState.Name == StateEnum.Match)
            {
                symbolsAmount = GetAmountOfInserts(currentIndex);
                percentageOfMachState = GetPercentageOfMachState(symbolsAmount);
            }

            return percentageOfMachState;
        }

        private double CalculatePercentageOfInsertState(double percentageOfMachState)
        {
            return GetPercentageOfInsertState(percentageOfMachState);
        }

        private void UpdateTransitions(State currentState, double percentageOfMachState, double percentageOfInsertState)
        {
            currentState.TransitionProbabilities = new Dictionary<StateEnum, double>()
                        {
                            { StateEnum.Match, percentageOfMachState },
                            { StateEnum.Insert, percentageOfInsertState },
                            { StateEnum.Delete, 0.0 }
                        };
        }

        private void CreateTransitionsStates(int previousIndex, int currentIndex, State currentState)
        {
            if (IsLastIndexStates(currentIndex)) return;

            var nextState = GetNextState(currentIndex);
            var percentageOfMachState = CalculatePercentageOfMachState(previousIndex, currentIndex, currentState, nextState);
            var percentageOfInsertState = CalculatePercentageOfInsertState(percentageOfMachState);

            UpdateTransitions(currentState, percentageOfMachState, percentageOfInsertState);
        }

        public double CalculateTotalProbability(char[] observations)
        {
            return ForwardViterbi(observations);
        }

        public double CalculateLogOdds(char[] observations)
        {
            var probability = CalculateTotalProbability(observations);
            return Math.Log(probability) - (observations.Length * Math.Log(1d / Constants.DNA.Length));
        }

        private double ForwardViterbi(char[] observation)
        {
            var prob = 1d;
            for (int i = 0; i < observation.Length; i++)
            {
                var symbol = observation[i];
                var currentState = GetCurrentState(i);
                var emissionProbability = 0d;
                var transitionProbabilities = 0d;

                if (currentState.TransitionProbabilities != null)
                    transitionProbabilities = currentState.TransitionProbabilities.Max(m => m.Value);

                if (currentState.EmissionProbabilities != null)
                    emissionProbability = currentState.EmissionProbabilities[symbol];

                if (emissionProbability > 0)
                    prob *= emissionProbability;

                if (transitionProbabilities > 0)
                    prob *= transitionProbabilities;
            }

            return prob;
        }

        public double Calculate(char[] sequenceA, char[] sequenceB)
        {
            return ForwardViterbi(sequenceB);
        }

        public double CalculateCloneRate(double affinity, int length)
        {
            return affinity;
        }

        public double CalculateMutationRate(double affinity, int length)
        {
            return 1 - affinity;
        }

        public bool IsBetterAffinity(double affinityAB, double affinityM)
        {
            return affinityAB > affinityM;
        }

        public List<Antibody> Order(List<Antibody> population, int numberHighAffinity)
        {
            return population.OrderByDescending(o => o.Affinity).Take(numberHighAffinity).ToList();
        }
    }
}