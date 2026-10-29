using System;
using UnityEditor;
using UnityEngine;

namespace CardCreator.Editor
{
    public static class CardStorybookSamples
    {
        public const string SamplePath = "Assets/CardCreator/Data/Rootbound Court/Briar Warden - Storybook.asset";
        public const string ArtworkPath = "Assets/CardCreator/Illustrations/Briar-Warden-Storybook.png";

        [MenuItem("Tools/Card Creator/Create Storybook Briar Warden")]
        public static void Create()
        {
            var existing = AssetDatabase.LoadAssetAtPath<CardDefinition>(SamplePath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                return;
            }

            AssetDatabase.ImportAsset(ArtworkPath, ImportAssetOptions.ForceUpdate);
            var artwork = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtworkPath);
            if (artwork == null) throw new InvalidOperationException("Storybook artwork is missing: " + ArtworkPath);

            RootboundCourtSamples.Create();
            var source = AssetDatabase.LoadAssetAtPath<CardDefinition>(RootboundCourtSamples.Folder + "/Briar Warden.asset");
            if (source == null) throw new InvalidOperationException("The original Briar Warden card could not be loaded.");

            var card = UnityEngine.Object.Instantiate(source);
            card.name = "Briar Warden - Storybook";
            card.artwork = artwork;
            card.artworkIncludesBackground = true;
            card.background = null;
            card.artworkScale = 1;
            card.artworkOffset = Vector2.zero;
            card.artistCredit = "AI-assisted original";
            AssetDatabase.CreateAsset(card, SamplePath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = card;
        }
    }
}
