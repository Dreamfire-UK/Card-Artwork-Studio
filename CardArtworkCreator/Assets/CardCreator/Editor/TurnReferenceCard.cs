using System.IO;
using UnityEditor;
using UnityEngine;

namespace CardCreator.Editor
{
    public static class TurnReferenceCard
    {
        public const string Path = "Assets/CardCreator/Data/References/Turn Structure.asset";

        [MenuItem("Tools/Card Creator/Create Turn Reference Card")]
        public static void Create()
        {
            var existing = AssetDatabase.LoadAssetAtPath<CardDefinition>(Path);
            if (existing != null) { Selection.activeObject = existing; return; }
            Directory.CreateDirectory("Assets/CardCreator/Data/References");
            AssetDatabase.Refresh();
            var card = ScriptableObject.CreateInstance<CardDefinition>();
            card.cardName = "Turn Structure";
            card.kind = CardKind.Reference;
            card.archetype = "Five Roads Reference";
            card.canPlayInFront = card.canPlayInRear = false;
            card.essence = card.rearProduces = Essence.None;
            card.frameAccent = new Color(.83f,.70f,.43f);
            card.artistCredit = "";
            card.referenceText =
                "<b>SETUP</b> 20 life. Draw 5. Each deck: 20 creatures + 1 evolution kept aside. Board: 5 columns, front and rear.\n\n" +
                "<color=#EBCB82><b>1  READY</b></color> Ready your creatures. Reset once-per-turn effects. A Webbed arrival skips its next ready step.\n\n" +
                "<color=#EBCB82><b>2  DRAW</b></color> Draw 1. First player skips their first draw. If you must draw from an empty deck, you lose.\n\n" +
                "<color=#EBCB82><b>3  REAR</b></color> You may place 1 creature from hand in an empty rear zone for free, exhausted.\n\n" +
                "<color=#EBCB82><b>4  ACT</b></color> Take up to 2 actions: play a front creature, move one front creature sideways 1 zone, or change stance. Exhaust ready rear creatures to pay printed Essence costs. New creatures enter exhausted.\n\n" +
                "<color=#EBCB82><b>5  BATTLE</b></color> Each ready front creature in Attack stance may attack once down its column; exhaust it. Guard may intercept. Deal simultaneous damage; an open lane hits life. Resolve abilities and Triumph after each battle.\n\n" +
                "<color=#EBCB82><b>6  END</b></color> End temporary bonuses. Damage and Scars remain. Pass the turn. At 0 life, you lose.";
            AssetDatabase.CreateAsset(card, Path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = card;
        }
    }
}
