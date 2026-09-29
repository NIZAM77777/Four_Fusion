using UnityEngine;
using UnityEngine.UIElements;

// Stateless presentation primitives. No input, game rules, saved state or scene actions live here.
internal enum FusionTone { Blue, Gold, Red, Green }

internal sealed class FusionSurface : VisualElement
{
    private readonly FusionTone tone;
    private readonly bool subdued;
    private bool accented;
    public FusionSurface(FusionTone tone = FusionTone.Blue, bool subdued = false)
    {
        this.tone = tone; this.subdued = subdued;
        pickingMode = PickingMode.Ignore; AddToClassList("surface");
        generateVisualContent += Draw;
    }
    public void Accent(bool value) { if (accented == value) return; accented = value; MarkDirtyRepaint(); }
    public static Color Edge(FusionTone tone) => tone == FusionTone.Gold ? new Color(1, .8f, .15f) : tone == FusionTone.Red ? new Color(1, .16f, .3f) : tone == FusionTone.Green ? new Color(.25f, 1, .32f) : new Color(.04f, .8f, 1);
    private void Draw(MeshGenerationContext context)
    {
        Rect r = contentRect; if (r.width < 1 || r.height < 1) return;
        Color edge = Edge(accented ? FusionTone.Gold : tone);
        Color top = tone == FusionTone.Gold ? new Color(1, .95f, .22f) : tone == FusionTone.Red ? new Color(1, .36f, .4f) : tone == FusionTone.Green ? new Color(.1f, .61f, .2f) : new Color(.025f, .28f, .82f);
        Color bottom = tone == FusionTone.Gold ? new Color(1, .56f, .015f) : tone == FusionTone.Red ? new Color(.77f, .015f, .1f) : tone == FusionTone.Green ? new Color(.01f, .16f, .1f) : new Color(.015f, .065f, .28f);
        if (subdued)
        {
            top = tone == FusionTone.Gold ? new Color(.23f,.105f,.015f) : tone == FusionTone.Red ? new Color(.2f,.02f,.055f) : tone == FusionTone.Green ? new Color(.01f,.19f,.07f) : new Color(.015f,.12f,.32f);
            bottom = tone == FusionTone.Gold ? new Color(.07f,.025f,.01f) : tone == FusionTone.Red ? new Color(.035f,.005f,.02f) : tone == FusionTone.Green ? new Color(.005f,.03f,.018f) : new Color(.005f,.02f,.09f);
        }
        float radius = Mathf.Min(24, r.height * .35f);
        FusionMesh.Rounded(context, r, radius + 3, new Color(edge.r, edge.g, edge.b, .10f), new Color(edge.r, edge.g, edge.b, .18f));
        r = Inset(r, 2); FusionMesh.Rounded(context, r, radius + 1, new Color(edge.r, edge.g, edge.b, .35f), new Color(edge.r * .3f, edge.g * .3f, edge.b * .7f, .8f));
        r = Inset(r, 2); FusionMesh.Rounded(context, r, radius, Color.Lerp(edge, Color.white, .4f), edge * new Color(.7f, .7f, 1, 1));
        r = Inset(r, 1.5f); FusionMesh.Rounded(context, r, radius - 1, top, bottom);
        FusionMesh.Ring(context, Inset(r, 2), radius - 3, .8f, new Color(1, 1, 1, .3f), new Color(edge.r, edge.g, edge.b, .1f));
    }
    internal static Rect Inset(Rect r, float v) => new Rect(r.x + v, r.y + v, Mathf.Max(0, r.width - 2*v), Mathf.Max(0, r.height - 2*v));
}

