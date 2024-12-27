using UnityEngine;
using UnityEngine.UI;

public class CardController : MonoBehaviour
{
    public CardFace CardFace { get; private set; }
    public Text RankText;
    public Text SuitText;

    private bool _isFacingUp = false;

    public bool IsFacingUp
    {
        get => _isFacingUp;
        set
        {
            _isFacingUp = value;
            UpdateDisplay();
        }
    }

    public string Rank
    {
        get => CardFace.Rank;
        set
        {
            CardFace.Rank = value;
            UpdateDisplay();
        }
    }

    public string Suit
    {
        get => CardFace.Suit;
        set
        {
            CardFace.Suit = value;
            UpdateDisplay();
        }
    }

    public CardController()
    {
        CardFace = new CardFace(this);
    }

    void Start()
    {
        UpdateDisplay();
    }

    void Update()
    {
    }

    public void SetCardFaceFromRaw(string raw)
    {
        Rank = raw.Substring(0, 1);
        Suit = raw.Substring(1, 1);
    }

    private void UpdateDisplay()
    {
        if (_isFacingUp)
        {
            RankText.text = CardFace.Rank;
            SuitText.text = CardFace.Suit;
            RankText.enabled = true;
            SuitText.enabled = true;
        }
        else
        {
            RankText.enabled = false;
            SuitText.enabled = false;
        }
    }
}
