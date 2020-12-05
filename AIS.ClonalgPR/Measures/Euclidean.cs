using System;
using System.Collections.Generic;
using System.Linq;
using AIS.ClonalgPR.Models;

namespace AIS.ClonalgPR.Measures
{
    public class Euclidean : IDistance
    {
        private int qtdAminoAcids = Constants.Aminoacids.Length;
        private double[,] probabilityMatrixSequenceA = new double[20, 20];
        private double[,] probabilityMatrixSequenceB = new double[20, 20];
        private double[] occurrencesAiSequenceA = new double[20];
        private double[] occurrencesAiSequenceB = new double[20];
        private double[,] occurrencesAiAjSequenceA = new double[20, 20];
        private double[,] occurrencesAiAjSequenceB = new double[20, 20];
        private double[] sequenceCharacteristicVectorA = new double[440];
        private double[] sequenceCharacteristicVectorB = new double[440];
        private double[] contentRatioVectorSequenceA = new double[20];
        private double[] contentRatioVectorSequenceB = new double[20];
        private double[] positionRatioVectorSequenceA = new double[20];
        private double[] positionRatioVectorSequenceB = new double[20];

        public double Calculate(char[] sequenceA, char[] sequenceB)
        {
            BuildFeatureVectors(sequenceA, sequenceB);

            var distance = 0.0;
            for (int i = 0; i < sequenceCharacteristicVectorA.Length; i++)
                distance += Math.Pow(sequenceCharacteristicVectorA[i] - sequenceCharacteristicVectorB[i], 2);

            return Math.Sqrt(distance);
        }

        public double CalculateCloneRate(double affinity, int length)
        {
            return Constants.Random.NextDouble();
        }

        public double CalculateMutationRate(double affinity, int length)
        {
            return Constants.Random.NextDouble();
        }

        private void BuildFeatureVectors(char[] sequenceA, char[] sequenceB)
        {
            var sequenceSize = sequenceA.Length;
            occurrencesAiSequenceA = SetOccurrenceAiSequence(sequenceA, sequenceSize);
            occurrencesAiAjSequenceA = SetOccurrenceAiAjSequence(sequenceA, sequenceSize);
            probabilityMatrixSequenceA = BuildPseudoMarkovVector(sequenceA, sequenceSize, occurrencesAiAjSequenceA, occurrencesAiSequenceA);
            contentRatioVectorSequenceA = BuildContentRatioVector(sequenceSize);
            positionRatioVectorSequenceA = BuildPositionRatioVector(sequenceA, sequenceSize);
            sequenceCharacteristicVectorA = ConcatenateVectors(probabilityMatrixSequenceA, contentRatioVectorSequenceA, positionRatioVectorSequenceA);

            sequenceSize = sequenceB.Length;
            occurrencesAiSequenceB = SetOccurrenceAiSequence(sequenceB, sequenceSize);
            occurrencesAiAjSequenceB = SetOccurrenceAiAjSequence(sequenceB, sequenceSize);
            probabilityMatrixSequenceB = BuildPseudoMarkovVector(sequenceB, sequenceSize, occurrencesAiAjSequenceB, occurrencesAiSequenceB);
            contentRatioVectorSequenceB = BuildContentRatioVector(sequenceSize);
            positionRatioVectorSequenceB = BuildPositionRatioVector(sequenceB, sequenceSize);
            sequenceCharacteristicVectorB = ConcatenateVectors(probabilityMatrixSequenceB, contentRatioVectorSequenceB, positionRatioVectorSequenceB);
        }

        private double[] SetOccurrenceAiSequence(char[] sequenceA, int sequenceSize)
        {
            var occurrencesAi = new double[20];
            for (int i = 0; i < qtdAminoAcids; i++)
            {
                var sumAllPositionsAi = 0;
                for (int j = 0; j < sequenceSize; j++)
                {
                    if (Constants.Aminoacids[i] == sequenceA[j])
                    {
                        occurrencesAi[i] += 1;
                        sumAllPositionsAi += j;
                    }
                }
            }
            return occurrencesAi;
        }

