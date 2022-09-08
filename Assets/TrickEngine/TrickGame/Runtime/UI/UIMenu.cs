using System;
using System.Collections.Generic;
using BeauRoutine;
using TrickCore;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TrickCore
{
    /// <summary>
    /// The base class of a UI menu. Inherit all your UI menu's from this!
    /// </summary>
    public abstract class UIMenu : MonoBehaviour
    {
        /// <summary>
        /// Tween settings for fading in when showing the menu
        /// </summary>
        [Header("Animation & Transition")]
        public TweenSettings ShowFadeInSettings;
    
        /// <summary>
        /// Tween settings for fading in when hiding the menu
        /// </summary>
        public TweenSettings HideFadeOutSettings;

        /// <summary>
        /// If enabled, we fade the actual UIMenu object
        /// </summary>
        public FadeEnableType MenuFading = FadeEnableType.Off;
        
        public List<TransitionGroup> Transitions = new List<TransitionGroup>();

        /// <summary>
        /// Handle screen size scaling
        /// </summary>
        [Header("Settings")]
        public bool ScaleWithScreen = true;
    
        /// <summary>
        /// True if always on Top (sorting order, still matters), ignores CloseAll and SaveStates
        /// </summary>
        public bool AlwaysOnTop;

        /// <summary>
        /// True to disable the main camera if the menu is shown and turn back on if the menu is hidden 
        /// </summary>
        public bool DisableMainCamera;
    
        /// <summary>
        /// A custom canvas camera, for rendering world objects into the UI
        /// </summary>
        public Camera CanvasCamera;
    
        /// <summary>
        /// The canvas camera plane distance
        /// </summary>
        public float PlaneDistance;
    
        /// <summary>
        /// True if we need to fix the render scale, otherwise the UI might not look sharp
        /// </summary>
        public bool FixRenderScale;

        private float _renderScaleBefore;
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private Routine _fadeRoutine;
        private int _numFadeOuts;
        private UIManager _manager;
        private Camera _mainCamera;
        private bool _isOpen;
        private int _startingSortingOrder;
        private RectTransform _rt;

        public float LastShowTime { get; set; }
        public bool IsOpen => _isOpen;
        public bool IsInstantiated { get; private set; }
        private Stack<Action> HideCallbackOnceQueue { get; set; } = new Stack<Action>();
    
        /// <summary>
        /// Check if the menu is focused, active and not faded out
        /// </summary>
        /// <returns></returns>
        public bool IsMenuFocused() => _numFadeOuts == 0 && gameObject.activeSelf;

        public void InternalInit()
        {
            _canvas = GetComponent<Canvas>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _startingSortingOrder = _canvas != null ? _canvas.sortingOrder : 0;
            _mainCamera = Camera.main;
            _rt = transform as RectTransform;
            AddressablesAwake();
        
        }

        public void InternalStart()
        {
            AddressablesStart();
        }

        protected virtual void AddressablesAwake()
        {
            if (CanvasCamera == null) return;
            CanvasCamera.transform.SetParent(null, true);
        
            Canvas canvas = GetComponent<Canvas>();
            canvas.worldCamera = CanvasCamera;
            canvas.planeDistance = PlaneDistance;
        
            CanvasCamera.transform.SetParent(transform, true);
        }

        protected virtual void AddressablesStart()
        {
        
        }

        [Obsolete]
        protected virtual void Awake()
        {

        }

        /// <summary>
        /// Set's a callback when the menu is hidden
        /// </summary>
        /// <param name="hideCallbackOnce"></param>
        /// <returns></returns>
        public UIMenu SetHideCallbackOnce(Action hideCallbackOnce)
        {
            HideCallbackOnceQueue.Push(hideCallbackOnce);
            return this;
        }

        /// <summary>
        /// Rescale the UI if not scaled yet.
        /// </summary>
        public void Rescale()
        {
            float normalScale = UIManager.Instance.TargetScaleReference;
            float thisScale = (float)Screen.width / Screen.height;
            float scaleFactor = normalScale / thisScale;
            _rt.sizeDelta = new Vector2(UIManager.Instance.TargetScaleWidth / scaleFactor, _rt.sizeDelta.y);
        }

        public virtual Routine FadeIn(TweenSettings fadeTime, float? alpha = null, Action callback = null)
        {
            if (alpha != null) SetAlpha(alpha.Value);
        
            _numFadeOuts--;
            if (_numFadeOuts < 0) _numFadeOuts = 0;

            if (!_manager.DisableMenuDebugging) Debug.Log($"[FadeIn-{GetType().Name}]: {_numFadeOuts}");
        
            if (_numFadeOuts == 0)
            {
                return _fadeRoutine.Replace(_canvasGroup != null
                    ? _canvasGroup.FadeTo(1.0f, fadeTime).OnStart(() =>
                    {
                        _canvasGroup.blocksRaycasts = true;
                        _canvasGroup.interactable = true;
                    }).OnComplete(callback).Play()
                    : default);
            }
            return _fadeRoutine;
        }

        public virtual Routine FadeIn(float fadeTime = 0.25f, float? alpha = null, Action callback = null) =>
            FadeIn(new TweenSettings(fadeTime), alpha, callback);

        public virtual Routine FadeOut(TweenSettings fadeTime, float? alpha = null, Action callback = null)
        {
            if (alpha != null) SetAlpha(alpha.Value);
            _numFadeOuts++;
            if (!_manager.DisableMenuDebugging) Debug.Log($"[FadeOut-{GetType().Name}]: {_numFadeOuts}");
            if (_numFadeOuts == 1)
            {
                return _fadeRoutine.Replace(_canvasGroup != null
                    ? _canvasGroup.FadeTo(0.0f, fadeTime).OnComplete(() =>
                    {
                        _canvasGroup.blocksRaycasts = false;
                        _canvasGroup.interactable = false;
                        callback?.Invoke();
                    }).Play()
                    : default);
            }
        
            return _fadeRoutine;
        }

        public virtual Routine FadeOut(float fadeTime = 0.25f, float? alpha = null, Action callback = null) =>
            FadeOut(new TweenSettings(fadeTime), alpha, callback);

        /// <summary>
        /// Sets the depth of the fadeout
        /// </summary>
        /// <param name="depth"></param>
        public void SetFadeOutDepth(int depth) => _numFadeOuts = depth;
    
        /// <summary>
        /// Gets the current alpha of the menu
        /// </summary>
        /// <returns></returns>
        public float GetAlpha() => _canvasGroup == null ? 0.0f : _canvasGroup.alpha;

        /// <summary>
        /// Set's the alpha of the menu
        /// </summary>
        /// <param name="alpha"></param>
        public void SetAlpha(float alpha)
        {
            if (_canvasGroup == null) return;
            _canvasGroup.alpha = alpha;
            _canvasGroup.blocksRaycasts = alpha > 0;
            _canvasGroup.interactable = alpha > 0;
        }

        public virtual void OnLogout()
        {

        }

        /// <summary>
        /// Toggles the open state of the menu
        /// </summary>
        public void Toggle()
        {
            if (_isOpen) Hide();
            else Show();
        }
    
    
        /// <summary>
        /// Show the UI
        /// </summary>
        public virtual UIMenu Show()
        {
            if (_isOpen) return this;
        
            if (ScaleWithScreen) Rescale();
            gameObject.SetActive(true);
        
            _manager.PreMenuShowEvent?.Invoke(this);
        
            if (Transitions != null && Transitions.Count > 0)
            {
                int completed = 0;
                foreach (var transition in Transitions)
                {
                    if (transition.TransitionPanelTransform == null)
                    {
                        if (++completed == Transitions.Count)
                            InternalShow();
                        continue;
                    }
                    
                    transition.TransitionPanelTransform.SetCanvasGroupInteractable(null, false);
                    
                    transition.TransitionPanelTransform.TransitionIn(transition.TransitionTweenSettings, transition.TransitionDirectionIn, () =>
                    {
                        transition.TransitionPanelTransform.SetCanvasGroupInteractable(null, true);
                        
                        // ReSharper disable once AccessToModifiedClosure
                        if (++completed == Transitions.Count)
                            InternalShow();
                    });
                    
                    if (transition.TransitionPanelFading.HasFlag(FadeEnableType.In))
                        transition.TransitionPanelTransform.FadeIn(ShowFadeInSettings);
                }
                
                if (MenuFading.HasFlag(FadeEnableType.In))
                    FadeIn(ShowFadeInSettings);
            }
            else
            {
                InternalShow();
                if (MenuFading.HasFlag(FadeEnableType.In))
                    FadeIn(ShowFadeInSettings);
            }

            void InternalShow()
            {
                _isOpen = true;
                _manager.PostMenuShowEvent?.Invoke(this);
                LastShowTime = Time.realtimeSinceStartup;
                if (DisableMainCamera)
                {
                    if (_mainCamera == null) _mainCamera = Camera.main;
                    if (_mainCamera != null) _mainCamera.enabled = false;
                }

                if (FixRenderScale && GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset asset)
                {
                    _renderScaleBefore = asset.renderScale;
            
                    if (asset.renderScale < 1)
                        asset.renderScale = 1.0f;
                }

#if UNITY_EDITOR
                if (!_manager.DisableMenuDebugging) Debug.Log($"[UIMenu] SHOW {this}");
#endif
            }
        
            return this;
        }

        /// <summary>
        /// Hides the current menu
        /// </summary>
        public virtual void Hide()
        {
            if (!_isOpen) return;
        
            _manager.PreMenuHideEvent?.Invoke(this);
        
            if (Transitions != null && Transitions.Count > 0)
            {
                int completed = 0;
                foreach (var transition in Transitions)
                {
                    if (transition.TransitionPanelTransform == null)
                    {
                        if (++completed == Transitions.Count)
                            InternalHide();
                        continue;
                    }

                    transition.TransitionPanelTransform.SetCanvasGroupInteractable(null, false);
                    transition.TransitionPanelTransform.TransitionOut(transition.TransitionTweenSettings, transition.TransitionDirectionOut, () =>
                    {
                        if (transition.TransitionPanelFading.HasFlag(FadeEnableType.Out))
                        {
                            // wait for the panel to fade out and count as a complete
                            transition.TransitionPanelTransform.FadeOut(HideFadeOutSettings, completeAction: () =>
                            {
                                // ReSharper disable once AccessToModifiedClosure
                                if (++completed == Transitions.Count) InternalHide();
                            });
                        }
                        else
                        {
                            if (++completed == Transitions.Count) InternalHide();
                        }
                        transition.TransitionPanelTransform.SetCanvasGroupInteractable(null, true);
                    });
                }

                if (MenuFading.HasFlag(FadeEnableType.Out))
                    FadeOut(HideFadeOutSettings);
            }
            else
            {
                if (MenuFading.HasFlag(FadeEnableType.Out))
                    FadeOut(HideFadeOutSettings, null, InternalHide);
                else
                    InternalHide();
            }

            void InternalHide()
            {
                _isOpen = false;
            
                gameObject.SetActive(false);
            
                if (_canvas != null) _canvas.sortingOrder = _startingSortingOrder;

                _manager.PostMenuHideEvent?.Invoke(this);
                if (DisableMainCamera)
                {
                    if (_mainCamera == null) _mainCamera = Camera.main;
                    if (_mainCamera != null) _mainCamera.enabled = true;
                }

                if (FixRenderScale && GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset asset)
                {
                    asset.renderScale = _renderScaleBefore;
                }

                if (HideCallbackOnceQueue.Count > 0)
                {
                    HideCallbackOnceQueue.Pop()?.Invoke();
                }

#if UNITY_EDITOR
                if (!_manager.DisableMenuDebugging) Debug.Log($"[UIMenu] HIDE {this}");
#endif
            }
        }

        /// <summary>
        /// Silently hides the menu, without invoking any events. Basically just disabling the gameObject.
        /// </summary>
        public void HideSilent()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Silently shows the menu, without invoking any events. Basically just enabling the gameObject.
        /// </summary>
        public void ShowSilent()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Sets the manager who created the UI
        /// </summary>
        /// <param name="uiManager"></param>
        internal void InternalSetManager(UIManager uiManager)
        {
            _manager = uiManager;
            IsInstantiated = true;
        }
    }
}