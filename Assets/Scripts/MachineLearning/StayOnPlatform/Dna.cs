﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MachineLearning.StayOnPlatform
{
    [Serializable]
    public class Dna
    {
        [SerializeField] private List<int> genes = new List<int>();
        private readonly int _dnaLength;
        private readonly List<int> _maxValues;
        
        public Dna(int length, List<int> values)
        {
            _dnaLength = length;
            _maxValues = values;
            SetRandom();
        }

        private void SetRandom()
        {
            genes.Clear();
            for (int i = 0; i < _dnaLength; i++)
            {
                genes.Add(Random.Range(0, _maxValues[i]));
            }
        }

        public void SetInt(int index, int value)
        {
            genes[index] = value;
        }

        public void Combine(Dna dna1, Dna dna2)
        {
            for (int i = 0; i < _dnaLength; i++)
            {
                if (i % 2 == 0)
                {
                    int gene = dna1.genes[i];
                    genes[i] = gene;
                }
                else
                {
                    int gene = dna2.genes[i];
                    genes[i] = gene;
                }
            }
        }

        public void Mutate()
        {
            int index = Random.Range(0, _dnaLength);
            genes[index] = Random.Range(0, _maxValues[index]);
        }

        public int GetGene(int index)
        {
            return genes[index];
        }
    }
}
