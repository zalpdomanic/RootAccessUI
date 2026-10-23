(() => {
  'use strict';
  const {Game,JOBS}=RootAccess;
  const $=id=>document.getElementById(id),KEY='root-access-mobile-v2';
  let game,raw;try{raw=JSON.parse(localStorage.getItem(KEY));game=new Game(raw);}catch{game=new Game();}
  let panel='closed',jobsExpanded=false,barFullUntil=0,activeRack=0,selectedSlot=-1,installSlot=2,installJob=null,lastSaved=0,rackKey='',installKey='',toastTimer;
  const cash=n=>'$'+(n>=1e6?(n/1e6).toFixed(2).replace(/0$/,'')+'M':n>=100000?(n/1000).toFixed(1)+'K':n>=1000?Math.floor(n).toLocaleString('en-US'):n.toFixed(n<100?1:0));
  const fullCash=n=>'$'+n.toLocaleString('en-US',{maximumFractionDigits:2});
  function fit(){const s=Math.min(window.innerWidth/1080,window.innerHeight/1920),minFont=Math.ceil(12/s*100)/100,menuHeight=Math.max(266,Math.ceil(minFont*6.6+56));$('viewport').style.width=1080*s+'px';$('viewport').style.height=1920*s+'px';$('game').style.setProperty('--min-font',minFont+'px');$('game').style.setProperty('--menu-height',menuHeight+'px');$('game').style.setProperty('--room-scale',(1060-menuHeight)/1060);$('game').dataset.smallScreen=s<.4;$('game').style.zoom=s;$('game').style.transform='none';}
  window.addEventListener('resize',fit);if(window.visualViewport)window.visualViewport.addEventListener('resize',fit);fit();
  function put(id,value){const el=$(id),str=String(value);if(el.textContent!==str)el.textContent=str;}
  function save(){try{localStorage.setItem(KEY,JSON.stringify(game.s));}catch{}}
  function tell(text){put('toast',text);$('toast').classList.add('visible');clearTimeout(toastTimer);toastTimer=setTimeout(()=>$('toast').classList.remove('visible'),2000);}
  function setJobsExpanded(value){jobsExpanded=panel==='jobs'&&value;$('game').dataset.jobsExpanded=jobsExpanded;$('jobs-expand').hidden=panel!=='jobs';$('jobs-expand').setAttribute('aria-expanded',jobsExpanded);$('jobs-expand').setAttribute('aria-label',jobsExpanded?'Collapse jobs':'Expand jobs');$('drawer-close').hidden=panel==='closed'||(panel==='jobs'&&!jobsExpanded);}
  function setPanel(p){if(p!==panel)jobsExpanded=false;panel=p;$('game').dataset.panel=p;$('drawer-body').hidden=p==='closed';$('jobs-view').hidden=p!=='jobs';$('rack-view').hidden=p!=='rack';$('rack-subtitle').hidden=p!=='rack';$('install-view').hidden=p!=='install';$('new-rack-view').hidden=p!=='new-rack';$('drawer-toggle').setAttribute('aria-expanded',p!=='closed');setJobsExpanded(jobsExpanded);put('drawer-title',p==='new-rack'?'NEW RACK':p==='rack'||p==='install'?'SERVER RACK':'JOBS');rackKey='';installKey='';render();}
  function rack(slot=-1,index=activeRack){activeRack=slot>=0?Math.floor(slot/5):index;selectedSlot=slot;setPanel('rack');}
  function openInstall(slot,job=null){installSlot=slot;installJob=job;setPanel('install');}
  $('segments').innerHTML='<span></span>'.repeat(10);
  $('job-list').innerHTML=JOBS.map((j,i)=>`<div class="job" id="job-${i}" data-run="${i}" tabindex="0" role="button" aria-label="Run ${j.name}"><div class="job-actions"><button class="run" data-run="${i}" aria-label="Start ${j.name}">RUN</button><button class="automate" data-automate="${i}" aria-label="Automate ${j.name}"><span>AUTO +</span><b class="auto-price"></b></button></div><div class="job-info"><div class="job-name">${j.name}</div><div class="job-stats"><span class="state"></span><span class="payout"></span></div><div class="job-track" role="progressbar" aria-label="${j.name} progress" aria-valuemin="0" aria-valuemax="100"><span></span></div></div><button class="level" data-level="${i}" aria-label="Level up ${j.name}"><span class="level-label">LEVEL UP</span><b></b></button></div>`).join('');
  function equipmentIcon(type){
    if(!type)return '<span class="empty-equipment">+</span>';
    const common='<defs><linearGradient id="metal-'+type+'" x2="1" y2="1"><stop stop-color="#3b5261"/><stop offset=".45" stop-color="#14232f"/><stop offset="1" stop-color="#050b12"/></linearGradient></defs>';
    const rig='<path d="M18 19 77 8 109 24 49 39Z" fill="#263a49" stroke="#577080"/><path d="M18 19 49 39 49 83 18 62Z" fill="#07121c" stroke="#3d5667"/><path d="M49 39 109 24 109 68 49 83Z" fill="url(#metal-rig)" stroke="#537082"/><path d="m56 46 45-11m-45 22 45-11m-45 22 45-11" stroke="#03090e" stroke-width="7"/><path d="m61 43 13-3m-13 14 13-3m-13 14 13-3" stroke="#00e2f2" stroke-width="3"/><path d="m84 38 14-3m-14 14 14-3m-14 14 14-3" stroke="#325566" stroke-width="2"/>';
    const modem='<path d="M27 48 23 9m78 24 4-25" stroke="#08111c" stroke-width="7"/><path d="M27 48 23 9m78 24 4-25" stroke="#68818e" stroke-width="2"/><path d="M14 51 80 35 115 52 49 70Z" fill="#2b3e4b" stroke="#5c7582"/><path d="M14 51 49 70 49 86 14 67Z" fill="#08131d" stroke="#395363"/><path d="M49 70 115 52 115 68 49 86Z" fill="url(#metal-modem)" stroke="#547080"/><path d="m58 75 5-1m4-1 5-1m4-1 5-1m4-1 5-1m4-1 5-1" stroke="#00e8f7" stroke-width="3"/>';
    return '<svg viewBox="0 0 128 96" aria-hidden="true">'+common+(type==='rig'?rig:modem)+'</svg>';
  }
  function renderRack(){
    const modem=game.modem(),price=1500000*2**((modem?.level||1)-1),key=JSON.stringify([game.s.slots,activeRack,selectedSlot,game.s.money>=price]);if(key===rackKey)return;rackKey=key;
    const previousScroll=$('rack-list').scrollTop;
    $('rack-list').innerHTML=game.s.slots.slice(activeRack*5,activeRack*5+5).map((s,n)=>{
      const i=activeRack*5+n,selected=selectedSlot===i,title=!s?'Empty shelf':s.type==='rig'?'Automation rig':'Fiber modem';
      let info='',action='';
      if(!s)action='<span class="rack-status install">INSTALL</span>';
      else if(s.type==='rig'){
        info='<small>'+JOBS[s.job].name+'</small>'+(selected?'<span class="rack-benefit">AUTO REPEAT</span>':'');
        action='<span class="rack-status auto">AUTO</span>';
      }else{
        const bonus=20+(s.level-1)*4,cost=1500000*2**(s.level-1);
        info='<span class="equipment-level">LV '+s.level+(selected&&s.level<5?' <b>→ '+(s.level+1)+'</b>':'')+'</span>';
        if(selected){
          info+='<span class="rack-benefit">ALL JOB SPEED <b>+'+bonus+'%'+(s.level<5?' → +'+(bonus+4)+'%':'')+'</b></span>';
          if(s.level<5)info+='<span class="rack-benefit">COMFORT <b>+2</b></span>';
          action=s.level>=5?'<span class="rack-status capped">MAX LEVEL</span>':'<button class="rack-upgrade action" data-upgrade '+(game.s.money<cost?'disabled':'')+'>UPGRADE<b>'+cash(cost)+'</b></button>';
        }else action='<span class="rack-status inspect">UPGRADE</span>';
      }
      return '<div class="rack-slot '+(selected?'selected ':'')+(s?.type||'empty')+'"><button class="rack-row" data-slot="'+i+'" aria-label="Shelf '+(n+1)+': '+title+'" aria-expanded="'+selected+'"><span class="slot-number">0'+(n+1)+'</span><span class="equipment-icon">'+equipmentIcon(s?.type)+'</span><span class="rack-name"><span class="equipment-title">'+title+'</span>'+info+'</span></button>'+action+'</div>';
    }).join('');
    $('rack-list').scrollTop=previousScroll;
    if(selectedSlot>=0)$('rack-list').children[selectedSlot%5]?.scrollIntoView({block:'nearest'});
  }
  function renderInstall(){
    const options=JOBS.map((j,i)=>({type:'rig',job:i,name:j.name+' rig',cost:game.automationCost(i),blocked:game.auto(i)?'INSTALLED':!game.unlocked(i)?'REP LV '+(i+1):null}));
    if(installJob===null)options.push({type:'modem',job:0,name:'Fiber modem',cost:15000,blocked:game.modem()?'INSTALLED':null});
    const shown=installJob===null?options:options.filter(x=>x.type==='rig'&&x.job===installJob);
    const key=JSON.stringify([installSlot,installJob,shown.map(x=>[x.blocked,game.s.money>=x.cost])]);if(key===installKey)return;installKey=key;
    put('install-title','RACK 0'+(Math.floor(installSlot/5)+1)+' / SHELF 0'+(installSlot%5+1));
    $('install-list').innerHTML=shown.map(x=>`<div class="install-item"><span>${x.name}<small>${x.type==='modem'?'+20% JOB SPEED':'AUTO REPEAT'}</small></span><button class="action" data-install="${x.type}" data-job="${x.job}" ${x.blocked||game.s.money<x.cost?'disabled':''}>${x.blocked||'INSTALL'}${x.blocked?'':`<b>${cash(x.cost)}</b>`}</button></div>`).join('');
  }
  function render(now=Date.now()){
    const s=game.s;put('money',cash(s.money));$('money').title=fullCash(s.money);put('income','+'+cash(game.rate(now))+'/s');
    const reputation=game.repStatus();put('rep-level','REP LV '+reputation.level);put('rep',reputation.progress+' / '+reputation.required);$('rep-fill').style.width=(reputation.progress/reputation.required*100)+'%';$('rep-fill').parentElement.setAttribute('aria-valuenow',reputation.progress);$('rep-fill').parentElement.setAttribute('aria-valuemax',reputation.required);$('rep-fill').parentElement.setAttribute('aria-label','Reputation level '+reputation.level);put('comfort',game.comfort(now));
    const running=s.jobs.filter(j=>j.running).length,auto=s.slots.filter(x=>x?.type==='rig'&&game.unlocked(x.job)).length;
    const expanded=game.rackCount()>1;$('game').dataset.hasExpansion=expanded;$('add-rack-target').hidden=expanded;$('new-rack-target').hidden=!expanded;$('rack-tabs').hidden=!expanded||panel!=='rack';document.querySelectorAll('[data-rack]').forEach(b=>b.setAttribute('aria-pressed',Number(b.dataset.rack)===activeRack));put('drawer-meta',panel==='new-rack'?'5 EMPTY SHELVES':panel==='rack'||panel==='install'?`0${activeRack+1} · ${s.slots.slice(activeRack*5,activeRack*5+5).filter(Boolean).length} / 5 SLOTS`:`${running} RUNNING • ${auto} AUTO`);put('rack-price',cash(game.rackPrice()));$('buy-rack').disabled=expanded||s.money<game.rackPrice();
    const visibleTaps=now<barFullUntil?10:s.taps;
    put('tap-value','+'+cash(game.tapValue(now)));put('full-value','+'+cash(game.tapValue(now)*15));put('tap-count',visibleTaps+' / 10');$('segments').setAttribute('aria-valuenow',visibleTaps);[...$('segments').children].forEach((el,i)=>el.classList.toggle('filled',i<visibleTaps));put('speed','×'+game.speed(now).toFixed(2));put('boost-time',s.boosts.length?'• '+Math.max(1,Math.ceil((Math.min(...s.boosts)-now)/1000))+'s':'');
    document.querySelectorAll('[data-buy]').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.buy)===String(s.buyMode)));
    s.jobs.forEach((j,i)=>{
      const el=$('job-'+i),locked=!game.unlocked(i);el.classList.toggle('locked',locked);el.setAttribute('aria-disabled',locked);
      const run=el.querySelector('.run');run.textContent=locked?'LOCKED':game.auto(i)?'AUTO':j.running?'RUNNING':'RUN';run.classList.toggle('auto',game.auto(i));run.disabled=locked||j.running;
      el.querySelector('.state').textContent=locked?'REP LV '+(i+1):`LV ${j.level} · ${j.running?Math.ceil((JOBS[i].seconds-j.progress)/game.speed(now))+'s':'Ready'}`;
      el.querySelector('.payout').textContent=cash(game.payout(i,now))+' / run';const progress=100*j.progress/JOBS[i].seconds;el.querySelector('.job-track>span').style.width=progress+'%';el.querySelector('.job-track').setAttribute('aria-valuenow',Math.floor(progress));
      const level=el.querySelector('.level'),q=game.quote(i,s.buyMode);level.disabled=!q.affordable;level.querySelector('.level-label').textContent=j.level>=25?'MAX LEVEL':'LEVEL UP'+(q.count?' +'+q.count:'');level.querySelector('b').textContent=j.level>=25?'25 / 25':cash(q.cost||game.cost(i));level.title=q.count+' levels · '+fullCash(q.cost)+' · Level cap 25';const autoBtn=el.querySelector('.automate');autoBtn.hidden=locked||game.auto(i);autoBtn.querySelector('.auto-price').textContent=cash(game.automationCost(i));const full=s.slots.every(Boolean);autoBtn.disabled=locked||full||s.money<game.automationCost(i);autoBtn.title=full?'RACK FULL · '+s.slots.length+' / '+s.slots.length:'AUTO + · '+fullCash(game.automationCost(i));autoBtn.setAttribute('aria-label','Automate '+JOBS[i].name+' for '+fullCash(game.automationCost(i))+(full?' · Rack full':''));
    });
    if(panel==='rack')renderRack();if(panel==='install')renderInstall();
  }
  function hack(){if(!$('settings-overlay').hidden)return;const {amount,bonus}=game.hack();const reward=document.createElement('span');reward.className='reward'+(bonus?' bonus':'');reward.textContent='+'+cash(bonus||amount);reward.style.marginLeft=(Math.random()*60-30)+'px';$('rewards').append(reward);setTimeout(()=>reward.remove(),950);if(bonus){barFullUntil=Date.now()+800;$('hack').classList.add('charged');setTimeout(()=>$('hack').classList.remove('charged'),200);}render();save();}
  function closeSettings(){$('settings-overlay').hidden=true;$('settings').focus();}
  $('hack').onclick=hack;
  $('buy-rack').onclick=()=>{if(game.buyRack()){rack(-1,1);save();}};
  $('drawer-toggle').onclick=()=>setPanel(panel==='closed'?'jobs':'closed');$('drawer-close').onclick=()=>setPanel('closed');
  $('jobs-expand').onclick=()=>setJobsExpanded(!jobsExpanded);
  // Compact jobs accept an upward drag; the expanded list keeps native scrolling.
  let swipe=null,suppressClickUntil=0;
  $('dock').addEventListener('pointerdown',e=>{
    if(!e.isPrimary||e.button!==0||panel!=='jobs')return;
    const header=e.target.closest('.drawer-header');
    if(!header&&(jobsExpanded||!e.target.closest('#jobs-view')))return;
    swipe={id:e.pointerId,x:e.clientX,y:e.clientY,expanded:jobsExpanded};
    e.target.setPointerCapture(e.pointerId);
  });
  $('dock').addEventListener('pointermove',e=>{
    if(!swipe||swipe.id!==e.pointerId)return;
    const dx=e.clientX-swipe.x,dy=e.clientY-swipe.y;
    if(Math.abs(dy)>12&&Math.abs(dy)>Math.abs(dx))e.preventDefault();
  });
  $('dock').addEventListener('pointerup',e=>{
    if(!swipe||swipe.id!==e.pointerId)return;
    const dx=e.clientX-swipe.x,dy=e.clientY-swipe.y,wasExpanded=swipe.expanded;swipe=null;
    if(Math.abs(dy)<35||Math.abs(dy)<Math.abs(dx)*1.2)return;
    suppressClickUntil=Date.now()+400;
    if(dy<0&&!wasExpanded)setJobsExpanded(true);
    else if(dy>0&&wasExpanded)setJobsExpanded(false);
  });
  $('dock').addEventListener('pointercancel',()=>{swipe=null;});
  $('dock').addEventListener('click',e=>{if(Date.now()<suppressClickUntil){e.preventDefault();e.stopImmediatePropagation();}},true);
  $('install-back').onclick=()=>rack();
  $('settings').onclick=()=>{$('settings-overlay').hidden=false;$('reset-confirm').hidden=true;$('resume').focus();};$('settings-close').onclick=closeSettings;$('resume').onclick=closeSettings;
  $('reset').onclick=()=>{$('reset-confirm').hidden=false;$('reset-yes').focus();};$('reset-no').onclick=()=>$('reset-confirm').hidden=true;
  $('reset-yes').onclick=()=>{game=new Game();barFullUntil=0;activeRack=0;selectedSlot=-1;rackKey='';installKey='';setPanel('closed');closeSettings();save();};
  document.addEventListener('click',e=>{
    const el=e.target.closest('button,[data-run]');if(!el||el.disabled)return;
    if(el.dataset.buy!==undefined){game.s.buyMode=el.dataset.buy==='max'?'max':Number(el.dataset.buy);render();save();return;}
    if(el.dataset.level!==undefined){if(game.level(+el.dataset.level,game.s.buyMode)){render();save();}return;}
    if(el.dataset.automate!==undefined){if(game.automate(+el.dataset.automate)){render();save();}return;}
    if(el.dataset.run!==undefined){game.tick();if(game.run(+el.dataset.run)){render();save();}return;}
    if(el.dataset.rack!==undefined){rack(-1,+el.dataset.rack);return;}
    if(el.dataset.room){if(el.dataset.room==='new-rack'){if(game.rackCount()<2)setPanel('new-rack');else rack(-1,1);}else if(el.dataset.room==='rack-two')rack(-1,1);else rack(-1,0);return;}
    if(el.dataset.slot!==undefined){const slot=+el.dataset.slot;if(!game.s.slots[slot]){openInstall(slot);return;}selectedSlot=selectedSlot===slot?-1:slot;renderRack();return;}
        if(el.dataset.install){if(game.install(installSlot,el.dataset.install,+el.dataset.job)){rack(installSlot);save();}return;}
    if(el.hasAttribute('data-upgrade')){if(game.upgradeModem()){render();save();}return;}
    if(el.hasAttribute('data-jobs'))setPanel('jobs');
  });
  document.addEventListener('keydown',e=>{
    if(!$('settings-overlay').hidden){if(e.key==='Escape'){e.preventDefault();closeSettings();}if(e.key==='Tab'){const buttons=[...$('settings-overlay').querySelectorAll('button')].filter(x=>!x.closest('[hidden]'));const first=buttons[0],last=buttons.at(-1);if(e.shiftKey&&document.activeElement===first){e.preventDefault();last.focus();}else if(!e.shiftKey&&document.activeElement===last){e.preventDefault();first.focus();}}return;}
    if(e.key==='Escape'){if(jobsExpanded)setJobsExpanded(false);else setPanel('closed');return;}
    if(e.target.classList.contains('job')&&(e.key==='Enter'||e.code==='Space')){e.preventDefault();if(!e.repeat)e.target.click();return;}
    if(e.code==='Space'&&e.target===document.body){e.preventDefault();if(!e.repeat){hack();$('hack').classList.add('pressed');}}
  });
  document.addEventListener('keyup',e=>{if(e.code==='Space')$('hack').classList.remove('pressed');});
  document.addEventListener('visibilitychange',()=>{if(document.hidden){game.tick();save();}else game.s.last=Date.now();});window.addEventListener('pagehide',save);
  setInterval(()=>{if(document.hidden)return;const now=Date.now();game.tick(now);render(now);if(now-lastSaved>2000){save();lastSaved=now;}},100);
  render();save();
})();
