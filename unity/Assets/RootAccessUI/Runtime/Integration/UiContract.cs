using System;
using System.Threading;
using System.Threading.Tasks;

namespace RootAccess.UI.Integration
{
    // Transport-independent display contract. No economy, saves, networking or Unity types.
    // Capture is called on the Unity thread. Return a coherent snapshot; do not mutate it afterwards.
    public interface IRootAccessUiAdapter
    {
        event Action Changed;
        UiFrame Capture();
        Task<UiCommandResult> ExecuteAsync(UiCommand command, CancellationToken cancellation);
    }

    [Serializable] public sealed class UiTarget
    {
        public string NodeId;
        public string Name;
        public string Scope;
        public int ScopeOccurrence;
        public int Occurrence;
        public UiTarget(string name, string scope=null, int scopeOccurrence=0, int occurrence=0)
        {Name=name;Scope=scope;ScopeOccurrence=scopeOccurrence;Occurrence=occurrence;}
    }
    [Serializable] public sealed class UiText
    {
        public UiTarget Target;public string Value;
        public UiText(UiTarget target,string value){Target=target;Value=value;}
    }
    [Serializable] public sealed class UiFill
    {
        public UiTarget Target;public float Fraction;
        public UiFill(UiTarget target,float fraction){Target=target;Fraction=fraction;}
    }
    [Serializable] public sealed class UiAction
    {
        public UiTarget Target;public string Id,Entity;public int Quantity=1;public bool Enabled=true;
        public UiAction(UiTarget target,string id,string entity=null,int quantity=1,bool enabled=true)
        {Target=target;Id=id;Entity=entity;Quantity=quantity;Enabled=enabled;}
    }
    [Serializable] public sealed class UiFrame
    {
        public string Screen="01-home";
        // Change when action targets change on the same screen. Text/fill updates need no rebuild.
        public string LayoutKey="";
        public bool EnableScrolling=true;
        public UiText[] Text=Array.Empty<UiText>();
        public UiFill[] Fills=Array.Empty<UiFill>();
        public UiAction[] Actions=Array.Empty<UiAction>();
        // null preserves the authored fixture. An empty array explicitly removes every item.
        public UiJob[] Jobs;
        public UiUpgrade[] Upgrades;
        public string FocusJobId;
    }
    [Serializable] public sealed class UiItemAction
    {
        public string Id,Label;
        public int Quantity=1;
        public bool Enabled=true;
        public UiItemAction(string id,string label,bool enabled=true,int quantity=1){Id=id;Label=label;Enabled=enabled;Quantity=quantity;}
    }
    [Serializable] public sealed class UiJob
    {
        public string Id,Code,Name,Status,Payout,LevelPrice,AutomationPrice;
        public bool Locked,Automated;
        public float Progress;
        // Display values and available actions are authoritative; UI computes no costs or unlock rules.
        public UiItemAction Run,LevelUp,Automate;
    }
    [Serializable] public sealed class UiUpgrade
    {
        public string Id,Name,Detail,Price;
        public UiItemAction Purchase;
    }
    public sealed class UiCommand
    {
        public string RequestId {get;}=Guid.NewGuid().ToString("N");
        public string Id {get;}public string Entity {get;}public int Quantity {get;}
        public UiCommand(string id,string entity=null,int quantity=1){Id=id;Entity=entity;Quantity=quantity;}
    }
    public sealed class UiCommandResult
    {
        public bool Success {get;}public string ErrorCode {get;}public string Message {get;}
        UiCommandResult(bool success,string code,string message){Success=success;ErrorCode=code;Message=message;}
        public static UiCommandResult Accepted()=>new UiCommandResult(true,null,null);
        public static UiCommandResult Rejected(string code,string message)=>new UiCommandResult(false,code,message);
    }
}
