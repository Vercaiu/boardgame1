using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CardDatabase", menuName = "Game/Card Database")]
public class CardDatabase : ScriptableObject
{
    [System.Serializable]
    public class CardDefinition
    {
        public int cardId;
        public int spriteId;
        public string cardName;
        public int snowgouleid;
        public int signid;
        public int trees;
        public int moose;
        public int bats;
        public int fire;
        public int geese;
    }

    public List<CardDefinition> allCards = new List<CardDefinition>();
    /// <summary>
    /// Get all cards as CardData list for the deck
    /// </summary>
    public List<CardData> GetDeckCardData()
    {
        List<CardData> deckCards = new List<CardData>();

        foreach (var def in allCards)
        {
            deckCards.Add(new CardData
            {
                cardId = def.cardId,
                spriteId = def.spriteId,
                cardName = def.cardName,
                snowgouleid = def.snowgouleid,
                signid = def.signid,
                trees = def.trees,
                moose = def.moose,
                bats = def.bats,
                fire = def.fire,
                geese = def.geese
            });
        }

        return deckCards;
    }

    /// <summary>
    /// Get a specific card by ID
    /// </summary>
    public CardDefinition GetCardById(int cardId)
    {
        return allCards.Find(c => c.cardId == cardId);
    }
}