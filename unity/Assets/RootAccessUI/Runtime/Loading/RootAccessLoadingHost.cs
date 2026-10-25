using System;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RootAccess.UI.Loading
{
    /// <summary>Presentation only. Call on Unity's main thread. The caller owns scene/ad work and rewards.</summary>
    public sealed class RootAccessLoadingHost : MonoBehaviour
    {
        public int SortingOrder = 200;
        public bool ReduceMotion;
        public bool Animate = true;
        public bool IsVisible { get; private set; }
        public bool IsFailed { get; private set; }
        public int Ticket { get; private set; }
        public CancellationToken Cancellation => lifetime?.Token ?? new CancellationToken(true);
        public FigmaView StartupView { get; private set; }
        public FigmaView AdView { get; private set; }
        public Button RetryButton { get; private set; }
        public Button CloseButton { get; private set; }
        public float? Progress { get; private set; }
        Canvas canvas;
        GameObject startupOwner, adOwner, blocker;
        FigmaElement progressGroup, connectingGroup, startupArc, adArc, adSpinner, errorIndicator, panel, actions, bottomCorner;
        Action<int> retry;
        Action closed;
        CancellationTokenSource lifetime;
        bool ad;
        float timeout, deadline, angle;
        Rect lastSafe;
        Vector2Int lastSize;
        GameObject previousSelection;

        void EnsureCanvas()
        {
            if (canvas != null) return;
            var c = new GameObject("Root Access Loading Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            c.transform.SetParent(transform, false);
            canvas = c.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = SortingOrder;
            blocker = new GameObject("Loading input blocker", typeof(RectTransform), typeof(Image));
            blocker.transform.SetParent(c.transform, false);
            var rect = (RectTransform)blocker.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            blocker.GetComponent<Image>().color = Color.clear;
            if (EventSystem.current == null) Debug.LogWarning("Loading buttons require the game's EventSystem. See the supplied demo.", this);
        }
        FigmaView Build(string screen, out GameObject owner)
        {
            owner = new GameObject(screen, typeof(RectTransform)); owner.transform.SetParent(canvas.transform, false);
            var r = (RectTransform)owner.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            var view = owner.AddComponent<FigmaView>(); view.Build(screen, owner.transform, false); return view;
        }
        void EnsureStartup()
        {
            if (StartupView != null) return;
            StartupView = Build("loading-startup-progress", out startupOwner);
            progressGroup = StartupView.Find("Loading status");
            var percent = progressGroup.Find("Progress value");
            percent.Rect.anchoredPosition = new Vector2(680,percent.Rect.anchoredPosition.y);
            percent.Rect.sizeDelta = new Vector2(160,percent.Rect.rect.height);
            percent.Label.alignment = TMPro.TextAlignmentOptions.TopRight;
            var connecting = FigmaView.Load("loading-startup-connecting");
            int index = Array.FindIndex(connecting.nodes, n => n.name == "Loading status");
            connectingGroup = StartupView.CopySubtree(connecting, index, StartupView.Root);
            connectingGroup.Data.x = 120; connectingGroup.Data.y = 1410;
            startupArc = connectingGroup.Find("Spinner / animated arc"); SetSpinPivot(startupArc);
        }
        void EnsureAd()
        {
            if (AdView != null) return;
            AdView = Build("loading-ad-unavailable", out adOwner);
            panel = AdView.Find("Ad loading panel"); actions = AdView.Find("Recovery actions"); errorIndicator = AdView.Find("RA/Loading/Indicator");
            bottomCorner = panel.Descendants.Last(e => e.Data.name == "Terminal corner accent");
            var source = FigmaView.Load("loading-ad-preparing");
            int index = Array.FindIndex(source.nodes, n => n.name == "RA/Loading/Indicator");
            adSpinner = AdView.CopySubtree(source, index, errorIndicator.transform.parent);
            adSpinner.Rect.anchoredPosition = errorIndicator.Rect.anchoredPosition;
            adArc = adSpinner.Find("Spinner / animated arc"); SetSpinPivot(adArc);
            RetryButton = AdView.Find("Retry action").Click(Retry);
            CloseButton = AdView.Find("Close action").Click(Close);
        }
        static void SetSpinPivot(FigmaElement arc)
        {
            arc.Rect.pivot = new Vector2(.5f,.5f);
            arc.Rect.anchoredPosition += new Vector2(arc.Data.w/2, -arc.Data.h/2);
        }
        public int BeginStartup(Action<int> retryAction = null, Action closeAction = null, float timeoutSeconds = 60)
            => Begin(false, retryAction, closeAction, timeoutSeconds);
        public int BeginAd(Action<int> retryAction = null, Action closeAction = null, float timeoutSeconds = 15)
            => Begin(true, retryAction, closeAction, timeoutSeconds);
        int Begin(bool isAd, Action<int> retryAction, Action closeAction, float seconds)
        {
            if (!isActiveAndEnabled) throw new InvalidOperationException("Enable the loading host before beginning a session.");
            if (!IsVisible) previousSelection = EventSystem.current?.currentSelectedGameObject;
            CancelLifetime(); Ticket++; lifetime = new CancellationTokenSource();
            ad = isAd; retry = retryAction; closed = closeAction; timeout = Mathf.Max(.1f, seconds);
            deadline = Time.unscaledTime + timeout; IsVisible = true; IsFailed = false; Progress = null; angle = 0;
            EnsureCanvas(); canvas.gameObject.SetActive(true);
            if (ad) { EnsureAd(); SetAdState(false); adOwner.transform.SetAsLastSibling(); }
            else { EnsureStartup(); SetStartupState(null); startupOwner.transform.SetAsLastSibling(); }
            if (startupOwner != null) startupOwner.SetActive(!ad);
            if (adOwner != null) adOwner.SetActive(ad);
            EventSystem.current?.SetSelectedGameObject(null); Layout(); return Ticket;
        }
        bool Current(int ticket) => IsVisible && !IsFailed && ticket == Ticket;
        public void ReportProgress(int ticket, float? fraction)
        {
            if (!Current(ticket) || ad) return;
            if (fraction.HasValue && (float.IsNaN(fraction.Value) || float.IsInfinity(fraction.Value))) return;
            float? next = fraction.HasValue ? Mathf.Clamp01(fraction.Value) : (float?)null;
            if (Progress == next) return;
            Progress = next; SetStartupState(next);
        }
        void SetStartupState(float? value)
        {
            progressGroup.gameObject.SetActive(value.HasValue); connectingGroup.gameObject.SetActive(!value.HasValue);
            if (value.HasValue)
            {
                var fill = progressGroup.Find("Progress / host value 0..1"); fill.Rect.sizeDelta = new Vector2(840*value.Value,16);
                var text = progressGroup.Find("Progress value").Label; string next = Mathf.FloorToInt(value.Value*100)+"%";
                if (text.text != next) text.text = next;
            }
        }
        void SetAdState(bool failed)
        {
            float height = failed ? 810 : 580; panel.Rect.sizeDelta = new Vector2(900,height);
            bottomCorner.Rect.anchoredPosition = new Vector2(bottomCorner.Data.x,-(height-46.73f));
            actions.gameObject.SetActive(failed); errorIndicator.gameObject.SetActive(failed); adSpinner.gameObject.SetActive(!failed);
            AdView.Find("Title").Text(failed ? (ad ? "Ad unavailable" : "Loading failed") : "Preparing ad");
            AdView.Find("Message").Text(failed ? (ad ? "The ad could not be loaded.\nTry again or return to the game." : "Loading could not be completed.\nPlease try again.") : "Connecting to the ad service.\nPlease wait...");
            RetryButton.interactable = retry != null; CloseButton.interactable = closed != null;
        }
        public void Complete(int ticket) { if (Current(ticket)) Hide(); }
        public void Fail(int ticket)
        {
            if (!Current(ticket)) return;
            IsFailed = true; CancelLifetime(); EnsureAd(); adOwner.SetActive(true); adOwner.transform.SetAsLastSibling(); SetAdState(true); Layout();
            EventSystem.current?.SetSelectedGameObject(RetryButton.interactable ? RetryButton.gameObject : CloseButton.interactable ? CloseButton.gameObject : null);
        }
        void Retry()
        {
            if (!IsVisible || !IsFailed || retry == null) return;
            var action = retry; int next = Begin(ad, retry, closed, timeout);
            try { action(next); } catch (Exception e) { Debug.LogException(e,this); Fail(next); }
        }
        void Close()
        {
            if (!IsVisible || !IsFailed || closed == null) return;
            var action = closed; Hide(); action();
        }
        public void Hide()
        {
            IsVisible = false; IsFailed = false; Ticket++; CancelLifetime();
            if (canvas != null) canvas.gameObject.SetActive(false);
            retry = null; closed = null;
            if (EventSystem.current != null && previousSelection != null && previousSelection.activeInHierarchy) EventSystem.current.SetSelectedGameObject(previousSelection);
            previousSelection = null;
        }
        void CancelLifetime() { var old = lifetime; lifetime = null; if (old != null) { try { old.Cancel(); } catch(Exception e) { Debug.LogException(e,this); } finally { old.Dispose(); } } }
        void Update()
        {
            if (!IsVisible) return;
            if (!IsFailed && Time.unscaledTime >= deadline) Fail(Ticket);
            if (lastSafe != Screen.safeArea || lastSize != new Vector2Int(Screen.width,Screen.height)) Layout();
            if (!IsFailed && Animate && !ReduceMotion)
            {
                angle = (angle + Time.unscaledDeltaTime*360/1.1f)%360;
                var arc = ad ? adArc : startupArc;
                if (arc != null && arc.gameObject.activeInHierarchy) arc.Rect.localRotation = Quaternion.Euler(0,0,-angle);
            }
            else { if (startupArc != null) startupArc.Rect.localRotation = Quaternion.identity; if (adArc != null) adArc.Rect.localRotation = Quaternion.identity; }
        }
        public void RefreshLayout() => Layout();
        void Layout()
        {
            lastSafe = Screen.safeArea; lastSize = new Vector2Int(Screen.width,Screen.height);
            // Fill background and input blocker to the display; interactive content respects safe area.
            if (StartupView != null)
            {
                float scale = Mathf.Min(Screen.width/1080f,Screen.height/1920f);
                PlaceRoot(StartupView.Root,new Rect(0,0,Screen.width,Screen.height),scale);
                float extra = Screen.height/scale-1920;
                var env = StartupView.Find("Environment / existing room and full-screen texture"); env.Rect.sizeDelta = new Vector2(1080,1920+extra);
                var scene = env.Find("Scene viewport"); scene.Rect.anchoredPosition = new Vector2(0,-(300+extra*.5f));
                var title = StartupView.Find("Identity"); title.Rect.anchoredPosition = new Vector2(80,-(156+(Screen.height-lastSafe.yMax)/scale));
                float bottom = lastSafe.yMin/scale;
                progressGroup.Rect.anchoredPosition = new Vector2(120,-(1410+extra-bottom));
                connectingGroup.Rect.anchoredPosition = progressGroup.Rect.anchoredPosition;
                StartupView.Find("Footer").Rect.anchoredPosition = new Vector2(80,-(1800+extra-bottom));
            }
            if (AdView != null)
            {
                float scale = Mathf.Min(lastSafe.width/1080f,lastSafe.height/1000f);
                PlaceRoot(AdView.Root,lastSafe,scale);
                panel.Rect.anchoredPosition = new Vector2(90,-(lastSafe.height/scale-panel.Rect.rect.height)*.5f);
                var dim = AdView.Find("Backdrop / 72 percent dim"); dim.Rect.anchoredPosition = new Vector2(-lastSafe.xMin/scale,(Screen.height-lastSafe.yMax)/scale); dim.Rect.sizeDelta = new Vector2(Screen.width/scale,Screen.height/scale);
            }
        }
        static void PlaceRoot(RectTransform root,Rect safe,float scale)
        {
            root.anchorMin = root.anchorMax = Vector2.zero; root.pivot = new Vector2(.5f,1);
            root.anchoredPosition = new Vector2(safe.center.x,safe.yMax); root.sizeDelta = new Vector2(1080,safe.height/scale); root.localScale = Vector3.one*scale;
        }
        void OnDisable() { Hide(); }
        void OnDestroy() { CancelLifetime(); }
    }
}