        private double[,] SetOccurrenceAiAjSequence(char[] sequenceA, int sequenceSize)
        {
            var occurrencesAiAj = new double[20, 20];
            for (int i = 0; i < qtdAminoAcids; i++)
            {
                for (int j = 0; j < qtdAminoAcids; j++)
                {
                    for (int k = 0; k < sequenceSize - 1; k++)
                    {
                        var orderedAminoAcidPair = Constants.Aminoacids[i].ToString() + Constants.Aminoacids[j].ToString();
                        var orderedAminoAcidPairSequence = sequenceA[k].ToString() + sequenceA[k + 1].ToString();
                        if (orderedAminoAcidPair == orderedAminoAcidPairSequence)
                            occurrencesAiAj[i, j] += 1;
                    }
                }
            }
            return occurrencesAiAj;
        }

        private int GetSumAllPositionsAi(char aminoacid, char[] sequenceA, int sequenceSize)
        {
            var sumAllPositionsAi = 0;
            for (int j = 0; j < sequenceSize; j++)
            {
                if (aminoacid == sequenceA[j])
                    sumAllPositionsAi += j;
            }
            return sumAllPositionsAi;
        }

        private double[,] BuildPseudoMarkovVector(char[] sequence, int sequenceSize, double[,] occurrencesAiAj, double[] occurrencesAi)
        {
            var probabilityMatrix = new double[20, 20];
            for (int i = 0; i < qtdAminoAcids; i++)
            {
                for (int j = 0; j < qtdAminoAcids; j++)
                {
                    if (occurrencesAi[i] == 0 || ((occurrencesAi[i] - 1) == 0))
                        probabilityMatrix[i, j] = 0;
                    else if (Constants.Aminoacids[i] != sequence[sequenceSize - 1])
                        probabilityMatrix[i, j] = occurrencesAiAj[i, j] / occurrencesAi[i];
                    else
                        probabilityMatrix[i, j] = occurrencesAiAj[i, j] / (occurrencesAi[i] - 1);
                }
            }
            return probabilityMatrix;
        }

        private double[] BuildContentRatioVector(int sequenceSize)
        {
            var contentRatioVector = new double[20];
            for (int i = 0; i < occurrencesAiSequenceA.Length; i++)
                contentRatioVector[i] = occurrencesAiSequenceA[i] / sequenceSize;

            return contentRatioVector;
        }

        private double[] BuildPositionRatioVector(char[] sequenceA, int sequenceSize)
        {
            var positionRatioVector = new double[20];
            for (int i = 0; i < qtdAminoAcids; i++)
            {
                var sumAllPositionsAi = GetSumAllPositionsAi(Constants.Aminoacids[i], sequenceA, sequenceSize);
                positionRatioVector[i] = (double)2 * sumAllPositionsAi / (sequenceSize * (sequenceSize + 1));
            }
            return positionRatioVector;
        }

        private double[] ConcatenateVectors(double[,] probabilityMatrixSequence, double[] contentRatioVectorSequence, double[] positionRatioVectorSequence)
        {
            var sequenceCharacteristicVector = new double[440];
            var index = 0;
            for (int i = 0; i < qtdAminoAcids; i++)
            {
                for (int j = 0; j < qtdAminoAcids; j++)
                {
                    sequenceCharacteristicVector[index] = probabilityMatrixSequence[i, j];
                    index++;
                }
            }

            for (int z = 0; z < qtdAminoAcids; z++)
            {
                sequenceCharacteristicVector[index] = contentRatioVectorSequence[z];
                index++;
            }

            for (int y = 0; y < qtdAminoAcids; y++)
            {
                sequenceCharacteristicVector[index] = positionRatioVectorSequence[y];
                index++;
            }
            return sequenceCharacteristicVector;
        }

        public bool IsBetterAffinity(double affinityAB, double affinityM)
        {
            return affinityAB < affinityM;
        }

        public List<Antibody> Order(List<Antibody> population, int numberHighAffinity)
        {
            return population.OrderBy(o => o.Affinity).Take(numberHighAffinity).ToList();
        }

    }
}
