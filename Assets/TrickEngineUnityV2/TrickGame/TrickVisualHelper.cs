using System;
using System.Collections;
using System.Linq;
using BeauRoutine;
using UnityEngine;
using UnityEngine.UI;

namespace TrickCore
{
    public class TrickVisualHelper : MonoBehaviour
    {
        public bool ParticleSystemInjectStartColor = true;
        public bool ParticleSystemInjectColorOverTime = true;
        public bool ParticleSystemInjectColorBySpeed = true;
    
        public CanvasGroup CurrentCanvasGroup { get; set; }
    
        // ps
        private bool _initializeForPS;
        private ParticleSystem.MinMaxGradient? _initMainModuleStartColor;
        private ParticleSystem.MinMaxGradient? _initColorOverTimeColor;
        private ParticleSystem.MinMaxGradient? _initColorBySpeed;
    
        // ui
        private bool _initializeForUI;
        private Routine _fadeRoutine;
        private Routine _scaleRoutine;
        private Routine _shakeRoutine;
        private Vector3? _localScale;
        private RectTransform _blurInside;
        private RectTransform _blurOutside;
        private Image _highlightMain;
        private Color? _originalBlurColor;
        private TrickHighlightBlur _borderBlur;

        private void TryInitializePS()
        {
            if (_initializeForPS) return;
        
            _initializeForPS = true;
        }
    
        private void TryInitializeUI()
        {
            if (_initializeForUI) return;
            CurrentCanvasGroup = GetComponent<CanvasGroup>();
            if (CurrentCanvasGroup == null) CurrentCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            _initializeForUI = true;
        }
    
        public static void SetAlpha(MonoBehaviour mono, float alpha)
        {
            var rt = mono.transform as RectTransform;
            SetAlpha(rt, alpha);
        }
    
        public static void SetAlpha(RectTransform mono, float alpha)
        {
            var tb = mono.GetComponent<TrickVisualHelper>();
            if (tb == null) tb = mono.gameObject.AddComponent<TrickVisualHelper>();
            tb.TryInitializeUI();
            tb.CurrentCanvasGroup.alpha = alpha;
        }

        public static void SetHighlighted(RectTransform mono, Image highlightBorderImage, HighlightState highlightState)
        {
            var trickButton = mono.GetComponent<TrickVisualMono>();
            if (trickButton == null) trickButton = mono.gameObject.AddComponent<TrickVisualMono>();
            trickButton.SetHighlighted(highlightBorderImage, highlightState);
        }

        public static void SetHighlighted(MonoBehaviour mono, Image highlightBorderImage, HighlightState highlightState)
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
    
        public static Routine Fade(MonoBehaviour mono, float fadeTarget = 0.0f, float fadeTime = 0.25f, float delay = 0.0f,
            float? setAlpha = null, bool interactable = false, bool withoutHost = false, Action completeAction = null)
        {
            var rt = mono.transform as RectTransform;
            if (rt != null) return Fade(rt, fadeTarget, fadeTime, delay, setAlpha, interactable, withoutHost, completeAction);
            return default;
        }
    
        public static Routine Fade(RectTransform mono, float fadeTarget = 0.0f, float fadeTime = 0.25f, float delay = 0.0f, float? setAlpha = null, bool interactable = false, bool withoutHost = false, Action completeAction = null)
        {
            var tb = mono.GetComponent<TrickVisualHelper>();
            if (tb == null) tb = mono.gameObject.AddComponent<TrickVisualHelper>();
            tb.TryInitializeUI();
            if (setAlpha != null) tb.CurrentCanvasGroup.alpha = setAlpha.GetValueOrDefault();
            var tween = tb.CurrentCanvasGroup.FadeTo(fadeTarget, fadeTime).DelayBy(delay).OnComplete(completeAction);
            tb._fadeRoutine.Replace(withoutHost ? tween.Play() : tween.Play(tb));
            tb.CurrentCanvasGroup.interactable = interactable;
            tb.CurrentCanvasGroup.blocksRaycasts = interactable;
            return tb._fadeRoutine;
        }

