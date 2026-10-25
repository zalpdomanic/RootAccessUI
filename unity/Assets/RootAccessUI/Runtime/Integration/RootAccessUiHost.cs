using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RootAccess.UI.Integration
{
    // Owns presentation only. The adapter owns routes, data and all transaction decisions.
    [DefaultExecutionOrder(100)]
    public sealed class RootAccessUiHost:MonoBehaviour
    {
        public bool ShowReferenceEnvironment;
        public int SortingOrder=60;
        public float CommandTimeoutSeconds=15;
        public bool IsBusy {get;private set;}
        public FigmaView View {get;private set;}
        public DynamicUiCollections Collections {get;private set;}
        public event Action<UiCommand,UiCommandResult> CommandFinished;
        public event Action<string> BindingFailed;
        public event Action<bool> BusyChanged;
        IRootAccessUiAdapter adapter;Canvas canvas;bool ownsCanvas;GameObject screenOwner,viewOwner;
        UiFrame frame;string screen,layout;volatile bool dirty;
        CancellationTokenSource lifetime;int generation;Rect safe;Vector2Int size;
        readonly Dictionary<UiAction,Button> actions=new Dictionary<UiAction,Button>();
        readonly Dictionary<FigmaElement,float> widths=new Dictionary<FigmaElement,float>();
        sealed class Binding {public UiAction Action;public Button Button;public int Seen;}
        readonly Dictionary<FigmaElement,Binding> bindings=new Dictionary<FigmaElement,Binding>();
        readonly Dictionary<string,FigmaElement> targets=new Dictionary<string,FigmaElement>();
        readonly List<FigmaElement> staleBindings=new List<FigmaElement>();int bindingPass;
        public int ListenerInstallCount {get;private set;}

        public void Mount(IRootAccessUiAdapter source,Canvas existingCanvas=null)
        {
            if(source==null)throw new ArgumentNullException(nameof(source));
            if(existingCanvas!=null&&(existingCanvas.renderMode!=RenderMode.ScreenSpaceOverlay||Mathf.Abs(existingCanvas.scaleFactor-1)>0.001f))
                throw new ArgumentException("This starter requires an overlay Canvas with scaleFactor 1; otherwise let it own an overlay Canvas.");
            Unmount();adapter=source;lifetime=new CancellationTokenSource();adapter.Changed+=Changed;
            canvas=existingCanvas;ownsCanvas=canvas==null;
            if(ownsCanvas){var c=new GameObject("Root Access integration canvas",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));c.transform.SetParent(transform,false);canvas=c.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=SortingOrder;}
            else if(canvas.GetComponent<GraphicRaycaster>()==null)throw new ArgumentException("The supplied Canvas needs a GraphicRaycaster.");
            if(EventSystem.current==null)Debug.LogWarning("Root Access UI requires the host game's EventSystem and input module.",this);
            dirty=true;Render();
        }
        void Changed(){dirty=true;} // Safe to raise from a worker thread; Unity work stays in Update.
        void Update()
        {
            if(adapter==null)return;
            if(dirty)try{Render();}catch(Exception e){dirty=false;BindingFailed?.Invoke(e.Message);Debug.LogException(e,this);}
            if(View!=null&&(safe!=Screen.safeArea||size!=new Vector2Int(Screen.width,Screen.height)))Layout();
        }
        void LateUpdate()
        {
            // ScrollRect updates before this host. Reconcile the visible range before rendering.
            if(adapter!=null&&Collections!=null&&Collections.NeedsViewportRefresh)
                try{Render(false);}catch(Exception e){BindingFailed?.Invoke(e.Message);Debug.LogException(e,this);}
        }
        void Layout(){safe=Screen.safeArea;size=new Vector2Int(Screen.width,Screen.height);View.ApplyViewport(safe);}
        public FigmaElement Resolve(UiTarget target)
        {
            if(target==null)throw new ArgumentException("Missing UI target.");
            string key=target.NodeId+"|"+target.Scope+"|"+target.ScopeOccurrence+"|"+target.Name+"|"+target.Occurrence;
            if(targets.TryGetValue(key,out var cached)&&cached!=null&&(string.IsNullOrEmpty(target.NodeId)||cached.Data.id==target.NodeId))return cached;
            var resolved=ResolveUncached(target);targets[key]=resolved;return resolved;
        }
        FigmaElement ResolveUncached(UiTarget target)
        {
            IEnumerable<FigmaElement> nodes=View.Elements;
            if(!string.IsNullOrEmpty(target.NodeId))return nodes.FirstOrDefault(e=>e.Data.id==target.NodeId)??throw new ArgumentException("Unknown node ID: "+target.NodeId);
            if(!string.IsNullOrEmpty(target.Scope)){
                var scope=nodes.Where(e=>e.Data.name==target.Scope).Skip(target.ScopeOccurrence).FirstOrDefault();
                if(scope==null)throw new ArgumentException("Unknown scope: "+target.Scope);nodes=scope.Descendants;
            }
            return nodes.Where(e=>e.Data.name==target.Name).Skip(target.Occurrence).FirstOrDefault()??throw new ArgumentException("Unknown UI binding: "+target.Name);
        }
        void ClearScreen()
        {
            actions.Clear();widths.Clear();bindings.Clear();targets.Clear();
            if(screenOwner!=null){screenOwner.SetActive(false);Destroy(screenOwner);}if(viewOwner!=null){viewOwner.SetActive(false);Destroy(viewOwner);}screenOwner=null;viewOwner=null;View=null;Collections=null;screen=null;layout=null;
        }
        void Render(bool capture=true)
        {
            dirty=false;var next=(capture?adapter.Capture():frame)??throw new InvalidOperationException("Adapter returned no UI frame.");
            if(!FigmaView.Load(next.Screen).nodes.Any())throw new ArgumentException("Empty screen.");
            DynamicUiCollections.Validate(next);
            bool rebuild=View==null||screen!=next.Screen||layout!=next.LayoutKey||(frame?.Jobs!=null)!=(next.Jobs!=null)||(frame?.Upgrades!=null)!=(next.Upgrades!=null);
            frame=next;
            if(rebuild){
                ClearScreen();screenOwner=new GameObject("Integration screen",typeof(RectTransform));screenOwner.transform.SetParent(canvas.transform,false);
                var r=(RectTransform)screenOwner.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
                viewOwner=new GameObject("Integration view binding");viewOwner.transform.SetParent(transform,false);View=viewOwner.AddComponent<FigmaView>();
                if(ShowReferenceEnvironment){var bg=screenOwner.AddComponent<Image>();bg.color=new Color(0,.07f,.114f);bg.raycastTarget=false;}
                // External scenes retain their own cameras/layers. Their blur is a future pipeline integration.
                View.Build(next.Screen,screenOwner.transform,ShowReferenceEnvironment);
                if(!ShowReferenceEnvironment){var root=View.Root.GetComponent<FigmaGraphic>();if(root!=null)root.enabled=false;foreach(var n in View.Elements.Where(e=>e.Data.parent==0&&(e.Data.name=="RA/Environment"||e.Data.name=="scene")))n.gameObject.SetActive(false);}
                var modal=View.Elements.FirstOrDefault(e=>e.Data.parent==0&&e.Data.name.EndsWith("-overlay"));
                if(modal!=null){foreach(var n in View.Elements.Where(e=>e.Data.parent==0&&e!=modal)){var g=n.GetComponent<CanvasGroup>();if(g==null)g=n.gameObject.AddComponent<CanvasGroup>();g.interactable=false;g.blocksRaycasts=false;}modal.Click(()=>{});}
                if(next.EnableScrolling)foreach(var name in new[]{"job-list","rack-list","install-list"})if(View.Find(name)!=null)View.SetupScroll(name);
                screen=next.Screen;layout=next.LayoutKey;Collections=new DynamicUiCollections(View);Layout();
            }
            // Keep one listener per live control; update its authoritative command in place.
            bindingPass++;actions.Clear();
            foreach(var text in next.Text??Array.Empty<UiText>()){var e=Resolve(text.Target);if(e.FirstText==null)throw new ArgumentException("Text target has no label: "+text.Target.Name);e.Text(text.Value??"");}
            foreach(var fill in next.Fills??Array.Empty<UiFill>()){
                var e=Resolve(fill.Target);if(!widths.TryGetValue(e,out float w)){w=e.Data.w;widths[e]=w;}
                e.Rect.sizeDelta=new Vector2(w*Mathf.Clamp01(fill.Fraction),e.Rect.sizeDelta.y);e.gameObject.SetActive(fill.Fraction>0);
            }
            foreach(var action in next.Actions??Array.Empty<UiAction>()){
                if(string.IsNullOrEmpty(action.Id))throw new ArgumentException("Missing command ID.");
                RegisterAction(action,Resolve(action.Target));
            }
            Collections.Sync(next,RegisterAction);
            staleBindings.Clear();foreach(var pair in bindings)if(pair.Key==null||pair.Value.Seen!=bindingPass){if(pair.Value.Button!=null)pair.Value.Button.interactable=false;staleBindings.Add(pair.Key);}
            foreach(var key in staleBindings)bindings.Remove(key);
        }
        void RegisterAction(UiAction action,FigmaElement element)
        {
            if(!bindings.TryGetValue(element,out var binding)){
                binding=new Binding();var captured=binding;binding.Button=element.Click(()=>{if(captured.Action!=null)Dispatch(captured.Action);});bindings[element]=binding;ListenerInstallCount++;
            }
            binding.Action=action;binding.Seen=bindingPass;binding.Button.interactable=action.Enabled&&!IsBusy;actions[action]=binding.Button;
        }
        async void Dispatch(UiAction action){await SendAsync(new UiCommand(action.Id,action.Entity,action.Quantity));}
        public async Task<UiCommandResult> SendAsync(UiCommand command)
        {
            if(adapter==null)return UiCommandResult.Rejected("unmounted","UI is not mounted.");
            if(IsBusy)return UiCommandResult.Rejected("busy","A command is pending.");
            if(command==null||!actions.Any(a=>a.Key.Enabled&&a.Key.Id==command.Id&&a.Key.Entity==command.Entity&&a.Key.Quantity==command.Quantity&&a.Value.IsInteractable()))
                return UiCommandResult.Rejected("disabled","Command is unavailable in this view.");
            var source=adapter;int session=generation;IsBusy=true;BusyChanged?.Invoke(true);
            foreach(var b in actions.Values)b.interactable=false;
            using(var request=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token)){
                UiCommandResult result;
                try{
                    var task=source.ExecuteAsync(command,request.Token)??throw new InvalidOperationException("Adapter returned no task.");
                    var delay=Task.Delay(TimeSpan.FromSeconds(Math.Max(.05f,CommandTimeoutSeconds)),request.Token);
                    if(await Task.WhenAny(task,delay)!=task){ObserveLate(task);request.Token.ThrowIfCancellationRequested();request.Cancel();result=UiCommandResult.Rejected("timeout","Result unknown; refresh authoritative state before retrying.");}
                    else result=await task??UiCommandResult.Rejected("invalid-result","Adapter returned no result.");
                }
                catch(OperationCanceledException){result=UiCommandResult.Rejected("cancelled","UI session ended.");}
                catch(Exception e){result=UiCommandResult.Rejected("adapter-error",e.Message);}
                if(session==generation&&this!=null&&isActiveAndEnabled){IsBusy=false;dirty=true;BusyChanged?.Invoke(false);CommandFinished?.Invoke(command,result);}
                return result;
            }
        }
        static async void ObserveLate(Task<UiCommandResult> task){try{await task;}catch{ /* The timed-out operation is observed; the adapter owns reconciliation. */ }}
        public void Unmount()
        {
            generation++;if(adapter!=null)adapter.Changed-=Changed;adapter=null;lifetime?.Cancel();lifetime?.Dispose();lifetime=null;IsBusy=false;dirty=false;ClearScreen();
            if(ownsCanvas&&canvas!=null){canvas.gameObject.SetActive(false);Destroy(canvas.gameObject);}canvas=null;ownsCanvas=false;
        }
        void OnDisable(){Unmount();}
        void OnDestroy(){Unmount();}
    }
}
