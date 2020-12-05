using System;

namespace AIS.ClonalgPR.Measures
{
    public class HiddenMarkovModelTest
    {
        private int states; // Number of states in a path
        private int symbols; // Alphabet
        private double[,] probabilityDistribution; // Probability distribution of amino acids in state q - matrix B NM
        private double[,] probabilityTransition;   // Probability of a transition from state q to r - matrix A N^2 
        private double[] pi; // Initial state probabilities

        public HiddenMarkovModelTest(int symbols, int states)
        {
            this.symbols = symbols;
            this.states = states;
            Initialize();
        }

        private void Initialize()
        {
            probabilityTransition = new double[states, states];
            for (int i = 0; i < states; i++)
                for (int j = 0; j < states; j++)
                    probabilityTransition[i, j] = 1.0 / states;

            probabilityDistribution = new double[states, symbols];
            for (int i = 0; i < states; i++)
                for (int j = 0; j < symbols; j++)
                    probabilityDistribution[i, j] = 1.0 / symbols;

            pi = new double[states];
            pi[0] = 1.0;
        }

        public double Learn(int[][] observations, double tolerance, int iterations = 0)
        {
            if (iterations == 0 && tolerance == 0)
                throw new ArgumentException("Iterations and limit cannot be both zero.");

            int numberOfObservations = observations.Length;
            int currentIteration = 1;
            bool stop = false;

            // Initialization
            double[][,,] epsilon = new double[numberOfObservations][,,]; // also referred as ksi or psi
            double[][,] gamma = new double[numberOfObservations][,];

            Initialization(numberOfObservations, observations, ref epsilon, ref gamma);

            // Calculate initial model log-likelihood
            double oldLikelihood = Double.MinValue;
            double newLikelihood = 0;

            do
            {
                for (int i = 0; i < numberOfObservations; i++)
                {
                    var sequence = observations[i];
                    int sequenceSize = sequence.Length;
                    double[] scaling;

                    double[,] fwd = Forward(observations[i], out scaling);
                    double[,] bwd = Backward(observations[i], scaling);

                    CalculateGammaValuesForNextComputations(i, sequenceSize, fwd, bwd, ref gamma);
                    CalculateEpsilonValuesForNextComputations(i, sequence, fwd, bwd, ref epsilon);
                    newLikelihood = ComputeLogLikelihood(scaling);
                }
                newLikelihood /= observations.Length;

                if (CheckConvergence(oldLikelihood, newLikelihood,
                    currentIteration, iterations, tolerance))
                {
                    stop = true;
                }

                else
                {
                    currentIteration++;
                    oldLikelihood = newLikelihood;
                    newLikelihood = 0.0;

                    ReEstimationOfInitialStateProbabilities(numberOfObservations, gamma);
                    ReEstimationOfTransitionProbabilities(numberOfObservations, observations, gamma, epsilon);
                    ReEstimationEmissionProbabilities(numberOfObservations, observations, gamma);
                }
            } while (!stop);

            Console.WriteLine("currentIteration = " + currentIteration);
            Console.WriteLine("newLikelihood = " + newLikelihood);

            return newLikelihood;
        }

        public double Evaluate(int[] observations, bool logarithm = false)
        {
            if (observations == null)
                throw new ArgumentNullException("It is not possible to evaluate null observations");

            if (observations.Length == 0)
                return 0.0;

            double likelihood = 0;
            double[] coefficients;

            Forward(observations, out coefficients);

            for (int i = 0; i < coefficients.Length; i++)
                likelihood += Math.Log(coefficients[i], 10);

            return logarithm ? likelihood : Math.Exp(likelihood);
        }

        private void Initialization(int numberOfObservations, int[][] observations, ref double[][,,] epsilon, ref double[][,] gamma)
        {
            for (int i = 0; i < numberOfObservations; i++)
            {
                int T = observations[i].Length;
                epsilon[i] = new double[T, states, states];
                gamma[i] = new double[T, states];
            }
        }

        private void CalculateGammaValuesForNextComputations(int iteration, int sequenceSize, double[,] fwd, double[,] bwd, ref double[][,] gamma)
        {
            for (int t = 0; t < sequenceSize; t++)
            {
                double s = 0;

                for (int k = 0; k < states; k++)
                    s += gamma[iteration][t, k] = fwd[t, k] * bwd[t, k];

                if (s != 0) // Scaling
                {
                    for (int k = 0; k < states; k++)
                        gamma[iteration][t, k] /= s;
                }
            }
        }

        private void CalculateEpsilonValuesForNextComputations(int iteration, int[] sequence, double[,] fwd, double[,] bwd, ref double[][,,] epsilon)
        {
            var sequenceSize = sequence.Length;
            for (int t = 0; t < sequenceSize - 1; t++)
            {
                double s = 0;

                for (int k = 0; k < states; k++)
                    for (int l = 0; l < states; l++)
                        s += epsilon[iteration][t, k, l] = fwd[t, k] * probabilityTransition[k, l] * bwd[t + 1, l] * probabilityDistribution[l, sequence[t + 1]];

                if (s != 0) // Scaling
                {
                    for (int k = 0; k < states; k++)
                        for (int l = 0; l < states; l++)
                            epsilon[iteration][t, k, l] /= s;
                }
            }
        }
        private double ComputeLogLikelihood(double[] scaling)
        {
            var likelihood = 0.0;
            for (int t = 0; t < scaling.Length; t++)
                likelihood += Math.Log(scaling[t], 10);

            return likelihood;
        }