        public static Routine FadeIn(MonoBehaviour mono, float fadeTime = 0.25f, float delay = 0.0f, float? setAlpha = null, bool withoutHost = false, Action completeAction = null)
        {
            return Fade(mono, 1.0f, fadeTime, delay, setAlpha, true, withoutHost, completeAction);
        }

        public static Routine FadeOut(MonoBehaviour mono, float fadeTime = 0.25f, float delay = 0.0f, float? setAlpha = null, bool withoutHost = false, Action completeAction = null)
        {
            return Fade(mono, 0.0f, fadeTime, delay, setAlpha, false, withoutHost, completeAction);
        }

        public static Routine FadeIn(RectTransform mono, float fadeTime = 0.25f, float delay = 0.0f, float? setAlpha = null, bool withoutHost = false, Action completeAction = null)
        {
            return Fade(mono, 1.0f, fadeTime, delay, setAlpha, true, withoutHost, completeAction);
        }

        public static Routine FadeOut(RectTransform mono, float fadeTime = 0.25f, float delay = 0.0f, float? setAlpha = null, bool withoutHost = false, Action completeAction = null)
        {
            return Fade(mono, 0.0f, fadeTime, delay, setAlpha, false, withoutHost, completeAction);
        }

        public static Routine ScaleTransformPingPong(MonoBehaviour mono, float scaleTarget, float scaleDuration = 0.35f, float delay = 0.0f, Action startCallback = null) => ScaleTransformPingPong(mono.transform as RectTransform, scaleTarget, scaleDuration, delay, startCallback);
        public static Routine ScaleTransformPingPong(RectTransform mono, float scaleTarget, float scaleDuration = 0.35f, float delay = 0.0f, Action startCallback = null)
        {
            var tb = mono.GetComponent<TrickVisualHelper>();
            if (tb == null) tb = mono.gameObject.AddComponent<TrickVisualHelper>();
            tb.TryInitializeUI();
            tb._localScale ??= mono.localScale;
            tb._localScale = tb._localScale.Value;
        
            return tb._scaleRoutine.Replace(mono.ScaleTo(tb._localScale.Value * scaleTarget, new TweenSettings()
            {
                Time = scaleDuration,
                Curve = Curve.Smooth
            }).DelayBy(delay).OnStart(startCallback).Yoyo().OnComplete(() =>
            {
                mono.localScale = tb._localScale.Value;
            }).Play(tb));
        }
    
        public static void ResetParticleSystemColor(ParticleSystem particleSystem, Color color, bool injectChildSystems)
        {
            if (particleSystem == null) return;
            var visualHelper = particleSystem.GetComponent<TrickVisualHelper>();
            if (visualHelper == null) visualHelper = particleSystem.gameObject.AddComponent<TrickVisualHelper>();
            visualHelper.TryInitializePS();
        
            void ResetParticleColor(ParticleSystem ps)
            {
                if (visualHelper._initMainModuleStartColor != null)
                {
                    if (visualHelper.ParticleSystemInjectStartColor)
                    {
                        var mainModule = ps.main;
                        mainModule.startColor = visualHelper._initMainModuleStartColor.Value;
                    }

                    if (visualHelper.ParticleSystemInjectColorOverTime)
                    {
                        var colorOverTimeModule = ps.colorOverLifetime;
                        if (colorOverTimeModule.enabled && visualHelper._initColorOverTimeColor != null) colorOverTimeModule.color = visualHelper._initColorOverTimeColor.Value;
                    }

                    if (visualHelper.ParticleSystemInjectColorBySpeed)
                    {
                        var colorBySpeedModule = ps.colorBySpeed;
                        if (colorBySpeedModule.enabled && visualHelper._initColorBySpeed != null) colorBySpeedModule.color = visualHelper._initColorBySpeed.Value;
                    }
                }
            }
            if (injectChildSystems)
            {
                foreach (var ps in particleSystem.GetComponentsInChildren<ParticleSystem>()) SetParticleSystemColor(ps, color, false);
            }
            else
            {
                ResetParticleColor(particleSystem);
            }
        }
        public static void SetParticleSystemColor(ParticleSystem particleSystem, Color color, bool injectChildSystems)
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
                    gradientMin = minMaxGradient.gradientMin != null ? new Gradient()
                    {
                        mode = minMaxGradient.gradientMin.mode,
                        alphaKeys = minMaxGradient.gradientMin.alphaKeys.ToArray(),
                        colorKeys = minMaxGradient.gradientMin.colorKeys.ToArray(),
                    } : minMaxGradient.gradientMin,
                    gradientMax = minMaxGradient.gradientMax != null ? new Gradient()
                    {
                        mode = minMaxGradient.gradientMax.mode,
                        alphaKeys = minMaxGradient.gradientMax.alphaKeys.ToArray(),
                        colorKeys = minMaxGradient.gradientMax.colorKeys.ToArray(),
                    } : minMaxGradient.gradientMax,
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
                if (visualHelper.ParticleSystemInjectStartColor)
                {
                    var mainModule = ps.main;
                    visualHelper._initMainModuleStartColor ??= mainModule.startColor;

                    var minMaxGradient = visualHelper._initMainModuleStartColor.Value;
                    mainModule.startColor = GetProcessedMinMaxGradient(minMaxGradient);
                }

