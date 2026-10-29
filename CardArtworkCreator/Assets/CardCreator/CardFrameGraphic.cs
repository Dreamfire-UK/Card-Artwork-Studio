using UnityEngine;
using UnityEngine.UI;

namespace CardCreator
{
    // Vector geometry keeps the frame and directional markers sharp at export resolution.
    [ExecuteAlways]
    public sealed class CardFrameGraphic : MaskableGraphic
    {
        public Color accent;
        public CardArrow arrows;
        public bool shadingOnly;
        public bool evolution;
        public Essence essence;
        public Essence secondEssence;
        private static readonly Color Ink = new Color(0.025f, 0.035f, 0.05f, 0.94f);
        private static readonly Color Gold = new Color(0.78f, 0.70f, 0.50f);
        private static readonly Color Light = new Color(0.96f, 0.90f, 0.73f);

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (shadingOnly)
            {
                Gradient(mesh, 0, 0, 750, 230, new Color(0.02f, 0.03f, 0.045f, 0.94f), Color.clear);
                float fadeStart = evolution ? 490 : 635;
                float fadeEnd = evolution ? 660 : 805;
                Gradient(mesh, 0, fadeStart, 750, fadeEnd - fadeStart, Color.clear, new Color(0.02f, 0.03f, 0.045f, 0.78f));
                Gradient(mesh, 0, fadeEnd, 750, 1050 - fadeEnd, new Color(0.02f, 0.03f, 0.045f, 0.78f),
                    new Color(0.02f, 0.03f, 0.045f, 0.96f));
                return;
            }

            // A dark bevel, a gold hairline and an Essence-colored inset.
            Outline(mesh, 11, 16, 8, Ink);
            Outline(mesh, 17, 20, 2.5f, Gold);
            Outline(mesh, 26, 23, 1.2f, new Color(accent.r, accent.g, accent.b, 0.7f));
            for (int corner = 0; corner < 4; corner++)
                Corner(mesh, corner);

            // Thin title and rules ornaments leave the illustration visible.
            Line(mesh, new Vector2(100, 139), new Vector2(327, 139), 1.2f, Gold);
            Line(mesh, new Vector2(423, 139), new Vector2(650, 139), 1.2f, Gold);
            Diamond(mesh, new Vector2(375, 139), 7, Gold);
            Diamond(mesh, new Vector2(352, 139), 3, accent);
            Diamond(mesh, new Vector2(398, 139), 3, accent);
            float separator = evolution ? 620 : 767;
            Line(mesh, new Vector2(61, separator), new Vector2(355, separator), 1.4f, Gold);
            Line(mesh, new Vector2(395, separator), new Vector2(689, separator), 1.4f, Gold);
            Diamond(mesh, new Vector2(375, separator), 9, Gold);
            Diamond(mesh, new Vector2(375, separator), 5, accent);
            ManaSymbol(mesh, new Vector2(665, 176), essence);
            if (secondEssence != Essence.None) ManaSymbol(mesh, new Vector2(623, 176), secondEssence);

            // Grade jewel and two compact stat seals.
            Diamond(mesh, new Vector2(71, 190), 36, Ink);
            Diamond(mesh, new Vector2(71, 190), 31, Gold);
            Diamond(mesh, new Vector2(71, 190), 27, Ink);
            Diamond(mesh, new Vector2(71, 190), 23, new Color(accent.r * 0.45f, accent.g * 0.45f, accent.b * 0.45f));
            Seal(mesh, new Vector2(614, 974));
            Seal(mesh, new Vector2(675, 974));

            Vector2[] positions =
            {
                new Vector2(375, 28), new Vector2(704, 46), new Vector2(722, 525),
                new Vector2(704, 1004), new Vector2(375, 1022), new Vector2(46, 1004),
                new Vector2(28, 525), new Vector2(46, 46)
            };
            for (int i = 0; i < 8; i++)
                if (((int)arrows & (1 << i)) != 0)
                    Arrow(mesh, positions[i], i * 45f);
        }

        private void ManaSymbol(VertexHelper mesh, Vector2 center, Essence kind)
        {
            if (kind == Essence.None) return;
            Diamond(mesh, center, 23, Ink);
            Vector2[] points;
            switch (kind)
            {
                case Essence.Grove:
                    points = new[] { new Vector2(10, -16), new Vector2(12, -2), new Vector2(5, 10),
                        new Vector2(-10, 13), new Vector2(-12, 0), new Vector2(-5, -10) };
                    break;
                case Essence.Ember:
                    points = new[] { new Vector2(1, -18), new Vector2(12, -1), new Vector2(11, 10),
                        new Vector2(0, 15), new Vector2(-12, 8), new Vector2(-12, -3) };
                    break;
                case Essence.Current:
                    points = new[] { new Vector2(0, -18), new Vector2(11, 0), new Vector2(12, 9),
                        new Vector2(0, 15), new Vector2(-12, 9), new Vector2(-11, 0) };
                    break;
                default:
                    points = new[] { new Vector2(0, -16), new Vector2(12, -7), new Vector2(15, 5),
                        new Vector2(0, 14), new Vector2(-15, 5), new Vector2(-12, -7) };
                    break;
            }
            for (int i = 0; i < points.Length; i++)
                Triangle(mesh, center, center + points[i], center + points[(i + 1) % points.Length], Light);
            if (kind == Essence.Grove)
            {
                Line(mesh, center + new Vector2(-10, 15), center + new Vector2(7, -11), 2, Ink);
                Line(mesh, center + new Vector2(-3, 4), center + new Vector2(7, 2), 1.5f, Ink);
            }
        }

