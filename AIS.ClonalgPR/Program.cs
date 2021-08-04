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
        private static List<Antigen> antigens = new List<Antigen>();
        private static TypeBioSequence TypeBioSequence
        {
            get
            {
                return IsDNA() ?
                    TypeBioSequence.DNA :
                     IsRNA() ?
                    TypeBioSequence.RNA :
                    IsProtein() ?
                    TypeBioSequence.PROTEIN :
                    TypeBioSequence.UNKNOWN;
            }
        }

        static void Main()
        {
            antigens = GetAntigens();
            if (antigens != null && antigens.Count > 0)
                ExecuteSingle(antigens);
            else 
            {
                var text = GetTextFile();
                var antigensMultiple = GetAntigensByFastaFileAligned(text);

                ExecuteMultiple(antigensMultiple);
            }
        }

        static void ExecuteSingle(List<Antigen> antigens) 
        {
            var sequences = GetSequencesByAntigens(antigens);
            var markov = new HiddenMarkovModel(sequences, TypeBioSequence);

            markov.Train();

            //Console.WriteLine("Probabilidade = " + markov.CalculateTotalProbability(new char[7] { 'C','A','G','C','C','C','A' }));
            //Console.WriteLine("Odds = " + markov.CalculateLogOdds(new char[7] { 'G', 'C', 'G', 'G', 'G', 'A', 'C' }));

            var clonalgPR = new ClonalgPR(distance: markov, antigens: antigens, typeBioSequence: TypeBioSequence);
            clonalgPR.Execute(maximumIterations: 1000, percentHighAffinity: 0.6, percentLowAffinity: 0.4);

            //SaveResult(clonalgPR.Results);        
        }
        static void ExecuteMultiple(List<List<Antigen>> antigens)
        {
            var typeBioSequence = TypeBioSequence.PROTEIN;

            //Parallel.For(0, antigens.Count(),
            //    index =>
            //    {
            //        var _antigens = antigens[index];
            //        var sequences = GetSequencesByAntigens(_antigens);
            //        var markov = new HiddenMarkovModel(sequences, typeBioSequence);

            //        markov.Train();

            //        var clonalgPR = new ClonalgPR(distance: markov, antigens: _antigens, typeBioSequence: typeBioSequence);
            //        clonalgPR.Execute(maximumIterations: 1000, percentHighAffinity: 0.7, percentLowAffinity: 0.3);
            //    });

            for (int i = 0; i < antigens.Count; i++)
            {
                var _antigens = antigens[i];
                var sequences = GetSequencesByAntigens(_antigens);
                var markov = new HiddenMarkovModel(sequences, typeBioSequence);

                markov.Train();

                var clonalgPR = new ClonalgPR(distance: markov, antigens: _antigens, typeBioSequence: typeBioSequence);
                clonalgPR.Execute(maximumIterations: 1000, percentHighAffinity: 0.8, percentLowAffinity: 0.2);
            }
        }

        private static bool IsDNA()
        {
            var sequence = antigens.Select(antigen => antigen.Sequence).FirstOrDefault();
            var regex = new Regex(@"^[AaCcTtGg\W]+$", RegexOptions.Multiline);
            var matches = regex.Matches(new string(sequence));
            return matches.Count() > 0;
        }

        private static bool IsRNA()
        {
            var sequence = antigens.Select(antigen => antigen.Sequence).FirstOrDefault();
            var regex = new Regex(@"^[AaCcUuGg\W]+$", RegexOptions.Multiline);
            var matches = regex.Matches(new string(sequence));
            return matches.Count() > 0;
        }

        private static bool IsProtein()
        {
            var sequence = antigens.Select(antigen => antigen.Sequence).FirstOrDefault();
            var regex = new Regex(@"^[AaCcDdEeFfGgHhIiKkLlMmNnPpQqRrSsTtVvWwYy\W]+$", RegexOptions.Multiline);
            var matches = regex.Matches(new string(sequence));
            return matches.Count() > 0;
        }

        private static List<char[]> GetSequencesByAntigens(List<Antigen> antigens)
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
            var length = characteristics.Length - 3;
            var targetArray = new string[length];
            Array.Copy(characteristics, 3, targetArray, 0, length);
            return string.Join("", targetArray);
        }

        private static string GetTextFile() 
        {
            var path = Helpers.GetPathFile();
            return File.ReadAllText(path);
        }

        private static List<Antigen> GetAntigens()
        {
            var text = GetTextFile();

            if (IsMsfFile(text))
                return GetAntigensByMsfFile(text);

            if (IsFastaFile(text))
                return GetAntigensByFastaFile(text);

            return new List<Antigen>();
        }

        private static List<List<Antigen>> GetAntigensByFastaFileAligned(string text) 
        {
            var antigensMultiple = new List<List<Antigen>>();
            var antigens = new List<Antigen>();
            var sequences = text.Split("tr", StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in sequences)
            {
                var antigenName = item.Split("|", StringSplitOptions.RemoveEmptyEntries);
                var antigenHost = antigenName[1].Split(" ", StringSplitOptions.RemoveEmptyEntries);
                var partOfAligment = antigenHost[1].Trim();

                if (!antigens.Any(antigen => antigen.Name == antigenName[0]))
                {
                    antigens.Add(
                        new Antigen()
                        {
                            Name = antigenName[0],
                            Host = antigenHost[0],
                            Sequence = partOfAligment.Trim().ToCharArray(),
                            Length = partOfAligment.Length
                        });
                }
                else 
                {
                    antigensMultiple.Add(antigens);
                    antigens = new List<Antigen>();
                    antigens.Add(
                        new Antigen()
                        {
                            Name = antigenName[0],
                            Host = antigenHost[0],
                            Sequence = partOfAligment.Trim().ToCharArray(),
                            Length = partOfAligment.Length
                        });
                }
            }
            antigensMultiple.Add(antigens);

            return antigensMultiple;
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
                var expression = @"(?<=" + sequenceName + @")\s{2,}[a-zA-Z].+$";
                regex = new Regex(expression, RegexOptions.Multiline);
                var partsOfSequence = regex.Matches(text);
                var sequence = string.Join("", partsOfSequence).Replace(" ", "");
                antigens.Add(new Antigen()
                {
                    Name = sequenceName,
                    Sequence = sequence.Trim().ToCharArray(),
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
                    Sequence = _sequence.Trim().ToUpper().ToCharArray(),
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
