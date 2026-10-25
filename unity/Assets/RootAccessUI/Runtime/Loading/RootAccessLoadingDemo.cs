using System.Collections;
using UnityEngine;

namespace RootAccess.UI.Loading
{
    /// <summary>Optional demonstration only. Replace it with your scene loader or ad SDK.</summary>
    public sealed class RootAccessLoadingDemo : MonoBehaviour
    {
        public RootAccessLoadingHost Host;
        public bool ShowOnStart = true;
        void Start() { if (Host == null) Host = GetComponent<RootAccessLoadingHost>(); if (ShowOnStart) PlayStartup(); }
        public void PlayStartup() { int ticket = Host.BeginStartup(t => StartCoroutine(Startup(t)), () => { }); StartCoroutine(Startup(ticket)); }
        IEnumerator Startup(int ticket)
        {
            yield return new WaitForSecondsRealtime(.75f);
            for (int i=0;i<=10;i++) { if (ticket != Host.Ticket) yield break; Host.ReportProgress(ticket,i/10f); yield return new WaitForSecondsRealtime(.2f); }
            Host.Complete(ticket);
        }
        public void PlayAdFailure() { int ticket = Host.BeginAd(t => StartCoroutine(AdFailure(t)), () => { }); StartCoroutine(AdFailure(ticket)); }
        IEnumerator AdFailure(int ticket) { yield return new WaitForSecondsRealtime(1.5f); Host.Fail(ticket); }
    }
}
