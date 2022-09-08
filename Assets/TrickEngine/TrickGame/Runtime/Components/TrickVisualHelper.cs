using System;
using System.Collections;
using System.Linq;
using BeauRoutine;
using UnityEngine;
using UnityEngine.UI;

namespace TrickCore
{
    /// <summary>
    /// Visual helper script for supporting tweening and such.
    /// TODO: Will add more documentation on usage
    /// Usage: Extensions methods are found in the class TrickVisualHelperExtensions (below here) with certain functions to tween, fade in, fade out and such.
    /// </summary>
    public class TrickVisualHelper : MonoBehaviour
    {
        public bool particleSystemInjectStartColor = true;
        public bool particleSystemInjectColorOverTime = true;
        public bool particleSystemInjectColorBySpeed = true;

        internal CanvasGroup CurrentCanvasGroup;
    
        // ps
        private bool _initializeForPS;
        internal ParticleSystem.MinMaxGradient? InitMainModuleStartColor;
        internal ParticleSystem.MinMaxGradient? InitColorOverTimeColor;
        internal ParticleSystem.MinMaxGradient? InitColorBySpeed;
    
        // ui
        internal bool InitializeForUI;
        internal Routine FadeRoutine;
        internal Routine TransitionRoutine;
        internal Routine ScaleRoutine;
        internal Routine ShakeRoutine;
        internal Vector3? LocalScale;
        internal Vector2? OriginalAnchorPosition;

        internal void TryInitializePS()
        {
            if (_initializeForPS) return;
        
            _initializeForPS = true;
        }

        internal void TryInitializeUI()
        {
            if (InitializeForUI) return;
            CurrentCanvasGroup = GetComponent<CanvasGroup>();
            if (CurrentCanvasGroup == null) CurrentCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            InitializeForUI = true;
        }
    }

    public static class TrickVisualHelperExtensions
    {
        public static void SetAlpha(this MonoBehaviour mono, float alpha)
        {
            var rt = mono.transform as RectTransform;
            SetAlpha(rt, alpha);
        }

        public static void SetAlpha(this RectTransform mono, float alpha)
        {
            var tb = mono.GetComponent<TrickVisualHelper>();
            if (tb == null) tb = mono.gameObject.AddComponent<TrickVisualHelper>();
            tb.TryInitializeUI();
            tb.CurrentCanvasGroup.alpha = alpha;
        }

        public static void SetHighlighted(this RectTransform mono, Image highlightBorderImage, HighlightState highlightState)
        {
            var trickButton = mono.GetComponent<TrickVisualMono>();
            if (trickButton == null) trickButton = mono.gameObject.AddComponent<TrickVisualMono>();
            trickButton.SetHighlighted(highlightBorderImage, highlightState);
        }

        public static void SetHighlighted(this MonoBehaviour mono, Image highlightBorderImage, HighlightState highlightState)
        {
            var trickButton = mono.GetComponent<TrickVisualMono>();
            if (trickButton == null) trickButton = mono.gameObject.AddComponent<TrickVisualMono>();
            if (highlightBorderImage != null) trickButton.HighlightBorderImage = highlightBorderImage;
            if (trickButton.HighlightBorderImage == null)
            {
                var highlightChild = trickButton.transform.Find("HighlightBorder");
                if (highlightChild != null) trickButton.HighlightBorderImage = highlightChild.GetComponent<Image>();
            }

            trickButton.enabled = false;
            trickButton.SetHighlighted(highlightBorderImage, highlightState);
        }

        public static void SetCanvasGroupInteractable(this MonoBehaviour mono, bool interactable, bool blockRaycast)
        {
            var tb = mono.GetComponent<TrickVisualHelper>();
            if (tb == null) tb = mono.gameObject.AddComponent<TrickVisualHelper>();
            tb.TryInitializeUI();
            tb.CurrentCanvasGroup.interactable = interactable;
            tb.CurrentCanvasGroup.blocksRaycasts = blockRaycast;
        }
        
        public static void SetCanvasGroupInteractable(this RectTransform mono, bool? interactable, bool? blockRaycast)
        {
            var tb = mono.GetComponent<TrickVisualHelper>();
            if (tb == null) tb = mono.gameObject.AddComponent<TrickVisualHelper>();
            tb.TryInitializeUI();
            if (interactable != null) tb.CurrentCanvasGroup.interactable = interactable.Value;
            if (blockRaycast != null) tb.CurrentCanvasGroup.blocksRaycasts = blockRaycast.Value;
        }
        
