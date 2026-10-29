using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardCreator
{
    public enum Essence { None, Grove, Gloom, Ember, Current }
    public enum CardKind { Creature, Evolution }
    public enum CardTrigger
    {
        Passive, WhenPlayed, WhenPromoted, WhenMoved, WhenStanceChanged,
        WhenAttacking, WhenGuarding, AfterBattle, WhenZoneEmpties,
        WhenEvolved, Action, OncePerTurn
    }

    [Flags]
    public enum CardArrow
    {
        None = 0, North = 1, NorthEast = 2, East = 4, SouthEast = 8,
        South = 16, SouthWest = 32, West = 64, NorthWest = 128
    }

    [Serializable]
    public sealed class CardAbility
    {
        public CardTrigger trigger;
        [TextArea(1, 2)] public string condition;
        [TextArea(2, 4)] public string effect;
        public bool oncePerTurn;
    }

    [CreateAssetMenu(fileName = "New Card", menuName = "Card Creator/Card")]
    public sealed class CardDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string cardName = "New Creature";
        public string archetype;
        public CardKind kind;
        public Essence essence = Essence.Grove;
        public Essence secondEssence;
        [Range(1, 3)] public int grade = 1;
        [Min(0)] public int power = 1;
        [Min(0)] public int defense = 1;

        [Header("Board and resources")]
        public bool canPlayInFront = true;
        public bool canPlayInRear = true;
        public Essence rearProduces = Essence.Grove;
        [Tooltip("Generic Essence in addition to the printed Essence. A Grade 2 usually has 1; Grade 3 has 2.")]
        [Min(0)] public int genericEssenceCost;
        public bool guard;
        public bool promote;
        [TextArea(2, 3)] public string promoteCondition;

        [Header("Stance arrows")]
        [Tooltip("Choose directions relative to the upright Attack stance. Guard rotates the card clockwise.")]
        public CardArrow attackArrows;
        [TextArea(2, 3)] public string arrowEffect;
        [Tooltip("Optional different effect while in Guard stance. Leave blank to use the same effect.")]
        [TextArea(2, 3)] public string guardArrowEffect;

        [Header("Abilities")]
        public List<CardAbility> abilities = new List<CardAbility>();
        [TextArea(2, 4)] public string flavorText;

        [Header("Evolution / Triumph")]
        public CardDefinition evolvesFrom;
        [Min(0)] public int requiredScars = 2;
        [TextArea(2, 4)] public string triumphQuest;
        [TextArea(2, 4)] public string onEvolution;
        [TextArea(2, 4)] public string evolutionOngoing;

        [Header("Artwork")]
        public Texture2D background;
        public Texture2D artwork;
        [Tooltip("Position of the character on the full card, in card pixels. Drag the preview to adjust.")]
        public Vector2 artworkOffset;
        [Range(0.25f, 3f)] public float artworkScale = 1f;
        public string artistCredit = "Ækashics";

        public CardArrow GuardArrows
        {
            get
            {
                int rotated = ((int)attackArrows << 2) | ((int)attackArrows >> 6);
                return (CardArrow)(rotated & 255);
            }
        }

        private void OnValidate()
        {
            grade = Mathf.Clamp(grade, 1, 3);
            artworkScale = Mathf.Clamp(artworkScale, 0.25f, 3f);
        }
    }
}
