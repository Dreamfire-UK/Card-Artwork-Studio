using UnityEngine;
using UnityEngine.UI;

namespace CardCreator
{
    [ExecuteAlways, RequireComponent(typeof(CanvasRenderer))]
    public sealed class CardFrameGraphic : MaskableGraphic
    {
        public Color accent;
        public CardArrow arrows;
        public bool shadingOnly, evolution;
        public Essence essence, secondEssence;
        private static readonly Color Panel = new Color(0.105f, 0.14f, 0.16f, 1);

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            float y = evolution ? 550 : 700;
            if (shadingOnly)
            {
                Quad(mesh, new Vector2(0, y), new Vector2(750, y), new Vector2(750,1050), new Vector2(0,1050), Panel);
                return;
            }
            // A single ink-like seam joins the illustration to the typography panel.
            Vector2[] seam = { new Vector2(0,y+2),new Vector2(64,y-1),new Vector2(159,y+2),new Vector2(230,y-2),
                new Vector2(332,y+2),new Vector2(449,y),new Vector2(548,y+3),new Vector2(665,y-2),new Vector2(750,y+1) };
            for(int i=0;i<seam.Length-1;i++) Line(mesh,seam[i],seam[i+1],8,accent);
            Circle(mesh,new Vector2(106,y),53,accent);
            Circle(mesh,new Vector2(106,y),44,Panel);
            Diamond(mesh,new Vector2(179,y),14,accent);
            Diamond(mesh,new Vector2(179,y),7,Panel);
            if (essence != Essence.None)
            {
                Circle(mesh,new Vector2(106,y+102),28,accent);
                Circle(mesh,new Vector2(106,y+102),24,Panel);
            }
            Line(mesh,new Vector2(184,977),new Vector2(672,977),1,new Color(0.57f,0.62f,0.63f,0.65f));
            // Links sit on the artwork perimeter, separate from the text and footer.
            if(arrows == CardArrow.None) return;
            Vector2[] positions = { new Vector2(375,26),new Vector2(718,32),new Vector2(724,y/2),
                new Vector2(718,y-41),new Vector2(375,y-26),new Vector2(32,y-41),new Vector2(26,y/2),new Vector2(32,32) };
            for(int i=0;i<8;i++)
            {
                bool active = ((int)arrows & (1 << i)) != 0;
                LinkMarker(mesh, positions[i], i * 45, active);
            }
        }
        private void LinkMarker(VertexHelper mesh, Vector2 center, float angle, bool active)
        {
            // Filled, triangular sockets with a metallic bevel, like Link Monster markers.
            // Active directions are always red; the card's cosmetic accent does not change them.
            ArrowTriangle(mesh, center, angle, 1.12f, new Color(0.025f, 0.03f, 0.04f));
            ArrowTriangle(mesh, center, angle, 1f, new Color(0.68f, 0.71f, 0.75f));
            ArrowTriangle(mesh, center + Rotate(new Vector2(0, 1), angle), angle, 0.84f,
                new Color(0.22f, 0.25f, 0.29f));
            Vector2 inset = center + Rotate(new Vector2(0, 1), angle);
            Vector2 tip = inset + Rotate(new Vector2(0, -19) * 0.73f, angle);
            Vector2 right = inset + Rotate(new Vector2(25, 14) * 0.73f, angle);
            Vector2 left = inset + Rotate(new Vector2(-25, 14) * 0.73f, angle);
            Triangle(mesh, tip, left, inset, active ? new Color(1f, 0.23f, 0.18f) : new Color(0.12f, 0.14f, 0.17f));
            Triangle(mesh, tip, inset, right, active ? new Color(0.83f, 0.055f, 0.06f) : new Color(0.055f, 0.065f, 0.08f));
            Triangle(mesh, left, right, inset, active ? new Color(0.61f, 0.025f, 0.04f) : new Color(0.025f, 0.03f, 0.04f));
        }
        private void ArrowTriangle(VertexHelper mesh, Vector2 center, float angle, float scale, Color color)
        {
            Triangle(mesh, center + Rotate(new Vector2(0, -19) * scale, angle),
                center + Rotate(new Vector2(25, 14) * scale, angle),
                center + Rotate(new Vector2(-25, 14) * scale, angle), color);
        }
        private static Vector2 Rotate(Vector2 p,float degrees)
        {
            float r=degrees*Mathf.Deg2Rad;
            return new Vector2(p.x*Mathf.Cos(r)-p.y*Mathf.Sin(r),p.x*Mathf.Sin(r)+p.y*Mathf.Cos(r));
        }
        private void Circle(VertexHelper mesh,Vector2 c,float radius,Color color)
        {
            for(int i=0;i<64;i++)Triangle(mesh,c,c+Rotate(Vector2.up*radius,i*5.625f),c+Rotate(Vector2.up*radius,(i+1)*5.625f),color);
        }
        private void Diamond(VertexHelper mesh,Vector2 c,float r,Color color)
            => Quad(mesh,c+Vector2.up*r,c+Vector2.right*r,c+Vector2.down*r,c+Vector2.left*r,color);
        private void Line(VertexHelper mesh,Vector2 a,Vector2 b,float width,Color color)
        {
            Vector2 d=(b-a).normalized,n=new Vector2(-d.y,d.x)*width*.5f;
            Quad(mesh,a+n,b+n,b-n,a-n,color);
        }
        private void Quad(VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color)
        { Triangle(mesh,a,b,c,color);Triangle(mesh,a,c,d,color); }
        private void Triangle(VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,Color color)
        {
            int start=mesh.currentVertCount;Vertex(mesh,a,color);Vertex(mesh,b,color);Vertex(mesh,c,color);mesh.AddTriangle(start,start+1,start+2);
        }
        private void Vertex(VertexHelper mesh,Vector2 p,Color color)
        {
            Rect r=rectTransform.rect;mesh.AddVert(new Vector3(r.xMin+p.x*r.width/750,r.yMax-p.y*r.height/1050,0),color,Vector2.zero);
        }
    }
}