        public static Routine Fade(this MonoBehaviour mono, float fadeTarget = 0.0f, float fadeTime = 0.25f, Curve curve = Curve.Linear,
            float delay = 0.0f,
            float? setAlpha = null, bool interactable = false, bool withoutHost = false, Action completeAction = null)
        {
            var rt = mono.transform as RectTransform;
            if (rt != null)
                return Fade(rt, fadeTarget, fadeTime, curve, delay, setAlpha, interactable, withoutHost, completeAction);
            return default;
        }

        public static Routine Fade(this RectTransform mono, float fadeTarget = 0.0f, float fadeTime = 0.25f, Curve curve = Curve.Linear,
            float delay = 0.0f, float? setAlpha = null, bool interactable = false, bool withoutHost = false,
            Action completeAction = null)
        {
            return Fade(mono, fadeTarget, new TweenSettings(fadeTime, curve), delay, setAlpha, interactable, withoutHost, completeAction);
        }

        public static Routine Fade(this RectTransform mono, float fadeTarget = 0.0f, TweenSettings tweenSettings = default,
            float delay = 0.0f, float? setAlpha = null, bool interactable = false, bool withoutHost = false,
            Action completeAction = null)
        {
            var tb = mono.GetComponent<TrickVisualHelper>();
            if (tb == null) tb = mono.gameObject.AddComponent<TrickVisualHelper>();
            tb.TryInitializeUI();
            if (setAlpha != null) tb.CurrentCanvasGroup.alpha = setAlpha.GetValueOrDefault();
            var tween = tb.CurrentCanvasGroup.FadeTo(fadeTarget, tweenSettings).DelayBy(delay).OnComplete(completeAction);
            tb.FadeRoutine.Replace(withoutHost ? tween.Play() : tween.Play(tb));
            tb.SetCanvasGroupInteractable(interactable, interactable);
            return tb.FadeRoutine;
        }

        public static Routine FadeIn(this MonoBehaviour mono, float fadeTime = 0.25f, Curve curve = Curve.Linear, float delay = 0.0f,
            float? setAlpha = null, bool withoutHost = false, Action completeAction = null)
        {
            return Fade(mono, 1.0f, fadeTime, curve, delay, setAlpha, true, withoutHost, completeAction);
        }

        public static Routine FadeOut(this MonoBehaviour mono, float fadeTime = 0.25f, Curve curve = Curve.Linear, float delay = 0.0f,
            float? setAlpha = null, bool withoutHost = false, Action completeAction = null)
        {
            return Fade(mono, 0.0f, fadeTime, curve, delay, setAlpha, false, withoutHost, completeAction);
        }

        public static Routine FadeIn(this RectTransform mono, float fadeTime = 0.25f, Curve curve = Curve.Linear, float delay = 0.0f,
            float? setAlpha = null, bool withoutHost = false, Action completeAction = null)
        {
            return Fade(mono, 1.0f, fadeTime, curve, delay, setAlpha, true, withoutHost, completeAction);
        }

        public static Routine FadeOut(this RectTransform mono, float fadeTime = 0.25f, Curve curve = Curve.Linear, float delay = 0.0f,
            float? setAlpha = null, bool withoutHost = false, Action completeAction = null)
        {
            return Fade(mono, 0.0f, fadeTime, curve, delay, setAlpha, false, withoutHost, completeAction);
        }

        
        public static Routine FadeIn(this MonoBehaviour mono, TweenSettings tweenSettings = default, float delay = 0.0f,
            float? setAlpha = null, bool withoutHost = false, Action completeAction = null)
        {
            return Fade(mono, 1.0f, tweenSettings, delay, setAlpha, true, withoutHost, completeAction);
        }

        private static Routine Fade(MonoBehaviour mono, float fadeTarget, TweenSettings tweenSettings, float delay, float? setAlpha, bool interactable, bool withoutHost, Action completeAction)
        {
            var rt = mono.transform as RectTransform;
            if (rt != null)
                return Fade(rt, fadeTarget, tweenSettings, delay, setAlpha, interactable, withoutHost, completeAction);
            return default;
        }

        public static Routine FadeOut(this MonoBehaviour mono, TweenSettings tweenSettings = default, float delay = 0.0f,
            float? setAlpha = null, bool withoutHost = false, Action completeAction = null)
        {
            return Fade(mono, 0.0f, tweenSettings, delay, setAlpha, false, withoutHost, completeAction);
        }

