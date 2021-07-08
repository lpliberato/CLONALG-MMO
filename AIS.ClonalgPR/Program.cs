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
            var antigens = GetAntigens();
            var markov = new HiddenMarkovModel(antigens.Select(s => s.Sequence).ToList());
            markov.Train();
            //Console.WriteLine("Probabilidade = " + markov.CalculateTotalProbability(new char[7] { 'A', 'C', 'A', 'C', 'A', 'T', 'C' }));
            //Console.WriteLine("Odds = " + markov.CalculateLogOdds(new char[7] { 'A', 'C', 'A', 'C', 'A', 'T', 'C' }));

            var clonalgPR = new ClonalgPR(distance: markov, antigens: antigens);
            clonalgPR.Execute(maximumIterations: 1, percentHighAffinity: 60, percentLowAffinity: 40);

            //SaveResult(clonalgPR.Results);
        }

        private static List<string> GetSequencesByAntigens(List<Antigen> antigens)
        {
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
    }
}
