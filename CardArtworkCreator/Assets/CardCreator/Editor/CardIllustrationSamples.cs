using System.IO;
using UnityEditor;
using UnityEngine;
namespace CardCreator.Editor {
public static class CardIllustrationSamples {
 public const string SamplePath = "Assets/CardCreator/Data/Rootbound Court/Briar Warden - Illustrated.asset";
 [MenuItem("Tools/Card Creator/Create Illustrated Briar Warden")]
 public static void Create() {
  AssetDatabase.ImportAsset("Assets/CardCreator/Illustrations/Briar-Warden-Illustrated.png", ImportAssetOptions.ForceUpdate);
  var existing = AssetDatabase.LoadAssetAtPath<CardDefinition>(SamplePath);
  if(existing != null) { Selection.activeObject = existing; return; }
  RootboundCourtSamples.Create();
  var source = AssetDatabase.LoadAssetAtPath<CardDefinition>(RootboundCourtSamples.Folder + "/Briar Warden.asset");
  var card = Object.Instantiate(source);
  card.artwork = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/CardCreator/Illustrations/Briar-Warden-Illustrated.png");
  card.artworkIncludesBackground = true;
  card.background = null;
  card.artworkScale = 1;
  card.artworkOffset = Vector2.zero;
  card.artistCredit = "AI-assisted original";
  AssetDatabase.CreateAsset(card,SamplePath);
  AssetDatabase.SaveAssets();
  Selection.activeObject = card;
 }
}
}
