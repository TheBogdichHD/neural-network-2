using System;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Distributions;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NeuralNetwork1
{
    public class StudentNetwork : BaseNetwork
    {
        private int[] structure;                   // Network structure
        private Matrix<double>[] weights;          // Weights between layers
        private Vector<double>[] biases;           // Biases for each layer
        private Vector<double>[] activations;      // Neuron activations for each layer
        private double learningRate = 0.1;

        public Stopwatch stopWatch = new Stopwatch();

        public StudentNetwork(int[] structure)
        {
            this.structure = structure;
            weights = new Matrix<double>[structure.Length - 1];
            biases = new Vector<double>[structure.Length - 1];
            activations = new Vector<double>[structure.Length];
            activations[0] = Vector<double>.Build.Dense(structure[0]);
            for (int i = 1; i < structure.Length; i++)
            {
                activations[i] = Vector<double>.Build.Dense(structure[i]);
                weights[i - 1] = Matrix<double>.Build.Random(structure[i], structure[i - 1], new ContinuousUniform(-1, 1));
                biases[i - 1] = Vector<double>.Build.Random(structure[i], new ContinuousUniform(-1, 1));
            }
        }

        protected override double[] Compute(double[] input)
        {

            MathNet.Numerics.Control.UseMultiThreading();
            activations[0] = Vector<double>.Build.DenseOfArray(input);

            for (int layer = 1; layer < structure.Length; layer++)
            {
                activations[layer] = (weights[layer - 1] * activations[layer - 1] + biases[layer - 1]).Map(Sigmoid);
            }

            return activations.Last().ToArray();
        }

        public override int Train(Sample sample, double acceptableError, bool parallel)
        {
            int iterations = 0;
            double error;

            do
            {
                iterations++;
                error = TrainSample(sample.input, sample.Output);
            } while (error > acceptableError);

            return iterations;
        }

        private double TrainSample(double[] input, double[] target)
        {
            MathNet.Numerics.Control.UseMultiThreading();
            Compute(input);

            Vector<double> outputError = Vector<double>.Build.DenseOfArray(target) - activations.Last();
            double totalError = outputError.PointwisePower(2).Sum();

            Vector<double>[] deltas = new Vector<double>[structure.Length - 1];
            deltas[deltas.Length - 1] = outputError.PointwiseMultiply(activations.Last().Map(SigmoidDerivative));

            for (int layer = structure.Length - 2; layer > 0; layer--)
            {
                deltas[layer - 1] = (weights[layer].Transpose() * deltas[layer]).PointwiseMultiply(activations[layer].Map(SigmoidDerivative));
            }

            Parallel.For(0, structure.Length - 1, layer =>
            {
                weights[layer] += learningRate * deltas[layer].OuterProduct(activations[layer]);
                biases[layer] += learningRate * deltas[layer];
            });

            return totalError;
        }

        public override double TrainOnDataSet(SamplesSet samplesSet, int epochsCount, double acceptableError, bool parallel)
        {
            MathNet.Numerics.Control.UseNativeMKL();
            double totalError = double.PositiveInfinity;

            stopWatch.Restart();

            for (int epoch = 0; epoch < epochsCount && totalError > acceptableError; epoch++)
            {
                totalError = 0.0;

                foreach (var sample in samplesSet.samples)
                {
                    totalError += TrainSample(sample.input, sample.Output);
                }

                totalError /= samplesSet.Count;
                OnTrainProgress((double)epoch / epochsCount, totalError, stopWatch.Elapsed);
            }

            OnTrainProgress(1.0, totalError, stopWatch.Elapsed);

            stopWatch.Stop();

            return totalError;
        }

        private double Sigmoid(double x) => 1.0 / (1.0 + Math.Exp(-x));
        private double SigmoidDerivative(double x) => x * (1.0 - x);
    }
}
