using System.IO;
using UnityEditor;
using UnityEngine;

namespace CardCreator.Editor
{
    public static class NightBroodSamples
    {
        public const string Folder = "Assets/CardCreator/Data/Night Brood";
        [MenuItem("Tools/Card Creator/Create Night Brood Test Deck")]
        public static void Create()
        {
            var old = AssetDatabase.LoadAssetAtPath<CardDeckDefinition>(Folder + "/Night Brood.asset");
            if (old != null) { Selection.activeObject = old; return; }
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var deck = ScriptableObject.CreateInstance<CardDeckDefinition>(); deck.deckName = "Night Brood";
            var moth = Card("Grave Moth",1,3,1);
            Ability(moth,CardTrigger.WhenZoneEmpties,"When the opposing front zone empties, gain +1 power for your next attack. Does not stack."); Add(deck,moth,3);
            var imp = Card("Shade Imp",1,2,2);
            Ability(imp,CardTrigger.WhenPlayed,"If played in front, the opposing creature loses 1 power this turn."); Add(deck,imp,3);
            var web = Card("Webling",1,2,2);
            Ability(web,CardTrigger.WhenPlayed,"Web an adjacent enemy front zone. Its next arrival stays exhausted through its next ready step."); Add(deck,web,3);
            var skitter = Card("Hollow Skitter",1,3,1); skitter.promote = true;
            skitter.promoteCondition = "When the opposing front zone empties, advance to your empty front zone exhausted."; Add(deck,skitter,2);
            var silk = Card("Silkspinner",2,2,5); silk.attackArrows = CardArrow.NorthWest | CardArrow.NorthEast;
            silk.arrowEffect = "Arrowed enemies have -1 power while attacking."; silk.guardArrowEffect = silk.arrowEffect; Add(deck,silk,2);
            var spectre = Card("Ruin Spectre",2,3,4);
            Ability(spectre,CardTrigger.WhenMoved,"After another ally moves, move to an adjacent empty front zone.",true); Add(deck,spectre,2);
            var widow = Card("Widow Matron",2,2,5); widow.attackArrows = CardArrow.NorthWest | CardArrow.North | CardArrow.NorthEast;
            widow.arrowEffect = "When an enemy leaves an arrowed zone, heal 1 damage from an ally."; widow.guardArrowEffect=widow.arrowEffect; Add(deck,widow,2);
            var demon = Card("Broodgate Demon",3,4,6); demon.guard = true;
            demon.attackArrows = CardArrow.NorthWest | CardArrow.North | CardArrow.NorthEast;
            demon.arrowEffect = "Arrowed enemies have -1 defence during your attacks.";
            demon.guardArrowEffect = "Arrowed allies get +1 defence."; Add(deck,demon,3);
            var queen = Card("Queen of the Unclaimed Path",3,5,6); queen.kind=CardKind.Evolution; queen.artworkOffset=new Vector2(0,215); queen.evolvesFrom=demon;
            queen.attackArrows=CardArrow.West | CardArrow.East | CardArrow.South; queen.requiredScars=2;
            queen.triumphQuest="Hit the opponent through a column whose enemy front zone was occupied this turn.";
            queen.onEvolution="Move one ally to an adjacent empty front zone.";
            queen.evolutionOngoing="Once per turn, an ally entering an arrowed front zone gets +1 power for its next battle.";
            deck.evolutionDeck.Add(new CardDeckEntry{card=queen,quantity=1});
            foreach(var e in deck.mainDeck) EditorUtility.SetDirty(e.card); EditorUtility.SetDirty(queen);
            AssetDatabase.CreateAsset(deck,Folder+"/Night Brood.asset"); AssetDatabase.SaveAssets(); Selection.activeObject=deck;
        }
        static CardDefinition Card(string name,int grade,int power,int defence)
        {
            string path=Folder+"/"+name+".asset";
            if(AssetDatabase.LoadAssetAtPath<CardDefinition>(path)!=null) throw new System.InvalidOperationException("Refusing to replace "+path);
            var c=ScriptableObject.CreateInstance<CardDefinition>(); c.cardName=name; c.archetype="Night Brood";
            c.essence=c.rearProduces=Essence.Gloom; c.grade=grade; c.genericEssenceCost=grade-1; c.power=power; c.defense=defence;
            c.frameAccent=new Color(.65f,.48f,.82f,1); c.artworkScale=1f; c.artworkOffset=new Vector2(0,175);
            c.artwork=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/CardCreator/EnhancedArtwork/"+name+".png");
            c.artistCredit="Ækashics / AI-enhanced"; AssetDatabase.CreateAsset(c,path); return c;
        }
        static void Ability(CardDefinition c,CardTrigger t,string text,bool once=false) => c.abilities.Add(new CardAbility{trigger=t,effect=text,oncePerTurn=once});
        static void Add(CardDeckDefinition d,CardDefinition c,int n) => d.mainDeck.Add(new CardDeckEntry{card=c,quantity=n});
    }
}
