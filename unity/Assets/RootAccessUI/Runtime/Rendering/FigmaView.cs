using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RootAccess.UI
{
    public sealed class FigmaElement : MonoBehaviour
    {
        public FigmaNode Data;
        public RectTransform Rect => (RectTransform)transform;
        FigmaElement[] descendantCache; TMP_Text labelCache,firstTextCache; FigmaGraphic shapeCache; Button buttonCache; Action clickAction;
        readonly Dictionary<string,FigmaElement> names=new Dictionary<string,FigmaElement>();
        public TMP_Text Label => labelCache!=null?labelCache:(labelCache=GetComponent<TMP_Text>());
        public FigmaGraphic Shape => shapeCache!=null?shapeCache:(shapeCache=GetComponent<FigmaGraphic>());
        public IEnumerable<FigmaElement> Descendants => descendantCache??(descendantCache=GetComponentsInChildren<FigmaElement>(true));
        public FigmaElement Find(string name) {if(names.TryGetValue(name,out var found)&&found!=null)return found;found=Descendants.FirstOrDefault(x=>x.Data.name==name);if(found!=null)names[name]=found;return found;}
        void OnTransformChildrenChanged(){for(var t=transform;t!=null;t=t.parent){var e=t.GetComponent<FigmaElement>();if(e!=null){e.names.Clear();e.firstTextCache=null;e.descendantCache=null;}}}
        public TMP_Text FirstText => firstTextCache!=null?firstTextCache:(firstTextCache=GetComponentInChildren<TMP_Text>(true));
        public void Text(string value) { var t=FirstText;if(t!=null)t.text=value; }
        public Button Click(Action action)
        {
            clickAction=action;if(buttonCache!=null)return buttonCache;
            var graphic=GetComponent<Graphic>();
            if(graphic==null){var img=gameObject.AddComponent<Image>();img.color=Color.clear;graphic=img;}
            graphic.raycastTarget=true;
            var button=GetComponent<Button>();if(button==null)button=gameObject.AddComponent<Button>();button.targetGraphic=graphic;button.transition=Selectable.Transition.None;
            if(!Data.name.EndsWith("-overlay")&&GetComponent<RootAccessPressFeedback>()==null)gameObject.AddComponent<RootAccessPressFeedback>();
            button.onClick.AddListener(()=>clickAction?.Invoke());buttonCache=button;return button;
        }
    }
    public sealed class FigmaView : MonoBehaviour
    {
        public FigmaScreen Source {get;private set;}
        public List<FigmaElement> Elements=new List<FigmaElement>();
        public RectTransform Root {get;private set;}
        public static readonly Dictionary<string,FigmaScreen> Screens=new Dictionary<string,FigmaScreen>();
        public static FigmaScreen Load(string name)
        {
            if(!Screens.TryGetValue(name,out var data)) {var asset=Resources.Load<TextAsset>("RootAccessUI/Screens/"+name); if(asset==null)throw new InvalidOperationException("Missing Figma screen "+name);data=JsonUtility.FromJson<FigmaScreen>(asset.text);Screens[name]=data;}
            return data;
        }
        public void Build(string name,Transform parent,bool enableBackdropBlur=true)
        {
            Source=Load(name);var built=new Dictionary<int,FigmaElement>();
            for(int i=0;i<Source.nodes.Length;i++)
            {
                var n=Source.nodes[i];var e=Create(n,n.parent<0?parent:built[n.parent].transform);built[i]=e;Elements.Add(e);
                if(n.parent<0)Root=e.Rect;
            }
            // Compact HUD: keep REP level and meter, but hide the numeric progress/target.
            // Both the outer meter group and the inner count wrapper are named "rep".
            foreach(var count in Elements.Where(e=>e.Data.name=="rep"&&!e.Descendants.Any(child=>child.Data.name=="rep-level")))
                count.gameObject.SetActive(false);
            var modal=Elements.FirstOrDefault(e=>e.Data.parent==0&&e.Data.effects.Any(f=>f.type=="BACKGROUND_BLUR"));
            if(modal!=null&&enableBackdropBlur)gameObject.AddComponent<FigmaBackdrop>().Initialize(this,parent,modal);
        }
        public static FigmaElement Create(FigmaNode n,Transform parent)
        {
            n=JsonUtility.FromJson<FigmaNode>(JsonUtility.ToJson(n));
            if(n.name=="Tap progress fill"){var container=parent.GetComponent<FigmaElement>();if(container!=null){float radius=Mathf.Max(0,container.Data.radii[0]-container.Data.strokeWidth);bool full=n.w>=container.Data.w-container.Data.strokeWidth*2-.1f;n.radii=new[]{radius,full?radius:0,full?radius:0,radius};}}
            var go=new GameObject(n.name,typeof(RectTransform));go.layer=parent.gameObject.layer;go.transform.SetParent(parent,false);
            var e=go.AddComponent<FigmaElement>();e.Data=n;SetTL(e.Rect,n.x,n.y,n.w,n.h);
            if(Mathf.Abs(n.rotation)>.01f)e.Rect.localRotation=Quaternion.Euler(0,0,n.rotation);
            if(n.opacity<.999f)go.AddComponent<CanvasGroup>().alpha=n.opacity;
            if(n.kind=="TEXT")
            {
                var text=go.AddComponent<TextMeshProUGUI>();text.raycastTarget=false;
                string font=n.font.Replace(" ","")+(n.font=="Share Tech Mono"?"-Regular":n.fontStyle=="Bold"?"-Bold":"");
                text.font=Resources.Load<TMP_FontAsset>("RootAccessUI/Fonts/"+font+" SDF");
                if(text.font==null)throw new InvalidOperationException("Font missing: "+font);
                text.text=n.text;text.fontSize=n.fontSize;text.enableAutoSizing=false;text.richText=false;
                text.color=n.fills!=null&&n.fills.Length>0?FigmaGraphic.RGBA(n.fills[0].color):Color.white;
                text.textWrappingMode=n.text.Contains("\n")||n.h>n.fontSize*1.6f?TextWrappingModes.Normal:TextWrappingModes.NoWrap;
                text.overflowMode=TextOverflowModes.Overflow;text.margin=Vector4.zero;
                text.alignment=n.align=="CENTER"?TextAlignmentOptions.Top:n.align=="RIGHT"?TextAlignmentOptions.TopRight:TextAlignmentOptions.TopLeft;
                text.characterSpacing=n.letterUnit=="PERCENT"?n.letterSpacing:n.letterSpacing*100/Mathf.Max(1,n.fontSize);
                if(n.lineHeight>0)text.lineSpacing=(n.lineHeight-n.fontSize)*100/Mathf.Max(1,n.fontSize);
            }
            else if((n.fills?.Length??0)>0||(n.strokes?.Length??0)>0||!string.IsNullOrEmpty(n.asset))go.AddComponent<FigmaGraphic>().Configure(n);
            if(n.clip)go.AddComponent<RectMask2D>();
            return e;
        }
        public FigmaElement Find(string name) => Elements.FirstOrDefault(x=>x!=null&&x.Data.name==name);
        public void Remove(FigmaElement element)
        {if(element==null)return;var children=element.Descendants.ToArray();Elements.RemoveAll(x=>children.Contains(x));element.gameObject.SetActive(false);Destroy(element.gameObject);}
        public IEnumerable<FigmaElement> All(string name) => Elements.Where(x=>x!=null&&x.Data.name==name);
        public FigmaElement CopySubtree(FigmaScreen source,int rootIndex,Transform parent)
        {
            var built=new Dictionary<int,FigmaElement>();FigmaElement result=null;
            for(int i=rootIndex;i<source.nodes.Length;i++)
            {
                var original=source.nodes[i];if(i!=rootIndex&&!built.ContainsKey(original.parent))continue;
                var node=JsonUtility.FromJson<FigmaNode>(JsonUtility.ToJson(original));
                if(i==rootIndex){node.x=0;node.y=0;}
                var e=Create(node,i==rootIndex?parent:built[node.parent].transform);Elements.Add(e);built[i]=e;if(i==rootIndex)result=e;
            }
            return result;
        }
        public void SetupScroll(string name)
        {
            var e=Find(name);if(e==null||e.transform.childCount==0)return;
            var content=e.transform.GetChild(0) as RectTransform;if(content==null)return;
            var sr=e.gameObject.AddComponent<ScrollRect>();sr.viewport=e.Rect;sr.content=content;sr.horizontal=false;sr.vertical=true;sr.movementType=ScrollRect.MovementType.Clamped;sr.scrollSensitivity=90;
            if(e.GetComponent<Graphic>()==null){var hit=e.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;}
            content.anchoredPosition=new Vector2(content.anchoredPosition.x,0);
            var bar=Find("Scroll position / "+name);
            if(bar!=null){var handle=bar.Descendants.FirstOrDefault(x=>x!=bar&&x.Shape!=null);if(handle!=null){var scrollbar=bar.gameObject.AddComponent<Scrollbar>();scrollbar.handleRect=handle.Rect;scrollbar.targetGraphic=handle.Shape;scrollbar.direction=Scrollbar.Direction.BottomToTop;scrollbar.transition=Selectable.Transition.None;handle.Shape.raycastTarget=true;if(bar.Shape!=null)bar.Shape.raycastTarget=true;sr.verticalScrollbar=scrollbar;}}
        }
        public static void SetTL(RectTransform r,float x,float y,float w,float h)
        {r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        public void ApplyViewport(Rect safe)
        {
            float scale=Mathf.Min(safe.width/1080f,safe.height/1920f),height=safe.height/Mathf.Max(.001f,scale),extra=height-1920;
            Root.anchorMin=Root.anchorMax=Vector2.zero;Root.pivot=new Vector2(.5f,0);Root.anchoredPosition=new Vector2(safe.center.x,safe.y);Root.sizeDelta=new Vector2(1080,height);Root.localScale=Vector3.one*scale;
            foreach(var e in Elements)
            {
                if(e==null||e.Data.parent!=0)continue;
                float y=e.Data.y;
                if(e.Data.name=="RA/Environment"){e.Rect.sizeDelta=new Vector2(1080,height);var scene=e.Find("Scene viewport");if(scene!=null)scene.Rect.anchoredPosition=new Vector2(scene.Data.x,-scene.Data.y-extra*.5f);}
                else if(e.Data.name=="Game controls")y+=extra;
                else if(e.Data.name=="scene")y+=extra*.5f;
                else if(e.Data.name.EndsWith("-overlay")){e.Rect.sizeDelta=new Vector2(1080,height);foreach(Transform c in e.transform){var ce=c.GetComponent<FigmaElement>();if(ce!=null)ce.Rect.anchoredPosition=new Vector2(ce.Data.x,-ce.Data.y-extra*.5f);}}
                e.Rect.anchoredPosition=new Vector2(e.Data.x,-y);
            }
            GetComponent<FigmaBackdrop>()?.SyncLayout();
        }
    }
}