internal static class FusionMesh
{
    private const int Steps = 16, Count = 4 * (Steps + 1);
    private static Vector2 Point(Rect r, float radius, int i)
    {
        radius = Mathf.Min(radius, Mathf.Min(r.width, r.height) * .5f);
        int corner = i / (Steps + 1); float angle = (-90 + corner * 90 + (i % (Steps + 1)) * 90f / Steps) * Mathf.Deg2Rad;
        Vector2 center = new Vector2(corner < 2 ? r.xMax - radius : r.xMin + radius, corner == 0 || corner == 3 ? r.yMin + radius : r.yMax - radius);
        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }
    private static Vertex Vert(Vector2 p, Rect r, Color top, Color bottom) => new Vertex { position = new Vector3(p.x, p.y, Vertex.nearZ), tint = Color.Lerp(top, bottom, Mathf.InverseLerp(r.yMin, r.yMax, p.y)), uv = Vector2.zero };
    public static void Rounded(MeshGenerationContext context, Rect r, float radius, Color top, Color bottom)
    {
        if (r.width <= 0 || r.height <= 0) return;
        var mesh = context.Allocate(Count + 1, Count * 3);
        mesh.SetNextVertex(Vert(r.center, r, top, bottom));
        for (int i = 0; i < Count; i++) mesh.SetNextVertex(Vert(Point(r, radius, i), r, top, bottom));
        for (int i = 0; i < Count; i++) { mesh.SetNextIndex(0); mesh.SetNextIndex((ushort)(i + 1)); mesh.SetNextIndex((ushort)((i + 1) % Count + 1)); }
    }
    public static void Ring(MeshGenerationContext context, Rect r, float radius, float width, Color top, Color bottom)
    {
        if (r.width <= width * 2 || r.height <= width * 2) return;
        var mesh = context.Allocate(Count * 2, Count * 6); Rect inner = FusionSurface.Inset(r, width);
        for (int i = 0; i < Count; i++) { mesh.SetNextVertex(Vert(Point(r, radius, i), r, top, bottom)); mesh.SetNextVertex(Vert(Point(inner, Mathf.Max(0, radius - width), i), r, top, bottom)); }
        for (int i = 0; i < Count; i++) { ushort a=(ushort)(i*2), b=(ushort)(((i+1)%Count)*2); mesh.SetNextIndex(a); mesh.SetNextIndex(b); mesh.SetNextIndex((ushort)(a+1)); mesh.SetNextIndex(b); mesh.SetNextIndex((ushort)(b+1)); mesh.SetNextIndex((ushort)(a+1)); }
    }
}

