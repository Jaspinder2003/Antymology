using System;
using System.Collections.Generic;
using UnityEngine;

namespace Antymology.Components.Agents
{
    public class Genome
    {
        public float[] Weights;

        public Genome(int size)
        {
            Weights = new float[size];
            for (int i = 0; i < size; i++)
            {
                Weights[i] = UnityEngine.Random.Range(-1.0f, 1.0f);
            }
        }

        public Genome(float[] weights)
        {
            Weights = new float[weights.Length];
            System.Array.Copy(weights, Weights, weights.Length);
        }

        public static Genome Crossover(Genome parent1, Genome parent2)
        {
            float[] newWeights = new float[parent1.Weights.Length];
            int crossoverPoint = UnityEngine.Random.Range(0, parent1.Weights.Length);

            for (int i = 0; i < parent1.Weights.Length; i++)
            {
                if (i < crossoverPoint)
                    newWeights[i] = parent1.Weights[i];
                else
                    newWeights[i] = parent2.Weights[i];
            }
            return new Genome(newWeights);
        }

        public void Mutate(float rate, float strength)
        {
            for (int i = 0; i < Weights.Length; i++)
            {
                if (UnityEngine.Random.value < rate)
                {
                    Weights[i] += UnityEngine.Random.Range(-strength, strength);
                }
            }
        }

        public float[] FeedForward(float[] inputs)
        {
            // Simple Matrix Multiplication
            // Assumes Weights length = inputs * outputs
            // We verify length usage implicitly or by convention
            
             // Let's assume we know output count = Weights.Length / inputs.Length
             if (inputs.Length == 0) return new float[0];
             
             int outputCount = Weights.Length / inputs.Length;
             float[] outputs = new float[outputCount];
             
             for (int o = 0; o < outputCount; o++)
             {
                 float sum = 0;
                 for (int i = 0; i < inputs.Length; i++)
                 {
                     sum += inputs[i] * Weights[o * inputs.Length + i];
                 }
                 outputs[o] = (float)System.Math.Tanh(sum); // Activation
             }
             return outputs;
        }
    }
}
