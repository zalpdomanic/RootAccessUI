using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RootAccess.UI
{
    // Source demo: HACK translates 8px and compresses its shadow over 60ms;
    // every active button brightens by 13%. The approved rest pose is unchanged.
    public sealed class RootAccessPressFeedback:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerEnterHandler,IPointerExitHandler,ICancelHandler,ISubmitHandler
    {
        public static bool ReduceMotion;
        public float Amount {get;private set;}
        public float Depth {get;private set;}
        public RectTransform RingVisualRoot {get;private set;}
        Button button;RectTransform rect;FigmaGraphic[] visuals;Vector2 rest;
        int? pointer;bool inside,keyboard,hasRest,isRing;float minimumPressUntil;
        void Awake()
        {
            button=GetComponent<Button>();rect=(RectTransform)transform;var name=GetComponent<FigmaElement>().Data.name;Depth=name=="Hack Button"?8:0;isRing=name=="Job Ring";
            if(!isRing)return;
            // Animate only the artwork. The Button hit rectangle and scroll layout stay fixed.
            var children=new RectTransform[rect.childCount];for(int i=0;i<children.Length;i++)children[i]=(RectTransform)rect.GetChild(i);
            var go=new GameObject("Ring press visuals",typeof(RectTransform));go.layer=gameObject.layer;RingVisualRoot=(RectTransform)go.transform;RingVisualRoot.SetParent(rect,false);
            RingVisualRoot.anchorMin=Vector2.zero;RingVisualRoot.anchorMax=Vector2.one;RingVisualRoot.pivot=new Vector2(.5f,.5f);RingVisualRoot.offsetMin=RingVisualRoot.offsetMax=Vector2.zero;
            foreach(var child in children){var position=child.anchoredPosition3D;child.SetParent(RingVisualRoot,false);child.anchoredPosition3D=position;}
        }
        bool Active=>button!=null&&button.IsActive()&&button.IsInteractable();
        void Begin(){if(!hasRest){rest=rect.anchoredPosition;hasRest=true;visuals=GetComponentsInChildren<FigmaGraphic>(true);}}
        public void OnPointerDown(PointerEventData e){if(e.button!=PointerEventData.InputButton.Left||!Active||pointer.HasValue)return;Begin();pointer=e.pointerId;inside=true;minimumPressUntil=Time.unscaledTime+.06f;}
        public void OnPointerUp(PointerEventData e){if(pointer==e.pointerId){if(isRing&&e.dragging){ResetPose();return;}pointer=null;inside=false;if(isRing&&Amount<.5f)minimumPressUntil=Time.unscaledTime+.06f;}}
        public void OnPointerEnter(PointerEventData e){if(pointer==e.pointerId)inside=true;}
        public void OnPointerExit(PointerEventData e){if(pointer==e.pointerId){inside=false;minimumPressUntil=0;}}
        public void SetKeyboardPressed(bool pressed){keyboard=pressed&&Active;if(keyboard){Begin();minimumPressUntil=Time.unscaledTime+.06f;}}
        public void OnSubmit(BaseEventData e){if(Active){Begin();minimumPressUntil=Time.unscaledTime+.06f;}}
        public void OnCancel(BaseEventData e){ResetPose();}
        void OnApplicationFocus(bool focused){if(!focused)ResetPose();}
        void OnApplicationPause(bool paused){if(paused)ResetPose();}
        void LateUpdate()
        {
            if(!hasRest)return;
            // RUN disables a released ring immediately. Finish the already accepted tap pulse;
            // externally disabling a held control still cancels it immediately.
            if(!Active&&(!isRing||!button.IsActive()||pointer.HasValue||keyboard)){ResetPose();return;}
            float target=(pointer.HasValue&&inside)||keyboard||Time.unscaledTime<minimumPressUntil?1:0;
            Amount=ReduceMotion?target:Mathf.MoveTowards(Amount,target,Time.unscaledDeltaTime/.06f);
            if(isRing)RingVisualRoot.localScale=Vector3.one*(1-(ReduceMotion?0:.04f*Amount));
            else rect.anchoredPosition=rest+Vector2.down*(ReduceMotion?0:Depth*Amount);
            foreach(var visual in visuals)if(visual!=null)visual.SetPressVisual(Amount,visual.gameObject==gameObject&&!ReduceMotion?Depth:0);
            if(Amount==0&&target==0){hasRest=false;visuals=null;}
        }
        void ResetPose()
        {pointer=null;inside=false;keyboard=false;minimumPressUntil=0;Amount=0;if(!hasRest)return;if(isRing){if(RingVisualRoot!=null)RingVisualRoot.localScale=Vector3.one;}else rect.anchoredPosition=rest;if(visuals!=null)foreach(var visual in visuals)if(visual!=null)visual.SetPressVisual(0,0);hasRest=false;visuals=null;}
        void OnDisable(){ResetPose();}
    }
}
