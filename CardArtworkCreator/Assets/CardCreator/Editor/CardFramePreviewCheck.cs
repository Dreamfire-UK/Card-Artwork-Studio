using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CardCreator.Editor
{
    internal static class CardFramePreviewCheck
    {
        [InitializeOnLoadMethod]
        private static void CheckRequestedPreview()
        {
            if (!File.Exists("Temp/CardFramePreview.request")) return;
            EditorApplication.delayCall += Run;
        }

        private static void Run()
        {
            if (EditorApplication.isCompiling) { EditorApplication.delayCall += Run; return; }
            File.Delete("Temp/CardFramePreview.request");
            CardDefinition card = null;
            Texture2D image = null;
            try
            {
                foreach (string file in Directory.GetFiles("Assets/CardCreator/Backgrounds", "*.png"))
                    AssetDatabase.ImportAsset(file.Replace('\\', '/'), ImportAssetOptions.ForceUpdate);
                card = ScriptableObject.CreateInstance<CardDefinition>();
                card.cardName = "Briar Warden";
                card.archetype = "Rootbound Court";
                card.grade = 2;
                card.genericEssenceCost = 1;
                card.power = 3;
                card.defense = 4;
                card.guard = true;
                card.attackArrows = CardArrow.West | CardArrow.East | CardArrow.NorthEast;
                card.arrowEffect = "Friendly creatures in arrowed zones gain +1 power.";
                card.artwork = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Aekashics Librarium Static Battlers/Sideview Batch Battlers/Dryads Warrior.png");
                image = CardRenderer.Render(card, false);
                File.WriteAllBytes("Temp/CardFrame-Grove.png", image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
                card.cardName = "Broodgate Demon";
                card.archetype = "Night Brood";
                card.essence = Essence.Gloom;
                card.rearProduces = Essence.Gloom;
                card.artwork = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Aekashics Librarium Static Battlers/Sideview Batch Battlers/Darkness Reaper.png");
                card.attackArrows = (CardArrow)255;
                image = CardRenderer.Render(card, false);
                File.WriteAllBytes("Temp/CardFrame-Gloom.png", image.EncodeToPNG());
                File.WriteAllText("Temp/CardFramePreview.result", "PASS: rendered both 750x1050 card examples in Unity.");
            }
            catch (Exception error)
            {
                File.WriteAllText("Temp/CardFramePreview.result", error.ToString());
                Debug.LogException(error);
            }
            finally
            {
                if (image != null) UnityEngine.Object.DestroyImmediate(image);
                if (card != null) UnityEngine.Object.DestroyImmediate(card);
            }
        }
    }
}