        public static Routine FadeIn(this RectTransform mono, TweenSettings tweenSettings = default, float delay = 0.0f,
            float? setAlpha = null, bool withoutHost = false, Action completeAction = null)
        {
            return Fade(mono, 1.0f, tweenSettings, delay, setAlpha, true, withoutHost, completeAction);
        }

        public static Routine FadeOut(this RectTransform mono, TweenSettings tweenSettings = default, float delay = 0.0f,
            float? setAlpha = null, bool withoutHost = false, Action completeAction = null)
        {
            return Fade(mono, 0.0f, tweenSettings, delay, setAlpha, false, withoutHost, completeAction);
        }
        
        
        public static Routine ScaleTransformPingPong(this MonoBehaviour mono, float scaleTarget, float scaleDuration = 0.35f,
            float delay = 0.0f, Action startCallback = null) => ScaleTransformPingPong(mono.transform as RectTransform,
            scaleTarget, scaleDuration, delay, startCallback);

        public static Routine ScaleTransformPingPong(this RectTransform mono, float scaleTarget, float scaleDuration = 0.35f,
            float delay = 0.0f, Action startCallback = null)
        {
            var tb = mono.GetComponent<TrickVisualHelper>();
            if (tb == null) tb = mono.gameObject.AddComponent<TrickVisualHelper>();
            tb.TryInitializeUI();
            tb.LocalScale ??= mono.localScale;
            tb.LocalScale = tb.LocalScale.Value;

            return tb.ScaleRoutine.Replace(mono.ScaleTo(tb.LocalScale.Value * scaleTarget, new TweenSettings()
                {
                    Time = scaleDuration,
                    Curve = Curve.Smooth
                }).DelayBy(delay).OnStart(startCallback).Yoyo()
                .OnComplete(() => { mono.localScale = tb.LocalScale.Value; }).Play(tb));
        }

        public static void ResetParticleSystemColor(this ParticleSystem particleSystem, Color color, bool injectChildSystems)
        {
            if (particleSystem == null) return;
            var visualHelper = particleSystem.GetComponent<TrickVisualHelper>();
            if (visualHelper == null) visualHelper = particleSystem.gameObject.AddComponent<TrickVisualHelper>();
            visualHelper.TryInitializePS();

            void ResetParticleColor(ParticleSystem ps)
            {
                if (visualHelper.InitMainModuleStartColor != null)
                {
                    if (visualHelper.particleSystemInjectStartColor)
                    {
                        var mainModule = ps.main;
                        mainModule.startColor = visualHelper.InitMainModuleStartColor.Value;
                    }

                    if (visualHelper.particleSystemInjectColorOverTime)
                    {
                        var colorOverTimeModule = ps.colorOverLifetime;
                        if (colorOverTimeModule.enabled && visualHelper.InitColorOverTimeColor != null)
                            colorOverTimeModule.color = visualHelper.InitColorOverTimeColor.Value;
                    }

                    if (visualHelper.particleSystemInjectColorBySpeed)
                    {
                        var colorBySpeedModule = ps.colorBySpeed;
                        if (colorBySpeedModule.enabled && visualHelper.InitColorBySpeed != null)
                            colorBySpeedModule.color = visualHelper.InitColorBySpeed.Value;
                    }
                }
            }

            if (injectChildSystems)
            {
                foreach (var ps in particleSystem.GetComponentsInChildren<ParticleSystem>())
                    SetParticleSystemColor(ps, color, false);
            }
            else
            {
                ResetParticleColor(particleSystem);
            }
        }