        private void ReEstimationOfInitialStateProbabilities(int numberOfObservations, double[][,] gamma)
        {
            for (int k = 0; k < states; k++)
            {
                double sum = 0;
                for (int i = 0; i < numberOfObservations; i++)
                    sum += gamma[i][0, k];
                pi[k] = sum / numberOfObservations;
            }
        }
        private void ReEstimationEmissionProbabilities(int numberOfObservations, int[][] observations, double[][,] gamma)
        {
            // 3.3 Re-estimation of emission probabilities
            for (int i = 0; i < states; i++)
            {
                for (int j = 0; j < symbols; j++)
                {
                    double den = 0, num = 0;

                    for (int k = 0; k < numberOfObservations; k++)
                    {
                        int T = observations[k].Length;

                        for (int l = 0; l < T; l++)
                        {
                            if (observations[k][l] == j)
                                num += gamma[k][l, i];
                        }

                        for (int l = 0; l < T; l++)
                            den += gamma[k][l, i];
                    }

                    // avoid locking a parameter in zero.
                    probabilityDistribution[i, j] = (num == 0) ? 1e-10 : num / den;
                }
            }
        }

        private void ReEstimationOfTransitionProbabilities(int numberOfSequences, int[][] observations, double[][,] gamma, double[][,,] epsilon)
        {
            for (int i = 0; i < states; i++)
            {
                for (int j = 0; j < states; j++)
                {
                    double den = 0, num = 0;

                    for (int k = 0; k < numberOfSequences; k++)
                    {
                        int T = observations[k].Length;

                        for (int l = 0; l < T - 1; l++)
                            num += epsilon[k][l, i, j];

                        for (int l = 0; l < T - 1; l++)
                            den += gamma[k][l, i];
                    }

                    probabilityTransition[i, j] = (den != 0) ? num / den : 0.0;
                }
            }
        }

        private double[,] Forward(int[] observations, out double[] scaling)
        {
            int sequenceSize = observations.Length;
            double[,] fwd = new double[sequenceSize, states];
            scaling = new double[sequenceSize];

            // 1. Initialization
            for (int i = 0; i < states; i++)
                scaling[0] += fwd[0, i] = pi[i] * probabilityDistribution[i, observations[0]];

            if (scaling[0] != 0)
            {
                for (int i = 0; i < states; i++)
                    fwd[0, i] = fwd[0, i] / scaling[0];
            }


            // 2. Induction
            for (int t = 1; t < sequenceSize; t++)
            {
                for (int i = 0; i < states; i++)
                {
                    double p = probabilityDistribution[i, observations[t]];

                    double sum = 0.0;
                    for (int j = 0; j < states; j++)
                        sum += fwd[t - 1, j] * probabilityTransition[j, i];
                    fwd[t, i] = sum * p;

                    scaling[t] += fwd[t, i];
                }

                if (scaling[t] != 0)
                {
                    for (int i = 0; i < states; i++)
                        fwd[t, i] = fwd[t, i] / scaling[t];
                }
            }

            return fwd;
        }
        private double[,] Backward(int[] observations, double[] scaling)
        {
            int T = observations.Length;

            double[,] bwd = new double[T, states];

            // For backward variables, we use the same scale factors
            //   for each time t as were used for forward variables.

            // 1.Initialization
            for (int i = 0; i < states; i++)
                bwd[T - 1, i] = 1.0 / scaling[T - 1];

            // 2. Induction
            for (int t = T - 2; t >= 0; t--)
            {
                for (int i = 0; i < states; i++)
                {
                    double sum = 0;
                    for (int j = 0; j < states; j++)
                        sum += probabilityTransition[i, j] * probabilityDistribution[j, observations[t + 1]] * bwd[t + 1, j];
                    bwd[t, i] += sum / scaling[t];
                }
            }

            return bwd;
        }

        private static bool CheckConvergence(double oldLikelihood, double newLikelihood,
                int currentIteration, int maxIterations, double tolerance)
        {
            // Update and verify stop criteria
            if (tolerance > 0)
            {
                // Stopping criteria is likelihood convergence
                if (Math.Abs(oldLikelihood - newLikelihood) <= tolerance)
                    return true;

                if (maxIterations > 0)
                {
                    // Maximum iterations should also be respected
                    if (currentIteration >= maxIterations)
                        return true;
                }
            }
            else
            {
                // Stopping criteria is number of iterations
                if (currentIteration == maxIterations)
                    return true;
            }

            // Check if we have reached an invalid state
            if (Double.IsNaN(newLikelihood) || Double.IsInfinity(newLikelihood))
            {
                return true;
            }

            return false;
        }
    }

}
