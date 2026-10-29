using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CardCreator.Editor
{
    public sealed class CardDeckWindow : EditorWindow
    {
        [SerializeField] private CardDeckDefinition deck;
        private VisualElement fields;
        private HelpBox summary;

        [MenuItem("Tools/Card Creator/Deck Builder")]
        public static void Open() => GetWindow<CardDeckWindow>("Deck Builder");

        private void OnEnable() => Undo.undoRedoPerformed += Refresh;
        private void OnDisable() => Undo.undoRedoPerformed -= Refresh;

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            var picker = new ObjectField("Deck") { objectType = typeof(CardDeckDefinition), value = deck, allowSceneObjects = false };
            picker.RegisterValueChangedCallback(evt => { deck = evt.newValue as CardDeckDefinition; BindDeck(); });
            rootVisualElement.Add(picker);
            rootVisualElement.Add(new Button(() =>
            {
                string path = EditorUtility.SaveFilePanelInProject("Create deck", "New Deck", "asset", "Save the deck list.");
                if (string.IsNullOrEmpty(path)) return;
                var created = CreateInstance<CardDeckDefinition>();
                AssetDatabase.CreateAsset(created, path);
                AssetDatabase.SaveAssets();
                picker.value = created;
            }) { text = "New Deck" });
            rootVisualElement.Add(new Label("Main deck: 20 cards. Rootbound target: 11 Grade 1 / 6 Grade 2 / 3 Grade 3. Evolution Deck: 1 boss."));
            summary = new HelpBox("Choose a deck.", HelpBoxMessageType.Info);
            rootVisualElement.Add(summary);
            fields = new ScrollView();
            rootVisualElement.Add(fields);
            BindDeck();
        }

        private void BindDeck()
        {
            if (fields == null) return;
            fields.Clear();
            if (deck != null)
            {
                var serialized = new SerializedObject(deck);
                foreach (string name in new[] { "deckName", "mainDeck", "evolutionDeck" })
                {
                    var field = new PropertyField(serialized.FindProperty(name));
                    field.Bind(serialized);
                    field.RegisterCallback<SerializedPropertyChangeEvent>(_ => Refresh());
                    fields.Add(field);
                }
            }
            Refresh();
        }

        private void Refresh()
        {
            if (summary == null) return;
            if (deck == null) { summary.text = "Choose a saved deck asset or create one."; return; }
            summary.text = Summarize(deck, out bool valid);
            summary.messageType = valid ? HelpBoxMessageType.Info : HelpBoxMessageType.Warning;
        }

        internal static string Summarize(CardDeckDefinition definition, out bool valid)
        {
            int total = 0, bosses = 0;
            int[] grades = new int[4];
            var errors = new List<string>();
            Count(definition.mainDeck, false, ref total, grades, errors);
            Count(definition.evolutionDeck, true, ref bosses, null, errors);
            if (total != 20) errors.Add("Main deck must contain 20 cards.");
            if (bosses != 1) errors.Add("The test Evolution Deck should contain 1 boss.");
            if (grades[1] != 11 || grades[2] != 6 || grades[3] != 3)
                errors.Add("Rootbound target is 11 / 6 / 3 across Grades 1 / 2 / 3.");
            valid = errors.Count == 0;
            return $"Main: {total}/20 · Grade 1: {grades[1]} · Grade 2: {grades[2]} · Grade 3: {grades[3]}\nEvolution: {bosses}/1\n" +
                (valid ? "Ready for the 20-card readability test." : string.Join("\n", errors));
        }

        private static void Count(List<CardDeckEntry> entries, bool evolution, ref int count, int[] grades, List<string> errors)
        {
            var seen = new HashSet<CardDefinition>();
            if (entries == null) return;
            foreach (var entry in entries)
            {
                if (entry == null || entry.card == null) { errors.Add("An entry has no card assigned."); continue; }
                if (entry.quantity < 1) { errors.Add(entry.card.cardName + ": quantity must be positive."); continue; }
                if (!seen.Add(entry.card)) errors.Add(entry.card.cardName + ": duplicate row; combine its quantities.");
                if ((entry.card.kind == CardKind.Evolution) != evolution)
                    errors.Add(entry.card.cardName + ": placed in the wrong deck.");
                count += entry.quantity;
                if (grades != null && entry.card.grade >= 1 && entry.card.grade <= 3)
                    grades[entry.card.grade] += entry.quantity;
            }
        }
    }
}