        public static void SetParticleSystemColor(this ParticleSystem particleSystem, Color color, bool injectChildSystems)
        {
            if (particleSystem == null) return;
            var visualHelper = particleSystem.GetComponent<TrickVisualHelper>();
            if (visualHelper == null) visualHelper = particleSystem.gameObject.AddComponent<TrickVisualHelper>();
            visualHelper.TryInitializePS();

            ParticleSystem.MinMaxGradient GetProcessedMinMaxGradient(ParticleSystem.MinMaxGradient minMaxGradient)
            {
                var newGradient = new ParticleSystem.MinMaxGradient()
                {
                    mode = minMaxGradient.mode,
                    colorMin = minMaxGradient.colorMin,
                    colorMax = minMaxGradient.colorMax,
                    gradientMin = minMaxGradient.gradientMin != null
                        ? new Gradient()
                        {
                            mode = minMaxGradient.gradientMin.mode,
                            alphaKeys = minMaxGradient.gradientMin.alphaKeys.ToArray(),
                            colorKeys = minMaxGradient.gradientMin.colorKeys.ToArray(),
                        }
                        : minMaxGradient.gradientMin,
                    gradientMax = minMaxGradient.gradientMax != null
                        ? new Gradient()
                        {
                            mode = minMaxGradient.gradientMax.mode,
                            alphaKeys = minMaxGradient.gradientMax.alphaKeys.ToArray(),
                            colorKeys = minMaxGradient.gradientMax.colorKeys.ToArray(),
                        }
                        : minMaxGradient.gradientMax,
                };
                switch (newGradient.mode)
                {
                    case ParticleSystemGradientMode.Color:
                        newGradient.color = color;
                        break;
                    case ParticleSystemGradientMode.Gradient:
                    {
                        var arr = newGradient.gradient.colorKeys.ToArray();
                        for (int i = 0; i < arr.Length; i++)
                        {
                            var temp = arr[i];
                            temp.color = color;
                            arr[i] = temp;
                        }

                        newGradient.gradient.colorKeys = arr;
                        break;
                    }
                    case ParticleSystemGradientMode.TwoColors:
                        newGradient.colorMin = color;
                        newGradient.colorMax = color;
                        break;
                    case ParticleSystemGradientMode.TwoGradients:
                    {
                        if (newGradient.gradientMin != null)
                        {
                            var minArr = newGradient.gradientMin.colorKeys.ToArray();
                            for (int i = 0; i < minArr.Length; i++)
                            {
                                var temp = minArr[i];
                                temp.color = color;
                                minArr[i] = temp;
                            }

                            newGradient.gradientMin.colorKeys = minArr;
                        }

                        if (newGradient.gradientMax != null)
                        {
                            var maxArr = newGradient.gradientMax.colorKeys.ToArray();
                            for (int i = 0; i < maxArr.Length; i++)
                            {
                                var temp = maxArr[i];
                                temp.color = color;
                                maxArr[i] = temp;
                            }

                            newGradient.gradientMax.colorKeys = maxArr;
                        }

                        break;
                    }
                    case ParticleSystemGradientMode.RandomColor:
                        newGradient.color = UnityEngine.Random.ColorHSV();
                        break;
                }

                return newGradient;
            }

            void SetParticleColor(ParticleSystem ps)
            {
                if (visualHelper.particleSystemInjectStartColor)
                {
                    var mainModule = ps.main;
                    visualHelper.InitMainModuleStartColor ??= mainModule.startColor;

                    var minMaxGradient = visualHelper.InitMainModuleStartColor.Value;
                    mainModule.startColor = GetProcessedMinMaxGradient(minMaxGradient);
                }

                if (visualHelper.particleSystemInjectColorOverTime)
                {
                    var colorOverTimeModule = ps.colorOverLifetime;
                    if (colorOverTimeModule.enabled)
                    {
                        visualHelper.InitColorOverTimeColor ??= colorOverTimeModule.color;
                        colorOverTimeModule.color =
                            GetProcessedMinMaxGradient(visualHelper.InitColorOverTimeColor.Value);
                    }
                }

                if (visualHelper.particleSystemInjectColorBySpeed)
                {
                    var colorBySpeedModule = ps.colorBySpeed;
                    if (colorBySpeedModule.enabled)
                    {
                        visualHelper.InitColorBySpeed ??= colorBySpeedModule.color;
                        colorBySpeedModule.color = GetProcessedMinMaxGradient(visualHelper.InitColorBySpeed.Value);
                    }
                }
            }

            if (injectChildSystems)
            {
                foreach (var ps in particleSystem.GetComponentsInChildren<ParticleSystem>())
                    SetParticleSystemColor(ps, color, false);
            }
            else
            {
                SetParticleColor(particleSystem);
            }
        }

