(function(root){
 'use strict';
 root.RootBases={create(api){
  const $=id=>document.getElementById(id), network=new RootBasesNetwork(api.game(),RootAccess.Game,RootAccess.JOBS);
  const homes=[['First hideout','assets/room-night.png'],['City apartment','assets/bases-preview/base-02.png'],['Open-plan loft','assets/bases-preview/base-03.png'],['Undiscovered base',null],['Undiscovered base',null]];
  const entry=document.createElement('button');entry.id='bases-open';entry.className='afk-launch';entry.setAttribute('aria-label','Open operation bases');entry.setAttribute('aria-controls','bases-panel');entry.innerHTML='<svg viewBox="0 0 24 24" aria-hidden="true"><path d="m3 10 9-7 9 7M5 9v12h14V9M10 21v-7h4v7"/></svg><span>BASES</span>';
  $('afk-open').hidden=true;$('afk-open').after(entry);
  const panel=document.createElement('section');panel.id='bases-panel';panel.hidden=true;panel.setAttribute('aria-labelledby','bases-title');$('shop-panel').after(panel);
  panel.innerHTML=`<header class="shop-header"><h1 id="bases-title">OPERATION BASES</h1><button id="bases-close" aria-label="Close bases">×</button></header>
  <div class="bases-study"><span>PREVIEW · UNLOCKED</span><div>${[1,2,3].map(n=>`<button data-base-stage="${n}">${n} ${n===1?'BASE':'BASES'}</button>`).join('')}</div></div>
  <div class="bases-network"><div><span>OTHER BASES</span><span id="bases-online"></span></div><strong id="bases-rate"></strong><small> /s</small><div class="bases-delivery"><span>AUTO → WALLET</span><b id="bases-paid"></b></div><p>Session income · Paid automatically</p></div>
  <div class="bases-list" id="bases-list" tabindex="0" aria-label="Your bases"></div><footer class="shop-footer"><span>Automation stays online when you switch.</span><span>Scroll ↓</span></footer>`;
  const cards=homes.map((h,i)=>{const card=document.createElement('article');card.className='bases-card';card.innerHTML=`<div class="bases-thumb">${h[1]?`<img src="${h[1]}" alt="${h[0]}" draggable="false">`:'<div class="bases-placeholder"></div>'}<span class="bases-lock">♙</span></div><div class="bases-info"><div><h2>BASE 0${i+1}</h2><small class="bases-current">CURRENT</small></div><p>${h[0]}</p><strong class="bases-card-rate"></strong><button class="bases-enter" aria-label="Enter base ${i+1}">ENTER →</button><span class="bases-locked">LOCKED</span></div><footer><span class="bases-auto"></span><b class="bases-card-paid"></b></footer>`;card.querySelector('.bases-lock').innerHTML='<svg viewBox="0 0 24 28" aria-hidden="true"><rect x="4" y="12" width="16" height="13" rx="2"/><path d="M7 12V8a5 5 0 0 1 10 0v4M12 17v4"/></svg>';card.querySelector('button').onclick=()=>{switchTo(i);close();api.tell('BASE 0'+(i+1)+' · Other bases stay online');};$('bases-list').append(card);return card;});
  const money=n=>'$'+n.toLocaleString('en-US',{minimumFractionDigits:2,maximumFractionDigits:2});
  function switchTo(i){if(!network.visit(i))return;api.setGame(network.game);updateRoom();api.render();}
  function updateRoom(){const image=document.querySelector('.room-art');image.src=homes[network.current][1];image.alt=homes[network.current][0];$('game').dataset.base=network.current+1;}
  function render(){
   if(panel.hidden)return;
   panel.style.bottom=($('dock').offsetHeight+108)+'px';
   $('bases-online').textContent=(network.unlocked-1)+' REMOTE';$('bases-rate').textContent='+'+money(network.remoteRate);$('bases-paid').textContent='+'+money(network.remotePaid);
   panel.querySelectorAll('[data-base-stage]').forEach(b=>b.setAttribute('aria-pressed',+b.dataset.baseStage===network.unlocked));
   cards.forEach((c,i)=>{const unlocked=i<network.unlocked,here=i===network.current,g=network.games[i];c.classList.toggle('locked',!unlocked);c.classList.toggle('current',here);c.querySelector('.bases-current').hidden=!here;c.querySelector('.bases-lock').hidden=unlocked;c.querySelector('.bases-locked').hidden=unlocked;c.querySelector('.bases-enter').hidden=!unlocked||here;c.querySelector('footer').hidden=!unlocked;c.querySelector('.bases-card-rate').textContent=unlocked?'+'+money(here?network.rate(g):network.rates[i])+'/s':'';c.querySelector('.bases-auto').textContent=unlocked?g.s.jobs.filter((_,j)=>g.auto(j)&&g.unlocked(j)).length+' AUTO · '+(here?'HERE':'REMOTE'):'';c.querySelector('.bases-card-paid').textContent=unlocked?'+'+money(network.paid[i])+' remote paid':'';});
  }
  function close(){panel.hidden=true;$('game').dataset.basesOpen='false';}
  function open(){if(RootLoading.visible)return;api.closeMenus();panel.hidden=false;$('game').dataset.basesOpen='true';render();$('bases-close').focus();}
  entry.onclick=open;$('bases-close').onclick=()=>{close();entry.focus();};
  panel.querySelectorAll('[data-base-stage]').forEach(b=>b.onclick=()=>{network.stage(+b.dataset.baseStage);api.setGame(network.game);updateRoom();$('bases-list').scrollTop=0;api.render();});
  panel.addEventListener('keydown',e=>{if(e.key==='Escape'){e.preventDefault();e.stopPropagation();close();entry.focus();}});
  // Keep AFK accessible from Settings while using its HUD slot for Bases.
  const afk=document.createElement('button');afk.className='menu-button secondary';afk.textContent='AFK EARNINGS';afk.onclick=()=>$('afk-open').click();$('resume').after(afk);
  network.stage(2);api.setGame(network.game);updateRoom();
  return {open,close,render,tick:now=>network.tick(now),rate:()=>network.remoteRate};
 }};
})(window);
