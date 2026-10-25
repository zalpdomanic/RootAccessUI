const assert = require('node:assert/strict');
const {LoadingSession} = require('./loading.js');
const flush = async () => { for(let i=0;i<8;i++) await Promise.resolve(); };
function clock() {
  let id=0;const tasks=new Map();
  return {setTimeout(fn,ms){const key=++id;tasks.set(key,{fn,ms});return key;},clearTimeout(key){tasks.delete(key);},fire(ms){for(const [key,t] of [...tasks])if(t.ms===ms){tasks.delete(key);t.fn();}},get count(){return tasks.size;}};
}
function setup(){const states=[],timer=clock(),session=new LoadingSession(state=>states.push(state),timer);return {states,timer,session};}
(async()=>{
  let checks=0;
  async function test(name,fn){await fn();checks++;console.log('PASS '+name);}
  await test('Real progress is bounded and invalid values are ignored',async()=>{
    const {session,states,timer}=setup();let report;
    session.start({kind:'startup',work:(_,r)=>{report=r;return new Promise(()=>{});}});await flush();
    report(.37);assert.equal(states.at(-1).progress,.37);report(NaN);assert.equal(states.at(-1).progress,.37);report(2);assert.equal(states.at(-1).progress,1);report(null);assert.equal(states.at(-1).progress,null);session.stop();assert.equal(timer.count,0);
  });
  await test('Completion waits for readiness and only completes once',async()=>{
    const {session,states,timer}=setup();let resolve,completed=0;
    session.start({kind:'ad',work:()=>new Promise(r=>resolve=r),onComplete:()=>completed++});await flush();timer.fire(600);await flush();assert.equal(completed,0);resolve('ready');await flush();assert.equal(completed,1);assert.equal(states.at(-1).phase,'hidden');assert.equal(timer.count,0);
  });
  await test('Failure gives no reward and retry starts a fresh session',async()=>{
    const {session,timer}=setup();let attempts=0,completed=0;
    const first=session.start({kind:'ad',work:()=>{if(++attempts===1)throw Error('offline');return 'ad';},onComplete:()=>completed++});await flush();assert(session.failed);assert.equal(completed,0);session.retry();session.retry();await flush();timer.fire(600);await flush();assert.equal(attempts,2);assert.equal(completed,1);assert(session.ticket>first);
  });
  await test('Timeout cancels work and rejects late completion',async()=>{
    const {session,timer}=setup();let resolve,signal,completed=0;
    session.start({kind:'ad',work:s=>{signal=s;return new Promise(r=>resolve=r);},onComplete:()=>completed++});await flush();timer.fire(15000);assert(session.failed);assert(signal.aborted);resolve('late');await flush();assert.equal(completed,0);assert.equal(timer.count,0);
  });
  await test('Closing restores control once and never completes an ad',async()=>{
    const {session,timer}=setup();let resolve,back=0,complete=0;
    session.start({kind:'ad',work:()=>new Promise(r=>resolve=r),onBack:()=>back++,onComplete:()=>complete++});await flush();session.close();session.close();resolve('late');timer.fire(600);await flush();assert.equal(back,1);assert.equal(complete,0);assert.equal(timer.count,0);
  });
  await test('Superseded request cannot update or complete the newer request',async()=>{
    const {session,timer,states}=setup();let oldResolve,oldReport,completed=0;
    session.start({kind:'ad',work:(_,report)=>{oldReport=report;return new Promise(r=>oldResolve=r);},onComplete:()=>completed++});await flush();
    session.start({kind:'startup',work:()=>new Promise(()=>{})});await flush();oldReport(.9);oldResolve('stale');timer.fire(600);await flush();assert.equal(completed,0);assert.equal(states.at(-1).kind,'startup');assert.equal(states.at(-1).progress,null);session.stop();
  });
  await test('Repeated sessions leave no scheduled callbacks after closing',async()=>{
    const {session,timer}=setup();for(let i=0;i<20;i++){session.start({kind:'ad',work:()=>new Promise(()=>{}),onBack:()=>{}});await flush();session.close();assert.equal(timer.count,0);}assert(!session.active);
  });
  console.log(checks+' loading lifecycle checks passed.');
})().catch(e=>{console.error(e);process.exitCode=1;});