        public static void Shake(this MonoBehaviour mono, bool b, float shake = 20.0f, float pauseDuration = 0.05f,
            float longPauseDuration = 0.85f, float tweenDuration = 0.35f, Curve tweenCurve = Curve.BounceInOut, int loops = -1)
        {
            IEnumerator HandleShake()
            {
                var tr = mono.transform;
                var go = mono.gameObject;
                int i = 0;
                while (go != null)
                {
                    yield return Routine.WaitCondition(() => tr == null || go.activeSelf, 0.1f);
                    if (tr == null) yield break;
                    yield return tr.RotateTo(Vector3.one * shake, new TweenSettings(tweenDuration, tweenCurve), Axis.Z);
                    yield return Routine.WaitSeconds(pauseDuration);
                    if (tr == null) yield break;
                    yield return tr.RotateTo(Vector3.one * -shake, new TweenSettings(tweenDuration, tweenCurve),
                        Axis.Z);

                    if (i % 3 == 0)
                    {
                        yield return tr.RotateTo(Vector3.zero, new TweenSettings(tweenDuration, tweenCurve), Axis.Z);
                        if (loops != -1)
                        {
                            loops--;
                            if (loops == 0) yield break;
                        }
                        yield return Routine.WaitSeconds(longPauseDuration);
                    }

                    i++;
                }
            }

            var visualHelper = mono.GetComponent<TrickVisualHelper>();
            if (visualHelper == null) visualHelper = mono.gameObject.AddComponent<TrickVisualHelper>();
            visualHelper.TryInitializeUI();

            if (b)
            {
                visualHelper.ShakeRoutine = visualHelper.ShakeRoutine.Replace(HandleShake()).SetPhase(RoutinePhase.Update);
            }
            else
            {
                visualHelper.transform.SetRotation(Vector3.zero, Axis.Z);
                visualHelper.ShakeRoutine.Stop();
            }
        }

