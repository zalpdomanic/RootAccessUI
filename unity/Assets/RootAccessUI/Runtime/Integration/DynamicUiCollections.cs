using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RootAccess.UI.Integration
{
    // Reconciles keyed records into existing Figma templates. No game rules or list-index identities.
    public sealed class DynamicUiCollections
    {
        sealed class Row {public FigmaElement Root;public string Variant;public FigmaElement[] Nodes;public string[] SourceIds;}
        const int VirtualizeAbove=16;
        readonly Dictionary<string,Stack<Row>> pools=new Dictionary<string,Stack<Row>>();
        readonly Dictionary<FigmaElement,string> styled=new Dictionary<FigmaElement,string>();
        readonly Dictionary<string,(FigmaNode node,FigmaNode text)> styles=new Dictionary<string,(FigmaNode,FigmaNode)>();
        UiJob[] lastJobs;UiUpgrade[] lastUpgrades;int jobCount,ringCount,upgradeCount;Vector2 ringOffset,jobOffset,upgradeOffset,ringViewport,jobViewport,upgradeViewport;
        public int LiveJobCount=>jobs.Count;public int LiveRingCount=>rings.Count;public int LiveUpgradeCount=>upgrades.Count;
        public int CreatedRows {get;private set;} public int RecycledRows {get;private set;} public int StyleApplications {get;private set;}
        public bool NeedsViewportRefresh => Changed(ringScroll,ringOffset,ringViewport)||Changed(jobScroll,jobOffset,jobViewport)||Changed(upgradeScroll,upgradeOffset,upgradeViewport);
        static bool Changed(ScrollRect sr,Vector2 offset,Vector2 size)=>sr!=null&&(sr.content.anchoredPosition!=offset||sr.viewport.rect.size!=size);
        void RememberViewport(){if(ringScroll!=null){ringOffset=ringScroll.content.anchoredPosition;ringViewport=ringScroll.viewport.rect.size;}if(jobScroll!=null){jobOffset=jobScroll.content.anchoredPosition;jobViewport=jobScroll.viewport.rect.size;}if(upgradeScroll!=null){upgradeOffset=upgradeScroll.content.anchoredPosition;upgradeViewport=upgradeScroll.viewport.rect.size;}}
        static bool Visible(ScrollRect sr,float start,float length,int count,bool horizontal=false){if(count<=VirtualizeAbove||sr==null)return true;float offset=horizontal?-sr.content.anchoredPosition.x:sr.content.anchoredPosition.y;float size=horizontal?sr.viewport.rect.width:sr.viewport.rect.height;float buffer=horizontal?101.12f:200;return start+length>=offset-buffer&&start<=offset+size+buffer;}
        readonly Dictionary<string,float> heights=new Dictionary<string,float>();
        float JobHeight(UiJob d){string key=d.Locked?"locked":d.Automated?"auto":"manual";if(heights.TryGetValue(key,out var height))return height;var s=FigmaView.Load(d.Locked?"07-jobs-locked":"05-jobs-top");height=s.nodes[Index(s,"Job Card",d.Locked?1:d.Automated?0:1)].h;heights[key]=height;return height;}
        public bool ScrollToJob(string id){if(lastJobs==null||jobScroll==null)return false;float y=0;foreach(var d in lastJobs){if(d.Id==id){jobScroll.StopMovement();jobContent.anchoredPosition=new Vector2(jobContent.anchoredPosition.x,y);ClampScroll(jobScroll);return true;}y+=JobHeight(d)+16;}return false;}
        public bool ScrollToRing(string id){if(lastJobs==null||ringScroll==null)return false;int index=Array.FindIndex(lastJobs,d=>d.Id==id);if(index<0)return false;ringScroll.StopMovement();ringContent.anchoredPosition=new Vector2(-index*101.12f,ringContent.anchoredPosition.y);ClampScroll(ringScroll,true);return true;}
        public bool ScrollToUpgrade(string id){if(lastUpgrades==null||upgradeScroll==null)return false;int index=Array.FindIndex(lastUpgrades,d=>d.Id==id);if(index<0)return false;var s=FigmaView.Load("23-install-equipment");float height=s.nodes[Index(s,"Install Option",1)].h;upgradeScroll.StopMovement();upgradeContent.anchoredPosition=new Vector2(upgradeContent.anchoredPosition.x,index*(height+14));ClampScroll(upgradeScroll);return true;}
        void Drop(Row row){foreach(var node in row.Nodes)styled.Remove(node);view.Remove(row.Root);}
        void Release(Row row,string collection){row.Root.gameObject.SetActive(false);string key=collection+"/"+row.Variant;if(!pools.TryGetValue(key,out var pool))pools[key]=pool=new Stack<Row>();if(pool.Count<8)pool.Push(row);else Drop(row);}
        void Retain(Dictionary<string,Row> rows,HashSet<string> keep,string collection){foreach(var key in rows.Keys.Where(k=>!keep.Contains(k)).ToArray()){Release(rows[key],collection);rows.Remove(key);}}
        void EmptyPool(string collection){foreach(var pair in pools.Where(p=>p.Key.StartsWith(collection+"/")))while(pair.Value.Count>0)Drop(pair.Value.Pop());}

        readonly FigmaView view;
        readonly Dictionary<string,Row> jobs=new Dictionary<string,Row>(),rings=new Dictionary<string,Row>(),upgrades=new Dictionary<string,Row>();
        RectTransform jobContent,ringContent,upgradeContent;ScrollRect jobScroll,ringScroll,upgradeScroll;
        bool jobsReady,ringsReady,upgradesReady;string focused;
        TMP_Text emptyJobs,emptyUpgrades;
        public int JobCount=>jobCount;public int RingCount=>ringCount;public int UpgradeCount=>upgradeCount;
        public ScrollRect RingScroll=>ringScroll;
        public ScrollRect JobScroll=>jobScroll;
        public ScrollRect UpgradeScroll=>upgradeScroll;
        public FigmaElement FindJob(string id)=>jobs.TryGetValue(id,out var row)?row.Root:null;
        public FigmaElement FindRing(string id)=>rings.TryGetValue(id,out var row)?row.Root:null;
        public FigmaElement FindUpgrade(string id)=>upgrades.TryGetValue(id,out var row)?row.Root:null;
        public DynamicUiCollections(FigmaView owner){view=owner;}

        public static void Validate(UiFrame frame)
        {
            if(frame.Jobs!=null)ValidateIds(frame.Jobs.Select(x=>x?.Id),"job");
            if(frame.Upgrades!=null)ValidateIds(frame.Upgrades.Select(x=>x?.Id),"upgrade");
        }
        static void ValidateIds(IEnumerable<string> ids,string type)
        {var found=new HashSet<string>(StringComparer.Ordinal);foreach(var id in ids)if(string.IsNullOrWhiteSpace(id)||!found.Add(id))throw new ArgumentException("Each "+type+" needs a non-empty unique Id: "+id);}
        static int Index(FigmaScreen s,string name,int occurrence=0)=>Enumerable.Range(0,s.nodes.Length).Where(i=>s.nodes[i].name==name).Skip(occurrence).First();
        Row Get(Dictionary<string,Row> rows,string id,string variant,string slug,string template,int occurrence,RectTransform parent,string collection)
        {
            if(rows.TryGetValue(id,out var row)&&row.Variant==variant)return row;
            if(row!=null)Release(row,collection);
            string poolKey=collection+"/"+variant;
            if(pools.TryGetValue(poolKey,out var pool)&&pool.Count>0){row=pool.Pop();RecycledRows++;}
            else {var source=FigmaView.Load(slug);var root=view.CopySubtree(source,Index(source,template,occurrence),parent);var nodes=root.Descendants.ToArray();row=new Row{Root=root,Variant=variant,Nodes=nodes,SourceIds=nodes.Select(n=>n.Data.id).ToArray()};CreatedRows++;}
            for(int i=0;i<row.Nodes.Length;i++)row.Nodes[i].Data.id="dynamic/"+collection+"/"+Uri.EscapeDataString(id)+"/"+row.SourceIds[i];
            rows[id]=row;row.Root.gameObject.SetActive(true);return row;
        }

        RectTransform ClearTemplates(string name)
        {
            var first=view.All(name).FirstOrDefault();if(first==null)return null;var parent=(RectTransform)first.transform.parent;
            foreach(var child in parent.Cast<Transform>().ToArray()){var e=child.GetComponent<FigmaElement>();if(e!=null)view.Remove(e);}return parent;
        }
        ScrollRect Vertical(string name)
        {var viewport=view.Find(name);if(viewport==null)return null;var sr=viewport.GetComponent<ScrollRect>();if(sr==null){view.SetupScroll(name);sr=viewport.GetComponent<ScrollRect>();}return sr;}
        void InitRings()
        {
            ringsReady=true;ringContent=ClearTemplates("Job Ring");if(ringContent==null)return;
            var viewport=view.Find("Job activity");if(viewport.GetComponent<RectMask2D>()==null)viewport.gameObject.AddComponent<RectMask2D>();
            var hit=viewport.GetComponent<Image>();if(hit==null)hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
            ringScroll=viewport.gameObject.AddComponent<ScrollRect>();ringScroll.viewport=viewport.Rect;ringScroll.content=ringContent;ringScroll.horizontal=true;ringScroll.vertical=false;ringScroll.movementType=ScrollRect.MovementType.Clamped;ringScroll.scrollSensitivity=60;
        }
        static void Position(FigmaElement e,float x,float y){e.Rect.anchoredPosition=new Vector2(x,-y);}
        static float Fraction(float value)=>float.IsNaN(value)||float.IsInfinity(value)?0:Mathf.Clamp01(value);
        static void Text(FigmaElement root,string name,string value,float width=0)
        {var e=root.Find(name);if(e==null||e.FirstText==null)return;SetLabel(e.FirstText,value,width);}
        static void SetLabel(TMP_Text label,string value,float width=0)
        {
            label.richText=false;label.text=value??"";label.textWrappingMode=TextWrappingModes.NoWrap;label.overflowMode=TextOverflowModes.Ellipsis;
            // TMP's line metrics exceed some imported CSS text boxes. Keep the top baseline,
            // but provide sufficient vertical room so horizontal ellipsis doesn't hide the line.
            label.rectTransform.sizeDelta=new Vector2(width>0?width:label.rectTransform.sizeDelta.x,Mathf.Max(label.rectTransform.sizeDelta.y,label.fontSize*1.5f));
        }
        static void ClampScroll(ScrollRect sr,bool horizontal=false)
        {
            if(sr==null)return;var p=sr.content.anchoredPosition;
            if(horizontal)p.x=Mathf.Clamp(p.x,-Mathf.Max(0,sr.content.rect.width-sr.viewport.rect.width),0);
            else p.y=Mathf.Clamp(p.y,0,Mathf.Max(0,sr.content.rect.height-sr.viewport.rect.height));
            sr.content.anchoredPosition=p;
        }
        void Style(FigmaElement action,bool enabled,string label)
        {
            string slug=label=="AUTO"?"05-jobs-top":enabled?"06-jobs-bottom":"07-jobs-locked";
            if(styled.TryGetValue(action,out var previous)&&previous==slug)return;
            if(!styles.TryGetValue(slug,out var style)){
                string match=label=="AUTO"?"AUTO":enabled?"RUN":"LOCKED";var s=FigmaView.Load(slug);
                for(int i=0;i<s.nodes.Length;i++)if(s.nodes[i].name=="Action"){
                    var t=s.nodes.Skip(i+1).FirstOrDefault(n=>n.kind=="TEXT");if(t?.text.Trim()!=match)continue;style=(s.nodes[i],t);styles[slug]=style;break;
                }
            }
            if(style.node==null)return;
            action.Data.fills=style.node.fills;action.Data.strokes=style.node.strokes;action.Shape?.Configure(action.Data);
            var group=action.GetComponent<CanvasGroup>();if(group!=null)group.alpha=style.node.opacity;
            if(action.FirstText!=null)action.FirstText.color=FigmaGraphic.RGBA(style.text.fills[0].color);styled[action]=slug;StyleApplications++;
        }
        void Bind(FigmaElement element,UiItemAction command,string entity,Action<UiAction,FigmaElement> bind,bool permitted=true)
        {
            if(element==null)return;bool enabled=permitted&&command!=null&&command.Enabled&&!string.IsNullOrEmpty(command.Id);
            var label=command?.Label??"—";var text=element.FirstText;if(text.transform.parent!=element.transform)text.transform.SetParent(element.transform,false);FigmaView.SetTL(text.rectTransform,12,0,element.Rect.rect.width-24,element.Rect.rect.height);text.alignment=TextAlignmentOptions.Center;SetLabel(text,label);Style(element,enabled,label);
            if(command!=null&&!string.IsNullOrEmpty(command.Id))bind(new UiAction(new UiTarget(element.Data.name){NodeId=element.Data.id},command.Id,entity,command.Quantity,enabled),element);
        }
        TMP_Text Empty(RectTransform parent,TMP_Text existing,bool visible,string message)
        {
            if(existing==null){var go=new GameObject("Empty collection",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);existing=go.GetComponent<TextMeshProUGUI>();existing.font=Resources.Load<TMP_FontAsset>("RootAccessUI/Fonts/PixelOperator SDF");existing.fontSize=36;existing.color=new Color(.643f,.714f,.753f);existing.raycastTarget=false;FigmaView.SetTL((RectTransform)go.transform,24,24,parent.rect.width-48,90);}
            existing.text=message;existing.gameObject.SetActive(visible);return existing;
        }

        public void Sync(UiFrame frame,Action<UiAction,FigmaElement> bind)
        {
            lastJobs=frame.Jobs;lastUpgrades=frame.Upgrades;jobCount=ringCount=frame.Jobs?.Length??0;upgradeCount=frame.Upgrades?.Length??0;
            if(frame.Jobs!=null){if(!ringsReady)InitRings();SyncRings(frame.Jobs,bind);SyncJobs(frame.Jobs,bind);}
            if(frame.Upgrades!=null)SyncUpgrades(frame.Upgrades,bind);
            ClampScroll(ringScroll,true);ClampScroll(jobScroll);ClampScroll(upgradeScroll);
            if(frame.FocusJobId!=focused){focused=frame.FocusJobId;if(focused!=null&&ScrollToJob(focused))SyncJobs(frame.Jobs,bind);}
            RememberViewport();
        }
        void SyncRings(UiJob[] data,Action<UiAction,FigmaElement> bind)
        {
            if(ringContent==null)return;
            ringContent.sizeDelta=new Vector2(Mathf.Max(ringScroll.viewport.rect.width,data.Length*101.12f-11.99f),89.13f);ClampScroll(ringScroll,true);
            var keep=new HashSet<string>();for(int i=0;i<data.Length;i++)if(Visible(ringScroll,i*101.12f,89.13f,data.Length,true))keep.Add(data[i].Id);Retain(rings,keep,"rings");if(data.Length==0)EmptyPool("rings");
            for(int i=0;i<data.Length;i++){
                var d=data[i];if(!keep.Contains(d.Id))continue;var e=Get(rings,d.Id,"ring","01-home","Job Ring",0,ringContent,"rings").Root;Position(e,i*101.12f,0);
                var feedback=e.GetComponent<RootAccessPressFeedback>();var label=e.FirstText;var labelParent=feedback!=null&&feedback.RingVisualRoot!=null?feedback.RingVisualRoot:e.transform;if(label.transform.parent!=labelParent)label.transform.SetParent(labelParent,false);FigmaView.SetTL(label.rectTransform,6,0,77.13f,89.13f);label.alignment=TextAlignmentOptions.Center;SetLabel(label,d.Locked?"?":d.Code);
                var arc=e.Find("Progress");float f=d.Locked?0:Fraction(d.Progress);arc.gameObject.SetActive(f>0);arc.Shape.SetArc(f);
                var caps=e.Descendants.Where(x=>x.Data.name=="Round endpoint").ToArray();for(int k=0;k<caps.Length;k++){caps[k].gameObject.SetActive(f>0);float angle=-Mathf.PI/2+(k==0?0:f*Mathf.PI*2),radius=arc.Data.w*.5f-caps[k].Data.w*.5f;caps[k].Rect.anchoredPosition=new Vector2(arc.Data.x+arc.Data.w*.5f+Mathf.Cos(angle)*radius-caps[k].Data.w*.5f,-(arc.Data.y+arc.Data.h*.5f+Mathf.Sin(angle)*radius-caps[k].Data.h*.5f));}
                // Manual rings share the card's authoritative action, including its enabled state.
                // A disabled/running manual job must not fall through to the drawer button.
                var run=d.Run;
                bind(new UiAction(new UiTarget("Job Ring"){NodeId=e.Data.id},
                    d.Automated?"select-job":run?.Id??"run",d.Id,run?.Quantity??1,
                    !d.Locked&&(d.Automated||(run!=null&&run.Enabled&&!string.IsNullOrEmpty(run.Id)))),e);
            }
            ringContent.sizeDelta=new Vector2(Mathf.Max(ringScroll.viewport.rect.width,data.Length*101.12f-11.99f),89.13f);
        }
        void SyncJobs(UiJob[] data,Action<UiAction,FigmaElement> bind)
        {
            if(!jobsReady){jobsReady=true;jobContent=ClearTemplates("Job Card");if(jobContent!=null)jobScroll=Vertical("job-list");}
            if(jobContent==null)return;float total=0;foreach(var d in data)total+=JobHeight(d)+16;
            jobContent.sizeDelta=new Vector2(jobContent.sizeDelta.x,Mathf.Max(150,total-16));ClampScroll(jobScroll);
            var keep=new HashSet<string>();float offset=0;foreach(var d in data){float h=JobHeight(d);if(Visible(jobScroll,offset,h,data.Length))keep.Add(d.Id);offset+=h+16;}
            Retain(jobs,keep,"jobs");if(data.Length==0)EmptyPool("jobs");float y=0;
            foreach(var d in data){
                if(!keep.Contains(d.Id)){y+=JobHeight(d)+16;continue;}
                string variant=d.Locked?"locked":d.Automated?"auto":"manual";
                var e=Get(jobs,d.Id,variant,d.Locked?"07-jobs-locked":"05-jobs-top","Job Card",d.Locked?1:d.Automated?0:1,jobContent,"jobs").Root;Position(e,0,y);y+=e.Data.h+16;
                var badge=e.Find("job-code-icon");var badgeText=badge.FirstText;FigmaView.SetTL(badgeText.rectTransform,4,0,badge.Rect.rect.width-8,badge.Rect.rect.height);badgeText.alignment=TextAlignmentOptions.Center;SetLabel(badgeText,d.Code);
                Text(e,"menu-eyebrow",d.Code,650);Text(e,"job-name",d.Name,650);Text(e,"state",d.Status,650);
                Text(e,"payout",d.Payout,360);Text(e,"level-price",d.LevelPrice,360);Text(e,"auto-price",d.AutomationPrice,360);
                var actions=e.Descendants.Where(n=>n.Data.name=="Action").ToArray();Bind(actions[0],d.Run,d.Id,bind,!d.Locked&&!d.Automated);
                if(actions.Length==3)Bind(actions[1],d.Automate,d.Id,bind,!d.Locked);
                Bind(actions.Last(),d.LevelUp,d.Id,bind,!d.Locked);
                var track=e.Descendants.First(n=>n.Data.name.EndsWith(" progress"));var fill=track.Descendants.First(n=>n!=track&&n.Shape!=null);fill.Rect.sizeDelta=new Vector2(track.Data.w*Fraction(d.Progress),fill.Data.h);fill.gameObject.SetActive(!d.Locked&&Fraction(d.Progress)>0);
            }
            jobContent.sizeDelta=new Vector2(jobContent.sizeDelta.x,Mathf.Max(150,y-16));emptyJobs=Empty(jobContent,emptyJobs,data.Length==0,"No jobs available");
        }
        void SyncUpgrades(UiUpgrade[] data,Action<UiAction,FigmaElement> bind)
        {
            if(!upgradesReady){upgradesReady=true;upgradeContent=ClearTemplates("Install Option");if(upgradeContent!=null)upgradeScroll=Vertical("install-list");}
            if(upgradeContent==null)return;var template=FigmaView.Load("23-install-equipment");float height=template.nodes[Index(template,"Install Option",1)].h;
            upgradeContent.sizeDelta=new Vector2(upgradeContent.sizeDelta.x,Mathf.Max(150,data.Length*(height+14)-14));ClampScroll(upgradeScroll);
            var keep=new HashSet<string>();for(int i=0;i<data.Length;i++)if(Visible(upgradeScroll,i*(height+14),height,data.Length))keep.Add(data[i].Id);Retain(upgrades,keep,"upgrades");if(data.Length==0)EmptyPool("upgrades");float y=0;
            foreach(var d in data){
                if(!keep.Contains(d.Id)){y+=height+14;continue;}
                var e=Get(upgrades,d.Id,"upgrade","23-install-equipment","Install Option",1,upgradeContent,"upgrades").Root;Position(e,0,y);y+=e.Data.h+14;
                SetLabel(e.FirstText,d.Name,620);Text(e,"SMALL",d.Detail,620);Text(e,"install-price",d.Price,620);
                var a=e.Descendants.First(n=>n!=e&&n.Data.name.StartsWith("Install "));Bind(a,d.Purchase,d.Id,bind);
            }
            upgradeContent.sizeDelta=new Vector2(upgradeContent.sizeDelta.x,Mathf.Max(150,y-14));emptyUpgrades=Empty(upgradeContent,emptyUpgrades,data.Length==0,"No upgrades available");
        }
    }
}
