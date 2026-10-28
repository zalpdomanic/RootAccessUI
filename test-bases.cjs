const assert=require('node:assert/strict');
const {Game,JOBS}=require('./engine.js');
const Network=require('./bases-network.js');
const start=100000;
const make=()=>new Network(new Game(null,start),Game,JOBS,start);
const near=(a,b)=>assert.ok(Math.abs(a-b)<1e-6,`${a} != ${b}`);
{
 const n=make();n.tick(start+10000);near(n.remotePaid,0);assert.equal(n.visit(1,start+10000),false);
}
{
 const n=make();n.stage(2,start);const before=n.game.s.money,rate=n.remoteRate;
 n.tick(start+10000);near(n.game.s.money-before,rate*10);near(n.paid[0],rate*10);near(n.paid[1],0);
 // Switching moves the shared wallet; it does not pay a second copy of remote time.
 n.game.s.last=start+10000;const money=n.game.s.money;
 n.visit(0,start+10000);near(n.game.s.money,money);n.tick(start+20000);
 near(n.game.s.money-money,n.remoteRate*10);near(n.remotePaid,rate*20);
}
{
 const n=make();n.stage(3,start);near(n.remoteRate,n.rates[0]+n.rates[1]);
 n.games[0].s.jobs[0].level=5;n.visit(0,start);n.visit(2,start);
 assert.equal(n.games[0].s.jobs[0].level,5);assert.equal(n.games[2].s.jobs[0].level,1);
 assert.ok(n.rates[0]>n.rates[1]);n.tick(start+10000);
 near(n.remotePaid,(n.rates[0]+n.rates[1])*10);
}
console.log('PASS: locked bases, remote income, shared wallet, no double payment, per-base upgrades');
