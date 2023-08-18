using System;
using System.Collections.Generic;
using System.Linq;
using BeauRoutine;
using TrickCore;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif

namespace TrickCore
{
    /// <summary>
    /// The base class of a UI menu. Inherit all your UI menu's from this!
    /// </summary>
    public abstract class UIMenu 
#if ODIN_INSPECTOR    
        : SerializedMonoBehaviour
#else
        : MonoBehaviour
#endif
    {
        [Header("Audio")] 
        public TrickAudioId MenuShowAudio;
        public bool StopMainTrack;
        
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

        internal float RenderScaleBefore;
        internal Camera MainCamera;
        internal Canvas MenuCanvas;
        internal int StartingSortingOrder;
        internal Stack<Action> HideCallbackOnceQueue { get; set; } = new Stack<Action>();
        
        private CanvasGroup _canvasGroup;
        private Routine _fadeRoutine;
        private int _numFadeOuts;
        private UIManager _manager;
        private bool _isOpen;
        private RectTransform _rt;
        private bool _transitionForceRebuild;

        public float LastShowTime { get; set; }
        public bool IsOpen => _isOpen;
        public bool IsTransitioning { get; private set; } = false;

        public bool IsInstantiated { get; private set; }
    
        private enum MenuAction { None, Show, Hide }
        private readonly Queue<MenuAction> _actionQueue = new Queue<MenuAction>();
        private MenuAction? _lastEnqueuedAction = null;

        public List<IUIMenuAction> DefaultActions = new()
        {
            new FixURPUIMenuAction(),
            new HandleAudioMenuAction(),
            new HandleMainCameraMenuAction(),
            new HandleSortingOrderMenuAction(),
            new HandleCallbackMenuAction(),
        };
        
        /// <summary>
        /// Check if the menu is focused, active and not faded out
        /// </summary>
        /// <returns></returns>
        public bool IsMenuFocused() => _numFadeOuts == 0 && gameObject.activeSelf;

        public void InternalInit()
        {
            MenuCanvas = GetComponent<Canvas>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            StartingSortingOrder = MenuCanvas != null ? MenuCanvas.sortingOrder : 0;
            MainCamera = Camera.main;
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

            if (_manager.MenuDebug != UIManager.MenuDebugMode.Off) Debug.Log($"[FadeIn-{GetType().Name}]: {_numFadeOuts}");
        
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
            if (_manager.MenuDebug != UIManager.MenuDebugMode.Off) Debug.Log($"[FadeOut-{GetType().Name}]: {_numFadeOuts}");
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
    
        // This function checks the current state and processes the next action in the queue.
        private void ProcessNextAction()
        {
            while (true)
            {
                if (IsTransitioning || _actionQueue.Count == 0) return;

                MenuAction nextAction = _actionQueue.Dequeue();

                if (nextAction == MenuAction.Show && !_isOpen)
                    ActualShow();
                else if (nextAction == MenuAction.Hide && _isOpen)
                    ActualHide();
                else
                    continue;
                break;
            }
        }

        // A helper function to add actions to the queue and initiate processing.
        private void EnqueueAction(MenuAction action)
        {
            if (_lastEnqueuedAction == action)
                return;

            _actionQueue.Enqueue(action);
            _lastEnqueuedAction = action;
            ProcessNextAction();
        }

        

        public virtual UIMenu Show()
        {
            EnqueueAction(MenuAction.Show);
            return this;
        }

        public virtual void Hide()
        {
            EnqueueAction(MenuAction.Hide);
        }
    
        /// <summary>
        /// Show the UI
        /// </summary>
        private UIMenu ActualShow()
        {
            if (_isOpen) return this;
            
            IsTransitioning = true;
        
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
                    
                    if (!_transitionForceRebuild)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(transition.TransitionPanelTransform);
                    }
                    
                    transition.TransitionPanelTransform.SetCanvasGroupInteractable(null, false);
                    
                    transition.TransitionPanelTransform.TransitionIn(transition.TransitionInSettings, transition.TransitionDirectionIn, transition.Delay, () =>
                    {
                        transition.TransitionPanelTransform.SetCanvasGroupInteractable(null, true);
                        
                        // ReSharper disable once AccessToModifiedClosure
                        if (++completed == Transitions.Count)
                            InternalShow();
                    });
                    
                    if (transition.PanelFading.HasFlag(FadeEnableType.In))
                        transition.TransitionPanelTransform.FadeIn(transition.FadeInSettings, delay: transition.Delay);
                }

                _transitionForceRebuild = true;
                
            }
            else
            {
                InternalShow();
            }

            if (MenuFading.HasFlag(FadeEnableType.In))
                FadeIn(ShowFadeInSettings);
            
            return this;

            void InternalShow()
            {
                LastShowTime = Time.realtimeSinceStartup;
                
                foreach (var action in DefaultActions)
                {
                    action.ExecuteShow(this);
                }
                
                if (_manager.MenuDebug != UIManager.MenuDebugMode.Off) Debug.Log($"[UIMenu] SHOW {this}");
                
                _manager.PostMenuShowEvent?.Invoke(this);
                _isOpen = true;
                IsTransitioning = false;
                ProcessNextAction();
            }
        }

        /// <summary>
        /// Hides the current menu
        /// </summary>
        private void ActualHide()
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
                    transition.TransitionPanelTransform.TransitionOut(transition.TransitionOutSettings, transition.TransitionDirectionOut, transition.Delay, () =>
                    {
                        // ReSharper disable once AccessToModifiedClosure
                        if (++completed == Transitions.Count) InternalHide();
                        transition.TransitionPanelTransform.SetCanvasGroupInteractable(null, true);
                    });
                    
                    if (transition.PanelFading.HasFlag(FadeEnableType.Out))
                    {
                        // wait for the panel to fade out and count as a complete
                        transition.TransitionPanelTransform.FadeOut(transition.FadeOutSettings, delay: transition.Delay);
                    }
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

            return;

            void InternalHide()
            {
                gameObject.SetActive(false);
            
                foreach (var action in DefaultActions)
                {
                    action.ExecuteHide(this);
                }

                if (_manager.MenuDebug != UIManager.MenuDebugMode.Off) Debug.Log($"[UIMenu] HIDE {this}");
                
                _manager.PostMenuHideEvent?.Invoke(this);
                _isOpen = false;
                IsTransitioning = false;
                ProcessNextAction();
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