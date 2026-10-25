using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using RootAccess.UI.Loading;

public sealed class LoadingDemoBootstrap : MonoBehaviour
{
    RootAccessLoadingHost host; RootAccessLoadingDemo demo;
    void Start()
    {
        if(EventSystem.current==null)new GameObject("Demo EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        host=gameObject.AddComponent<RootAccessLoadingHost>();demo=gameObject.AddComponent<RootAccessLoadingDemo>();demo.Host=host;
    }
    void OnGUI()
    {
        if(host==null||host.IsVisible)return;
        if(GUI.Button(new Rect(20,20,300,60),"Startup loading example"))demo.PlayStartup();
        if(GUI.Button(new Rect(20,100,300,60),"Ad failure and retry example"))demo.PlayAdFailure();
        GUI.Label(new Rect(20,180,500,80),"Demo only. Bind the prefab to your own scene loader/ad SDK.");
    }
}
