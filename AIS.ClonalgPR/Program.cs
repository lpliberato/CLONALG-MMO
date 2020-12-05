using AIS.ClonalgPR.Measures;
using AIS.ClonalgPR.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AIS.ClonalgPR
{
    class Program
    {
        static void Main()
        {
            //var clonalgPR = new ClonalgPR(distance: new Euclidean());
            //var antigens = GetAntigens();
            //clonalgPR.Execute(antigens: antigens, maximumIterations: 1, percentHighAffinity: 60, percentLowAffinity: 40);

            //SaveResult(clonalgPR.Results);
            TestWithAlphabetOfSize2();
        }

        private static List<string> GetSequencesByAntigens(List<Antigen> antigens) {
            return antigens.Select(s => s.Sequence).ToList();
        }

        private static string[] GetCharacteristics(string antigen)
        {
            var splitOptions = new char[3] { '|', '\n', '\r' };
            return (!string.IsNullOrEmpty(antigen)) ? string.Join("", antigen).Split(splitOptions, StringSplitOptions.RemoveEmptyEntries) : new string[] { };
        }

        private static string GetNameAntigen(string[] characteristics)
        {
            return (characteristics.Length > 0) ? characteristics[0] : "";
        }

        private static string GetHostAntigen(string[] characteristics)
        {
            return (characteristics.Length > 1) ? characteristics[1] : "";
        }

        private static string GetSequenceAntigen(string[] characteristics)
        {
            var length = characteristics.Length - 4;
            var targetArray = new string[length];
            Array.Copy(characteristics, 4, targetArray, 0, length);
            return string.Join("", targetArray);
        }

        private static List<Antigen> GetAntigens()
        {
            var path = Helpers.GetPathFile();
            var text = File.ReadAllText(path);

            if (IsMsfFile(text))
                return GetAntigensByMsfFile(text);

            if (IsFastaFile(text))
                return GetAntigensByFastaFile(text);

            return new List<Antigen>();
        }

        private static bool IsFastaFile(string text)
        {
            var regex = new Regex(@"^>[A-Za-z1-9\/\|\s].+$", RegexOptions.Multiline);
            var matches = regex.Matches(text);
            return matches.Count > 0;
        }

        private static bool IsMsfFile(string text)
        {
            var regex = new Regex(@"[^Name: ]\w.+(?=\soo)", RegexOptions.Multiline);
            var matches = regex.Matches(text);
            return matches.Count > 0;
        }

        private static List<Antigen> GetAntigensByMsfFile(string text)
        {
            var antigens = new List<Antigen>();
            var regex = new Regex(@"[^Name: ]\w.+(?=\soo)", RegexOptions.Multiline);
            var matches = regex.Matches(text);

            foreach (var item in matches)
            {
                var sequenceName = item.ToString();
                var expression = @"(?<=" + sequenceName + @")\s{2,}[a-zA-Z\.].+(?!\n)";
                regex = new Regex(expression, RegexOptions.Multiline);
                var partsOfSequence = regex.Matches(text);
                var sequence = string.Join("", partsOfSequence).Replace(" ", "");
                antigens.Add(new Antigen()
                {
                    Name = sequenceName,
                    Sequence = sequence,
                    Length = sequence.Length
                });
            }

            return antigens;
        }

        private static List<Antigen> GetAntigensByFastaFile(string text)
        {
            var antigens = new List<Antigen>();
            var sequences = text.Split(">", StringSplitOptions.RemoveEmptyEntries);

            foreach (var sequence in sequences)
            {
                var characteristics = GetCharacteristics(sequence);
                var _sequence = GetSequenceAntigen(characteristics);

                antigens.Add(new Antigen
                {
                    Name = GetNameAntigen(characteristics),
                    Host = GetHostAntigen(characteristics),
                    Sequence = _sequence.ToUpper(),
                    Length = _sequence.Length
                });
            }

            return antigens;
        }

        private static void SaveResult(Result results)
        {
            var filePath = Path.Combine(Helpers.GetPath(), "results.json");
            using (StreamWriter file = File.CreateText(filePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, results);
            }
        }

        private static void TestWithAlphabetOfSize2()
        {
            int[][] sequences = new int[][]
            {
                new int[] { 0,1,1,1,1,0,1,1,1,1 },
                new int[] { 0,1,1,1,0,1,1,1,1,1 },
                new int[] { 0,1,1,0,1,1,0,1,1,1 },
                new int[] { 0,1,0,1,1,1         },
                new int[] { 0,1,0,1,1,1,1       },
                new int[] { 0,1,1,1,1,1,1,1,0,1 },
                new int[] { 0,1,1,1,1,1,1,0,1,1 },
            };

            // Creates a new Hidden Markov Model with 3 states for
            //  an output alphabet of two characters (zero and one)
            HiddenMarkovModelTest hmm = new HiddenMarkovModelTest(2, 3);

            // Try to fit the model to the data until the difference in
            //  the average likelihood changes only by as little as 0.0001
            hmm.Learn(sequences, 0.0001);

            // Calculate the probability that the given
            //  sequences originated from the model
            double l1 = hmm.Evaluate(new int[] { 1, 0, 1 });      // 0.9999
            double l2 = hmm.Evaluate(new int[] { 0, 1, 1, 1 });  // 0.9166

            double l3 = hmm.Evaluate(new int[] { 1, 1 });      // 0.0000
            double l4 = hmm.Evaluate(new int[] { 1, 0, 0, 0 });  // 0.0000

            double l5 = hmm.Evaluate(new int[] { 0, 1, 0, 1, 1, 1, 1, 1, 1 }); // 0.0342
            double l6 = hmm.Evaluate(new int[] { 0, 1, 1, 1, 1, 1, 1, 0, 1 }); // 0.0342
        }

        private static void TestWithAlphabetOfSize20()
        {
            int[][] sequences = new int[][]
            {
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 },
                new int[] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 }
            };

            // Creates a new Hidden Markov Model with 3 states for
            //  an output alphabet of two characters (zero and one)
            HiddenMarkovModelTest hmm = new HiddenMarkovModelTest(20, 3);

            // Try to fit the model to the data until the difference in
            //  the average likelihood changes only by as little as 0.0001
            hmm.Learn(sequences, 0.0001);

            // Calculate the probability that the given
            //  sequences originated from the model
            double l1 = hmm.Evaluate(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });      // 0.9999
            //double p;
            //int [] d1 = hmm.Decode(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 }, out p);      // 0.9999
            Console.WriteLine(l1);
        }

        private static void TestWithAlphabetOfSize22()
        {
            int[][] sequences = new int[][]
            {
                new int[] { 0,1,1,1,1,1,1,1,1,1 },
                new int[] { 0,1,1,1,1,1,1,1,1,1 },
                new int[] { 0,1,1,1,1,1,1,1,1,1 },
                new int[] { 0,1,1,1,1,1,1,1,1,1 },
                new int[] { 0,1,1,1,1,1,1,1,1,1 },
                new int[] { 0,1,1,1,1,1,1,1,1,0 },
                new int[] { 0,1,1,1,1,1,1,1,1,0},
            };

            // Creates a new Hidden Markov Model with 3 states for
            //  an output alphabet of two characters (zero and one)
            HiddenMarkovModel hmm = new HiddenMarkovModel(10, 10);

            // Try to fit the model to the data until the difference in
            //  the average likelihood changes only by as little as 0.0001
            hmm.Learn(sequences, 0.0001);

            // Calculate the probability that the given
            //  sequences originated from the model
            double l1 = hmm.Evaluate(new int[] { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1 });      // 0.9999
            double l2 = hmm.Evaluate(new int[] { 0, 1, 1, 1, 1 });  // 0.9166
            double l7 = hmm.Evaluate(new int[] { 0, 1, 1, 1 });  // 0.9166

            double l3 = hmm.Evaluate(new int[] { 0, 1, 1, 1, 1, 1, 1, 1, 1, 0 });
            double l4 = hmm.Evaluate(new int[] { 1, 0, 0, 0 });  // 0.0000

            double l5 = hmm.Evaluate(new int[] { 0, 1, 0, 1, 1, 1, 1, 1, 1 }); // 0.0342
            double l6 = hmm.Evaluate(new int[] { 0, 1, 1, 1, 1, 1, 1, 0, 1 }); // 0.0342
        }
    }
}
