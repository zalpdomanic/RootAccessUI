using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace RootAccess.UI.Integration
{
    // Test double only. Replace this adapter with the team's game facade; no backend assumed.
    public sealed class IntegrationExampleAdapter:IRootAccessUiAdapter
    {
        public event Action Changed;
        public double Money=12000;
        public int Taps,Level=1,CommandCount;
        public bool Automated;
        public int DelayMilliseconds=150;
        public bool RejectNext;
        public string Route="home";
        public UiCommand LastCommand;
        public void Refresh(){Changed?.Invoke();}
        static UiTarget T(string name)=>new UiTarget(name);
        static UiTarget J(string name,int card,int occurrence=0)=>new UiTarget(name,"Job Card",card,occurrence);
        static string Cash(double n)=>"$"+(n>=1000?(n/1000).ToString("0.0",CultureInfo.InvariantCulture)+"K":n.ToString("0",CultureInfo.InvariantCulture));
        public UiFrame Capture()
        {
            var f=new UiFrame{Screen=Route=="jobs"?"05-jobs-top":Route=="settings"?"35-settings":"03-home-hack-full"};
            var text=new List<UiText>{new UiText(T("money"),Cash(Money)),new UiText(new UiTarget("income",occurrence:1),Automated?"+$1.0/s":"+$0.0/s")};
            var actions=new List<UiAction>();
            if(Route=="settings")actions.Add(new UiAction(new UiTarget("Menu Button",occurrence:0),"navigate","home"));
            else {
                actions.Add(new UiAction(T("Hack Button"),"hack"));
                actions.Add(new UiAction(T("drawer-toggle"),"navigate",Route=="jobs"?"home":"jobs"));
                actions.Add(new UiAction(T("Settings"),"navigate","settings"));
            }
            if(Route=="home")f.Fills=new[]{new UiFill(T("Tap progress fill"),Taps/10f)};
            if(Route=="jobs"){
                text.Add(new UiText(J("state",0),"LV "+Level+" · Ready"));
                text.Add(new UiText(J("level-price",0),"$100"));
                text.Add(new UiText(J("Action",1,1),Automated?"AUTO":"AUTO +"));
                text.Add(new UiText(J("auto-price",1),"$5,000"));
                actions.Add(new UiAction(J("Action",0,1),"level-up","SIG_01",enabled:Money>=100));
                actions.Add(new UiAction(J("Action",1,1),"automate","ARC_02",enabled:!Automated&&Money>=5000));
                actions.Add(new UiAction(T("Close jobs"),"navigate","home"));
            }
            f.Text=text.ToArray();f.Actions=actions.ToArray();return f;
        }
        public async Task<UiCommandResult> ExecuteAsync(UiCommand command,CancellationToken cancellation)
        {
            LastCommand=command;CommandCount++;
            if(command.Id=="navigate"){Route=command.Entity;Refresh();return UiCommandResult.Accepted();}
            await Task.Delay(DelayMilliseconds,cancellation);cancellation.ThrowIfCancellationRequested();
            if(RejectNext){RejectNext=false;return UiCommandResult.Rejected("example-rejection","Test request rejected; balance was not changed.");}
            switch(command.Id){
                case "hack":Taps=(Taps+1)%10;Money++;break;
                case "level-up":if(Money<100)return UiCommandResult.Rejected("funds","Not enough credits.");Money-=100;Level++;break;
                case "automate":if(Automated||Money<5000)return UiCommandResult.Rejected("unavailable","Automation unavailable.");Money-=5000;Automated=true;break;
                default:return UiCommandResult.Rejected("unknown-command",command.Id);
            }
            Refresh();return UiCommandResult.Accepted();
        }
    }
}
