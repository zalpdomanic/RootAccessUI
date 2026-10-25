using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace RootAccess.UI
{
    // The backdrop is rendered live, so balances and animations continue beneath a modal.
    // This render texture is a visual effect; final QA captures the full overlay canvas.
    public sealed class FigmaBackdrop:MonoBehaviour
    {
        FigmaView view;Camera capture;Canvas background;RectTransform modalRoot;RawImage display;
        RenderTexture source,temporary,blurred;Material blur;float radius;int width,height;
        public void Initialize(FigmaView owner,Transform overlay,FigmaElement modal)
        {
            view=owner;radius=modal.Data.effects.First(e=>e.type=="BACKGROUND_BLUR").radius;
            var camObject=new GameObject("Live UI backdrop camera",typeof(Camera));camObject.transform.SetParent(transform,false);
            capture=camObject.GetComponent<Camera>();capture.orthographic=true;capture.clearFlags=CameraClearFlags.SolidColor;capture.backgroundColor=Color.black;capture.cullingMask=1<<31;capture.depth=-10;capture.nearClipPlane=.1f;capture.farClipPlane=500;capture.transform.position=new Vector3(0,0,-100);
            var canvasObject=new GameObject("Live modal background",typeof(RectTransform),typeof(Canvas));canvasObject.layer=31;canvasObject.transform.SetParent(transform,false);
            background=canvasObject.GetComponent<Canvas>();background.additionalShaderChannels=AdditionalCanvasShaderChannels.TexCoord1|AdditionalCanvasShaderChannels.TexCoord2|AdditionalCanvasShaderChannels.TexCoord3;background.renderMode=RenderMode.ScreenSpaceCamera;background.worldCamera=capture;background.planeDistance=100;
            var rootObject=new GameObject("Modal coordinate space",typeof(RectTransform));rootObject.transform.SetParent(overlay,false);modalRoot=(RectTransform)rootObject.transform;
            modal.transform.SetParent(modalRoot,false);
            view.Root.SetParent(background.transform,false);SetLayer(view.Root.gameObject,31);
            var imageObject=new GameObject("Live blurred backdrop",typeof(RectTransform),typeof(RawImage));imageObject.transform.SetParent(overlay,false);imageObject.transform.SetSiblingIndex(modalRoot.GetSiblingIndex());
            display=imageObject.GetComponent<RawImage>();display.raycastTarget=false;var rect=(RectTransform)imageObject.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            blur=new Material(Resources.Load<Shader>("RootAccessUI/BackdropBlur"));Resize();RenderPipelineManager.endCameraRendering+=Rendered;
        }
        static void SetLayer(GameObject go,int layer){go.layer=layer;foreach(Transform child in go.transform)SetLayer(child.gameObject,layer);}
        void Resize()
        {
            width=Screen.width;height=Screen.height;Release();
            source=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);source.name="Live UI backdrop";
            temporary=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32);blurred=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32);
            capture.targetTexture=source;capture.orthographicSize=height*.5f;display.texture=blurred;
        }
        public void SyncLayout()
        {
            if(capture==null)return;if(width!=Screen.width||height!=Screen.height)Resize();
            modalRoot.anchorMin=view.Root.anchorMin;modalRoot.anchorMax=view.Root.anchorMax;modalRoot.pivot=view.Root.pivot;modalRoot.anchoredPosition=view.Root.anchoredPosition;modalRoot.sizeDelta=view.Root.sizeDelta;modalRoot.localScale=view.Root.localScale;
        }
        void Rendered(ScriptableRenderContext context,Camera camera)
        {
            if(camera!=capture||source==null)return;
            float pixels=radius*view.Root.localScale.x/3f;
            blur.SetVector("_Direction",new Vector4(pixels/width,0,0,0));Graphics.Blit(source,temporary,blur);
            blur.SetVector("_Direction",new Vector4(0,pixels/height,0,0));Graphics.Blit(temporary,blurred,blur);
        }
        void Release(){if(source!=null){source.Release();Destroy(source);}if(temporary!=null){temporary.Release();Destroy(temporary);}if(blurred!=null){blurred.Release();Destroy(blurred);}}
        void OnDestroy(){RenderPipelineManager.endCameraRendering-=Rendered;if(capture!=null)capture.targetTexture=null;Release();if(blur!=null)Destroy(blur);if(modalRoot!=null)Destroy(modalRoot.gameObject);if(display!=null)Destroy(display.gameObject);}
    }
}
