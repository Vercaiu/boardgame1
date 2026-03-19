using UnityEngine;
using UnityEngine.UI;

public class OpponentCardView : MonoBehaviour
{
    public Image artwork;
    public TMPro.TextMeshProUGUI nameText;

    public void ShowCardBack(Sprite back)
    {
        artwork.sprite = back;
        nameText.text = "";
    }

    public void ShowCard(CardData card, Sprite sprite)
    {
        artwork.sprite = sprite;
        nameText.text = card.cardName.ToString();
    }
}