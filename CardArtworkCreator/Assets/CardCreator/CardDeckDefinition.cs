using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardCreator
{
    [Serializable]
    public sealed class CardDeckEntry
    {
        public CardDefinition card;
        [Min(1)] public int quantity = 1;
    }

    [CreateAssetMenu(fileName = "New Deck", menuName = "Card Creator/Deck")]
    public sealed class CardDeckDefinition : ScriptableObject
    {
        public string deckName = "New test deck";
        public List<CardDeckEntry> mainDeck = new List<CardDeckEntry>();
        public List<CardDeckEntry> evolutionDeck = new List<CardDeckEntry>();
    }
}