                if (visualHelper.ParticleSystemInjectColorOverTime)
                {
                    var colorOverTimeModule = ps.colorOverLifetime;
                    if (colorOverTimeModule.enabled)
                    {
                        visualHelper._initColorOverTimeColor ??= colorOverTimeModule.color;
                        colorOverTimeModule.color = GetProcessedMinMaxGradient(visualHelper._initColorOverTimeColor.Value);
                    }
                }

                if (visualHelper.ParticleSystemInjectColorBySpeed)
                {
                    var colorBySpeedModule = ps.colorBySpeed;
                    if (colorBySpeedModule.enabled)
                    {
                        visualHelper._initColorBySpeed ??= colorBySpeedModule.color;
                        colorBySpeedModule.color = GetProcessedMinMaxGradient(visualHelper._initColorBySpeed.Value);
                    }
                }
            }

            if (injectChildSystems)
            {
                foreach (var ps in particleSystem.GetComponentsInChildren<ParticleSystem>()) SetParticleSystemColor(ps, color, false);
            }
            else
            {
                SetParticleColor(particleSystem);
            }
        }
    
        public static void Shake(MonoBehaviour mono, bool b, float shake = 20.0f, float pauseDuration = 0.05f, float longPauseDuration = 0.85f, float tweenDuration = 0.35f, Curve tweenCurve = Curve.BounceInOut)
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
                    yield return tr.RotateTo(Vector3.one * -shake, new TweenSettings(tweenDuration, tweenCurve), Axis.Z);

                    if (i % 3 == 0)
                    {
                        yield return tr.RotateTo(Vector3.zero, new TweenSettings(tweenDuration, tweenCurve), Axis.Z);
                        yield return Routine.WaitSeconds(longPauseDuration);
                    }

                    i++;
                }
            }
        
            var visualHelper = mono.GetComponent<TrickVisualHelper>();
            if (visualHelper == null) visualHelper = mono.gameObject.AddComponent<TrickVisualHelper>();
            visualHelper.TryInitializeUI();

            if (b) visualHelper._shakeRoutine = visualHelper._shakeRoutine.Replace(HandleShake()).SetPhase(RoutinePhase.Update);
            else
            {
                visualHelper.transform.SetRotation(Vector3.zero, Axis.Z);
                visualHelper._shakeRoutine.Stop();
            }
        }
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
                        yield return TrickVisualHelper.Fade(HighlightBorderImage, HighlightBorderAlphaRange.x, 0.3f, 0.0f,
                            null,
                            true);
                        yield return Routine.WaitSeconds(1.0f);
                        if (go == null) yield break;
                        yield return TrickVisualHelper.Fade(HighlightBorderImage, HighlightBorderAlphaRange.y, 0.5f, 0.0f,
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
}