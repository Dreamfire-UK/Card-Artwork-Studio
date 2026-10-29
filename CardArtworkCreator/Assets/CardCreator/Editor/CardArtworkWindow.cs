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

        private Image preview;
        private Button exportButton;
        private VisualElement inspectorHost;
        private Label status;
        private Texture2D previewTexture;
        private bool previewQueued;
        private bool dragging;
        private Vector2 lastPointer;

        [MenuItem("Tools/Card Creator/Open Card Creator")]
        public static void Open()
        {
            var window = GetWindow<CardArtworkWindow>("Card Creator");
            window.minSize = new Vector2(820, 640);
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += QueuePreview;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= QueuePreview;
            if (previewTexture != null)
                DestroyImmediate(previewTexture);
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.Clear();
            root.AddToClassList("card-creator");
            var stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
            if (stylesheet != null)
                root.styleSheets.Add(stylesheet);

            var toolbar = new VisualElement();
            toolbar.AddToClassList("card-creator__toolbar");
            root.Add(toolbar);

            var cardField = new ObjectField("Card")
            {
                objectType = typeof(CardDefinition),
                allowSceneObjects = false,
                value = card
            };
            cardField.AddToClassList("card-creator__card-field");
            cardField.RegisterValueChangedCallback(evt =>
            {
                card = evt.newValue as CardDefinition;
                ShowInspector();
                QueuePreview();
            });
            toolbar.Add(cardField);
            toolbar.Add(new Button(() => CreateCard(cardField)) { text = "New Card" });

            var content = new VisualElement();
            content.AddToClassList("card-creator__content");
            root.Add(content);

            var left = new ScrollView();
            left.AddToClassList("card-creator__editor");
            content.Add(left);
            inspectorHost = left;
            ShowInspector();

            var right = new ScrollView();
            right.AddToClassList("card-creator__right");
            content.Add(right);
            right.Add(new Label("CARD PREVIEW") { name = "previewHeading" });
            var hint = new Label("Drag the artwork across the full card. Adjust Artwork Scale and Background at left.");
            hint.AddToClassList("card-creator__hint");
            right.Add(hint);

            preview = new Image { scaleMode = ScaleMode.ScaleToFit };
            preview.AddToClassList("card-creator__preview");
            preview.RegisterCallback<PointerDownEvent>(OnPointerDown);
            preview.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            preview.RegisterCallback<PointerUpEvent>(OnPointerUp);
            right.Add(preview);

            var stance = new Toggle("Preview Guard stance") { value = showGuardStance };
            stance.RegisterValueChangedCallback(evt =>
            {
                showGuardStance = evt.newValue;
                QueuePreview();
            });
            right.Add(stance);

            exportButton = new Button(ExportPng) { text = "Export card PNG (750 x 1050)" };
            exportButton.AddToClassList("card-creator__export");
            right.Add(exportButton);

            status = new Label();
            status.AddToClassList("card-creator__status");
            right.Add(status);
            QueuePreview();
        }

        private void ShowInspector()
        {
            if (inspectorHost == null)
                return;
            inspectorHost.Clear();
            if (card == null)
            {
                inspectorHost.Add(new Label("Choose a Card asset or click New Card."));
                return;
            }

            var inspector = new InspectorElement(new SerializedObject(card));
            inspector.RegisterCallback<SerializedPropertyChangeEvent>(_ => QueuePreview());
            inspectorHost.Add(inspector);
        }

        private void CreateCard(ObjectField cardField)
        {
            var path = EditorUtility.SaveFilePanelInProject("Create card", "New Card", "asset",
                "Choose where to save this card asset.");
            if (string.IsNullOrEmpty(path))
                return;

            var created = CreateInstance<CardDefinition>();
            AssetDatabase.CreateAsset(created, path);
            AssetDatabase.SaveAssets();
            cardField.value = created;
            Selection.activeObject = created;
        }

        private void QueuePreview()
        {
            if (previewQueued)
                return;
            previewQueued = true;
            EditorApplication.delayCall += RefreshPreview;
        }

        private void RefreshPreview()
        {
            previewQueued = false;
            if (preview == null || preview.panel == null)
                return;

            if (previewTexture != null)
            {
                preview.image = null;
                DestroyImmediate(previewTexture);
                previewTexture = null;
            }

            exportButton.SetEnabled(card != null);
            if (card == null)
            {
                status.text = "Create or select a card to begin.";
                return;
            }

            try
            {
                previewTexture = CardRenderer.Render(card, showGuardStance);
                preview.image = previewTexture;
                status.text = showGuardStance
                    ? "Guard stance rotates the printed arrows clockwise."
                    : "Attack stance. Drag to position the character.";
            }
            catch (Exception exception)
            {
                status.text = "Preview failed: " + exception.Message;
                Debug.LogException(exception);
            }
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (card == null || card.artwork == null || evt.button != 0)
                return;
            dragging = true;
            lastPointer = new Vector2(evt.position.x, evt.position.y);
            preview.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!dragging || card == null)
                return;
            var current = new Vector2(evt.position.x, evt.position.y);
            var delta = current - lastPointer;
            lastPointer = current;
            float factor = CardRenderer.Width / Mathf.Max(1, preview.resolvedStyle.width);
            Undo.RecordObject(card, "Move card artwork");
            card.artworkOffset += new Vector2(delta.x * factor, -delta.y * factor);
            EditorUtility.SetDirty(card);
            QueuePreview();
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!dragging)
                return;
            dragging = false;
            preview.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void ExportPng()
        {
            if (card == null)
                return;
            var filename = string.IsNullOrWhiteSpace(card.cardName) ? card.name : card.cardName;
            foreach (char invalid in Path.GetInvalidFileNameChars())
                filename = filename.Replace(invalid, '_');
            var path = EditorUtility.SaveFilePanel("Export card PNG", Application.dataPath,
                filename, "png");
            if (string.IsNullOrEmpty(path))
                return;

            Texture2D output = null;
            try
            {
                output = CardRenderer.Render(card, showGuardStance);
                File.WriteAllBytes(path, output.EncodeToPNG());
                var assetsPath = Path.GetFullPath(Application.dataPath)
                    .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (Path.GetFullPath(path).StartsWith(assetsPath, StringComparison.OrdinalIgnoreCase))
                    AssetDatabase.Refresh();
                status.text = "Exported: " + path;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Export failed", exception.Message, "OK");
            }
            finally
            {
                if (output != null)
                    DestroyImmediate(output);
            }
        }
    }
}

