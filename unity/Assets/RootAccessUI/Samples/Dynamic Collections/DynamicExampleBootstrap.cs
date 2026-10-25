using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace RootAccess.UI.Integration
{
    public sealed class DynamicExampleBootstrap:MonoBehaviour
    {
        [Range(0,50)] public int JobCount=8;
        public bool ShowReferenceEnvironment;
        public RootAccessUiHost Host {get;private set;}
        public DynamicExampleAdapter Adapter {get;private set;}
        float elapsed;
        void Start()
        {
            if(EventSystem.current==null){var input=new GameObject("Example UI input",typeof(EventSystem),typeof(InputSystemUIInputModule));input.transform.SetParent(transform,false);}
            Adapter=new DynamicExampleAdapter(JobCount);Host=gameObject.AddComponent<RootAccessUiHost>();Host.ShowReferenceEnvironment=ShowReferenceEnvironment;Host.Mount(Adapter);
        }
        void Update(){if(Adapter==null)return;elapsed+=Time.unscaledDeltaTime;if(elapsed>=.1f){Adapter.Advance(elapsed);elapsed=0;}}
    }
}
