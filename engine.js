(function (root) {
  'use strict';
  const JOBS = [
    {name:'Decode Signal',code:'SIG_01',description:'Gürültünün içinden sinyali ayıkla.',base:3,seconds:6,rep:0,repGain:2,cost:6.7},
    {name:'Recover Archive',code:'ARC_02',description:'Kayıp veri bloklarını yeniden birleştir.',base:45,seconds:16,rep:30,repGain:5,cost:225},
    {name:'Map the Network',code:'NET_03',description:'Ağın bağlantı haritasını tamamla.',base:280,seconds:28,rep:120,repGain:10,cost:1800},
    {name:'Ghost Protocol',code:'GHO_04',description:'İzole bir sistemde protokolü çöz.',base:1800,seconds:45,rep:400,repGain:20,cost:12000}
  ];
  class Game {
    constructor(saved, now=Date.now()) {
      this.s = saved && saved.version===1 ? saved : Game.seed('demo',now);
      // Retire the old free starter modem once; keep purchased/upgraded equipment.
      if(!this.s.rulesVersion){if(this.s.mode==='demo'&&this.s.slots[1]?.type==='modem'&&this.s.slots[1].level===1)this.s.slots[1]=null;this.s.rulesVersion=2;}
      while(this.s.slots.length<5)this.s.slots.push(null);
      // Apply the requested level-one starting point once to existing demos.
      if(this.s.rulesVersion<3){this.s.rep=0;this.s.jobs.forEach((j,i)=>{if(i>0){j.running=false;j.progress=0;}});this.s.rulesVersion=3;}
      if(![1,10,100,'max'].includes(this.s.buyMode))this.s.buyMode=1;
      const savedAt=Number.isFinite(this.s.last)?Math.min(now,this.s.last):now;
      if(!this.s.afk){
        const old=this.s.offlineReward;
        this.s.afk={elapsed:old?Math.min(old.credited||old.away||60000,28800000):0,amount:old?.amount||0,jobs:old?.jobs||0,pending:0,pendingJobs:0};
        delete this.s.offlineReward;
      }
      this.s.income=[]; this.s.last=now;
      this.s.boosts=this.s.boosts.filter(t=>t>savedAt);
    }
    static seed(mode,now=Date.now()) {return {version:1,rulesVersion:3,mode,money:mode==='demo'?2480000:25,rep:0,comfort:mode==='demo'?5:3,taps:0,totalTaps:0,earned:0,completed:0,boosts:[],income:[],last:now,slots:mode==='demo'?[{type:'rig',job:0},null,null,null,null]:[null,null,null,null,null],jobs:JOBS.map((_,i)=>({level:1,progress:0,running:mode==='demo'&&i===0})),log:[]};}
    rackCount(){return Math.ceil(this.s.slots.length/5);}
    rackPrice(){return 25000;}
    buyRack(){if(this.rackCount()>=2||this.s.money<this.rackPrice())return false;this.s.money-=this.rackPrice();this.s.slots.push(null,null,null,null,null);this.log('Server rack 02 kuruldu · 5 raf');return true;}
    repStatus(){let level=1,progress=this.s.rep,required=30;while(progress>=required){progress-=required;level++;required=level===2?90:level===3?280:Math.ceil(required*1.5);}return {level,progress,required};}
    unlocked(i){return this.repStatus().level>=i+1;}
    automationCost(i){return i===0?250:2500*(i+1);}
    automate(i){return this.install(this.s.slots.findIndex(s=>!s),'rig',i);}
    auto(i){return this.s.slots.some(s=>s?.type==='rig'&&s.job===i);}
    modem(){return this.s.slots.find(s=>s?.type==='modem');}
    comfort(){return this.s.comfort;}
    multiplier(){return 1+this.comfort()/100;}
    tapValue(now=Date.now()){return this.multiplier(now);}
    speed(now=Date.now()){return 1+(this.modem()?0.2+(this.modem().level-1)*0.04:0)+this.s.boosts.filter(t=>t>now).length*0.25;}
    payout(i,now=Date.now()){return JOBS[i].base*Math.pow(1.2,this.s.jobs[i].level-1)*this.multiplier(now);}
    cost(i){return JOBS[i].cost*Math.pow(1.48,this.s.jobs[i].level-1);}
    gain(amount,now){this.s.money+=amount;this.s.earned+=amount;this.s.income.push({t:now,a:amount});}
    log(message,now=Date.now()){this.s.log.unshift({message,t:now});this.s.log=this.s.log.slice(0,12);}
    rate(now=Date.now()){this.s.income=this.s.income.filter(x=>now-x.t<15000);return this.s.income.reduce((n,x)=>n+x.a,0)/15;}
    afkStatus(){
      const a=this.s.afk,elapsed=a.elapsed;
      return {amount:a.amount,jobs:a.jobs,elapsed,capped:elapsed>=28800000,ready:elapsed>=60000&&a.amount>0,next:elapsed>=28800000?0:60000-elapsed%60000};
    }
    collectAfk(multiplier=1,now=Date.now()){
      if(multiplier!==1&&multiplier!==2)return null;
      this.tick(now);if(!this.afkStatus().ready)return null;
      const a=this.s.afk,reward={amount:a.amount*multiplier,jobs:a.jobs,multiplier};
      this.s.money+=reward.amount;this.s.earned+=reward.amount;
      // Keep the unfinished minute so collection never discards earned credits.
      this.s.afk={elapsed:a.elapsed%60000,amount:0,jobs:0,pending:a.pending,pendingJobs:a.pendingJobs};
      return reward;
    }
    resumeOffline(savedAt,now=Date.now()){
      const away=Number.isFinite(savedAt)?Math.max(0,now-savedAt):0;
      if(!this.s.jobs.some((_,i)=>this.auto(i)&&this.unlocked(i))){
        if(away){this.s.last=savedAt;this.tick(now);}else this.s.last=now;
        this.s.income=[];return this.afkStatus();
      }
      const credited=Math.min(away,Math.max(0,28800000-this.s.afk.elapsed));
      if(credited){
        this.s.last=savedAt;this.afkAccruing=true;
        try{this.tick(savedAt+credited);}finally{this.afkAccruing=false;}
      }
      this.s.last=now;this.s.boosts=this.s.boosts.filter(t=>t>now);this.s.income=[];
      return this.afkStatus();
    }
    tick(now=Date.now()){
      let from=this.s.last;if(now<=from)return;this.s.last=now;
      // Offline minute boundaries settle the bank; boosts and REP unlocks stay exact.
      while(from<now){
        let to=Math.min(now,...this.s.boosts.filter(t=>t>from));
        if(this.afkAccruing)to=Math.min(to,from+60000-this.s.afk.elapsed%60000);
        if(this.s.jobs.some((j,i)=>!this.unlocked(i)&&(j.running||this.auto(i)))){
          const next=this.s.jobs.reduce((ms,j,i)=>this.unlocked(i)&&(j.running||this.auto(i))?Math.min(ms,(JOBS[i].seconds-j.progress)/this.speed(from)*1000):ms,to-from);
          to=Math.min(to,from+Math.max(1,Math.ceil(next)));
        }
        this.advance((to-from)/1000*this.speed(from),to,from);
        if(this.afkAccruing){
          const a=this.s.afk,minute=Math.floor(a.elapsed/60000);a.elapsed+=to-from;
          if(Math.floor(a.elapsed/60000)>minute){a.amount+=a.pending;a.jobs+=a.pendingJobs;a.pending=0;a.pendingJobs=0;}
        }
        from=to;
      }
      this.s.boosts=this.s.boosts.filter(t=>t>now);this.rate(now);
    }
    advance(seconds,now,payoutAt=now){const unlocked=this.s.jobs.map((_,i)=>this.unlocked(i));this.s.jobs.forEach((j,i)=>{if(!unlocked[i])return;const automated=this.auto(i);if(automated)j.running=true;if(!j.running)return;j.progress+=seconds;const count=Math.min(Math.floor(j.progress/JOBS[i].seconds),automated?Infinity:1);if(!count)return;j.progress-=count*JOBS[i].seconds;const amount=this.payout(i,payoutAt)*count;if(automated&&this.afkAccruing){this.s.afk.pending+=amount;this.s.afk.pendingJobs+=count;}else this.gain(amount,now);this.s.rep+=JOBS[i].repGain*count;this.s.completed+=count;if(!automated){j.running=false;j.progress=0;this.log(JOBS[i].name+' tamamlandı',now);}});}
    hack(now=Date.now()){this.tick(now);const amount=this.tapValue(now);this.gain(amount,now);this.s.totalTaps++;this.s.taps++;this.advance(0.35,now);let bonus=0;if(this.s.taps===10){this.s.taps=0;bonus=amount*15;this.gain(bonus,now);this.s.boosts.push(now+12000);this.log('Bar doldu · +%25 hız / 12 sn',now);}return {amount,bonus};}
    run(i){if(!this.unlocked(i)||this.s.jobs[i].running)return false;this.s.jobs[i].running=true;this.s.jobs[i].progress=0;return true;}
    quote(i,mode=1){
      const j=this.s.jobs[i],limit=Math.min(25-j.level,mode==='max'?25:([1,10,100].includes(mode)?mode:1));
      let count=0,cost=0;
      for(let n=0;n<limit;n++){const next=JOBS[i].cost*Math.pow(1.48,j.level+n-1);if(mode==='max'&&cost+next>this.s.money+1e-8)break;cost+=next;count++;}
      return {count,cost,affordable:count>0&&this.unlocked(i)&&cost<=this.s.money+1e-8};
    }
    level(i,mode=1){const q=this.quote(i,mode);if(!q.affordable)return false;this.s.money=Math.max(0,this.s.money-q.cost);this.s.jobs[i].level+=q.count;this.log(JOBS[i].name+' → LV '+this.s.jobs[i].level);return true;}
    install(slot,type,job=0){if(!Number.isInteger(slot)||slot<0||slot>=this.s.slots.length||!['rig','modem'].includes(type)||this.s.slots[slot])return false;if(type==='rig'&&(!Number.isInteger(job)||job<0||job>=JOBS.length))return false;let cost=type==='rig'?this.automationCost(job):15000;if(type==='rig'&&(this.auto(job)||!this.unlocked(job)))return false;if(type==='modem'&&this.modem())return false;if(this.s.money<cost)return false;this.s.money-=cost;this.s.slots[slot]=type==='rig'?{type,job}:type==='modem'?{type,level:1}:{type};if(type==='rig')this.run(job);this.log(type==='rig'?'Rig kuruldu · '+JOBS[job].name:'Fiber modem kuruldu');return true;}
    upgradeModem(){let m=this.modem();if(!m||m.level>=5)return false;let cost=1500000*Math.pow(2,m.level-1);if(this.s.money<cost)return false;this.s.money-=cost;m.level++;this.s.comfort+=2;this.log('Modem → L'+m.level+' · Comfort +2');return true;}
  }
  root.RootAccess={Game,JOBS};if(typeof module!=='undefined')module.exports={Game,JOBS};
})(typeof window==='undefined'?globalThis:window);
