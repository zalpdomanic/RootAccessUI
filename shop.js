(function(root){
  'use strict';
  const icons={cpu:'<rect x="6" y="6" width="16" height="16" rx="1"/><rect x="10" y="10" width="8" height="8"/><path d="M9 2v4m5-4v4m5-4v4M9 22v4m5-4v4m5-4v4M2 9h4m-4 5h4m-4 5h4m16-10h4m-4 5h4m-4 5h4"/>',keyboard:'<path d="M3 8h22v13H3zM6 12h2m2 0h2m2 0h2m2 0h2m2 0h1M6 16h2m2 0h10"/>',gpu:'<path d="M3 8h22v13H3zM7 21v3m4-3v3m4-3v3"/><circle cx="11" cy="14.5" r="4"/><path d="M18 11h4m-4 4h4"/>',server:'<path d="M7 3h14v22H7zM7 10h14M7 17h14M10 6h3m-3 7h3m-3 7h3"/>',router:'<path d="M3 15h22v9H3zM7 15V4m14 11V4M7 20h2m3 0h2m3 0h4M10 6q4-5 8 0"/>',chair:'<path d="M8 4h12v11H8zM6 13v7h16v-7M14 20v5m-6 0h12"/>',lamp:'<path d="M11 3h6l4 18H7zM9 25h10M11 10q6 2 2 7"/>',plant:'<path d="M8 18h12l-2 8h-8zM14 18V7M14 13Q3 13 5 5q10 0 9 8zm0-5q0-8 9-6 0 8-9 9"/>'};
  const categories={pc:['PC UPGRADES','Boost each tap and the full-bar cash reward.'],equipment:['HACKING EQUIPMENT','Earn more per job. Finish each run faster.'],furniture:['FURNITURE','Comfort boosts job payouts and offline earnings.']};
  const icon=name=>'<svg viewBox="0 0 28 28" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true">'+icons[name]+'</svg>';
  const percent=value=>Math.round(value*100)+'%';
  root.RootShop={create(api){
    const $=id=>document.getElementById(id),panel=$('shop-panel'),entry=$('shop-open');let category='pc';
    panel.innerHTML=`<header class="shop-header"><h1 id="shop-title">SHOP</h1><span id="shop-money"></span><button id="shop-close" aria-label="Close shop">×</button></header>
      <div class="shop-tabs" role="tablist" aria-label="Shop categories">${Object.keys(categories).map((key,i)=>`<button role="tab" id="shop-tab-${key}" data-category="${key}" aria-controls="shop-catalog" aria-selected="${i===0}" tabindex="${i===0?0:-1}">${key==='pc'?'PC':key.toUpperCase()}</button>`).join('')}</div>
      <div class="shop-summary"><div><span id="shop-category-title"></span><b id="shop-stat"></b></div><p id="shop-hint"></p></div>
      <div id="shop-catalog" role="tabpanel" aria-labelledby="shop-tab-pc" tabindex="0"></div>
      <div class="shop-equipment-links" id="shop-equipment-links" hidden><button id="shop-setup">RACKS &amp; AUTO +</button><button id="shop-manage">MY EQUIPMENT</button></div>
      <footer class="shop-footer"><span id="shop-footnote"></span><span>Scroll ↓</span></footer>`;
    const nodes=new Map();
    // Build the catalog once. Live values update existing nodes without losing scroll/focus.
    RootAccess.SHOP.forEach(item=>{
      const card=document.createElement('article');card.className='shop-product';card.dataset.category=item.category;card.dataset.product=item.id;
      card.innerHTML=`<div class="shop-product-head"><span class="shop-icon">${icon(item.icon)}</span><div class="shop-product-info"><div><h2>${item.name}</h2><span class="shop-level"></span></div><p class="shop-effect"></p></div></div><div class="shop-offer"><span class="shop-price"></span><button data-shop-buy="${item.id}"></button></div>`;
      $('shop-catalog').append(card);nodes.set(item.id,{card,level:card.querySelector('.shop-level'),effect:card.querySelector('.shop-effect'),price:card.querySelector('.shop-price'),button:card.querySelector('button')});
    });
    function put(el,value){if(el.textContent!==value)el.textContent=value;}
    function render(){
      if(panel.hidden)return;
      const game=api.game();put($('shop-money'),api.cash(game.s.money));
      put($('shop-stat'),category==='pc'?'Active hacking':category==='equipment'?'All jobs':'Comfort '+game.comfort());
      nodes.forEach((n,id)=>{
        const q=game.shopQuote(id),item=q.item;n.card.hidden=item.category!==category;if(n.card.hidden)return;
        put(n.level,item.comfort?(q.maxed?'OWNED':'DECOR'):'LV '+q.level+' / '+item.max);
        const step=item.step,level=q.level,next=Math.min(item.max,level+1);
        let effect;
        if(item.comfort)effect=`Comfort +${item.comfort} · Job +${item.comfort*2}% · AFK +${item.comfort*3}% extra`;
        else if(item.effect==='tap')effect='Tap bonus '+api.precise(step*level)+' → '+api.precise(step*next);
        else{const label={tapPercent:'Tap payout',bar:'Full-bar reward',job:'Job payout',duration:'Job duration'}[item.effect],sign=item.effect==='duration'?'−':'+';effect=label+' '+sign+percent(step*level)+' → '+sign+percent(step*next);}
        put(n.effect,effect);put(n.price,q.locked?'REP LV '+item.rep:q.maxed?(item.comfort?'Comfort active':'Fully upgraded'):api.cash(q.cost));
        const label=q.locked?'LOCKED':q.maxed?(item.comfort?'OWNED':'MAX LEVEL'):!q.affordable?'NEED CASH':item.comfort?'BUY':'UPGRADE';
        put(n.button,label);n.button.disabled=!q.affordable;n.card.classList.toggle('locked',q.locked);
        n.button.setAttribute('aria-label',label+' · '+item.name+(q.affordable?' for '+api.precise(q.cost):''));
      });
    }
    function select(key,focus=false){
      category=key;panel.dataset.category=key;
      panel.querySelectorAll('[data-category][role=tab]').forEach(b=>{const chosen=b.dataset.category===key;b.setAttribute('aria-selected',String(chosen));b.tabIndex=chosen?0:-1;});
      $('shop-catalog').setAttribute('aria-labelledby','shop-tab-'+key);$('shop-catalog').scrollTop=0;
      put($('shop-category-title'),categories[key][0]);put($('shop-hint'),categories[key][1]);
      put($('shop-footnote'),key==='furniture'?'AFK extra applies only while away':'Permanent upgrades');$('shop-equipment-links').hidden=key!=='equipment';render();if(focus)$('shop-tab-'+key).focus();
    }
    function close(restoreFocus=true){panel.hidden=true;$('game').dataset.shopOpen='false';if(restoreFocus)entry.focus();}
    function open(key=category){if(root.RootLoading.visible)return;api.closeMenus();api.setPanel('closed');panel.hidden=false;$('game').dataset.shopOpen='true';select(key,true);}
    entry.onclick=()=>open();$('shop-close').onclick=()=>close();
    panel.querySelectorAll('[role=tab]').forEach((b,i)=>{b.onclick=()=>select(b.dataset.category);b.onkeydown=e=>{if(!['ArrowLeft','ArrowRight','Home','End'].includes(e.key))return;e.preventDefault();const next=e.key==='Home'?0:e.key==='End'?2:(i+(e.key==='ArrowRight'?1:2))%3;select(Object.keys(categories)[next],true);};});
    panel.addEventListener('keydown',e=>{if(e.key==='Escape'){e.preventDefault();e.stopPropagation();close();}});
    panel.addEventListener('click',e=>{const b=e.target.closest('[data-shop-buy]');if(!b||b.disabled)return;const id=b.dataset.shopBuy,item=RootAccess.SHOP.find(x=>x.id===id);if(api.game().buyShop(id)){api.save();api.render();api.tell(item.name+(item.comfort?' · +'+item.comfort+' Comfort':' upgraded'));if(b.disabled)$('shop-catalog').focus({preventScroll:true});}});
    $('shop-setup').onclick=()=>{close(false);api.setPanel('new-rack');};$('shop-manage').onclick=()=>{close(false);api.rack();};
    select('pc');return {open,close,render,get visible(){return !panel.hidden;}};
  }};
})(window);
