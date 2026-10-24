(() => {
  'use strict';
  const {Game,JOBS}=RootAccess;
  const $=id=>document.getElementById(id),KEY='root-access-mobile-v2';
  let game,raw,savedAt;try{raw=JSON.parse(localStorage.getItem(KEY));savedAt=raw?.last;game=new Game(raw);game.resumeOffline(savedAt);}catch{game=new Game();}
  let panel='closed',barFullUntil=0,activeRack=0,selectedSlot=-1,installSlot=2,installJob=null,installOrigin='rack',installType=null,lastSaved=0,rackKey='',installKey='',toastTimer;
  const cash=n=>'$'+(n>=1e6?(n/1e6).toFixed(2).replace(/0$/,'')+'M':n>=100000?(n/1000).toFixed(1)+'K':n>=1000?Math.floor(n).toLocaleString('en-US'):n.toFixed(n<100?1:0));
  const fullCash=n=>'$'+n.toLocaleString('en-US',{maximumFractionDigits:2});
  const afkCash=n=>'$'+n.toLocaleString('en-US',{minimumFractionDigits:2,maximumFractionDigits:2});
  function fit(){const s=Math.min(window.innerWidth/1080,window.innerHeight/1920),minFont=Math.ceil(14/s*100)/100;$('viewport').style.width=1080*s+'px';$('viewport').style.height=1920*s+'px';$('game').style.setProperty('--min-font',minFont+'px');$('game').style.setProperty('--price-font',Math.max(12,Math.round(30*s))/s+'px');$('game').dataset.smallScreen=s<.4;$('game').style.zoom=s;$('game').style.transform='none';}
  window.addEventListener('resize',fit);if(window.visualViewport)window.visualViewport.addEventListener('resize',fit);fit();
  function put(id,value){const el=$(id),str=String(value);if(el.textContent!==str)el.textContent=str;}
  function save(){try{localStorage.setItem(KEY,JSON.stringify(game.s));}catch{}}
  function tell(text){put('toast',text);$('toast').classList.add('visible');clearTimeout(toastTimer);toastTimer=setTimeout(()=>$('toast').classList.remove('visible'),2000);}
  const timer=ms=>{const t=Math.max(0,Math.floor(ms/1000));return [Math.floor(t/3600),Math.floor(t/60)%60,t%60].map(n=>String(n).padStart(2,'0')).join(':');};
  const shortTimer=ms=>timer(Math.ceil(ms/1000)*1000).slice(3);
  let offlineFocus=null,adRemaining=0,adLast=0,collectMessage='',collectMessageUntil=0;
  function renderAfk(now=Date.now()){
    const status=game.afkStatus();$('afk-ready').hidden=!status.ready;
    $('afk-open').title=status.ready?'AFK earnings ready: '+fullCash(status.amount):'AFK earnings';
    if($('offline-overlay').hidden)return;
    put('afk-timer',timer(status.elapsed));put('offline-jobs',status.jobs.toLocaleString('en-US'));
    const amount=afkCash(status.amount);put('offline-credits',amount);put('afk-collect-amount',amount);put('afk-double-amount',afkCash(status.amount*2));
    put('offline-duration','Your rigs earn while the game is closed or in the background.');
    put('afk-next',status.capped?'Storage full': 'Next AFK update in '+shortTimer(status.next));
    $('afk-minute-fill').style.width=(status.capped?100:status.elapsed%60000/600)+'%';
    $('offline-collect').disabled=!status.ready||adRemaining>0;$('afk-double').disabled=!status.ready||adRemaining>0;
    const hasRig=game.s.jobs.some((_,i)=>game.auto(i)&&game.unlocked(i));
    put('afk-hint',now<collectMessageUntil?collectMessage:status.capped?'8-hour limit reached · Collect to continue':!hasRig?'Install an AUTO rig to earn while away':status.ready?'Paused while playing · Collect whenever you like':'Paused while playing · Unlocks after '+shortTimer(Math.max(0,60000-status.elapsed))+' AFK');
    if(adRemaining>0){
      adRemaining=Math.max(0,adRemaining-(now-adLast));adLast=now;
      put('afk-ad-countdown',Math.ceil(adRemaining/1000)+'s');
      if(!adRemaining){$('afk-ad').hidden=true;$('afk-actions').hidden=false;claimAfk(2);}
    }
  }
  function showOffline(){
    if(!$('settings-overlay').hidden)closeSettings();
    if($('offline-overlay').hidden)offlineFocus=document.activeElement;
    $('offline-overlay').hidden=false;
    [...$('game').children].forEach(el=>{if(el!==$('offline-overlay'))el.inert=true;});
    renderAfk();$('afk-close').focus();
  }
  function cancelAd(){adRemaining=0;$('afk-ad').hidden=true;$('afk-actions').hidden=false;renderAfk();}
  function closeAfk(){cancelAd();$('offline-overlay').hidden=true;[...$('game').children].forEach(el=>el.inert=false);if(offlineFocus?.isConnected&&offlineFocus!==document.body)offlineFocus.focus();else $('afk-open').focus();}
  function claimAfk(multiplier){const reward=game.collectAfk(multiplier);if(!reward)return;save();collectMessage='Collected '+fullCash(reward.amount)+(multiplier===2?' · 2× reward':'');collectMessageUntil=Date.now()+5000;render();$('afk-close').focus();}
  $('afk-open').onclick=showOffline;$('afk-close').onclick=closeAfk;
  $('offline-collect').onclick=()=>claimAfk(1);
  $('afk-double').onclick=()=>{if(!game.afkStatus().ready||adRemaining)return;adRemaining=5000;adLast=Date.now();$('afk-actions').hidden=true;$('afk-ad').hidden=false;put('afk-ad-countdown','5s');$('afk-ad-cancel').focus();};
  $('afk-ad-cancel').onclick=()=>{cancelAd();$('afk-double').focus();};
  function lowerJobs(){setPanel('closed');}
  function setPanel(p){panel=p;$('game').dataset.panel=p;$('drawer-body').hidden=p==='closed';$('jobs-view').hidden=p!=='jobs';$('rack-view').hidden=p!=='rack';$('rack-subtitle').hidden=p!=='rack'&&p!=='new-rack';$('install-view').hidden=p!=='install';$('new-rack-view').hidden=p!=='new-rack';$('drawer-toggle').setAttribute('aria-expanded',p!=='closed');$('jobs-collapse').hidden=p!=='jobs';$('drawer-close').hidden=p==='closed'||p==='jobs';put('drawer-title',p==='new-rack'?'EXPAND SETUP':p==='rack'||p==='install'?'SERVER RACK':'JOBS');rackKey='';installKey='';render();}
  function rack(slot=-1,index=activeRack){activeRack=slot>=0?Math.floor(slot/5):index;selectedSlot=slot;setPanel('rack');}
  function openInstall(slot,job=null,origin='rack',type=null){installSlot=slot;installJob=job;installOrigin=origin;installType=type;activeRack=Math.floor(slot/5);$('install-back').setAttribute('aria-label',origin==='new-rack'?'Back to expansion options':'Back to rack');setPanel('install');}

  $('job-list').innerHTML=JOBS.map((j,i)=>`<div class="job" id="job-${i}" data-run="${i}" tabindex="0" role="button" aria-label="Run ${j.name}"><div class="job-actions"><button class="run" data-run="${i}" aria-label="Start ${j.name}">RUN</button><button class="automate" data-automate="${i}" aria-label="Automate ${j.name}"><span>AUTO +</span><b class="auto-price"></b></button></div><div class="job-info"><div class="job-name">${j.name}</div><div class="job-stats"><span class="state"></span><span class="payout"></span></div><div class="job-track" role="progressbar" aria-label="${j.name} progress" aria-valuemin="0" aria-valuemax="100"><span></span></div></div><button class="level" data-level="${i}" aria-label="Level up ${j.name}"><span class="level-label">LEVEL UP</span><b></b></button></div>`).join('');
  $('job-rings').innerHTML=JOBS.map((j,i)=>`<span class="job-ring" id="job-ring-${i}" role="img"><svg viewBox="0 0 100 100" aria-hidden="true"><circle class="ring-track" cx="50" cy="50" r="44"/><circle class="ring-fill" cx="50" cy="50" r="44" pathLength="100"/></svg><span>${j.code.split('_')[0]}</span></span>`).join('');
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
  $('expansion-rig-icon').innerHTML=equipmentIcon('rig');
  $('expansion-modem-icon').innerHTML=equipmentIcon('modem');
  $('expansion-rack-icon').innerHTML='<svg viewBox="0 0 128 128"><path d="M29 12h70v104H29Z" fill="#102330" stroke="#74808b" stroke-width="3"/><path d="M38 24h52v18H38Zm0 28h52v18H38Zm0 28h52v18H38Z" fill="#1e3542" stroke="#56707e" stroke-width="2"/><path d="M44 33h8m-8 28h8m-8 28h8" stroke="#e8c77a" stroke-width="3"/><path d="M25 116h78" stroke="#74808b" stroke-width="4"/></svg>';
  function renderExpansion(){
    const free=game.s.slots.findIndex(slot=>!slot),available=JOBS.map((_,i)=>i).filter(i=>!game.auto(i));
    put('expansion-rig-price',available.length?cash(Math.min(...available.map(i=>game.automationCost(i)))):'ALL INSTALLED');
    put('expansion-rig-label',available.length?'STARTING AT':'AUTOMATION');
    $('choose-rig').disabled=free<0||!available.length;
    $('buy-modem').disabled=free<0||!!game.modem()||game.s.money<15000;
    put('buy-modem',game.modem()?'INSTALLED':free<0?'RACK FULL':'INSTALL ↗');
    put('expansion-note',free<0?'Your rack is full. Add a new rack to install more equipment.':'Rigs and modems use one empty shelf in your current setup.');
  }
  $('choose-rig').onclick=()=>{const slot=game.s.slots.findIndex(s=>!s);if(slot>=0)openInstall(slot,null,'new-rack','rig');};
  $('buy-modem').onclick=()=>{const slot=game.s.slots.findIndex(s=>!s);if(slot>=0&&game.install(slot,'modem')){rack(slot);save();}};
  function renderInstall(){
    const options=JOBS.map((j,i)=>({type:'rig',job:i,name:j.name+' rig',cost:game.automationCost(i),blocked:game.auto(i)?'INSTALLED':!game.unlocked(i)?'REP LV '+(i+1):null}));
    if(installJob===null)options.push({type:'modem',job:0,name:'Fiber modem',cost:15000,blocked:game.modem()?'INSTALLED':null});
    const shown=(installJob===null?options:options.filter(x=>x.type==='rig'&&x.job===installJob)).filter(x=>!installType||x.type===installType);
    const key=JSON.stringify([installSlot,installJob,shown.map(x=>[x.blocked,game.s.money>=x.cost])]);if(key===installKey)return;installKey=key;
    put('install-title','RACK 0'+(Math.floor(installSlot/5)+1)+' / SHELF 0'+(installSlot%5+1));
    $('install-list').innerHTML=shown.map(x=>`<div class="install-item"><span>${x.name}<small>${x.type==='modem'?'+20% JOB SPEED':'AUTO REPEAT'}</small>${x.blocked==='INSTALLED'?'':`<strong class="install-price">${cash(x.cost)}</strong>`}</span><button class="action" data-install="${x.type}" data-job="${x.job}" aria-label="${x.blocked||('Install '+x.name+' for '+fullCash(x.cost))}" ${x.blocked||game.s.money<x.cost?'disabled':''}>${x.blocked||'INSTALL'}</button></div>`).join('');
  }
  function render(now=Date.now()){
    const s=game.s;put('money',cash(s.money));$('money').title=fullCash(s.money);put('income','+'+cash(game.rate(now))+'/s');
    const reputation=game.repStatus();put('rep-level','REP LV '+reputation.level);put('rep',reputation.progress+' / '+reputation.required);$('rep-fill').style.width=(reputation.progress/reputation.required*100)+'%';$('rep-fill').parentElement.setAttribute('aria-valuenow',reputation.progress);$('rep-fill').parentElement.setAttribute('aria-valuemax',reputation.required);$('rep-fill').parentElement.setAttribute('aria-label','Reputation level '+reputation.level);put('comfort',game.comfort(now));
    const running=s.jobs.filter(j=>j.running).length,auto=s.slots.filter(x=>x?.type==='rig'&&game.unlocked(x.job)).length;
    const jobsHeader=panel==='closed'||panel==='jobs';$('drawer-meta').hidden=jobsHeader;$('job-rings').hidden=!jobsHeader;
    $('upgrade-ready').hidden=!jobsHeader||!s.jobs.some((_,i)=>game.quote(i,1).affordable);
    const expanded=game.rackCount()>1;$('game').dataset.hasExpansion=expanded;$('add-rack-target').hidden=expanded;$('new-rack-target').hidden=!expanded;$('rack-tabs').hidden=!expanded||panel!=='rack';document.querySelectorAll('[data-rack]').forEach(b=>b.setAttribute('aria-pressed',Number(b.dataset.rack)===activeRack));put('drawer-meta',panel==='new-rack'?'SPACE & EQUIPMENT':panel==='rack'||panel==='install'?`0${activeRack+1} · ${s.slots.slice(activeRack*5,activeRack*5+5).filter(Boolean).length} / 5 SLOTS`:`${running} RUNNING • ${auto} AUTO`);put('rack-price',cash(game.rackPrice()));$('buy-rack').disabled=expanded||s.money<game.rackPrice();
    const visibleTaps=now<barFullUntil?10:s.taps;
    put('tap-value','+'+cash(game.tapValue(now)));put('full-value','+'+cash(game.tapValue(now)*15));$('hack').style.setProperty('--hack-fill',visibleTaps/10);$('hack').classList.toggle('empty',visibleTaps===0);$('hack').classList.toggle('charged',visibleTaps===10);put('speed','×'+game.speed(now).toFixed(2));put('hack-status',visibleTaps+' of 10 taps. Job speed '+game.speed(now).toFixed(2)+' times.');
    document.querySelectorAll('[data-buy]').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.buy)===String(s.buyMode)));
    s.jobs.forEach((j,i)=>{
      const el=$('job-'+i),locked=!game.unlocked(i);el.classList.toggle('locked',locked);el.setAttribute('aria-disabled',locked);
      const ring=$('job-ring-'+i),active=!locked&&j.running,ringProgress=Math.min(100,100*j.progress/JOBS[i].seconds);
      ring.classList.toggle('running',active);ring.classList.toggle('locked',locked);ring.querySelector('.ring-fill').style.strokeDashoffset=100-(active?ringProgress:0);ring.querySelector('span').textContent=locked?'?':JOBS[i].code.split('_')[0];
      const ringLabel=JOBS[i].name+': '+(locked?'locked':active?Math.floor(ringProgress)+'% complete':'ready');ring.setAttribute('aria-label',ringLabel);ring.title=ringLabel;
      const run=el.querySelector('.run');run.textContent=locked?'LOCKED':game.auto(i)?'AUTO':j.running?'RUNNING':'RUN';run.classList.toggle('auto',game.auto(i));run.disabled=locked||j.running;
      el.querySelector('.state').textContent=locked?'REP LV '+(i+1):`LV ${j.level} · ${j.running?Math.ceil((JOBS[i].seconds-j.progress)/game.speed(now))+'s':'Ready'}`;
      el.querySelector('.payout').textContent=cash(game.payout(i,now))+' / run';const progress=100*j.progress/JOBS[i].seconds;el.querySelector('.job-track>span').style.width=progress+'%';el.querySelector('.job-track').setAttribute('aria-valuenow',Math.floor(progress));
      const level=el.querySelector('.level'),q=game.quote(i,s.buyMode);level.disabled=!q.affordable;level.querySelector('.level-label').textContent=j.level>=25?'MAX LEVEL':'LEVEL UP'+(q.count?' +'+q.count:'');level.querySelector('b').textContent=j.level>=25?'25 / 25':cash(q.cost||game.cost(i));level.title=q.count+' levels · '+fullCash(q.cost)+' · Level cap 25';const autoBtn=el.querySelector('.automate');autoBtn.hidden=locked||game.auto(i);autoBtn.querySelector('.auto-price').textContent=cash(game.automationCost(i));const full=s.slots.every(Boolean);autoBtn.disabled=locked||full||s.money<game.automationCost(i);autoBtn.title=full?'RACK FULL · '+s.slots.length+' / '+s.slots.length:'AUTO + · '+fullCash(game.automationCost(i));autoBtn.setAttribute('aria-label','Automate '+JOBS[i].name+' for '+fullCash(game.automationCost(i))+(full?' · Rack full':''));
    });
    if(panel==='rack')renderRack();if(panel==='install')renderInstall();if(panel==='new-rack')renderExpansion();renderAfk(now);
  }
  function hack(){if(!$('settings-overlay').hidden||!$('offline-overlay').hidden)return;barFullUntil=0;const {amount,bonus}=game.hack();const reward=document.createElement('span');reward.className='reward'+(bonus?' bonus':'');reward.textContent='+'+cash(bonus||amount);reward.style.marginLeft=(Math.random()*60-30)+'px';$('rewards').append(reward);setTimeout(()=>reward.remove(),950);if(bonus)barFullUntil=Date.now()+450;render();save();}
  function closeSettings(){$('settings-overlay').hidden=true;$('settings').focus();}
  $('hack').onclick=hack;
  $('buy-rack').onclick=()=>{if(game.buyRack()){rack(-1,1);save();}};
  $('drawer-toggle').onclick=()=>setPanel(panel==='closed'?'jobs':'closed');$('drawer-close').onclick=()=>setPanel('closed');
  $('jobs-collapse').onclick=lowerJobs;
  // Jobs has one open state; a downward header swipe closes it.
  let swipe=null,suppressClickUntil=0;
  $('dock').addEventListener('pointerdown',e=>{
    if(!e.isPrimary||e.button!==0||panel!=='jobs')return;
    if(!e.target.closest('.drawer-header'))return;
    swipe={id:e.pointerId,x:e.clientX,y:e.clientY};
    e.target.setPointerCapture(e.pointerId);
  });
  $('dock').addEventListener('pointermove',e=>{
    if(!swipe||swipe.id!==e.pointerId)return;
    const dx=e.clientX-swipe.x,dy=e.clientY-swipe.y;
    if(Math.abs(dy)>12&&Math.abs(dy)>Math.abs(dx))e.preventDefault();
  });
  $('dock').addEventListener('pointerup',e=>{
    if(!swipe||swipe.id!==e.pointerId)return;
    const dx=e.clientX-swipe.x,dy=e.clientY-swipe.y;swipe=null;
    if(Math.abs(dy)<35||Math.abs(dy)<Math.abs(dx)*1.2)return;
    suppressClickUntil=Date.now()+400;
    if(dy>0)lowerJobs();
  });
  $('dock').addEventListener('pointercancel',()=>{swipe=null;});
  $('dock').addEventListener('click',e=>{if(Date.now()<suppressClickUntil){e.preventDefault();e.stopImmediatePropagation();}},true);
  $('install-back').onclick=()=>installOrigin==='new-rack'?setPanel('new-rack'):rack();
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
    if(!$('offline-overlay').hidden){
      if(e.key==='Escape'){e.preventDefault();closeAfk();}
      if(e.key==='Tab'){const buttons=[...$('offline-overlay').querySelectorAll('button')].filter(b=>!b.disabled&&!b.closest('[hidden]')),first=buttons[0],last=buttons.at(-1);if(e.shiftKey&&document.activeElement===first){e.preventDefault();last.focus();}else if(!e.shiftKey&&document.activeElement===last){e.preventDefault();first.focus();}}return;
    }
    if(!$('settings-overlay').hidden){if(e.key==='Escape'){e.preventDefault();closeSettings();}if(e.key==='Tab'){const buttons=[...$('settings-overlay').querySelectorAll('button')].filter(x=>!x.closest('[hidden]'));const first=buttons[0],last=buttons.at(-1);if(e.shiftKey&&document.activeElement===first){e.preventDefault();last.focus();}else if(!e.shiftKey&&document.activeElement===last){e.preventDefault();first.focus();}}return;}
    if(e.key==='Escape'){lowerJobs();return;}
    if(e.target.classList.contains('job')&&(e.key==='Enter'||e.code==='Space')){e.preventDefault();if(!e.repeat)e.target.click();return;}
    if(e.code==='Space'&&e.target===document.body){e.preventDefault();if(!e.repeat){hack();$('hack').classList.add('pressed');}}
  });
  document.addEventListener('keyup',e=>{if(e.code==='Space')$('hack').classList.remove('pressed');});
  document.addEventListener('visibilitychange',()=>{if(document.hidden){game.tick();save();}else{game.resumeOffline(game.s.last);adLast=Date.now();save();render();}});window.addEventListener('pagehide',save);
  window.addEventListener('pageshow',e=>{if(e.persisted&&!document.hidden){game.resumeOffline(game.s.last);adLast=Date.now();save();render();}});
  setInterval(()=>{if(document.hidden)return;const now=Date.now();game.tick(now);render(now);if(now-lastSaved>2000){save();lastSaved=now;}},100);
  render();save();
})();
