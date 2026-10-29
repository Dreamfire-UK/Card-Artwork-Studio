using System;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CardCreator.Editor
{
    internal static class CardRenderer
    {
        internal const int Width = 750;
        internal const int Height = 1050;

        internal static Texture2D Render(CardDefinition card, bool guardStance)
        {
            if (card == null)
                throw new ArgumentNullException(nameof(card));

            var cameraObject = new GameObject("Card export camera", typeof(Camera));
            var canvasObject = new GameObject("Card export canvas", typeof(RectTransform), typeof(Canvas));
            RenderTexture target = null;
            Font font = null;
            Font titleFont = null;
            var previous = RenderTexture.active;
            try
            {
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                canvasObject.hideFlags = HideFlags.HideAndDontSave;
                cameraObject.transform.position = new Vector3(100000, 100000, -10);
                canvasObject.layer = 5;

                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.orthographic = true;
                camera.orthographicSize = Height * 0.5f;
                camera.cullingMask = 1 << 5;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 20;
                target = RenderTexture.GetTemporary(Width, Height, 24, RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.sRGB);
                camera.targetTexture = target;

                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                canvas.pixelPerfect = true;
                font = Font.CreateDynamicFontFromOSFont("Arial", 32);
                titleFont = Font.CreateDynamicFontFromOSFont("Georgia", 40);

                var root = canvasObject.GetComponent<RectTransform>();
                var accent = EssenceColor(card.essence);
                // Both layers extend beneath the entire frame, including the text area.
                var artPanel = AddPanel(root, "Full card illustration", 0, 0, Width, Height,
                    new Color(accent.r * 0.3f, accent.g * 0.3f, accent.b * 0.3f));
                artPanel.gameObject.AddComponent<RectMask2D>();
                var background = card.background != null ? card.background : DefaultBackground(card.essence);
                if (background != null)
                    AddTexture(artPanel, "Background", background, 0, 0, Width, Height, true);
                if (card.artwork != null)
                {
                    float fit = Mathf.Min(730f / card.artwork.width, 1000f / card.artwork.height);
                    float artWidth = card.artwork.width * fit * card.artworkScale;
                    float artHeight = card.artwork.height * fit * card.artworkScale;
                    var artwork = AddTexture(artPanel, "Character", card.artwork, 0, 0,
                        artWidth, artHeight, false);
                    artwork.anchorMin = artwork.anchorMax = new Vector2(0.5f, 0.5f);
                    artwork.pivot = new Vector2(0.5f, 0.5f);
                    artwork.anchoredPosition = card.artworkOffset;
                }

                AddFrame(root, accent, CardArrow.None, true);
                AddFrame(root, accent, guardStance ? card.GuardArrows : card.attackArrows, false);

                Color ivory = new Color(0.96f, 0.93f, 0.84f);
                Color mutedGold = new Color(0.79f, 0.73f, 0.57f);
                var title = AddText(root, titleFont, card.cardName, 89, 58, 572, 52,
                    39, ivory, FontStyle.Bold, TextAnchor.MiddleCenter);
                title.resizeTextForBestFit = true;
                title.resizeTextMinSize = 23;
                title.resizeTextMaxSize = 39;

                string type = card.kind == CardKind.Evolution ? "EVOLUTION" : "CREATURE";
                string subtitle = string.IsNullOrWhiteSpace(card.archetype)
                    ? type : card.archetype.ToUpperInvariant() + "  /  " + type;
                AddText(root, font, subtitle, 100, 112, 550, 22, 15, mutedGold,
                    FontStyle.Normal, TextAnchor.MiddleCenter);

                AddText(root, titleFont, card.grade.ToString(), 45, 166, 52, 45,
                    31, ivory, FontStyle.Bold, TextAnchor.MiddleCenter);
                AddText(root, font, "GRADE", 38, 233, 66, 17, 11, mutedGold,
                    FontStyle.Bold, TextAnchor.MiddleCenter);

                string essence = card.secondEssence == Essence.None
                    ? card.essence.ToString() : card.essence + " / " + card.secondEssence;
                AddText(root, font, essence.ToUpperInvariant(), 405, 171, 268, 25,
                    17, ivory, FontStyle.Bold, TextAnchor.UpperRight);
                string cost = card.genericEssenceCost > 0 ? " + " + card.genericEssenceCost + " ANY" : "";
                AddText(root, font, card.kind == CardKind.Evolution ? "EVOLUTION DECK" : "1 ESSENCE" +
                    (card.secondEssence != Essence.None ? " + 1 " + card.secondEssence.ToString().ToUpperInvariant() : "") + cost,
                    355, 199, 318, 23, 13, mutedGold, FontStyle.Normal, TextAnchor.UpperRight);

                var keywords = new StringBuilder();
                if (card.guard) keywords.Append("GUARD    ");
                if (card.promote) keywords.Append("PROMOTE    ");
                if (card.kind == CardKind.Evolution) keywords.Append("TRIUMPH");
                AddText(root, font, keywords.ToString().Trim(), 65, 730, 620, 26,
                    18, ivory, FontStyle.Bold, TextAnchor.MiddleCenter);
                AddText(root, font, BuildRules(card, guardStance), 65, 789, 620, 151,
                    21, ivory);

                AddText(root, titleFont, card.power.ToString(), 591, 952, 46, 38,
                    27, ivory, FontStyle.Bold, TextAnchor.MiddleCenter);
                AddText(root, titleFont, card.defense.ToString(), 652, 952, 46, 38,
                    27, ivory, FontStyle.Bold, TextAnchor.MiddleCenter);
                AddText(root, font, "ATK", 590, 1010, 48, 15, 10, mutedGold, FontStyle.Bold, TextAnchor.MiddleCenter);
                AddText(root, font, "DEF", 651, 1010, 48, 15, 10, mutedGold, FontStyle.Bold, TextAnchor.MiddleCenter);
                AddText(root, font, string.IsNullOrWhiteSpace(card.artistCredit) ? "" : "ART · " + card.artistCredit,
                    65, 977, 470, 23, 14, mutedGold);

                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                var output = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                output.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                output.Apply();
                return output;
            }
            finally
            {
                RenderTexture.active = previous;
                if (target != null) RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(canvasObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                if (font != null) UnityEngine.Object.DestroyImmediate(font);
                if (titleFont != null) UnityEngine.Object.DestroyImmediate(titleFont);
            }
        }

        private static string BuildRules(CardDefinition card, bool guardStance)
        {
            var parts = new StringBuilder();
            if (card.kind == CardKind.Creature && card.canPlayInRear && card.rearProduces != Essence.None)
                parts.Append("REAR — Exhaust: make 1 ").Append(card.rearProduces).Append(" Essence.\n");
            if (card.promote && !string.IsNullOrWhiteSpace(card.promoteCondition))
                parts.Append("PROMOTE — ").Append(card.promoteCondition.Trim()).Append('\n');
            string arrowEffect = guardStance && !string.IsNullOrWhiteSpace(card.guardArrowEffect)
                ? card.guardArrowEffect : card.arrowEffect;
            if (card.attackArrows != CardArrow.None && !string.IsNullOrWhiteSpace(arrowEffect))
                parts.Append("ARROWS — ").Append(arrowEffect.Trim()).Append('\n');
            if (card.kind == CardKind.Evolution)
            {
                parts.Append("TRIUMPH — Evolve onto ")
                    .Append(card.evolvesFrom != null ? card.evolvesFrom.cardName : "named base")
                    .Append(" with ").Append(card.requiredScars).Append(" scars");
                if (!string.IsNullOrWhiteSpace(card.triumphQuest))
                    parts.Append("; ").Append(card.triumphQuest.Trim());
                parts.Append('\n');
                if (!string.IsNullOrWhiteSpace(card.onEvolution))
                    parts.Append("ON EVOLUTION — ").Append(card.onEvolution.Trim()).Append('\n');
                if (!string.IsNullOrWhiteSpace(card.evolutionOngoing))
                    parts.Append(card.evolutionOngoing.Trim()).Append('\n');
            }
            foreach (var ability in card.abilities)
            {
                if (ability == null || string.IsNullOrWhiteSpace(ability.effect)) continue;
                parts.Append(ability.trigger.ToString().ToUpperInvariant()).Append(" — ");
                if (!string.IsNullOrWhiteSpace(ability.condition))
                    parts.Append(ability.condition.Trim()).Append(": ");
                parts.Append(ability.effect.Trim()).Append('\n');
            }
            if (!string.IsNullOrWhiteSpace(card.flavorText))
                parts.Append('\n').Append(card.flavorText.Trim());
            return parts.ToString().TrimEnd();
        }

        private static void AddFrame(RectTransform parent, Color accent, CardArrow arrows, bool shading)
        {
            var rect = AddRect(parent, shading ? "Text shading" : "Engraved frame", 0, 0, Width, Height);
            var graphic = rect.gameObject.AddComponent<CardFrameGraphic>();
            graphic.accent = accent;
            graphic.arrows = arrows;
            graphic.shadingOnly = shading;
            graphic.raycastTarget = false;
            graphic.SetVerticesDirty();
        }

        private static RectTransform AddPanel(RectTransform parent, string name,
            float x, float y, float width, float height, Color color)
        {
            var rect = AddRect(parent, name, x, y, width, height);
            var image = rect.gameObject.AddComponent<RawImage>();
            image.texture = Texture2D.whiteTexture;
            image.color = color;
            return rect;
        }

        private static RectTransform AddTexture(RectTransform parent, string name, Texture texture,
            float x, float y, float width, float height, bool crop)
        {
            var rect = AddRect(parent, name, x, y, width, height);
            var image = rect.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            if (crop)
            {
                float imageAspect = (float)texture.width / texture.height;
                float panelAspect = width / height;
                if (imageAspect > panelAspect)
                {
                    float visibleWidth = panelAspect / imageAspect;
                    image.uvRect = new Rect((1 - visibleWidth) * 0.5f, 0, visibleWidth, 1);
                }
                else
                {
                    float visibleHeight = imageAspect / panelAspect;
                    image.uvRect = new Rect(0, (1 - visibleHeight) * 0.5f, 1, visibleHeight);
                }
            }
            return rect;
        }

        private static UnityEngine.UI.Text AddText(RectTransform parent, Font font, string value, float x, float y,
            float width, float height, int size, Color color, FontStyle style = FontStyle.Normal,
            TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var rect = AddRect(parent, "Text", x, y, width, height);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value ?? string.Empty;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            if (height >= 150)
            {
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 13;
                text.resizeTextMaxSize = size;
            }
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform AddRect(RectTransform parent, string name,
            float x, float y, float width, float height)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.hideFlags = HideFlags.HideAndDontSave;
            child.layer = 5;
            var rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static Color EssenceColor(Essence essence)
        {
            switch (essence)
            {
                case Essence.Grove: return new Color(0.31f, 0.68f, 0.39f);
                case Essence.Gloom: return new Color(0.57f, 0.39f, 0.74f);
                case Essence.Ember: return new Color(0.95f, 0.43f, 0.25f);
                case Essence.Current: return new Color(0.27f, 0.7f, 0.82f);
                default: return new Color(0.73f, 0.69f, 0.57f);
            }
        }

        private static Texture2D DefaultBackground(Essence essence)
        {
            string name;
            switch (essence)
            {
                case Essence.Grove: name = "Grove Glade"; break;
                case Essence.Gloom: name = "Gloom Ruins"; break;
                case Essence.Ember: name = "Ember Forge"; break;
                case Essence.Current: name = "Current Coast"; break;
                default: return null;
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/CardCreator/Backgrounds/" + name + ".png");
        }
    }
}
