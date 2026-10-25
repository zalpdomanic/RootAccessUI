using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace RootAccess.UI.Integration
{
    // Attach to an empty scene. The real game supplies its own adapter and EventSystem instead.
    public sealed class IntegrationExampleBootstrap:MonoBehaviour
    {
        public bool ShowReferenceEnvironment;
        public RootAccessUiHost Host {get;private set;}
        public IntegrationExampleAdapter Adapter {get;private set;}
        GameObject ownedInput;
        void Start()
        {
            if(EventSystem.current==null){ownedInput=new GameObject("Example UI input",typeof(EventSystem),typeof(InputSystemUIInputModule));ownedInput.transform.SetParent(transform,false);}
            Adapter=new IntegrationExampleAdapter();Host=gameObject.AddComponent<RootAccessUiHost>();Host.ShowReferenceEnvironment=ShowReferenceEnvironment;
            Host.CommandFinished+=(command,result)=>{if(!result.Success)Debug.LogWarning("UI command "+command.Id+": "+result.ErrorCode+" — "+result.Message);};
            Host.Mount(Adapter);
        }
    }
}
