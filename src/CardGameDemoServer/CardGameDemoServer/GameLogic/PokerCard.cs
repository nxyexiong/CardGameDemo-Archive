using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardGameDemoServer.GameLogic
{
    internal class PokerCard : IComparable<PokerCard>
    {
        public const string RankOrder = "23456789TJQKAZ";
        public const string SuitOrder = "DCHSBR";

        public string Rank { get; set; } = string.Empty;
        public string Suit { get; set; } = string.Empty;

        public int CompareTo(PokerCard? other)
        {
            if (other == null) return 1;

            int rank1Index = RankOrder.IndexOf(Rank);
            int rank2Index = RankOrder.IndexOf(other.Rank);

            if (rank1Index != rank2Index)
                return rank1Index.CompareTo(rank2Index);

            int suit1Index = SuitOrder.IndexOf(Suit);
            int suit2Index = SuitOrder.IndexOf(other.Suit);

            return suit1Index.CompareTo(suit2Index);
        }

        public string RawData() => $"{Rank}{Suit}";

        public static PokerCard? From(string raw)
        {
            var rank = raw.Substring(0, 1);
            var suit = raw.Substring(1, 1);
            if (!RankOrder.Contains(rank) || !SuitOrder.Contains(suit))
                return null;
            return new PokerCard { Rank = rank, Suit = suit };
        }

        public static int CompareHands(List<PokerCard> a, List<PokerCard> b)
        {
            var sortedA = new List<PokerCard>(a);
            sortedA.Sort();
            var sortedB = new List<PokerCard>(b);
            sortedB.Sort();

            var sortedARanks = a.Select(x => RankOrder.IndexOf(x.Rank)).OrderBy(x => x).ToList();
            var sortedBRanks = b.Select(x => RankOrder.IndexOf(x.Rank)).OrderBy(x => x).ToList();

            var isAStraight = ((sortedARanks[2] - sortedARanks[1]) == 1) && ((sortedARanks[1] - sortedARanks[0]) == 1);
            var isBStraight = ((sortedBRanks[2] - sortedBRanks[1]) == 1) && ((sortedBRanks[1] - sortedBRanks[0]) == 1);

            var isASameSuit = a[0].Suit.Equals(a[1].Suit) && a[1].Suit.Equals(a[2].Suit);
            var isBSameSuit = b[0].Suit.Equals(b[1].Suit) && b[1].Suit.Equals(b[2].Suit);

            var isASameRank = a[0].Rank.Equals(a[1].Rank) && a[1].Rank.Equals(a[2].Rank);
            var isBSameRank = b[0].Rank.Equals(b[1].Rank) && b[1].Rank.Equals(b[2].Rank);

            var isADouble = a[0].Rank.Equals(a[1].Rank) || a[1].Rank.Equals(a[2].Rank) || a[0].Rank.Equals(a[2].Rank);
            var isBDouble = b[0].Rank.Equals(b[1].Rank) || b[1].Rank.Equals(b[2].Rank) || b[0].Rank.Equals(b[2].Rank);

            var patternA = 0;
            if (isASameRank) patternA = 5;
            else if (isAStraight && isASameSuit) patternA = 4;
            else if (isASameSuit) patternA = 3;
            else if (isAStraight) patternA = 2;
            else if (isADouble) patternA = 1;
            else patternA = 0;

            var patternB = 0;
            if (isBSameRank) patternB = 5;
            else if (isBStraight && isBSameSuit) patternB = 4;
            else if (isBSameSuit) patternB = 3;
            else if (isBStraight) patternB = 2;
            else if (isBDouble) patternB = 1;
            else patternB = 0;

            if (patternA != patternB)
                return patternA - patternB;

            if (patternA == 5)
            {
                return RankOrder.IndexOf(a[0].Rank) - RankOrder.IndexOf(b[0].Rank);
            }
            else if (patternA == 4)
            {
                if (sortedARanks[0] != sortedBRanks[0])
                    return sortedARanks[0] - sortedBRanks[0];
                return SuitOrder.IndexOf(a[0].Suit) - SuitOrder.IndexOf(b[0].Suit);
            }
            else if (patternA == 3)
            {
                if (sortedA[2].Rank != sortedB[2].Rank)
                    return RankOrder.IndexOf(sortedA[2].Rank) - RankOrder.IndexOf(sortedB[2].Rank);
                if (sortedA[1].Rank != sortedB[1].Rank)
                    return RankOrder.IndexOf(sortedA[1].Rank) - RankOrder.IndexOf(sortedB[1].Rank);
                if (sortedA[0].Rank != sortedB[0].Rank)
                    return RankOrder.IndexOf(sortedA[0].Rank) - RankOrder.IndexOf(sortedB[0].Rank);
                if (sortedA[0].Suit != sortedB[0].Suit)
                    return SuitOrder.IndexOf(sortedA[0].Suit) - SuitOrder.IndexOf(sortedB[0].Suit);
            }
            else if (patternA == 2)
            {
                if (sortedA[0].Rank != sortedB[0].Rank)
                    return RankOrder.IndexOf(sortedA[0].Rank) - RankOrder.IndexOf(sortedB[0].Rank);
                if (sortedA[2].Suit != sortedB[2].Suit)
                    return SuitOrder.IndexOf(sortedA[2].Suit) - SuitOrder.IndexOf(sortedB[2].Suit);
                if (sortedA[1].Suit != sortedB[1].Suit)
                    return SuitOrder.IndexOf(sortedA[1].Suit) - SuitOrder.IndexOf(sortedB[1].Suit);
                if (sortedA[0].Suit != sortedB[0].Suit)
                    return SuitOrder.IndexOf(sortedA[0].Suit) - SuitOrder.IndexOf(sortedB[0].Suit);
            }
            else if (patternA == 1)
            {
                var doubleA = new List<PokerCard>();
                PokerCard? singleA = null;
                if (a[0].Rank == a[1].Rank)
                {
                    doubleA.Add(a[0]);
                    doubleA.Add(a[1]);
                    singleA = a[2];
                }
                else if (a[0].Rank == a[2].Rank)
                {
                    doubleA.Add(a[0]);
                    doubleA.Add(a[2]);
                    singleA = a[1];
                }
                else if (a[1].Rank == a[2].Rank)
                {
                    doubleA.Add(a[1]);
                    doubleA.Add(a[2]);
                    singleA = a[0];
                }
                doubleA.Sort();

                var doubleB = new List<PokerCard>();
                PokerCard? singleB = null;
                if (b[0].Rank == b[1].Rank)
                {
                    doubleB.Add(b[0]);
                    doubleB.Add(b[1]);
                    singleB = b[2];
                }
                else if (b[0].Rank == b[2].Rank)
                {
                    doubleB.Add(b[0]);
                    doubleB.Add(b[2]);
                    singleB = b[1];
                }
                else if (b[1].Rank == b[2].Rank)
                {
                    doubleB.Add(b[1]);
                    doubleB.Add(b[2]);
                    singleB = b[0];
                }
                doubleB.Sort();

                if (doubleA[0].Rank != doubleB[0].Rank)
                    return RankOrder.IndexOf(doubleA[0].Rank) - RankOrder.IndexOf(doubleB[0].Rank);
                if (singleA!.Rank != singleB!.Rank)
                    return RankOrder.IndexOf(singleA.Rank) - RankOrder.IndexOf(singleB.Rank);

                if (doubleA[1].Suit != doubleB[1].Suit)
                    return SuitOrder.IndexOf(doubleA[1].Suit) - SuitOrder.IndexOf(doubleB[1].Suit);
            }
            else
            {
                if (sortedA[2].Rank != sortedB[2].Rank)
                    return RankOrder.IndexOf(sortedA[2].Rank) - RankOrder.IndexOf(sortedB[2].Rank);
                if (sortedA[1].Rank != sortedB[1].Rank)
                    return RankOrder.IndexOf(sortedA[1].Rank) - RankOrder.IndexOf(sortedB[1].Rank);
                if (sortedA[0].Rank != sortedB[0].Rank)
                    return RankOrder.IndexOf(sortedA[0].Rank) - RankOrder.IndexOf(sortedB[0].Rank);
                if (sortedA[2].Suit != sortedB[2].Suit)
                    return SuitOrder.IndexOf(sortedA[2].Suit) - SuitOrder.IndexOf(sortedB[2].Suit);
                if (sortedA[1].Suit != sortedB[1].Suit)
                    return SuitOrder.IndexOf(sortedA[1].Suit) - SuitOrder.IndexOf(sortedB[1].Suit);
                if (sortedA[0].Suit != sortedB[0].Suit)
                    return SuitOrder.IndexOf(sortedA[0].Suit) - SuitOrder.IndexOf(sortedB[0].Suit);
            }

            return 0;
        }
    }
}
