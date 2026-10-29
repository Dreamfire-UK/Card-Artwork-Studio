using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CardCreator.Editor
{
    public static class CardBalanceExport
    {
        [Serializable] public class Entry { public int quantity; public string cardJson,assetPath,artworkPath,backgroundPath,evolvesFrom; }
        [Serializable] public class Deck { public string name; public List<Entry> main=new List<Entry>(),evolution=new List<Entry>(); }
        [Serializable] public class Bundle { public string schema="card-balance-v1"; public List<Deck> decks=new List<Deck>(); public List<Entry> variants=new List<Entry>(); }
        [MenuItem("Tools/Card Creator/Balance/Export Decks and Card Previews")]
        public static void ExportMenu() { var path=EditorUtility.OpenFolderPanel("Export balance data and cards", "", ""); if(!string.IsNullOrEmpty(path)) Export(path,true); }
        public static void Export(string output,bool previews)
        {
            Directory.CreateDirectory(output); Directory.CreateDirectory(Path.Combine(output,"cards")); var b=new Bundle();
            var seen=new HashSet<CardDefinition>();
            foreach(var guid in AssetDatabase.FindAssets("t:CardDeckDefinition",new[]{"Assets/CardCreator/Data"}))
            {
                var asset=AssetDatabase.LoadAssetAtPath<CardDeckDefinition>(AssetDatabase.GUIDToAssetPath(guid)); var d=new Deck{name=asset.deckName};
                foreach(var e in asset.mainDeck) { if(e.card==null) throw new Exception("Missing card in "+d.name); d.main.Add(Item(e.card,e.quantity)); seen.Add(e.card); }
                foreach(var e in asset.evolutionDeck) { if(e.card==null) throw new Exception("Missing evolution in "+d.name); d.evolution.Add(Item(e.card,e.quantity)); seen.Add(e.card); }
                b.decks.Add(d);
            }
            foreach(var guid in AssetDatabase.FindAssets("t:CardDefinition",new[]{"Assets/CardCreator/Data/Rootbound Court"}))
            { var c=AssetDatabase.LoadAssetAtPath<CardDefinition>(AssetDatabase.GUIDToAssetPath(guid)); if(!seen.Contains(c)) b.variants.Add(Item(c,0)); }
            File.WriteAllText(Path.Combine(output,"decks.json"),JsonUtility.ToJson(b,true));
            if(previews) foreach(var c in seen)
            {
                foreach(bool guard in new[]{false,true})
                {
                    var texture=CardRenderer.Render(c,guard,out var report);
                    if(report.rulesOverflow) { UnityEngine.Object.DestroyImmediate(texture); throw new Exception("Rules overflow: "+c.cardName); }
                    File.WriteAllBytes(Path.Combine(output,"cards",c.cardName+(guard?"-Guard":"-Attack")+".png"),texture.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            foreach(var guid in AssetDatabase.FindAssets("t:CardDefinition",new[]{"Assets/CardCreator/Data"})) {
                var reference=AssetDatabase.LoadAssetAtPath<CardDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if(reference.kind!=CardKind.Reference) continue;
                var image=CardRenderer.Render(reference,false,out var layout);
                try {
                    if(layout.rulesOverflow) throw new Exception("Reference overflow: "+reference.cardName);
                    File.WriteAllBytes(Path.Combine(output,"cards",reference.cardName+".png"),image.EncodeToPNG());
                } finally { UnityEngine.Object.DestroyImmediate(image); }
            }
            Debug.Log("BALANCE EXPORT PASS: "+b.decks.Count+" decks, "+seen.Count+" unique deck cards.");
        }
        static Entry Item(CardDefinition c,int n) => new Entry{quantity=n,cardJson=JsonUtility.ToJson(c,true),assetPath=AssetDatabase.GetAssetPath(c),artworkPath=AssetDatabase.GetAssetPath(c.artwork),backgroundPath=AssetDatabase.GetAssetPath(c.background),evolvesFrom=c.evolvesFrom==null?"":c.evolvesFrom.cardName};
    }
}
