using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace CardCreator.Editor
{
    public static class RootboundCourtSamples
    {
        public const string Folder = "Assets/CardCreator/Data/Rootbound Court";
        [MenuItem("Tools/Card Creator/Create Rootbound Court Test Deck")]
        public static void Create()
        {
            var existing = AssetDatabase.LoadAssetAtPath<CardDeckDefinition>(Folder + "/Rootbound Court.asset");
            if (existing != null) { Selection.activeObject = existing; return; }
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            var deck = ScriptableObject.CreateInstance<CardDeckDefinition>();
            deck.deckName = "Rootbound Court";
            var seed = Card("Seedling Attendant", 1, 1, 2, "Forest Sprout", true);
            Ability(seed, CardTrigger.Passive, "When an adjacent ally survives battle, heal 1 damage from it.");
            Add(deck, seed, 3);
            var stag = Card("Thornwatch Stag", 1, 2, 3, "Forest Deer", true);
            stag.guard = true;
            Add(deck, stag, 3);
            var scout = Card("Vine Scout", 1, 2, 1, "Dryads Archer");
            Ability(scout, CardTrigger.WhenPlayed, "Move a rear ally sideways into an empty rear zone.");
            Add(deck, scout, 3);
            var sprout = Card("Briar Sprout", 1, 1, 2, "Forest Spora", true);
            sprout.promote = true;
            sprout.promoteCondition = "When an adjacent-column ally survives battle, advance to your empty front zone exhausted. Give that ally +1 defence this turn.";
            Add(deck, sprout, 2);
            var warden = Card("Briar Warden", 2, 3, 4, "Dryads Warrior");
            warden.attackArrows = CardArrow.West | CardArrow.East;
            warden.arrowEffect = "Arrowed allies get +1 power.";
            warden.guardArrowEffect = "Arrowed allies get +1 defence.";
            Add(deck, warden, 2);
            var mender = Card("Grove Mender", 2, 2, 4, "Dryads Mage");
            Ability(mender, CardTrigger.OncePerTurn, "Heal 1 damage from an ally in this or an adjacent column.");
            Add(deck, mender, 2);
            var thicket = Card("Walking Thicket", 2, 3, 5, "Forest Golem", true);
            thicket.guard = true;
            Ability(thicket, CardTrigger.WhenGuarding, "If protecting an adjacent-column ally, gain +1 defence this battle.");
            Add(deck, thicket, 2);
            var herald = Card("Heartwood Herald", 3, 4, 5, "Boss Dryad Queen Rafflesia");
            herald.attackArrows = CardArrow.West | CardArrow.East | CardArrow.North;
            herald.arrowEffect = "Arrowed allies get +1 power.";
            herald.guardArrowEffect = "Arrowed allies get +1 defence.";
            Add(deck, herald, 3);
            var boss = Card("Yggdrasil, Living Citadel", 3, 5, 7, "Boss Dryad Yggdrasil");
            boss.kind = CardKind.Evolution;
            boss.evolvesFrom = herald;
            boss.requiredScars = 2;
            boss.attackArrows = CardArrow.West | CardArrow.East | CardArrow.South;
            boss.triumphQuest = "End an opponent's battle holding 3 consecutive columns, with at least 2 front creatures.";
            boss.onEvolution = "Heal 1 damage from each ally in those columns.";
            boss.evolutionOngoing = "Once per turn, save a defeated arrowed ally by moving it to an empty rear zone.";
            boss.artworkScale = 1.05f;
            boss.artworkOffset = new Vector2(0, 180);
            deck.evolutionDeck.Add(new CardDeckEntry { card = boss, quantity = 1 });
            foreach (var entry in deck.mainDeck) EditorUtility.SetDirty(entry.card);
            EditorUtility.SetDirty(boss);
            AssetDatabase.CreateAsset(deck, Folder + "/Rootbound Court.asset");
            AssetDatabase.SaveAssets();
            Selection.activeObject = deck;
        }
        private static CardDefinition Card(string name, int grade, int power, int defense, string art, bool front = false)
        {
            string path = Folder + "/" + name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<CardDefinition>(path) != null)
                throw new InvalidOperationException("An existing card would be replaced: " + path);
            var card = ScriptableObject.CreateInstance<CardDefinition>();
            card.cardName = name;
            card.archetype = "Rootbound Court";
            card.grade = grade;
            card.genericEssenceCost = grade - 1;
            card.power = power;
            card.defense = defense;
            card.artwork = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Aekashics Librarium Static Battlers/" +
                (front ? "Frontview" : "Sideview") + " Batch Battlers/" + art + ".png");
            card.artworkOffset = new Vector2(0, 105);
            card.artworkScale = 0.88f;
            AssetDatabase.CreateAsset(card, path);
            return card;
        }
        private static void Add(CardDeckDefinition deck, CardDefinition card, int quantity)
            => deck.mainDeck.Add(new CardDeckEntry { card = card, quantity = quantity });
        private static void Ability(CardDefinition card, CardTrigger trigger, string effect)
            => card.abilities.Add(new CardAbility { trigger = trigger, effect = effect });
    }
}
