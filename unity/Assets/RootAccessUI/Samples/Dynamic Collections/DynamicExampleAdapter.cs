using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RootAccess.UI.Integration
{
    // Replaceable in-memory game service. All calculations here are examples, never UI rules.
    public sealed class DynamicExampleAdapter:IRootAccessUiAdapter
    {
        public sealed class Job {public string Id,Code,Name;public int Level=1;public bool Locked,Automated,Running;public float Progress;}
        public sealed class Upgrade {public string Id,Name,Detail;public bool Owned,Locked;public double Price;}
        public event Action Changed;
        public readonly List<Job> Jobs=new List<Job>();
        public readonly List<Upgrade> Upgrades=new List<Upgrade>();
        public double Money=25000;public int Taps,Quantity=1;public string Route="home",FocusJobId;
        public int DelayMilliseconds=80;public UiCommand LastCommand;
        public DynamicExampleAdapter(int count=8)
        {
            var names=new[]{"Decode Signal","Recover Archive","Map Network","Ghost Protocol","Break Cipher","Trace Packet","Scan Node","Relay Traffic"};
            var codes=new[]{"SIG","ARC","NET","GHO","CRY","PKT","NOD","RLY"};
            for(int i=0;i<count;i++)Jobs.Add(new Job{Id="job-"+i,Code=i<8?codes[i]:"J"+i,Name=i<8?names[i]:"Extra Job "+i,Progress=(i%5)*.17f,Automated=i==0,Running=i<2,Locked=i==7});
            for(int i=0;i<count;i++)Upgrades.Add(new Upgrade{Id="upgrade-"+i,Name="Signal Module "+(i+1),Detail="Improve the connected job",Price=(i+1)*250,Locked=i==7,Owned=i==0});
        }
        public void Refresh()=>Changed?.Invoke();
        public void Advance(float seconds)
        {foreach(var job in Jobs.Where(j=>!j.Locked&&(j.Running||j.Automated))){job.Progress+=seconds/8;if(job.Progress>=1){job.Progress%=1;Money+=job.Level*3;if(!job.Automated){job.Running=false;job.Progress=0;}}}Refresh();}
        static UiTarget T(string name)=>new UiTarget(name);
        static UiTarget Mode(int index)=>new UiTarget("BUTTON","Buy Modes",occurrence:index);
        static string Cash(double n)=>"$"+(n>=1000?(n/1000).ToString("0.0",CultureInfo.InvariantCulture)+"K":n.ToString("0",CultureInfo.InvariantCulture));
        public UiFrame Capture()
        {
            var frame=new UiFrame{Screen=Route=="jobs"?"05-jobs-top":Route=="upgrades"?"23-install-equipment":Route=="settings"?"35-settings":"03-home-hack-full",FocusJobId=FocusJobId};
            var text=new List<UiText>{new UiText(T("money"),Cash(Money)),new UiText(new UiTarget("income",occurrence:1),"+$0.0/s")};
            var actions=new List<UiAction>();
            frame.Jobs=Jobs.Select(j=>{
                int count=Math.Min(Quantity,Math.Max(0,10-j.Level));double cost=count*100;
                return new UiJob{Id=j.Id,Code=j.Code,Name=j.Name,Locked=j.Locked,Automated=j.Automated,Progress=j.Progress,
                    Status=j.Locked?"REP LV 5 REQUIRED":"LV "+j.Level+(j.Running?" · Running":" · Ready"),Payout=Cash(j.Level*3),LevelPrice=count>0?Cash(cost):"MAX LEVEL",AutomationPrice="$500",
                    Run=new UiItemAction("run",j.Locked?"LOCKED":j.Automated?"AUTO":j.Running?"RUNNING":"RUN",!j.Locked&&!j.Running&&!j.Automated),
                    LevelUp=new UiItemAction("level-up",count>0?"LEVEL UP +"+count:"MAX LEVEL",!j.Locked&&count>0&&Money>=cost,count),
                    Automate=new UiItemAction("automate","AUTO +",!j.Locked&&!j.Automated&&Money>=500)};
            }).ToArray();
            if(Route=="settings"){
                actions.Add(new UiAction(new UiTarget("Menu Button",occurrence:0),"navigate","home"));
            }else {
                actions.Add(new UiAction(T("Hack Button"),"hack"));actions.Add(new UiAction(T("Settings"),"navigate","settings"));
                if(Route=="home"||Route=="jobs")actions.Add(new UiAction(T("drawer-toggle"),"navigate",Route=="home"?"jobs":"home"));
            }
            if(Route=="home")frame.Fills=new[]{new UiFill(T("Tap progress fill"),Taps/10f)};
            if(Route=="jobs"){
                actions.Add(new UiAction(T("Close jobs"),"navigate","home"));
                int[] quantities={1,10,100};for(int i=0;i<3;i++){text.Add(new UiText(Mode(i),quantities[i]+"×"));actions.Add(new UiAction(Mode(i),"quantity",quantity:quantities[i]));}
                text.Add(new UiText(Mode(3),"UPGRADES"));actions.Add(new UiAction(Mode(3),"navigate","upgrades"));
            }
            if(Route=="upgrades"){
                frame.Upgrades=Upgrades.Select(u=>new UiUpgrade{Id=u.Id,Name=u.Name,Detail=u.Detail,Price=Cash(u.Price),Purchase=new UiItemAction("buy-upgrade",u.Owned?"OWNED":u.Locked?"LOCKED":"UPGRADE",!u.Owned&&!u.Locked&&Money>=u.Price)}).ToArray();
                text.Add(new UiText(T("drawer-title"),"UPGRADES"));text.Add(new UiText(T("drawer-meta"),Upgrades.Count+" OPTIONS"));text.Add(new UiText(T("install-title"),"AVAILABLE UPGRADES"));
                actions.Add(new UiAction(T("Back to rack"),"navigate","jobs"));actions.Add(new UiAction(T("Close menu"),"navigate","home"));
            }
            frame.Text=text.ToArray();frame.Actions=actions.ToArray();return frame;
        }
        public async Task<UiCommandResult> ExecuteAsync(UiCommand c,CancellationToken token)
        {
            LastCommand=c;
            if(c.Id=="navigate"){Route=c.Entity;FocusJobId=null;Refresh();return UiCommandResult.Accepted();}
            if(c.Id=="select-job"){if(!Jobs.Any(j=>j.Id==c.Entity&&!j.Locked))return UiCommandResult.Rejected("missing","Job unavailable.");Route="jobs";FocusJobId=c.Entity;Refresh();return UiCommandResult.Accepted();}
            if(c.Id=="quantity"){Quantity=c.Quantity;Refresh();return UiCommandResult.Accepted();}
            await Task.Delay(DelayMilliseconds,token);token.ThrowIfCancellationRequested();
            if(c.Id=="hack"){Taps=(Taps+1)%10;Money++;Refresh();return UiCommandResult.Accepted();}
            if(c.Id=="buy-upgrade"){
                var upgrade=Upgrades.Find(u=>u.Id==c.Entity);if(upgrade==null||upgrade.Owned||upgrade.Locked||Money<upgrade.Price)return UiCommandResult.Rejected("unavailable","Upgrade unavailable.");
                Money-=upgrade.Price;upgrade.Owned=true;Refresh();return UiCommandResult.Accepted();
            }
            var job=Jobs.Find(j=>j.Id==c.Entity);if(job==null||job.Locked)return UiCommandResult.Rejected("missing","Job unavailable.");
            switch(c.Id){
                case "run":if(job.Running||job.Automated)return UiCommandResult.Rejected("running","Job already running.");job.Running=true;break;
                case "automate":if(job.Automated||Money<500)return UiCommandResult.Rejected("funds","Automation unavailable.");Money-=500;job.Automated=job.Running=true;break;
                case "level-up":int count=Math.Min(c.Quantity,Math.Max(0,10-job.Level));if(count<=0||Money<count*100)return UiCommandResult.Rejected("funds","Upgrade unavailable.");Money-=count*100;job.Level+=count;break;
                default:return UiCommandResult.Rejected("unknown-command",c.Id);
            }
            Refresh();return UiCommandResult.Accepted();
        }
    }
}