internal sealed class FusionIcon : VisualElement
{
    private readonly string symbol;
    private readonly Color ink;
    public FusionIcon(string symbol, Color? ink = null)
    {
        this.symbol = symbol; this.ink = ink ?? Color.white;
        pickingMode = PickingMode.Ignore; AddToClassList("icon"); generateVisualContent += Draw;
    }
    private void Draw(MeshGenerationContext context)
    {
        var p = context.painter2D; p.strokeColor = p.fillColor = ink; p.lineWidth = Mathf.Max(1.8f, contentRect.width * .09f); p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round;
        float s = Mathf.Min(contentRect.width, contentRect.height);
        Vector2 V(float x,float y) => new Vector2((contentRect.width-s)*.5f+x*s, (contentRect.height-s)*.5f+y*s);
        void Path(bool fill, params Vector2[] points) { p.BeginPath(); p.MoveTo(points[0]); for(int i=1;i<points.Length;i++) p.LineTo(points[i]); if(fill){p.ClosePath();p.Fill();}else p.Stroke(); }
        void Line(float x,float y,float xx,float yy) => Path(false,V(x,y),V(xx,yy));
        void Circle(float x,float y,float radius,bool fill) { p.BeginPath(); p.Arc(V(x,y),radius*s,0,360); if(fill)p.Fill();else p.Stroke(); }
        switch(symbol)
        {
            case "play": Path(true,V(.28f,.12f),V(.88f,.5f),V(.28f,.88f)); break;
            case "pause": Line(.32f,.2f,.32f,.8f); Line(.68f,.2f,.68f,.8f); break;
            case "back": Path(false,V(.65f,.15f),V(.3f,.5f),V(.65f,.85f)); break;
            case "next": Path(false,V(.35f,.15f),V(.7f,.5f),V(.35f,.85f)); break;
            case "check": Path(false,V(.18f,.5f),V(.4f,.72f),V(.83f,.24f)); break;
            case "crack": p.strokeColor = new Color(.025f,.01f,.055f); Path(false,V(.55f,.05f),V(.44f,.3f),V(.61f,.46f),V(.4f,.65f),V(.5f,.94f)); break;
            case "home": Path(true,V(.08f,.45f),V(.5f,.08f),V(.92f,.45f),V(.8f,.45f),V(.8f,.9f),V(.6f,.9f),V(.6f,.62f),V(.4f,.62f),V(.4f,.9f),V(.2f,.9f),V(.2f,.45f)); break;
            case "settings":
                p.BeginPath(); for(int i=0;i<32;i++){float a=i*Mathf.PI/16; float r=i%4<2?.45f:.34f; var v=V(.5f+Mathf.Cos(a)*r,.5f+Mathf.Sin(a)*r);if(i==0)p.MoveTo(v);else p.LineTo(v);}p.ClosePath();p.Fill();p.fillColor=new Color(.015f,.1f,.36f);Circle(.5f,.5f,.17f,true); break;
            case "exit": Path(false,V(.46f,.12f),V(.17f,.12f),V(.17f,.88f),V(.46f,.88f)); Line(.42f,.5f,.87f,.5f); Path(false,V(.7f,.3f),V(.9f,.5f),V(.7f,.7f)); break;
            case "restart": case "undo":
                p.BeginPath(); p.Arc(V(.5f,.53f),s*.31f, symbol=="undo"?210:225, symbol=="undo"?490:540); p.Stroke(); Path(true,V(.12f,.15f),V(.15f,.51f),V(.48f,.25f)); break;
            case "ad": Path(false,V(.14f,.16f),V(.86f,.16f),V(.86f,.84f),V(.14f,.84f),V(.14f,.16f)); Path(true,V(.39f,.32f),V(.7f,.5f),V(.39f,.68f)); for(int i=0;i<3;i++){Line(.1f,.28f+i*.22f,.2f,.28f+i*.22f);Line(.8f,.28f+i*.22f,.9f,.28f+i*.22f);} break;
            case "sound": Path(true,V(.12f,.36f),V(.3f,.36f),V(.57f,.14f),V(.57f,.86f),V(.3f,.64f),V(.12f,.64f)); p.BeginPath();p.Arc(V(.5f,.5f),s*.37f,-55,55);p.Stroke();break;
            case "music": Line(.38f,.67f,.38f,.2f);Line(.38f,.2f,.82f,.12f);Line(.82f,.12f,.82f,.58f);Circle(.25f,.73f,.13f,true);Circle(.7f,.65f,.13f,true);break;
            case "language": Circle(.5f,.5f,.4f,false);Line(.1f,.5f,.9f,.5f);Line(.5f,.1f,.5f,.9f);Path(false,V(.35f,.16f),V(.25f,.5f),V(.35f,.84f));Path(false,V(.65f,.16f),V(.75f,.5f),V(.65f,.84f));break;
            case "pieces": Circle(.38f,.42f,.3f,false);Circle(.63f,.65f,.26f,false); break;
            case "star":
                p.BeginPath();for(int i=0;i<10;i++){float a=(-90+i*36)*Mathf.Deg2Rad,r=i%2==0?.46f:.22f;var v=V(.5f+Mathf.Cos(a)*r,.5f+Mathf.Sin(a)*r);if(i==0)p.MoveTo(v);else p.LineTo(v);}p.ClosePath();p.Fill();break;
            default: Circle(.5f,.5f,.4f,false);Circle(.5f,.29f,.035f,true);Line(.5f,.47f,.5f,.73f);break;
        }
    }
}

