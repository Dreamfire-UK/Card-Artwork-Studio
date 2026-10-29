using System;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CardCreator.Editor
{
    public sealed class CardArtworkWindow : EditorWindow
    {
        private const string StylePath = "Assets/CardCreator/Editor/CardArtworkWindow.uss";
        [SerializeField] private CardDefinition card;
        [SerializeField] private bool showGuardStance;
        [SerializeField] private string activeTab = "Design";
        private Image preview;
        private Button exportButton, attackButton, guardButton;
        private VisualElement inspectorHost, tabs;
        private Label status, artInfo, previewTitle;
        private Texture2D previewTexture;
        private bool previewQueued, dragging;
        private Vector2 lastPointer;
        private int dragUndoGroup;

        [MenuItem("Tools/Card Creator/Open Card Creator")]
        public static void Open()
        {
            var window = GetWindow<CardArtworkWindow>("Card Artwork Studio");
            window.minSize = new Vector2(920, 760);
        }
        private void OnEnable() { Undo.undoRedoPerformed += OnUndo; }
        private void OnUndo() { ShowInspector(); QueuePreview(); }
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndo;
            EditorApplication.delayCall -= RefreshPreview;
            previewQueued = false;
            if (previewTexture != null) DestroyImmediate(previewTexture);
        }
        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.Clear(); root.AddToClassList("card-creator");
            var stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
            if (stylesheet != null) root.styleSheets.Add(stylesheet);
            var header = new VisualElement(); header.AddToClassList("studio-header");
            var brand = new Label("CARD ARTWORK STUDIO"); brand.AddToClassList("studio-brand"); header.Add(brand);
            var subtitle = new Label("FIVE ROADS  /  CARD WORKSHOP"); subtitle.AddToClassList("studio-eyebrow"); header.Add(subtitle);
            root.Add(header);
            var toolbar = new VisualElement(); toolbar.AddToClassList("card-creator__toolbar"); root.Add(toolbar);
            var picker = new ObjectField("Card") { objectType = typeof(CardDefinition), allowSceneObjects = false, value = card };
            picker.AddToClassList("card-creator__card-field");
            picker.RegisterValueChangedCallback(e => { card = e.newValue as CardDefinition; ShowInspector(); QueuePreview(); });
            toolbar.Add(picker);
            toolbar.Add(new Button(() => CreateCard(picker)) { text = "New card" });
            toolbar.Add(new Button(CardDeckWindow.Open) { text = "Deck builder" });
            toolbar.Add(new Button(RootboundCourtSamples.Create) { text = "Rootbound deck" });
            var content = new VisualElement(); content.AddToClassList("card-creator__content"); root.Add(content);
            var left = new VisualElement(); left.AddToClassList("card-creator__editor"); content.Add(left);
            tabs = new VisualElement(); tabs.AddToClassList("studio-tabs"); left.Add(tabs);
            foreach (string name in new[] { "Design", "Rules", "Artwork" })
            {
                string selectedName = name;
                var button = new Button(() => { activeTab = selectedName; UpdateTabs(); ShowInspector(); }) { text = name, name = name };
                button.AddToClassList("studio-tab"); tabs.Add(button);
            }
            inspectorHost = new ScrollView { horizontalScrollerVisibility = ScrollerVisibility.Hidden }; inspectorHost.AddToClassList("studio-fields"); left.Add(inspectorHost);
            var right = new VisualElement(); right.AddToClassList("card-creator__right"); content.Add(right);
            var previewHeader = new VisualElement(); previewHeader.AddToClassList("preview-header"); right.Add(previewHeader);
            previewTitle = new Label("CARD PREVIEW"); previewTitle.AddToClassList("studio-eyebrow"); previewHeader.Add(previewTitle);
            var stance = new VisualElement(); stance.AddToClassList("studio-row"); previewHeader.Add(stance);
            attackButton = new Button(() => SetStance(false)) { text = "Attack" };
            guardButton = new Button(() => SetStance(true)) { text = "Guard" };
            attackButton.AddToClassList("stance-button"); guardButton.AddToClassList("stance-button");
            stance.Add(attackButton); stance.Add(guardButton);
            var previewScroll = new ScrollView(); previewScroll.AddToClassList("preview-scroll"); right.Add(previewScroll);
            preview = new Image { scaleMode = ScaleMode.ScaleToFit }; preview.AddToClassList("card-creator__preview");
            preview.RegisterCallback<PointerDownEvent>(OnPointerDown);
            preview.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            preview.RegisterCallback<PointerUpEvent>(OnPointerUp);
            preview.RegisterCallback<PointerCaptureOutEvent>(_ => dragging = false);
            preview.RegisterCallback<WheelEvent>(OnWheel);
            previewScroll.Add(preview);
            var controls = new VisualElement(); controls.AddToClassList("preview-controls"); right.Add(controls);
            var hint = new Label("Drag to position · Scroll to scale"); hint.AddToClassList("studio-hint"); controls.Add(hint);
            controls.Add(new Button(ResetFraming) { text = "Reset framing" });
            artInfo = new Label(); artInfo.AddToClassList("studio-hint"); right.Add(artInfo);
            exportButton = new Button(ExportPng) { text = "Export PNG  ·  750 × 1050" }; exportButton.AddToClassList("card-creator__export"); right.Add(exportButton);
            status = new Label(); status.AddToClassList("card-creator__status"); right.Add(status);
            UpdateTabs(); SetStance(showGuardStance); ShowInspector(); QueuePreview();
        }
        private void UpdateTabs()
        {
            if (tabs == null) return;
            foreach (var child in tabs.Children()) child.EnableInClassList("is-selected", child.name == activeTab);
        }
        private void SetStance(bool guard)
        {
            showGuardStance = guard;
            attackButton?.EnableInClassList("is-selected", !guard);
            guardButton?.EnableInClassList("is-selected", guard);
            QueuePreview();
        }
        private void ShowInspector()
        {
            if (inspectorHost == null) return;
            inspectorHost.Unbind(); inspectorHost.Clear();
            if (card == null)
            {
                var welcome = new Label("Your next card starts here.\nChoose an existing card above, or create a new card to begin.");
                welcome.AddToClassList("studio-empty"); inspectorHost.Add(welcome); return;
            }
            var serialized = new SerializedObject(card);
            if (activeTab == "Design")
            {
                Group(serialized, "Identity", "cardName", "archetype", "kind", "frameAccent");
                if (card.kind == CardKind.Reference) return;
                Group(serialized, "Essence & cost", "essence", "secondEssence", "grade", "genericEssenceCost");
                Group(serialized, "Battle values", "power", "defense");
                Group(serialized, "Board placement", "canPlayInFront", "canPlayInRear", "rearProduces");
            }
            else if (activeTab == "Rules")
            {
                if (card.kind == CardKind.Reference) {
                    Group(serialized, "Turn reference text", "referenceText");
                    return;
                }
                Heading("Directional links");
                var hint = new Label("Choose Attack directions. Guard rotates them clockwise."); hint.AddToClassList("studio-hint"); inspectorHost.Add(hint);
                AddArrowGrid();
                Group(serialized, "Arrow effects", "arrowEffect", "guardArrowEffect");
                Group(serialized, "Keywords & abilities", "guard", "promote", "promoteCondition", "abilities", "flavorText");
                Group(serialized, "Evolution / Triumph", "evolvesFrom", "requiredScars", "triumphQuest", "onEvolution", "evolutionOngoing");
            }
            else
            {
                Group(serialized, "Illustration & framing", "artwork", "artworkIncludesBackground", "background", "artworkScale", "artworkOffset", "artistCredit");
                Heading("Asset library");
                inspectorHost.Add(new CardArtworkLibrary(AssignArtwork));
            }
        }
        private void Heading(string text)
        {
            var label = new Label(text); label.AddToClassList("studio-section"); inspectorHost.Add(label);
        }
        private void Group(SerializedObject serialized, string heading, params string[] properties)
        {
            Heading(heading);
            foreach (string name in properties)
            {
                var property = serialized.FindProperty(name);
                if (property == null) continue;
                var field = new PropertyField(property);
                field.Bind(serialized);
                field.RegisterCallback<SerializedPropertyChangeEvent>(_ => QueuePreview());
                inspectorHost.Add(field);
            }
        }
        private void AddArrowGrid()
        {
            var grid = new VisualElement(); grid.AddToClassList("arrow-grid");
            CardArrow[] arrows = { CardArrow.NorthWest, CardArrow.North, CardArrow.NorthEast, CardArrow.West, CardArrow.None, CardArrow.East, CardArrow.SouthWest, CardArrow.South, CardArrow.SouthEast };
            string[] labels = { "↖", "↑", "↗", "←", "•", "→", "↙", "↓", "↘" };
            VisualElement row = null;
            for (int i = 0; i < arrows.Length; i++)
            {
                if (i % 3 == 0) { row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; grid.Add(row); }
                CardArrow direction = arrows[i];
                var button = new Button { text = labels[i], tooltip = direction == CardArrow.None ? "Attack stance" : direction.ToString() };
                button.AddToClassList("arrow-toggle");
                button.SetEnabled(direction != CardArrow.None);
                button.EnableInClassList("is-selected", direction != CardArrow.None && (card.attackArrows & direction) != 0);
                button.clicked += () =>
                {
                    if (card == null || direction == CardArrow.None) return;
                    Undo.RecordObject(card, "Change card arrows"); card.attackArrows ^= direction;
                    EditorUtility.SetDirty(card);
                    button.EnableInClassList("is-selected", (card.attackArrows & direction) != 0); QueuePreview();
                };
                row.Add(button);
            }
            inspectorHost.Add(grid);
        }
        private void AssignArtwork(Texture2D texture, bool background)
        {
            if (card == null) return;
            Undo.RecordObject(card, background ? "Set card background" : "Set card artwork");
            if (background) { card.background = texture; card.artworkIncludesBackground = false; }
            else
            {
                card.artwork = texture;
                string path = AssetDatabase.GetAssetPath(texture);
                if (path == "Assets/CardCreator/Illustrations/Briar-Warden-Illustrated.png")
                {
                    card.artworkIncludesBackground = true;
                    card.artistCredit = "AI-assisted original";
                    card.artworkOffset = Vector2.zero;
                    card.artworkScale = 1;
                }
                else if (path.StartsWith("Assets/Aekashics Librarium Static Battlers/"))
                {
                    card.artworkIncludesBackground = false;
                    card.artistCredit = "Ækashics";
                }
            }
            EditorUtility.SetDirty(card); QueuePreview();
        }
        private void ResetFraming()
        {
            if (card == null) return;
            Undo.RecordObject(card, "Reset card framing"); card.artworkOffset = Vector2.zero; card.artworkScale = 1;
            EditorUtility.SetDirty(card); QueuePreview();
        }
        private void CreateCard(ObjectField picker)
        {
            string path = EditorUtility.SaveFilePanelInProject("Create card", "New Card", "asset", "Save your card asset.");
            if (string.IsNullOrEmpty(path)) return;
            var created = CreateInstance<CardDefinition>(); AssetDatabase.CreateAsset(created, path); AssetDatabase.SaveAssets();
            picker.value = created; Selection.activeObject = created;
        }
        private void QueuePreview()
        {
            if (previewQueued) return;
            previewQueued = true; EditorApplication.delayCall += RefreshPreview;
        }
        private void RefreshPreview()
        {
            previewQueued = false;
            if (preview == null || preview.panel == null) return;
            if (previewTexture != null) { preview.image = null; DestroyImmediate(previewTexture); previewTexture = null; }
            exportButton.SetEnabled(false);
            if (card == null) { status.text = "Choose a card to begin."; artInfo.text = ""; return; }
            try
            {
                previewTexture = CardRenderer.Render(card, showGuardStance, out var report); preview.image = previewTexture;
                previewTitle.text = card.cardName;
                status.text = string.IsNullOrEmpty(report.Message) ? "Ready to export · Full-card artwork" : report.Message;
                status.EnableInClassList("has-warning", !string.IsNullOrEmpty(report.Message));
                exportButton.SetEnabled(!report.rulesOverflow);
                if (card.kind == CardKind.Reference) { artInfo.text = "Turn reference · edit text in the Rules tab."; artInfo.EnableInClassList("has-warning",false); }
                else if (card.artwork != null)
                {
                    var art = card.artwork;
                    float divider = report.dividerY;
                    float enlargement = (card.artworkIncludesBackground
                        ? Mathf.Max(750f / art.width, divider / art.height)
                        : Mathf.Min(690f / art.width, (divider - 70f) / art.height)) * card.artworkScale;
                    artInfo.text = $"Artwork: {art.width} × {art.height} px" + (enlargement > 1.25f ? $" · Enlarged {enlargement:0.0}× — sharper art recommended" : "");
                    artInfo.EnableInClassList("has-warning", enlargement > 1.25f);
                }
                else artInfo.text = "Add a character in the Artwork tab.";
            }
            catch (Exception error) { status.text = "Preview failed: " + error.Message; Debug.LogException(error); }
        }
        private void OnPointerDown(PointerDownEvent evt)
        {
            if (card == null || card.artwork == null || evt.button != 0) return;
            dragging = true; lastPointer = new Vector2(evt.position.x, evt.position.y);
            Undo.IncrementCurrentGroup(); dragUndoGroup = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Position card artwork");
            preview.CapturePointer(evt.pointerId); evt.StopPropagation();
        }
        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!dragging || card == null) return;
            var current = new Vector2(evt.position.x, evt.position.y); var delta = current - lastPointer; lastPointer = current;
            float scale = Mathf.Min(preview.contentRect.width / CardRenderer.Width, preview.contentRect.height / CardRenderer.Height);
            Undo.RecordObject(card, "Position card artwork"); card.artworkOffset += new Vector2(delta.x, -delta.y) / Mathf.Max(0.001f, scale);
            EditorUtility.SetDirty(card); QueuePreview(); evt.StopPropagation();
        }
        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!dragging) return;
            dragging = false; Undo.CollapseUndoOperations(dragUndoGroup); preview.ReleasePointer(evt.pointerId); evt.StopPropagation();
        }
        private void OnWheel(WheelEvent evt)
        {
            if (card == null || card.artwork == null) return;
            Undo.RecordObject(card, "Scale card artwork"); card.artworkScale = Mathf.Clamp(card.artworkScale * Mathf.Exp(-evt.delta.y * 0.025f), 0.25f, 3f);
            EditorUtility.SetDirty(card); QueuePreview(); evt.StopPropagation();
        }
        private void ExportPng()
        {
            if (card == null) return;
            string filename = string.IsNullOrWhiteSpace(card.cardName) ? card.name : card.cardName;
            foreach (char invalid in Path.GetInvalidFileNameChars()) filename = filename.Replace(invalid, '_');
            string path = EditorUtility.SaveFilePanel("Export card PNG", Application.dataPath, filename, "png");
            if (string.IsNullOrEmpty(path)) return;
            Texture2D output = null;
            try
            {
                output = CardRenderer.Render(card, showGuardStance, out var report);
                if (report.rulesOverflow) throw new InvalidOperationException(report.Message);
                File.WriteAllBytes(path, output.EncodeToPNG());
                string creditsPath = "Assets/CardCreator/OnlineAssets/SOURCES-AND-CREDITS.txt";
                if (File.Exists(creditsPath))
                    File.WriteAllText(Path.ChangeExtension(path, ".credits.txt"),
                        "Card artwork: " + card.artistCredit + Environment.NewLine + Environment.NewLine + File.ReadAllText(creditsPath));
                var assetsPath = Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (Path.GetFullPath(path).StartsWith(assetsPath, StringComparison.OrdinalIgnoreCase)) AssetDatabase.Refresh();
                status.text = "Exported: " + path;
            }
            catch (Exception error) { Debug.LogException(error); EditorUtility.DisplayDialog("Export failed", error.Message, "OK"); }
            finally { if (output != null) DestroyImmediate(output); }
        }
    }
}

