using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardView : MonoBehaviour
{
    [Header("UI References")]
    public Image artwork;
    public TMP_Text nameText;

    private CardData cardData;



    public void SetCard(CardData data, Sprite sprite)
    {
        cardData = data;
        artwork.sprite = sprite;
    }

    public CardData GetData()
    {
        return cardData;
    }
}
