using System;

public class CardFace
{
    // rank encoding: 2, 3, 4, 5, 6, 7, 8, 9, T(ten), J(jack), Q(queen), K(king), A(ace), Z(Joker)
    // suit encoding: D(diamond), C(club), H(heart), S(spade), B(black joker), R(red joker)

    public CardController Controller { get; private set; }
    public string Rank { get; set; } = string.Empty;
    public string Suit { get; set; } = string.Empty;

    public CardFace(CardController controller)
    {
        Controller = controller;
    }
}