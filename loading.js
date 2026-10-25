(function (root) {
  'use strict';
  // One session owns all callbacks. Retry/close invalidate late completions.
  class LoadingSession {
    constructor(render, clock = globalThis) { this.render = render; this.clock = clock; this.ticket = 0; this.active = false; }
    start(options) {
      this.stop(); this.options = options; this.active = true; this.failed = false;
      const ticket = this.ticket, controller = new AbortController(); this.controller = controller;
      const current = () => this.active && !this.failed && ticket === this.ticket;
      this.render({kind: options.kind, phase: 'loading', progress: null});
      this.timeout = this.clock.setTimeout(() => this.fail(ticket), options.timeoutMs ?? 15000);
      const dwell = new Promise(resolve => { this.dwell = this.clock.setTimeout(resolve, options.minVisibleMs ?? 600); });
      const report = progress => {
        if (!current() || (progress !== null && !Number.isFinite(progress))) return;
        this.render({kind: options.kind, phase: 'loading', progress: progress === null ? null : Math.max(0, Math.min(1, progress))});
      };
      Promise.all([Promise.resolve().then(() => {
        if (!current()) return;
        return options.work(controller.signal, report);
      }), dwell]).then(([result]) => {
        if (!current()) return;
        this.stop(); this.render({phase: 'hidden'}); options.onComplete?.(result);
      }, () => { if (current()) this.fail(ticket); });
      return ticket;
    }
    fail(ticket) {
      if (!this.active || this.failed || ticket !== this.ticket) return;
      this.failed = true; this.clock.clearTimeout(this.timeout); this.clock.clearTimeout(this.dwell);
      this.controller.abort(); this.render({kind: this.options.kind, phase: 'error'});
    }
    retry() { if (this.active && this.failed) this.start(this.options); }
    close() {
      if (!this.active || !this.options.onBack) return;
      const callback = this.options.onBack; this.stop(); this.render({phase: 'hidden'}); callback();
    }
    stop() {
      this.ticket++; this.active = false;
      this.clock.clearTimeout(this.timeout); this.clock.clearTimeout(this.dwell); this.controller?.abort();
    }
  }
  if (typeof module !== 'undefined' && module.exports) { module.exports = {LoadingSession}; return; }
  const $ = id => document.getElementById(id), overlay = $('loading-overlay');
  let previousFocus, inertStates = null, lastPhase;
  const session = new LoadingSession(state => {
    if (state.phase === 'hidden') {
      overlay.hidden = true;
      inertStates?.forEach(([el, inert]) => { el.inert = inert; }); inertStates = null;
      if (previousFocus?.isConnected && !previousFocus.closest('[inert]')) previousFocus.focus();
      return;
    }
    if (overlay.hidden) {
      previousFocus = document.activeElement;
      inertStates = [...$('game').children].filter(el => el !== overlay).map(el => [el, el.inert]);
      inertStates.forEach(([el]) => { el.inert = true; }); overlay.inert = false; overlay.hidden = false;
      overlay.focus();
    }
    const startup = state.kind === 'startup', error = state.phase === 'error';
    if (lastPhase === 'error' && !error) overlay.focus();
    overlay.dataset.kind = state.kind; overlay.dataset.phase = state.phase;
    $('loading-startup').hidden = !startup || error; $('loading-panel').hidden = startup && !error;
    if (startup && !error) {
      const unknown = state.progress === null;
      $('startup-progress').hidden = unknown; $('startup-connecting').hidden = !unknown;
      if (!unknown) {
        const value = Math.round(state.progress * 100);
        $('loading-percent').textContent = value + '%'; $('loading-fill').style.width = value + '%';
        $('loading-track').setAttribute('aria-valuenow', value);
      }
    } else {
      $('loading-eyebrow').textContent = startup ? 'CONNECTION' : 'ADVERTISEMENT';
      $('loading-title').textContent = error ? (startup ? 'Loading failed' : 'Ad unavailable') : 'Preparing ad';
      $('loading-description').textContent = error ? (startup ? 'Your room could not be loaded.\nPlease try again.' : 'The ad could not be loaded.\nTry again or return to the game.') : 'Getting your ad ready.\nThis may take a moment.';
      $('loading-error-icon').hidden = !error; $('loading-ad-spinner').hidden = error;
      $('loading-actions').hidden = !error; $('loading-back').disabled = !session.options.onBack;
      if (error && lastPhase !== 'error') $('loading-retry').focus();
    }
    lastPhase = state.phase;
  });
  $('loading-back').onclick = () => session.close(); $('loading-retry').onclick = () => session.retry();
  // This capture listener runs before game keyboard shortcuts and AFK focus traps.
  document.addEventListener('keydown', e => {
    if (!session.active) return;
    e.stopImmediatePropagation();
    if (e.key === 'Escape') { e.preventDefault(); session.close(); }
    if (e.key === 'Tab') {
      const controls = [...overlay.querySelectorAll('button')].filter(el => !el.disabled && !el.closest('[hidden]'));
      if (!controls.length) { e.preventDefault(); overlay.focus(); return; }
      const index = controls.indexOf(document.activeElement);
      e.preventDefault(); controls[(index + (e.shiftKey ? -1 : 1) + controls.length) % controls.length].focus();
    }
  }, true);
  root.RootLoading = {start: options => session.start(options), close: () => session.close(), get visible() { return session.active; }};
})(typeof globalThis !== 'undefined' ? globalThis : this);
