using System;
using UnityEngine;

namespace UniDoom.CampusBlock4
{
    public enum Block4Symbol { Square, Triangle, Circle, Diamond, Cross }

    [CreateAssetMenu(menuName = "UniDoom/Bloco 4/Enigma das fotografias")]
    public sealed class Block4PuzzleDefinition : ScriptableObject
    {
        [Serializable]
        public struct Clue
        {
            public Block4Symbol symbol;
            [Range(1, 5)] public int digit;
            public string location;
        }

        [SerializeField] Clue[] clues;
        [SerializeField] Block4Symbol[] sequence;
        public Clue[] Clues => (Clue[])clues.Clone();
        public Block4Symbol[] Sequence => (Block4Symbol[])sequence.Clone();
        public int Length => sequence == null ? 0 : sequence.Length;

        public bool IsValid
        {
            get
            {
                if (clues == null || sequence == null || clues.Length != 5 || sequence.Length != 5) return false;
                int symbols = 0, digits = 0, order = 0;
                foreach (var clue in clues)
                {
                    int symbol = (int)clue.symbol;
                    if (symbol < 0 || symbol > 4 || clue.digit < 1 || clue.digit > 5) return false;
                    if ((symbols & (1 << symbol)) != 0 || (digits & (1 << clue.digit)) != 0) return false;
                    symbols |= 1 << symbol; digits |= 1 << clue.digit;
                }
                foreach (var symbol in sequence)
                {
                    int id = (int)symbol;
                    if (id < 0 || id > 4 || (order & (1 << id)) != 0) return false;
                    order |= 1 << id;
                }
                return true;
            }
        }

        public int DigitFor(Block4Symbol symbol)
        {
            if (clues != null) foreach (var clue in clues) if (clue.symbol == symbol) return clue.digit;
            return -1;
        }

        public bool Accepts(string entry)
        {
            if (!IsValid || entry == null || entry.Length != sequence.Length) return false;
            for (int i = 0; i < sequence.Length; i++)
                if (entry[i] != '0' + DigitFor(sequence[i])) return false;
            return true;
        }
    }
}
