using UnityEngine;
using System.Collections.Generic;

// Native Unity implementation; coordinates follow the approved map artwork.
public class IslandGame:MonoBehaviour {
    Texture2D map,island,dog,cat,white;
    Vector2 player=new Vector2(.625f,.240f),velocity,cam;
    readonly Vector2 npc=new Vector2(.411f,.582f);
    bool village,bag,dialogue;int carrots,apples;float jump=-10,transition=-10;int face=1;
    Rect world;float zoom=3; GUIStyle text,title,button;
    class Fruit{public Vector2 p;public bool apple,gone;}
    class Flight{public Vector2 p,start;public bool apple;public float time;}
    readonly List<Fruit> fruits=new List<Fruit>();readonly List<Flight> flights=new List<Flight>();
    Vector4[] houses={new Vector4(.407f,.235f,.536f,.414f),new Vector4(.572f,.045f,.695f,.21f),new Vector4(.082f,.46f,.276f,.668f),new Vector4(.647f,.591f,.787f,.803f),new Vector4(.722f,.168f,.765f,.238f)};
    Vector4[] paths={new Vector4(.451f,.389f,.519f,.518f),new Vector4(.559f,.203f,.632f,.350f),new Vector4(.165f,.666f,.225f,.795f),new Vector4(.650f,.775f,.745f,.910f)};
    Vector4[] trees={new Vector4(.59f,.075f,.075f,.12f),new Vector4(.79f,.11f,.065f,.12f),new Vector4(.378f,.258f,.06f,.10f),new Vector4(.726f,.554f,.032f,.06f),new Vector4(.797f,.527f,.034f,.06f),new Vector4(.846f,.594f,.035f,.06f)};
    void Awake(){
        if(Camera.main==null){var cameraObject=new GameObject("Main Camera");cameraObject.tag="MainCamera";var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;cameraObject.transform.position=new Vector3(0,0,-10);camera.backgroundColor=new Color(.824f,.925f,.98f);}
        map=Load("Village");island=Load("StartIsland");cat=Load("Cat");dog=CutDog(Load("DogOriginal"));
        white=Texture2D.whiteTexture;
        for(int r=0;r<3;r++)for(int c=0;c<4;c++)fruits.Add(new Fruit{p=new Vector2(.728f+c*.018f-r*.013f,.387f+r*.027f)});
        foreach(var p in new[]{new Vector2(.726f,.554f),new Vector2(.797f,.527f),new Vector2(.846f,.594f)})
            foreach(var d in new[]{new Vector2(-.014f,0),new Vector2(.012f,.006f),new Vector2(0,-.023f)})fruits.Add(new Fruit{p=p+d,apple=true});
        cam=player;
    }
    Texture2D Load(string n){var t=Resources.Load<Texture2D>("Island/"+n);if(t)t.filterMode=FilterMode.Point;return t;}
    Texture2D CutDog(Texture2D source){
        var t=new Texture2D(300,270,TextureFormat.RGBA32,false);t.filterMode=FilterMode.Point;
        var colors=source.GetPixels(355,source.height-695,300,270);
        for(int i=0;i<colors.Length;i++){var c=colors[i];if(c.r>.70f&&c.g>.68f&&c.b<.61f&&Mathf.Abs(c.r-c.g)<.255f)c.a=0;colors[i]=c;}
        t.SetPixels(colors);t.Apply();return t;
    }
    bool In(Vector2 p,Vector4 b){return p.x>b.x&&p.y>b.y&&p.x<b.z&&p.y<b.w;}
    bool Allowed(Vector2 p){
        if(p.x<.012f||p.x>.988f||p.y<.02f||p.y>.985f)return false;
        if(In(p,new Vector4(.815f,.345f,.995f,.655f)))return false;
        foreach(var b in new[]{new Vector4(.218f,.220f,.065f,.068f),new Vector4(.447f,.855f,.065f,.077f)}){
            float n=(p.x-b.x)/(b.z*.84f);
            if(Mathf.Abs(n)<1){float edge=b.y+.037f-.027f*Mathf.Sqrt(1-n*n);if(p.y>=edge&&p.y<=edge+.025f)return false;}
            if(Mathf.Pow((p.x-b.x)/b.z,2)+Mathf.Pow((p.y-b.y)/b.w,2)<=1)return true;
        }
        foreach(var a in paths)if(In(p,a))return true;
        if(In(p,new Vector4(.65f,.345f,.805f,.515f)))return true;
        foreach(var a in houses)if(In(p,a))return false;
        foreach(var a in trees)if(Mathf.Pow((p.x-a.x)/a.z,2)+Mathf.Pow((p.y-a.y)/a.w,2)<1)return false;
        int pass=0;
        for(int yy=-6;yy<=6;yy++)for(int xx=-6;xx<=6;xx++){
            Color c=map.GetPixel(Mathf.Clamp((int)(p.x*map.width)+xx,0,map.width-1),Mathf.Clamp((int)((1-p.y)*map.height)+yy,0,map.height-1));
            if((c.g>.49f&&c.r>.45f&&c.g>c.r*.94f&&c.g>c.b*1.25f)||(c.r>.745f&&c.g>.529f&&c.g<c.r*.94f&&c.b<c.g*.86f))pass++;
        }
        return pass>=50;
    }
    float Distance(Vector2 a,Vector2 b){var d=a-b;d.y*=.563f;return d.magnitude;}
    Fruit Nearby(){Fruit best=null;float min=1;foreach(var f in fruits){float d=Distance(player,f.p);if(!f.gone&&d<(f.apple?.048f:.018f)&&d<min){min=d;best=f;}}return best;}
    void Update(){
        float age=Time.unscaledTime-transition;if(age>=1.42f&&age<2.4f)village=true;
        if(!village)return;
        Vector2 input=new Vector2((Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0),(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0)-(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0));
        if(Input.GetMouseButton(0)&&Input.mousePosition.x>110&&Input.mousePosition.y<Screen.height-100){
            var mouse=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y);
            input=mouse-ToScreen(player);if(input.magnitude<8)input=Vector2.zero;
        }
        if(dialogue)input=Vector2.zero;input=Vector2.ClampMagnitude(input,1);
        velocity=Vector2.Lerp(velocity,input,1-Mathf.Exp(-24*Time.deltaTime));if(Mathf.Abs(velocity.x)>.08f)face=velocity.x>0?1:-1;
        for(int i=0;i<4;i++){var p=player;p.x+=velocity.x*.11f*Time.deltaTime/4;if(Allowed(p))player.x=p.x;p=player;p.y+=velocity.y*.195f*Time.deltaTime/4;if(Allowed(p))player.y=p.y;}
        cam=Vector2.Lerp(cam,player,1-Mathf.Exp(-7*Time.deltaTime));
        if(Input.GetKeyDown(KeyCode.E)){
            if(dialogue){dialogue=false;return;}
            if(Distance(player,npc)<.035f){dialogue=true;return;}
            var f=Nearby();if(f!=null){f.gone=true;if(f.apple)apples++;else carrots++;jump=Time.time;flights.Add(new Flight{start=ToScreen(f.p),apple=f.apple,time=Time.time});}
        }
        flights.RemoveAll(f=>Time.time-f.time>.7f);
    }
    Vector2 ToScreen(Vector2 p){return new Vector2(world.x+p.x*world.width,world.y+p.y*world.height);}
    void Box(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,white);GUI.color=Color.white;}
    void Panel(Rect r){Box(new Rect(r.x-3,r.y-3,r.width+6,r.height+6),new Color(.30f,.38f,.28f));Box(r,new Color(1,.96f,.84f));}
    void Sprite(Texture2D t,Rect r,bool flip=false){if(!t)return;GUI.DrawTextureWithTexCoords(r,t,new Rect(flip?1:0,0,flip?-1:1,1));}
    void FruitIcon(Vector2 p,bool apple,float s=1){
        if(apple){Box(new Rect(p.x-5*s,p.y-7*s,10*s,10*s),new Color(.60f,.21f,.16f));Box(new Rect(p.x-4*s,p.y-6*s,8*s,8*s),new Color(.94f,.35f,.22f));Box(new Rect(p.x-2*s,p.y-5*s,2*s,2*s),new Color(1,.85f,.57f));Box(new Rect(p.x,p.y-10*s,2*s,4*s),new Color(.33f,.43f,.17f));}
        else{Box(new Rect(p.x-4*s,p.y-4*s,8*s,9*s),new Color(.91f,.43f,.1f));Box(new Rect(p.x-2*s,p.y-4*s,4*s,7*s),new Color(1,.66f,.20f));Box(new Rect(p.x-s,p.y+5*s,2*s,3*s),new Color(.77f,.35f,.08f));Box(new Rect(p.x-s,p.y-12*s,2*s,8*s),new Color(.24f,.48f,.20f));Box(new Rect(p.x-5*s,p.y-10*s,10*s,2*s),new Color(.33f,.58f,.23f));}
    }
    void OnGUI(){
        if(map==null||island==null)return;
        if(text==null){var font=Font.CreateDynamicFontFromOSFont(new[]{"PingFang SC","Microsoft YaHei","Noto Sans CJK SC","Arial Unicode MS","Arial"},16);text=new GUIStyle(GUI.skin.label){fontSize=16,wordWrap=true,font=font};text.normal.textColor=new Color(.22f,.32f,.25f);title=new GUIStyle(text){fontSize=42};button=new GUIStyle(GUI.skin.button){fontSize=16,font=font};}
        float sw=Screen.width,sh=Screen.height,t=Time.time;
        if(!village){
            Box(new Rect(0,0,sw,sh),new Color(.824f,.925f,.98f));float iw=Mathf.Min(sw*.55f,sh*.70f*island.width/island.height),ih=iw*island.height/island.width;
            Rect ir=new Rect(sw*.98f-iw,sh*.085f,iw,ih);float z=Mathf.Clamp01((Time.unscaledTime-transition)/1.4f);z=transition<0?0:z;
            var matrix=GUI.matrix;GUIUtility.ScaleAroundPivot(Vector2.one*(1+4*z*z),new Vector2(ir.x+iw*.49f,ir.y+ih*.49f));Sprite(island,ir);Birds(ir);GUI.matrix=matrix;
            if(z==0)for(int i=0;i<180;i++){float p=Mathf.Repeat(i/180f+t*.3f,1);float yy=ir.y+ir.height*.96f+p*(sh-ir.y-ir.height*.96f);Box(new Rect(ir.x+ir.width*.486f+Mathf.Sin(i*73.1f)*ir.width*.035f,yy,2,5),new Color(.65f,.92f,1,1-Mathf.InverseLerp(.7f,1,p)));}
            if(z==0){GUI.Label(new Rect(sw*.07f,sh*.36f,350,65),"小岛奇遇",title);GUI.Label(new Rect(sw*.07f,sh*.47f,350,40),"走一走，遇见属于你的小幸运。",text);if(GUI.Button(new Rect(sw*.07f,sh*.56f,180,48),"开始游戏 →",button))transition=Time.unscaledTime;}
        }else{
            float mw=Mathf.Max(sw,sh*map.width/map.height)*zoom,mh=mw*map.height/map.width;
            world=new Rect(Mathf.Clamp(sw*.5f-cam.x*mw,sw-mw,0),Mathf.Clamp(sh*.5f-cam.y*mh,sh-mh,0),mw,mh);Sprite(map,world);
            Water(world,t);
            foreach(var f in fruits)if(!f.gone)FruitIcon(ToScreen(f.p),f.apple,mw/1670);
            Vector2 cp=ToScreen(npc);float size=mw*.025f;Box(new Rect(cp.x-size*.35f,cp.y-2,size*.7f,4),new Color(.15f,.2f,.15f,.18f));
            GUI.DrawTextureWithTexCoords(new Rect(cp.x-size/2,cp.y-size+Mathf.Sin(t*2)*.6f,size,size),cat,new Rect(1080f/cat.width,1-1080f/cat.height,-860f/cat.width,860f/cat.height));
            var pp=ToScreen(player);float ds=mw*.024f;Box(new Rect(pp.x-ds*.35f,pp.y-2,ds*.7f,4),new Color(.15f,.2f,.15f,.18f));
            float j=Time.time-jump;float lift=j<.7f?Mathf.Abs(Mathf.Sin(j/.7f*Mathf.PI*2))*ds*.4f:0;
            DrawDog(pp,ds,lift,t);
            Birds(world);
            if((Nearby()!=null||Distance(player,npc)<.035f)&&!dialogue){Rect er=new Rect(pp.x+58,pp.y-76,20,22);Panel(er);GUI.Label(er,"E",text);}
            if(GUI.Button(new Rect(20,120,56,56),GUIContent.none,GUIStyle.none))bag=!bag;
            Paw(new Vector2(22,120));
            if(bag){FruitIcon(new Vector2(40,205),false,1.2f);GUI.Label(new Rect(60,190,70,30),"× "+carrots,text);FruitIcon(new Vector2(40,245),true,1.2f);GUI.Label(new Rect(60,230,70,30),"× "+apples,text);}
            foreach(var f in flights){float a=Mathf.Clamp01((t-f.time)/.7f);FruitIcon(Vector2.Lerp(f.start,new Vector2(48,148),a*a),f.apple,Mathf.Lerp(mw/1670,.3f,a));}
            if(dialogue){Rect d=new Rect(Mathf.Clamp(cp.x-230,12,sw-250),Mathf.Max(12,cp.y-size-150),238,125);Panel(d);Box(new Rect(d.x+210,d.y+125,12,5),new Color(1,.96f,.84f));Box(new Rect(d.x+213,d.y+130,6,5),new Color(1,.96f,.84f));GUI.Label(new Rect(d.x+10,d.y+8,218,85),"白猫\n喵～欢迎来到河畔村庄！城堡正在准备生日派对，路上可以采些胡萝卜和苹果哦。",text);if(GUI.Button(new Rect(d.x+100,d.y+92,125,26),"继续散步",button))dialogue=false;}
            Panel(new Rect(25,25,205,40));GUI.Label(new Rect(35,32,190,30),"第二幕 · 河畔村庄",text);
            if(GUI.Button(new Rect(sw-155,25,130,40),"← 返回小岛",button)){village=false;transition=-10;dialogue=false;}
        }
        float aTime=Time.unscaledTime-transition;
        if(transition>=0&&aTime>1.1f&&aTime<2.35f){float a=aTime<1.42f?(aTime-1.1f)/.32f:aTime<1.7f?1:1-(aTime-1.7f)/.65f;Box(new Rect(0,0,sw,sh),new Color(1,1,1,a));}
    }
    void DrawDog(Vector2 p,float size,float lift,float t){
        var old=GUI.matrix;GUIUtility.ScaleAroundPivot(new Vector2(face>0?-1:1,1),p);
        float bob=Mathf.Sin(t*(velocity.magnitude>.05f?18:2.5f))*.5f;
        float top=p.y-size*.9f-lift+bob,u=size/300;
        GUI.DrawTextureWithTexCoords(new Rect(p.x-size/2,top,size,210*u),dog,new Rect(0,60f/270,1,210f/270));
        int[] edges={0,90,165,225,300};
        for(int i=0;i<4;i++){float stride=velocity.magnitude>.05f?Mathf.Sin(t*18+(i%2)*Mathf.PI):0;int a=edges[i],b=edges[i+1];GUI.DrawTextureWithTexCoords(new Rect(p.x-size/2+a*u+stride*12*u,top+210*u-Mathf.Max(0,stride)*9*u,(b-a)*u,60*u),dog,new Rect(a/300f,0,(b-a)/300f,60f/270));}
        GUI.matrix=old;
    }
    void Paw(Vector2 p){Color outline=Color.white,fill=new Color(1,.75f,.60f);foreach(var r in new[]{new Rect(2,7,9,13),new Rect(15,2,10,14),new Rect(30,5,10,14),new Rect(42,16,9,13),new Rect(12,25,29,23)}){Box(new Rect(p.x+r.x-2,p.y+r.y-2,r.width+4,r.height+4),outline);Box(new Rect(p.x+r.x,p.y+r.y,r.width,r.height),fill);}}
    void Water(Rect r,float t){for(int i=0;i<1000;i++){float yy=Mathf.Repeat(i*.618034f+t*(.012f+i%4*.002f),1),xx=Mathf.Repeat(i*.13751f,1);if(xx<.29f&&yy>.38f&&yy<.57f)continue;Color c=map.GetPixelBilinear(xx,1-yy);if(c.r<.61f&&c.g>.65f&&c.b>.80f&&c.b-c.r>.25f)Box(new Rect(r.x+xx*r.width,r.y+yy*r.height,4,1),new Color(.94f,1,1,.45f));}}
    void Birds(Rect r){for(int i=0;i<5;i++){float p=Time.time*(.16f+i*.025f)+i*1.37f;var b=new Vector2(r.x+r.width*(.18f+i*.14f+Mathf.Sin(p)*.07f),r.y+r.height*(.12f+i*.12f+Mathf.Cos(p*2)*.025f));float wing=Mathf.Sin(Time.time*11+i)>0?-3:2;Box(new Rect(b.x,b.y,3,2),new Color(.25f,.36f,.44f));Box(new Rect(b.x-3,b.y+wing,3,2),Color.white);Box(new Rect(b.x+3,b.y+wing,3,2),Color.white);}}
}
