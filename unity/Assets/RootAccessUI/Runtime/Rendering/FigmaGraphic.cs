using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace RootAccess.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FigmaGraphic : MaskableGraphic
    {
        public FigmaNode Node {get;private set;}
        // Materials contain immutable appearance only. Geometry and motion are per-vertex.
        [Serializable] sealed class Recipe {public string kind,asset,mask;public float strokeWidth;public FigmaPaint[] fills,strokes;public FigmaEffect[] effects;public int colorSpace;}
        sealed class Entry {public Material Material;public int Users;}
        static readonly Dictionary<string,Entry> shared=new Dictionary<string,Entry>();
        Material owned;string materialKey;Texture texture;float padding,pressAmount,pressDepth;Vector4 arc,radii;
        public float PressBrightness=>1+.13f*pressAmount;
        public float ShadowLift=>pressDepth*pressAmount;
        public Vector4 Arc=>arc;
        public static int SharedMaterialCount=>shared.Count;
        public static int MaterialCreations {get;private set;}
        public static int ConfigurationChanges {get;private set;}
        FigmaPaint[] lastFills,lastStrokes;FigmaEffect[] lastEffects;string lastAsset,lastMask,lastKind;float lastStroke;int lastColorSpace;
        void Channels(){const AdditionalCanvasShaderChannels required=AdditionalCanvasShaderChannels.TexCoord1|AdditionalCanvasShaderChannels.TexCoord2|AdditionalCanvasShaderChannels.TexCoord3;if(canvas!=null&&(canvas.additionalShaderChannels&required)!=required)canvas.additionalShaderChannels|=required;}
        protected override void OnCanvasHierarchyChanged(){base.OnCanvasHierarchyChanged();Channels();}
        void ReleaseMaterial(){if(materialKey==null)return;if(shared.TryGetValue(materialKey,out var entry)&&--entry.Users==0){shared.Remove(materialKey);Destroy(entry.Material);}materialKey=null;owned=null;}

        protected override void Awake(){base.Awake();raycastTarget=false;}
        public override Texture mainTexture => texture != null ? texture : Texture2D.whiteTexture;
        public static Color RGBA(float[] a) => a != null && a.Length>=4 ? new Color(a[0],a[1],a[2],a[3]) : Color.clear;
        static Color ShaderColor(Color c)=>QualitySettings.activeColorSpace==ColorSpace.Linear?c.linear:c;
        public void Configure(FigmaNode node)
        {
            // Painting must never change the input ownership established by FigmaElement.Click.
            Node=node;
            Channels();color=Color.white;
            var r=node.radii??new float[4];SetRadii(new Vector4(r[0],r[1],r[2],r[3]));
            bool first=owned==null;var a=node.arc;if(first)arc=a!=null?new Vector4(a.startingAngle,a.endingAngle,a.innerRadius,0):new Vector4(0,Mathf.PI*2,0,0);
            int space=(int)QualitySettings.activeColorSpace;
            if(!first&&ReferenceEquals(lastFills,node.fills)&&ReferenceEquals(lastStrokes,node.strokes)&&ReferenceEquals(lastEffects,node.effects)&&lastAsset==node.asset&&lastMask==node.mask&&lastKind==node.kind&&lastStroke==node.strokeWidth&&lastColorSpace==space)return;
            lastFills=node.fills;lastStrokes=node.strokes;lastEffects=node.effects;lastAsset=node.asset;lastMask=node.mask;lastKind=node.kind;lastStroke=node.strokeWidth;lastColorSpace=space;
            var recipe=new Recipe{kind=node.kind=="ELLIPSE"?"ELLIPSE":"RECTANGLE",asset=node.asset,mask=node.mask,strokeWidth=node.strokeWidth,fills=node.fills,strokes=node.strokes,effects=node.effects,colorSpace=space};
            string key=JsonUtility.ToJson(recipe);if(key==materialKey)return;
            ReleaseMaterial();materialKey=key;
            texture=string.IsNullOrEmpty(node.asset)?null:Resources.Load<Texture2D>(node.asset);
            padding=0;if(node.effects!=null)foreach(var effect in node.effects)if(effect.type=="DROP_SHADOW")padding=Mathf.Max(padding,effect.radius+Mathf.Abs(effect.offsetX)+Mathf.Abs(effect.offsetY)+effect.spread+2);
            if(shared.TryGetValue(key,out var cached)){cached.Users++;owned=cached.Material;material=owned;ConfigurationChanges++;SetAllDirty();return;}
            owned=new Material(Resources.Load<Shader>("RootAccessUI/FigmaShape")){name="Root Access shared UI style"};shared[key]=new Entry{Material=owned,Users=1};material=owned;MaterialCreations++;ConfigurationChanges++;
            owned.SetFloat("_Kind",node.kind=="ELLIPSE"?1:0);
            owned.SetFloat("_StrokeWidth",node.strokes!=null&&node.strokes.Length>0?node.strokeWidth:0);
            owned.SetColor("_Stroke",node.strokes!=null&&node.strokes.Length>0?PaintColor(node.strokes[0]):Color.clear);
            WritePaint(node.fills!=null&&node.fills.Length>0?node.fills[0]:null);
            texture=string.IsNullOrEmpty(node.asset)?null:Resources.Load<Texture2D>(node.asset);
            owned.SetFloat("_Asset",texture!=null?1:0);
            var mask=string.IsNullOrEmpty(node.mask)?null:Resources.Load<Texture2D>(node.mask);
            owned.SetTexture("_MaskTex",mask);owned.SetFloat("_HasMask",mask!=null?1:0);
            Vector4[] geo=new Vector4[4],colors=new Vector4[4];Vector4 types=Vector4.zero;int count=0;padding=0;
            if(node.effects!=null)foreach(var e in node.effects)
            {
                if(count==4)break;if(e.type!="DROP_SHADOW"&&e.type!="INNER_SHADOW")continue;
                geo[count]=new Vector4(e.offsetX,e.offsetY,e.radius,e.spread);colors[count]=ShaderColor(RGBA(e.color));types[count]=e.type=="INNER_SHADOW"?2:1;
                if(e.type=="DROP_SHADOW")padding=Mathf.Max(padding,e.radius+Mathf.Abs(e.offsetX)+Mathf.Abs(e.offsetY)+e.spread+2);count++;
            }
            owned.SetVectorArray("_EffectGeometry",geo);owned.SetVectorArray("_EffectColors",colors);owned.SetVector("_EffectType",types);owned.SetFloat("_EffectCount",count);SetAllDirty();
        }
        public void SetPaint(FigmaPaint p){Node.fills=p==null?Array.Empty<FigmaPaint>():new[]{p};Configure(Node);}
        void WritePaint(FigmaPaint p)
        {
            owned.SetFloat("_PaintType",p==null?0:p.type=="GRADIENT_LINEAR"?2:p.type=="GRADIENT_RADIAL"?3:1);
            owned.SetColor("_Solid",p==null?Color.clear:PaintColor(p));
            var matrix=p?.matrix??new float[]{1,0,0,0,1,0};owned.SetVector("_G0",new Vector4(matrix[0],matrix[1],matrix[2],0));owned.SetVector("_G1",new Vector4(matrix[3],matrix[4],matrix[5],0));
            var colors=new Vector4[16];var positions=new Vector4[16];int count=Mathf.Min(16,p?.stops?.Length??0);
            for(int i=0;i<count;i++){var c=ShaderColor(RGBA(p.stops[i].color));c.a*=p.opacity;colors[i]=c;positions[i].x=p.stops[i].position;}
            owned.SetVectorArray("_Stops",colors);owned.SetVectorArray("_StopPositions",positions);owned.SetFloat("_StopCount",count);
        }
        static Color PaintColor(FigmaPaint p){var c=ShaderColor(RGBA(p.color));c.a*=p.opacity;return c;}
        public void SetArc(float fraction)
        {var next=new Vector4(-Mathf.PI/2,-Mathf.PI/2+Mathf.PI*2*Mathf.Clamp01(fraction),Node.arc?.innerRadius??.85f,0);if(arc==next)return;arc=next;SetVerticesDirty();}
        public void SetRadii(Vector4 value){if(radii==value)return;radii=value;SetVerticesDirty();}
        public void SetPressVisual(float amount,float depth)
        {if(pressAmount==amount&&pressDepth==depth)return;pressAmount=amount;pressDepth=depth;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;float w=Mathf.Max(.01f,r.width),h=Mathf.Max(.01f,r.height);
            AddVertex(vh,new Vector3(r.xMin-padding,r.yMin-padding),new Vector2(-padding/w,-padding/h),w,h);
            AddVertex(vh,new Vector3(r.xMin-padding,r.yMax+padding),new Vector2(-padding/w,1+padding/h),w,h);
            AddVertex(vh,new Vector3(r.xMax+padding,r.yMax+padding),new Vector2(1+padding/w,1+padding/h),w,h);
            AddVertex(vh,new Vector3(r.xMax+padding,r.yMin-padding),new Vector2(1+padding/w,-padding/h),w,h);
            vh.AddTriangle(0,1,2);vh.AddTriangle(2,3,0);
        }
        void AddVertex(VertexHelper vh,Vector3 position,Vector2 uv,float width,float height)
        {var v=UIVertex.simpleVert;v.position=position;v.color=color;v.uv0=uv;v.uv1=new Vector4(width,height,PressBrightness,ShadowLift);v.uv2=arc;v.uv3=radii;vh.AddVert(v);}
        protected override void OnDestroy(){ReleaseMaterial();base.OnDestroy();}
    }
}