internal sealed class FusionBoardFrame : VisualElement
{
    public FusionBoardFrame() { pickingMode=PickingMode.Ignore; AddToClassList("board-frame"); generateVisualContent += Draw; }
    private void Draw(MeshGenerationContext context)
    {
        Rect r = contentRect; Color cyan=new Color(.02f,.8f,1);
        FusionMesh.Ring(context,r,19,5,new Color(0,.4f,1,.28f),new Color(.1f,.1f,1,.55f));
        FusionMesh.Ring(context,FusionSurface.Inset(r,3),16,2,Color.white,cyan);
        FusionMesh.Ring(context,FusionSurface.Inset(r,5),14,2,cyan,new Color(.02f,.15f,.9f));
    }
}

internal sealed class FusionResultBurst : VisualElement
{
    public FusionResultBurst()
    {
        pickingMode = PickingMode.Ignore; AddToClassList("result-burst");
        generateVisualContent += context =>
        {
            Rect r=contentRect; if(r.width<1 || r.height<1)return;
            var mesh=context.Allocate(48,48); Vector2 center=r.center;
            for(int i=0;i<16;i++)
            {
                float a=i*Mathf.PI/8;
                Vector2 tip1=center+new Vector2(Mathf.Cos(a-.065f)*r.width*.5f,Mathf.Sin(a-.065f)*r.height*.5f);
                Vector2 tip2=center+new Vector2(Mathf.Cos(a+.065f)*r.width*.5f,Mathf.Sin(a+.065f)*r.height*.5f);
                mesh.SetNextVertex(new Vertex{position=new Vector3(center.x,center.y,Vertex.nearZ),tint=new Color(1,.72f,.12f,.55f)});
                mesh.SetNextVertex(new Vertex{position=new Vector3(tip1.x,tip1.y,Vertex.nearZ),tint=new Color(1,.5f,.05f,0)});
                mesh.SetNextVertex(new Vertex{position=new Vector3(tip2.x,tip2.y,Vertex.nearZ),tint=new Color(1,.5f,.05f,0)});
                mesh.SetNextIndex((ushort)(i*3));mesh.SetNextIndex((ushort)(i*3+1));mesh.SetNextIndex((ushort)(i*3+2));
            }
        };
    }
}

internal static class FusionArt
{
    private static Sprite cellSprite;
    public static Texture2D Load(string name) => Resources.Load<Texture2D>("Fusion/Art/" + name);
    // A seamless board tile with a transparent socket. The original piece renderer and
    // its falling/bounce/highlight animations remain visible through the socket.
    public static Sprite CellSprite
    {
        get
        {
            if(cellSprite != null) return cellSprite;
            const int n=128; var texture=new Texture2D(n,n,TextureFormat.RGBA32,false) { name="Fusion socket", filterMode=FilterMode.Bilinear, wrapMode=TextureWrapMode.Clamp };
            var pixels=new Color32[n*n];
            for(int y=0;y<n;y++) for(int x=0;x<n;x++)
            {
                float d=new Vector2((x+.5f)/n-.5f,(y+.5f)/n-.5f).magnitude;
                Color c=new Color(.018f,.22f,.92f);
                if(d<.415f) c=new Color(0,0,0,0);
                else if(d<.439f) c=Color.Lerp(new Color(.005f,.02f,.13f),new Color(.05f,.25f,.72f),(d-.415f)/.024f);
                else if(d<.449f) c=new Color(.1f,.7f,1,.85f);
                c.a *= Mathf.Clamp01((d-.411f)*n);
                pixels[y*n+x]=c;
            }
            texture.SetPixels32(pixels);texture.Apply(false,true);
            cellSprite=Sprite.Create(texture,new Rect(0,0,n,n),new Vector2(.5f,.5f),n*.95f,0,SpriteMeshType.FullRect);cellSprite.name="Fusion board tile";return cellSprite;
        }
    }
}
