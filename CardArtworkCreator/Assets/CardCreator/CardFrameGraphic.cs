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
        public float dividerY;
        public Essence essence, secondEssence;
        private static readonly Color Panel = new Color(0.105f, 0.14f, 0.16f, 1);

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            float y = dividerY > 0 ? dividerY : evolution ? 620 : 700;
            if (shadingOnly)
            {
                Quad(mesh, new Vector2(0, y), new Vector2(750, y), new Vector2(750,1050), new Vector2(0,1050), new Color(Panel.r, Panel.g, Panel.b, .87f));
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
                Circle(mesh,new Vector2(106,y+74),28,accent);
                Circle(mesh,new Vector2(106,y+74),24,Panel);
            }
            Line(mesh,new Vector2(48,977),new Vector2(672,977),1,new Color(0.57f,0.62f,0.63f,0.65f));
            // Links sit on the artwork perimeter, separate from the text and footer.
            if(arrows == CardArrow.None) return;
            Vector2[] positions = { new Vector2(375,40),new Vector2(700,48),new Vector2(710,y/2),
                new Vector2(700,y-70),new Vector2(375,y-40),new Vector2(50,y-70),new Vector2(40,y/2),new Vector2(50,48) };
            for(int i=0;i<8;i++)
            {
                bool active = ((int)arrows & (1 << i)) != 0;
                LinkMarker(mesh, positions[i], i * 45, active);
            }
        }
        private void LinkMarker(VertexHelper mesh, Vector2 center, float angle, bool active)
        {
            // Large Link-inspired silhouettes using the same ink and accent as the card frame.
            ArrowTriangle(mesh, center, angle, 1.65f, Panel);
            ArrowTriangle(mesh, center, angle, 1.45f, active ? accent : Color.Lerp(Panel, accent, 0.38f));
            ArrowTriangle(mesh, center + Rotate(new Vector2(0, 1), angle), angle, 1.13f,
                active ? Color.Lerp(accent, Color.white, 0.2f) : Panel);
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
