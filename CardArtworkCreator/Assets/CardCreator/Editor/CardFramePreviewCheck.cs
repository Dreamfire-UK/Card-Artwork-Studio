using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace CardCreator.Editor
{
    public static class CardFramePreviewCheck
    {
        [MenuItem("Tools/Card Creator/Validate and Render Rootbound Court")]
        public static void Run()
        {
            RootboundCourtSamples.Create();
            string folder = RootboundCourtSamples.Folder;
            string output = Environment.GetEnvironmentVariable("CARD_PREVIEW_OUTPUT");
            if (string.IsNullOrEmpty(output)) output = "CardPreviews";
            Directory.CreateDirectory(output);
            var deck = AssetDatabase.LoadAssetAtPath<CardDeckDefinition>(folder + "/Rootbound Court.asset");
            string summary = CardDeckWindow.Summarize(deck, out bool valid);
            Require(valid, summary);
            int promotions = 0;
            foreach (var entry in deck.mainDeck)
            {
                if (entry.card.promote) promotions++;
                Require(entry.card.artwork != null, "Missing art: " + entry.card.cardName);
                Render(entry.card, false, output);
                Render(entry.card, true, output);
            }
            Require(promotions == 1, "Exactly one distinct Promote creature expected.");
            Render(deck.evolutionDeck[0].card, false, output);
            Render(deck.evolutionDeck[0].card, true, output);
            var warden = AssetDatabase.LoadAssetAtPath<CardDefinition>(folder + "/Briar Warden.asset");
            Require(CardPrintLayout.Cost(warden) == "1 Grove + 1 any", "Explicit cost");
            Require(CardPrintLayout.Rules(warden, false).Contains("+1 power"), "Attack effect");
            Require(CardPrintLayout.Rules(warden, true).Contains("+1 defence"), "Guard effect");
            Require(warden.GuardArrows == (CardArrow.North | CardArrow.South), "Clockwise arrow rotation");
            var probe = ScriptableObject.CreateInstance<CardDefinition>();
            try
            {
                probe.flavorText = new string('W', 3000);
                var image = CardRenderer.Render(probe, false, out var report);
                UnityEngine.Object.DestroyImmediate(image);
                Require(report.flavorHidden && !report.rulesOverflow, "Hide flavour first");
                probe.abilities.Add(new CardAbility { effect = new string('W', 3000) });
                image = CardRenderer.Render(probe, false, out report);
                UnityEngine.Object.DestroyImmediate(image);
                Require(report.rulesOverflow, "Report overflowing rules");
            }
            finally { UnityEngine.Object.DestroyImmediate(probe); }
            File.WriteAllText(Path.Combine(output, "validation.txt"), "PASS: all 9 cards in both stances; readable rules, explicit costs, arrow rotation, flavour fallback, overflow detection.\n" + summary);
            Debug.Log("Card Creator validation PASS. Exports: " + Path.GetFullPath(output));
        }
        private static void Render(CardDefinition card, bool guard, string folder)
        {
            var image = CardRenderer.Render(card, guard, out var report);
            try
            {
                Require(!report.rulesOverflow, card.cardName + ": " + report.Message);
                Require(image.width == 750 && image.height == 1050, "Export dimensions");
                File.WriteAllBytes(Path.Combine(folder, card.cardName.Replace(",", "") + (guard ? "-Guard" : "-Attack") + ".png"), image.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(image); }
        }
        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException("Card validation failed: " + message);
        }
    }
}
