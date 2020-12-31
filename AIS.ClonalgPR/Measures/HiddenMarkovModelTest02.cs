﻿using AIS.ClonalgPR.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIS.ClonalgPR.Measures
{
    public class HiddenMarkovModelTest02
    {
        private List<string> Sequences { get; set; }
        private List<Dictionary<char, int>> Aminoacids { get; set; }
        private List<Dictionary<char, double>> Probabilities { get; set; }
        private List<List<Dictionary<StatesEnum, double>>> Transitions { get; set; }

        public HiddenMarkovModelTest02(List<string> sequences)
        {
            Sequences = sequences;
            Aminoacids = new List<Dictionary<char, int>>();
            Probabilities = new List<Dictionary<char, double>>();
            Transitions = new List<List<Dictionary<StatesEnum, double>>>();
        }

        public void Train()
        {
            CreateStates();

            var qtdSequences = Sequences.Count;
            var sequenceSize = Sequences[0].Length;

            for (int i = 0; i < sequenceSize; i++)
            {
                var aminoacids = new Dictionary<char, int>();
                var probabilities = new Dictionary<char, double>();
                for (int j = 0; j < qtdSequences; j++)
                {
                    var sequence = Sequences[j].ToCharArray();
                    var aminoacid = sequence[i];
                    if (!aminoacids.ContainsKey(aminoacid))
                    {
                        aminoacids.Add(aminoacid, 1);
                        probabilities.Add(aminoacid, (double)1 / qtdSequences);
                    }
                    else
                    {
                        aminoacids[aminoacid] += 1;
                        probabilities[aminoacid] = (double)aminoacids[aminoacid] / qtdSequences;
                    }
                }
                Aminoacids.Add(aminoacids);
                Probabilities.Add(probabilities);
            }
        }

        private void CreateStates()
        {
            var qtdSequences = Sequences.Count;
            var sequenceSize = Sequences[0].Length;
            var states = new List<Dictionary<StatesEnum, double>>();

            for (int i = 0; i < sequenceSize; i++)
            {
                var s = new Dictionary<StatesEnum, double>()
                {
                    { StatesEnum.Match, 0.0 },
                    { StatesEnum.Insert, 0.0 },
                    { StatesEnum.Delete, 0.0 }
                };

                var matchCount = 0;
                var deleteCount = 0;
                for (int j = 0; j < qtdSequences; j++)
                {
                    var sequence = Sequences[j].ToCharArray();
                    var aminoacid = sequence[i];
                    if (!Constants.Gaps.Contains(aminoacid))
                        matchCount++;
                    else
                        deleteCount++;
                }

                s[StatesEnum.Match] = (double)matchCount / (double)qtdSequences;
                if (deleteCount > (qtdSequences / 2))
                    s[StatesEnum.Insert] = (double)deleteCount / (double)qtdSequences;
                else if (deleteCount > 0)
                    s[StatesEnum.Delete] = (double)deleteCount / (double)qtdSequences;

                states.Add(s);
            }
            Transitions.Add(states);
        }
    }
}