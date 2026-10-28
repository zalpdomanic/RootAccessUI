(function(root){
  'use strict';
  // Session-only design study: one job/equipment state per base, one shared wallet.
  class Network {
    constructor(initial,Game,JOBS,now=Date.now()){
      this.Game=Game;this.JOBS=JOBS;this.current=0;this.unlocked=1;this.last=now;this.remotePaid=0;
      this.games=[initial,...[1,2].map(()=>new Game(JSON.parse(JSON.stringify(initial.s)),now))];
      this.paid=[0,0,0];this.rates=this.games.map(g=>this.rate(g));
    }
    rate(g){const modem=g.modem(),speed=1+(modem ? .2+(modem.level-1)*.04 : 0);return g.s.jobs.reduce((sum,_,i)=>sum+(g.auto(i)&&g.unlocked(i)?g.payout(i)/this.JOBS[i].seconds*speed/g.durationFactor():0),0);}
    get game(){return this.games[this.current];}
    get remoteRate(){return this.rates.reduce((n,r,i)=>n+(i<this.unlocked&&i!==this.current?r:0),0);}
    tick(now=Date.now()){
      const dt=Math.max(0,now-this.last)/1000;this.last=now;
      this.rates.forEach((r,i)=>{if(i>=this.unlocked||i===this.current)return;const amount=r*dt;this.paid[i]+=amount;this.remotePaid+=amount;this.game.s.money+=amount;this.game.s.earned+=amount;});
    }
    visit(index,now=Date.now()){
      if(index<0||index>=this.unlocked)return false;
      this.tick(now);this.game.tick(now);this.rates[this.current]=this.rate(this.game);
      const wallet=this.game.s.money;this.current=index;this.game.s.money=wallet;this.game.s.last=now;this.game.s.income=[];
      return true;
    }
    stage(count,now=Date.now()){
      this.tick(now);this.unlocked=Math.max(1,Math.min(3,count));return this.visit(this.unlocked-1,now);
    }
  }
  root.RootBasesNetwork=Network;
  if(typeof module!=='undefined')module.exports=Network;
})(typeof window==='undefined'?globalThis:window);
