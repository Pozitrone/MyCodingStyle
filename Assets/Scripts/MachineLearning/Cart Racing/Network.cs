﻿using System;
using System.Collections.Generic;
using System.Linq;
using MachineLearning.NeuralNetwork;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace MachineLearning.Cart_Racing
{
    public class Network
    {
        private readonly int _amountOfInputs;
        private readonly int _amountOfHiddenLayers;
        public double learningRate;

        private readonly List<NeuronLayer> _layers = new List<NeuronLayer>();

        public Network(int amountOfInputs, int amountOfOutputs, int amountOfHiddenLayers, int neuronsPerHiddenLayer,
            double learningRate)
        {
            _amountOfInputs = amountOfInputs;
            int amountOfOutputs1 = amountOfOutputs;
            _amountOfHiddenLayers = amountOfHiddenLayers;
            int neuronsPerHiddenLayer1 = neuronsPerHiddenLayer;
            this.learningRate = Mathf.Clamp01((float) learningRate);

            if (_amountOfHiddenLayers > 0)
            {
                _layers.Add(new NeuronLayer(neuronsPerHiddenLayer1, _amountOfInputs));

                for (int i = 0; i < _amountOfHiddenLayers - 1; i++)
                {
                    _layers.Add(new NeuronLayer(neuronsPerHiddenLayer1, neuronsPerHiddenLayer1));
                }

                _layers.Add(new NeuronLayer(amountOfOutputs1, neuronsPerHiddenLayer1));
            }
            else
            {
                _layers.Add(new NeuronLayer(amountOfOutputs1, _amountOfInputs));
            }
        }

        public List<double> Run(List<double> inputValues, List<double> desiredOutputs, bool updateWeights = true)
        {
            List<double> outputs = new List<double>();

            if (inputValues.Count != _amountOfInputs)
            {
                throw new ArgumentOutOfRangeException(nameof(inputValues),
                    "There must be the same amount of input values as inputs.");
            }

            List<double> inputs = new List<double>(inputValues);
            for (int layerIndex = 0; layerIndex < _amountOfHiddenLayers + 1; layerIndex++)
            {
                if (layerIndex > 0)
                {
                    inputs = new List<double>(outputs);
                }

                outputs.Clear();

                for (int neuronIndex = 0; neuronIndex < _layers[layerIndex].amountOfNeurons; neuronIndex++)
                {
                    double dotProduct = 0;
                    _layers[layerIndex].neurons[neuronIndex].inputs.Clear();

                    for (int inputIndex = 0;
                        inputIndex < _layers[layerIndex].neurons[neuronIndex].amountOfInputs;
                        inputIndex++)
                    {
                        _layers[layerIndex].neurons[neuronIndex].inputs.Add(inputs[inputIndex]);
                        dotProduct += _layers[layerIndex].neurons[neuronIndex].weights[inputIndex] * inputs[inputIndex];
                    }

                    dotProduct -= _layers[layerIndex].neurons[neuronIndex].bias;

                    NeuronLayerType neuronLayerType = layerIndex == _amountOfHiddenLayers
                        ? NeuronLayerType.Output
                        : NeuronLayerType.Hidden;

                    _layers[layerIndex].neurons[neuronIndex].output = ActivationFunction(dotProduct, neuronLayerType);
                    outputs.Add(_layers[layerIndex].neurons[neuronIndex].output);
                }
            }

            if (updateWeights) UpdateWeights(outputs, desiredOutputs);

            return outputs;
        }

        private void UpdateWeights(List<double> outputs, List<double> desiredOutputs)
        {
            for (int layerIndex = _amountOfHiddenLayers; layerIndex >= 0; layerIndex--)
            {
                for (int neuronIndex = 0; neuronIndex < _layers[layerIndex].amountOfNeurons; neuronIndex++)
                {
                    double error;
                    if (layerIndex == _amountOfHiddenLayers)
                    {
                        error = desiredOutputs[neuronIndex] - outputs[neuronIndex];
                        _layers[layerIndex].neurons[neuronIndex].errorGradient =
                            outputs[neuronIndex] * (1 - outputs[neuronIndex]) * error;
                    }
                    else
                    {
                        _layers[layerIndex].neurons[neuronIndex].errorGradient =
                            _layers[layerIndex].neurons[neuronIndex].output *
                            (1 - _layers[layerIndex].neurons[neuronIndex].output);

                        double errorGradientSum = 0;

                        for (int nextLayerIndex = 0;
                            nextLayerIndex < _layers[layerIndex + 1].amountOfNeurons;
                            nextLayerIndex++)
                        {
                            errorGradientSum += _layers[layerIndex + 1].neurons[nextLayerIndex].errorGradient *
                                                _layers[layerIndex + 1].neurons[nextLayerIndex].weights[neuronIndex];
                        }

                        _layers[layerIndex].neurons[neuronIndex].errorGradient *= errorGradientSum;
                    }

                    for (int inputIndex = 0;
                        inputIndex < _layers[layerIndex].neurons[neuronIndex].amountOfInputs;
                        inputIndex++)
                    {
                        if (layerIndex == _amountOfHiddenLayers)
                        {
                            error = desiredOutputs[neuronIndex] - outputs[neuronIndex];
                            _layers[layerIndex].neurons[neuronIndex].weights[inputIndex] +=
                                learningRate * _layers[layerIndex].neurons[neuronIndex].inputs[inputIndex] * error;
                        }
                        else
                        {
                            _layers[layerIndex].neurons[neuronIndex].weights[inputIndex] +=
                                learningRate * _layers[layerIndex].neurons[neuronIndex].inputs[inputIndex] *
                                _layers[layerIndex].neurons[neuronIndex].errorGradient;
                        }
                    }

                    _layers[layerIndex].neurons[neuronIndex].bias +=
                        learningRate * -1 * _layers[layerIndex].neurons[neuronIndex].errorGradient;
                }
            }
        }

        private static double ActivationFunction(double value)
        {
            return ActivationFunctions.Sigmoid(value);
        }

        private static double ActivationFunction(double value, NeuronLayerType layer)
        {
            return layer == NeuronLayerType.Output
                ? ActivationFunctions.Sigmoid(value)
                : ActivationFunctions.ReLu(value);
        }

        public List<double> Train(List<double> inputs, List<double> outputs)
        {
            return Run(inputs, outputs, true);
        }

        public List<double> Execute(List<double> inputs, List<double> outputs)
        {
            return Run(inputs, outputs, false);
        }


        public void LoadWeightsFromString(string currentWeights)
        {
            if(currentWeights == "") return;
            string[] weightValues = currentWeights.Split(',');
            int w = 0;
            foreach (Neuron n in _layers.SelectMany(l => l.neurons))
            {
                for(int i = 0; i < n.weights.Count; i++)
                {
                    n.weights[i] = Convert.ToDouble(weightValues[w]);
                    w++;
                }
                n.bias = Convert.ToDouble(weightValues[w]);
                w++;
            }
        }

        public string GetWeights()
        {
            string weightStr = "";
            foreach (Neuron n in _layers.SelectMany(l => l.neurons))
            {
                weightStr = n.weights.Aggregate(weightStr, (current, w) => current + (w + ","));
                weightStr += n.bias + ",";
            }
            return weightStr;
        }
    }
}