        private void Corner(VertexHelper mesh, int corner)
        {
            Vector2[] pattern =
            {
                new Vector2(36, 114), new Vector2(36, 64), new Vector2(64, 36),
                new Vector2(114, 36)
            };
            for (int i = 0; i < pattern.Length - 1; i++)
                Line(mesh, Mirror(pattern[i], corner), Mirror(pattern[i + 1], corner), 2, Gold);
            Line(mesh, Mirror(new Vector2(45, 94), corner), Mirror(new Vector2(45, 65), corner), 1, Light);
            Line(mesh, Mirror(new Vector2(45, 65), corner), Mirror(new Vector2(65, 45), corner), 1, Light);
            Line(mesh, Mirror(new Vector2(65, 45), corner), Mirror(new Vector2(94, 45), corner), 1, Light);
            Diamond(mesh, Mirror(new Vector2(52, 52), corner), 8, Gold);
            Diamond(mesh, Mirror(new Vector2(52, 52), corner), 4, Ink);
            Diamond(mesh, Mirror(new Vector2(36, 117), corner), 3, accent);
            Diamond(mesh, Mirror(new Vector2(117, 36), corner), 3, accent);
        }

        private static Vector2 Mirror(Vector2 p, int corner)
        {
            return new Vector2((corner == 1 || corner == 2) ? 750 - p.x : p.x,
                corner >= 2 ? 1050 - p.y : p.y);
        }

        private void Arrow(VertexHelper mesh, Vector2 center, float angle)
        {
            // A spearhead silhouette with an inset chevron, pointing out of the card.
            Vector2[] shape = { new Vector2(0, -25), new Vector2(22, 13),
                new Vector2(0, 5), new Vector2(-22, 13) };
            ArrowLayer(mesh, center, angle, shape, 1.22f, Ink);
            ArrowLayer(mesh, center, angle, shape, 1f, Light);
            ArrowLayer(mesh, center + Rotate(new Vector2(0, 2), angle), angle, shape, 0.77f, Gold);
            ArrowLayer(mesh, center + Rotate(new Vector2(0, 3), angle), angle, shape, 0.53f, accent);
        }

        private void ArrowLayer(VertexHelper mesh, Vector2 center, float angle, Vector2[] shape,
            float scale, Color color)
        {
            var points = new Vector2[shape.Length];
            for (int i = 0; i < shape.Length; i++) points[i] = center + Rotate(shape[i] * scale, angle);
            // Split the concave spearhead into two triangles at its inner notch.
            Triangle(mesh, points[0], points[1], points[2], color);
            Triangle(mesh, points[0], points[2], points[3], color);
        }

        private static Vector2 Rotate(Vector2 p, float angle)
        {
            float radians = angle * Mathf.Deg2Rad;
            return new Vector2(p.x * Mathf.Cos(radians) - p.y * Mathf.Sin(radians),
                p.x * Mathf.Sin(radians) + p.y * Mathf.Cos(radians));
        }

        private void Seal(VertexHelper mesh, Vector2 center)
        {
            Diamond(mesh, center, 32, Gold);
            Diamond(mesh, center, 29, Ink);
        }

        private void Outline(VertexHelper mesh, float inset, float cut, float weight, Color color)
        {
            Vector2[] p = { new Vector2(inset + cut, inset), new Vector2(750 - inset - cut, inset),
                new Vector2(750 - inset, inset + cut), new Vector2(750 - inset, 1050 - inset - cut),
                new Vector2(750 - inset - cut, 1050 - inset), new Vector2(inset + cut, 1050 - inset),
                new Vector2(inset, 1050 - inset - cut), new Vector2(inset, inset + cut) };
            for (int i = 0; i < p.Length; i++) Line(mesh, p[i], p[(i + 1) % p.Length], weight, color);
        }

        private void Diamond(VertexHelper mesh, Vector2 p, float radius, Color color)
        {
            Triangle(mesh, p + Vector2.up * radius, p + Vector2.right * radius, p + Vector2.down * radius, color);
            Triangle(mesh, p + Vector2.up * radius, p + Vector2.down * radius, p + Vector2.left * radius, color);
        }

        private void Line(VertexHelper mesh, Vector2 a, Vector2 b, float weight, Color color)
        {
            Vector2 d = (b - a).normalized;
            Vector2 n = new Vector2(-d.y, d.x) * weight * 0.5f;
            Triangle(mesh, a + n, b + n, b - n, color);
            Triangle(mesh, a + n, b - n, a - n, color);
        }

        private void Triangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int start = mesh.currentVertCount;
            Vertex(mesh, a, color); Vertex(mesh, b, color); Vertex(mesh, c, color);
            mesh.AddTriangle(start, start + 1, start + 2);
        }

        private void Gradient(VertexHelper mesh, float x, float y, float w, float h, Color top, Color bottom)
        {
            int start = mesh.currentVertCount;
            Vertex(mesh, new Vector2(x, y), top); Vertex(mesh, new Vector2(x + w, y), top);
            Vertex(mesh, new Vector2(x + w, y + h), bottom); Vertex(mesh, new Vector2(x, y + h), bottom);
            mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start, start + 2, start + 3);
        }

        private void Vertex(VertexHelper mesh, Vector2 p, Color color)
        {
            Rect r = rectTransform.rect;
            mesh.AddVert(new Vector3(r.xMin + p.x * r.width / 750,
                r.yMax - p.y * r.height / 1050, 0), color, Vector2.zero);
        }
    }
}
