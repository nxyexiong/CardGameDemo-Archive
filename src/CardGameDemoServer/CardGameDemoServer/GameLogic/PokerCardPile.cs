using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardGameDemoServer.GameLogic
{
    internal class PokerCardPile
    {
        private List<PokerCard> _pile = [];

        public void Init(int deckCount = 1, bool haveJoker = false)
        {
            _pile.Clear();
            for (var i = 0; i < deckCount; i++)
            {
                foreach (var suit in PokerCard.SuitOrder)
                {
                    foreach (var rank in PokerCard.RankOrder)
                    {
                        if (rank == 'Z' && !haveJoker) continue;
                        if (rank == 'Z' && suit != 'R' && suit != 'B') continue;
                        if (rank != 'Z' && (suit == 'R' || suit == 'B')) continue;
                        _pile.Add(new PokerCard { Rank = $"{rank}", Suit = $"{suit}" });
                    }
                }
            }
        }

        public void Shuffle()
        {
            var random = new Random();
            for (var i = _pile.Count - 1; i > 1; i--)
            {
                var rnd = random.Next(i + 1);
                var value = _pile[rnd];
                _pile[rnd] = _pile[i];
                _pile[i] = value;
            }
        }

        public int Count()
        {
            return _pile.Count;
        }

        public PokerCard? Draw()
        {
            if (Count() > 0)
            {
                var ret = _pile[0];
                _pile.RemoveAt(0);
                return ret;
            }
            return null;
        }
    }
}
