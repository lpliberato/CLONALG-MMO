using AIS.ClonalgPR.Models;
using System.Collections;
using System.Collections.Generic;

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
        }

        public void Train()
        {
            CreateStates();

            var qtdSequences = Sequences.Count;
            var sequenceSize = Sequences[0].Length;
            var aminoacids = new Dictionary<char, int>();
            var probabilities = new Dictionary<char, double>();

            for (int k = 0; k < sequenceSize; k++)
            {
                for (int z = 0; z < qtdSequences; z++)
                {
                    var sequence = Sequences[z].ToCharArray();
                    var aminoacid = sequence[k];
                    if (!aminoacids.ContainsKey(aminoacid))
                    {
                        aminoacids.Add(aminoacid, 1);
                        probabilities.Add(aminoacid, 1 / qtdSequences);
                    }
                    else
                    {
                        aminoacids[aminoacid] += 1;
                        probabilities[aminoacid] = aminoacids[aminoacid] / qtdSequences;
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

            for (int k = 0; k < sequenceSize; k++)
            {
                var s = new Dictionary<StatesEnum, double>()
                {
                    { StatesEnum.Match, 0.0 },
                    { StatesEnum.Insert, 0.0 },
                    { StatesEnum.Delete, 0.0 }
                };

                var matchCount = 0;
                var deleteCount = 0;
                for (int z = 0; z < qtdSequences; z++)
                {
                    var sequence = Sequences[z].ToCharArray();
                    var aminoacid = sequence[k];
                    if (aminoacid != '-')
                        matchCount++;
                    else
                        deleteCount++;
                }

                if (matchCount == qtdSequences)
                    s[StatesEnum.Match] = matchCount / qtdSequences;
                else if (deleteCount > (qtdSequences / 2))
                    s[StatesEnum.Insert] = deleteCount / qtdSequences;
                else
                    s[StatesEnum.Delete] = deleteCount / qtdSequences;

                states.Add(s);
            }
            Transitions.Add(states);
        }
    }
}
