using System;
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
            => Render(card, guardStance, out _);

        internal static Texture2D Render(CardDefinition card, bool guardStance, out CardLayoutReport report)
        {
            report = new CardLayoutReport();
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
                font = AssetDatabase.LoadAssetAtPath<Font>("Assets/CardCreator/OnlineAssets/Inter.ttf")
                    ?? Font.CreateDynamicFontFromOSFont("Arial", 32);
                titleFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/CardCreator/OnlineAssets/Cinzel.ttf")
                    ?? Font.CreateDynamicFontFromOSFont("Georgia", 40);

                var root = canvasObject.GetComponent<RectTransform>();
                var accent = card.frameAccent;
                float divider = card.kind == CardKind.Evolution ? 550 : 700;
                // Both layers extend beneath the entire frame, including the text area.
                var artPanel = AddPanel(root, "Full card illustration", 0, 0, Width, Height,
                    new Color(accent.r * 0.3f, accent.g * 0.3f, accent.b * 0.3f));
                artPanel.gameObject.AddComponent<RectMask2D>();
                var background = card.background != null ? card.background : DefaultBackground(card.essence);
                if (background != null && !card.artworkIncludesBackground)
                    AddTexture(artPanel, "Background", background, 0, 0, Width, Height, true);
                if (card.artwork != null)
                {
                    float fit = card.artworkIncludesBackground
                        ? Mathf.Max(750f / card.artwork.width, divider / card.artwork.height)
                        : Mathf.Min(730f / card.artwork.width, 1000f / card.artwork.height);
                    float artWidth = card.artwork.width * fit * card.artworkScale;
                    float artHeight = card.artwork.height * fit * card.artworkScale;
                    var artwork = AddTexture(artPanel, "Character", card.artwork, 0, 0,
                        artWidth, artHeight, false);
                    artwork.anchorMin = artwork.anchorMax = new Vector2(0.5f, 0.5f);
                    artwork.pivot = new Vector2(0.5f, 0.5f);
                    artwork.anchoredPosition = card.artworkOffset + (card.artworkIncludesBackground ? new Vector2(0, (Height - divider) * 0.5f) : Vector2.zero);
                }

                AddFrame(root, card, accent, CardArrow.None, true);
                AddFrame(root, card, accent, guardStance ? card.GuardArrows : card.attackArrows, false);

                Color ivory = new Color(0.93f, 0.94f, 0.92f);
                Color muted = new Color(0.65f, 0.70f, 0.71f);
                AddText(root, font, card.grade.ToString(), 69, divider-38, 74, 76, 48, accent,
                    FontStyle.Bold, TextAnchor.MiddleCenter);
                var title = AddText(root, font, card.cardName.ToUpperInvariant(), 184, divider+29, 506, 52,
                    34, ivory, FontStyle.Bold);
                title.resizeTextForBestFit = true;
                title.resizeTextMinSize = 24;
                title.resizeTextMaxSize = 34;
                string type = card.kind == CardKind.Evolution ? "EVOLUTION" : "CREATURE";
                AddText(root, font, type + "  /  " + CardPrintLayout.Cost(card), 184, divider+81, 506, 24,
                    16, muted);
                if (card.canPlayInRear && card.rearProduces != Essence.None)
                    AddIcon(root, EssenceIcon(card.rearProduces), 86, divider+82, 40, 40, accent);
                if(card.guard) AddIcon(root, "checked-shield", 86, divider+150, 40, 40, accent);
                if(card.promote) AddText(root, font, "P", 86, divider+(card.guard ? 210 : 150), 40,40,28,accent,FontStyle.Bold,TextAnchor.MiddleCenter);
                float rulesY = divider + 124;
                float rulesHeight = 960 - rulesY;
                var rules = AddText(root, font, CardPrintLayout.Rules(card, guardStance),
                    184, rulesY, 506, rulesHeight, 23, ivory);
                rules.lineSpacing = 1.02f;
                report.rulesOverflow = rules.preferredHeight > rulesHeight + 0.5f;
                if (!string.IsNullOrWhiteSpace(card.flavorText))
                {
                    float available = rulesHeight - rules.preferredHeight - 14;
                    var flavor = AddText(root, font, card.flavorText.Trim(), 184,
                        rulesY + rules.preferredHeight + 14, 506, Mathf.Max(1, available), 19,
                        muted, FontStyle.Italic);
                    report.flavorHidden = report.rulesOverflow || flavor.preferredHeight > available;
                    flavor.gameObject.SetActive(!report.flavorHidden);
                }
                AddText(root, font, "Art: " + (card.artistCredit ?? ""), 184, 990, 290, 38, 11, muted);
                AddIcon(root, "broadsword", 526, 992, 23, 23, muted);
                AddText(root, font, card.power.ToString(), 555, 987, 34, 34, 24, ivory, FontStyle.Bold);
                AddIcon(root, "checked-shield", 612, 992, 23, 23, muted);
                AddText(root, font, card.defense.ToString(), 641, 987, 34, 34, 24, ivory, FontStyle.Bold);

                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                var output = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                output.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                RoundCorners(output);
                output.Apply();
                return output;
            }
            finally
            {
                RenderTexture.active = previous;
                if (target != null) RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(canvasObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                if (font != null && !AssetDatabase.Contains(font)) UnityEngine.Object.DestroyImmediate(font);
                if (titleFont != null && !AssetDatabase.Contains(titleFont)) UnityEngine.Object.DestroyImmediate(titleFont);
            }
        }

        private static string EssenceIcon(Essence essence)
        {
            switch (essence)
            {
                case Essence.Grove: return "three-leaves";
                case Essence.Ember: return "small-fire";
                case Essence.Current: return "water-drop";
                default: return "moon";
            }
        }

        private static void AddIcon(RectTransform parent, string name, float x, float y, float w, float h, Color tint)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/CardCreator/OnlineAssets/" + name + ".png");
            if (texture == null) return;
            var rect = AddTexture(parent, name, texture, x, y, w, h, false);
            rect.GetComponent<RawImage>().color = tint;
        }

        private static void AddFrame(RectTransform parent, CardDefinition card, Color accent, CardArrow arrows, bool shading)
        {
            var rect = AddRect(parent, shading ? "Text shading" : "Engraved frame", 0, 0, Width, Height);
            var graphic = rect.gameObject.AddComponent<CardFrameGraphic>();
            graphic.accent = accent;
            graphic.evolution = card.kind == CardKind.Evolution;
            graphic.essence = card.canPlayInRear ? card.rearProduces : Essence.None;
            graphic.secondEssence = Essence.None;
            graphic.arrows = arrows;
            graphic.shadingOnly = shading;
            graphic.raycastTarget = false;
            graphic.SetVerticesDirty();
        }

        private static void RoundCorners(Texture2D texture)
        {
            const int radius = 24;
            for (int y=0;y<radius;y++) for(int x=0;x<radius;x++)
            {
                float distance = Vector2.Distance(new Vector2(x+0.5f,y+0.5f),new Vector2(radius,radius));
                float alpha = Mathf.Clamp01(radius-distance+0.5f);
                if(alpha >= 1) continue;
                foreach(var p in new[] { new Vector2Int(x,y),new Vector2Int(Width-1-x,y),new Vector2Int(x,Height-1-y),new Vector2Int(Width-1-x,Height-1-y) })
                { var color=texture.GetPixel(p.x,p.y);color.a*=alpha;texture.SetPixel(p.x,p.y,color); }
            }
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
            text.supportRichText = false;
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