        public static void TransitionIn(this RectTransform registerRoot, TweenSettings tweenSettings,
            TrickTransitionDirection direction, float delay, Action completeCallback = null)
        {
            if (registerRoot == null) return;
            var visualHelper = registerRoot.GetComponent<TrickVisualHelper>();
            if (visualHelper == null) visualHelper = registerRoot.gameObject.AddComponent<TrickVisualHelper>();
            visualHelper.TryInitializeUI();

            var rt = registerRoot;
            var siz = rt.rect.size;
            var anchor = rt.anchoredPosition;
            visualHelper.OriginalAnchorPosition ??= anchor;
            switch (direction)
            {
                case TrickTransitionDirection.None:
                    break;
                case TrickTransitionDirection.Left:
                    rt.anchoredPosition = new Vector2(-siz.x * 2 * rt.pivot.x, visualHelper.OriginalAnchorPosition.Value.y);
                    visualHelper.TransitionRoutine = rt
                        .AnchorPosTo(visualHelper.OriginalAnchorPosition.Value, tweenSettings)
                        .OnComplete(completeCallback).DelayBy(delay).Play();
                    break;
                case TrickTransitionDirection.Top:
                    rt.anchoredPosition = new Vector2(visualHelper.OriginalAnchorPosition.Value.x, siz.y * 2 * rt.pivot.y);
                    visualHelper.TransitionRoutine = rt
                        .AnchorPosTo(visualHelper.OriginalAnchorPosition.Value, tweenSettings)
                        .OnComplete(completeCallback).DelayBy(delay).Play();
                    break;
                case TrickTransitionDirection.Right:
                    rt.anchoredPosition = new Vector2(siz.x * 2 * rt.pivot.x, visualHelper.OriginalAnchorPosition.Value.y);
                    visualHelper.TransitionRoutine = rt
                        .AnchorPosTo(visualHelper.OriginalAnchorPosition.Value, tweenSettings)
                        .OnComplete(completeCallback).DelayBy(delay).Play();
                    break;
                case TrickTransitionDirection.Bottom:
                    rt.anchoredPosition = new Vector2(visualHelper.OriginalAnchorPosition.Value.x, -siz.y * 2 * rt.pivot.y);
                    visualHelper.TransitionRoutine = rt
                        .AnchorPosTo(visualHelper.OriginalAnchorPosition.Value, tweenSettings)
                        .OnComplete(completeCallback).DelayBy(delay).Play();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        public static void TransitionIn(this MonoBehaviour registerRoot, TweenSettings tweenSettings,
            TrickTransitionDirection direction, float delay, Action completeCallback = null)
        {
            TransitionIn((RectTransform)registerRoot.transform, tweenSettings, direction, delay, completeCallback);
        }

        public static void TransitionOut(this RectTransform registerRoot, TweenSettings tweenSettings,
            TrickTransitionDirection direction, float delay, Action completeCallback = null)
        {
            if (registerRoot == null) return;

            var visualHelper = registerRoot.GetComponent<TrickVisualHelper>();
            if (visualHelper == null) visualHelper = registerRoot.gameObject.AddComponent<TrickVisualHelper>();
            visualHelper.TryInitializeUI();
            
            var rt = registerRoot;
            var siz = rt.rect.size;
            var anchor = rt.anchoredPosition;
            visualHelper.OriginalAnchorPosition ??= anchor;
            switch (direction)
            {
                case TrickTransitionDirection.None:
                    break;
                case TrickTransitionDirection.Left:
                    rt.anchoredPosition = visualHelper.OriginalAnchorPosition.GetValueOrDefault();
                    visualHelper.TransitionRoutine = rt.AnchorPosTo(new Vector2(-siz.x * 2 * rt.pivot.x, visualHelper.OriginalAnchorPosition.Value.y), tweenSettings)
                        .OnComplete(completeCallback).DelayBy(delay).Play();
                    break;
                case TrickTransitionDirection.Top:
                    rt.anchoredPosition = visualHelper.OriginalAnchorPosition.GetValueOrDefault();
                    visualHelper.TransitionRoutine = rt.AnchorPosTo(new Vector2(visualHelper.OriginalAnchorPosition.Value.x, siz.y * 2 * rt.pivot.y), tweenSettings)
                        .OnComplete(completeCallback).DelayBy(delay).Play();
                    break;
                case TrickTransitionDirection.Right:
                    rt.anchoredPosition = visualHelper.OriginalAnchorPosition.GetValueOrDefault();
                    visualHelper.TransitionRoutine = rt.AnchorPosTo(new Vector2(siz.x * 2 * rt.pivot.x, visualHelper.OriginalAnchorPosition.Value.y), tweenSettings)
                        .OnComplete(completeCallback).DelayBy(delay).Play();
                    break;
                case TrickTransitionDirection.Bottom:
                    rt.anchoredPosition = visualHelper.OriginalAnchorPosition.GetValueOrDefault();
                    visualHelper.TransitionRoutine = rt.AnchorPosTo(new Vector2(visualHelper.OriginalAnchorPosition.Value.x, -siz.y * 2 * rt.pivot.y), tweenSettings)
                        .OnComplete(completeCallback).DelayBy(delay).Play();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        public static void TransitionOut(this MonoBehaviour registerRoot, TweenSettings tweenSettings,
            TrickTransitionDirection direction, float delay, Action completeCallback = null)
        {
            TransitionOut((RectTransform)registerRoot.transform, tweenSettings, direction, delay, completeCallback);
        }
    }

    public enum TrickTransitionDirection
    {
        None,
        Left,
        Top,
        Right,
        Bottom,
    }

    public class TrickVisualMono : MonoBehaviour
    {
        public Vector2 HighlightBorderAlphaRange = new Vector2(0.0f, 1.0f);
        public HighlightState CurrentHighlightState { get; set; }
        public Image HighlightBorderImage { get; set; }

        private Routine _highlightRoutine;
    
        public void SetHighlighted(Image highlightImage, HighlightState state, Color? highlightColor = null)
        {
            if (highlightImage != null) HighlightBorderImage = highlightImage;

            if (HighlightBorderImage == null) return;
        
            IEnumerator Blink()
            {
                var go = gameObject;
                while (this != null)
                {
                    yield return Routine.WaitCondition(() => go == null || go.activeSelf, 0.1f);
                    if (go == null) yield break;
                    if (HighlightBorderImage != null)
                    {
                        yield return HighlightBorderImage.Fade(HighlightBorderAlphaRange.x, 0.3f, Curve.Linear, 0.0f,
                            null,
                            true);
                        yield return Routine.WaitSeconds(1.0f);
                        if (go == null) yield break;
                        yield return HighlightBorderImage.Fade(HighlightBorderAlphaRange.y, 0.5f, Curve.Linear, 0.0f,
                            null,
                            true);
                    }
                    else
                    {
                        yield break;
                    }
                }
            }

            if (highlightColor != null) HighlightBorderImage.color = highlightColor.GetValueOrDefault();

            switch (state)
            {
                case HighlightState.Off:
                    if (HighlightBorderImage != null) HighlightBorderImage.gameObject.SetActive(false);
                    _highlightRoutine.Stop();
                    break;
                case HighlightState.AlwaysOn:
                    if (HighlightBorderImage != null) HighlightBorderImage.gameObject.SetActive(true);
                    _highlightRoutine.Stop();
                    break;
                case HighlightState.Blinking:
                    if (HighlightBorderImage != null) HighlightBorderImage.gameObject.SetActive(true);
                    _highlightRoutine.Replace(Blink());
                    break;
            }

            CurrentHighlightState = state;
        }
    }
    
    public enum HighlightState
    {
        Off,
        AlwaysOn,
        Blinking,
    }
}