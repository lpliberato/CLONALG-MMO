using System;

namespace AIS.ClonalgPR
{
    public static class Constants
    {
        private static char[] _aminoacids = null;
        private static char[] _dna = null;
        private static char[] _rna = null;
        private static Random _random = null;

        public const int qtdMinAntibodies = 1;
        public const int qtdMaxAntibodies = 100;
        public const int sequenceMinSize = 4;
        public const int sequenceMaxSize = 500;

        public static char[] Aminoacids
        {
            get
            {
                if (_aminoacids == null)
                    _aminoacids = new char[20] { 'A', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'K', 'L', 'M', 'N', 'P', 'Q', 'R', 'S', 'T', 'V', 'W', 'Y'};

                return _aminoacids;
            }
        }

        public static char[] DNA
        {
            get
            {
                if (_dna == null)
                    _dna = new char[4] { 'A', 'C', 'T', 'G' };

                return _dna;
            }
        }

        public static char[] RNA
        {
            get
            {
                if (_rna == null)
                    _rna = new char[4] { 'A', 'C', 'U', 'G' };

                return _rna;
            }
        }

        public static Random Random
        {
            get
            {
                if (_random == null)
                    _random = new Random();

                return _random;
            }
        }
    }
}
