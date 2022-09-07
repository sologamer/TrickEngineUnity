using System;
using System.Collections.Generic;
using System.Linq;
using TrickCore;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TrickCore
{
    /// <summary>
    /// The UIManager for management of User Interfaces (UIMenu classes).
    /// Supporting fade in/out and transition
    /// Usage:
    /// - UIManager.Instance.GetMenu
    /// - UIManager.Instance.TryShow
    /// - UIManager.Instance.TryHide
    /// </summary>
    public class UIManager : MonoSingleton<UIManager>
    {
        public List<UIMenu> MenuAssets = new List<UIMenu>();

        [Tooltip("The resolution we try to match the screen size with for the CanvasScaler")]
        public float MatchScreenValue = 1.34f;
    
        [Tooltip("A delay prevent from playing button audio took quick")]
        public float PlayAudioButtonPreventDelay = 0.1f;
    
        [Tooltip("The audio source to play the button sound in")]
        public AudioSource ButtonAudioSource;
    
        [Tooltip("Enable for debugging menu's")]
        public bool DisableMenuDebugging;
    
        public float TargetScaleWidth = 1920f;
        public float TargetScaleHeight = 1080f;
        public float TargetScaleReference => TargetScaleWidth / TargetScaleHeight;

        public UnityEvent<int, int> ScreenSizeChangeEvent { get; } = new UnityEvent<int, int>();
        public UnityEvent<UIMenu> PreMenuShowEvent { get; } = new UnityEvent<UIMenu>();
        public UnityEvent<UIMenu> PreMenuHideEvent { get; } = new UnityEvent<UIMenu>();
        public UnityEvent<UIMenu> PostMenuShowEvent { get; } = new UnityEvent<UIMenu>();
        public UnityEvent<UIMenu> PostMenuHideEvent { get; } = new UnityEvent<UIMenu>();
    
        #region Internal states
        private Vector2Int _lastScreenSize;
        private float LastTimeButtonClickAudioPlayed { get; set; }
        private Stack<List<UIMenu>> SavedStates { get; set; } = new Stack<List<UIMenu>>();

        private List<UIMenu> _uiMenus = new List<UIMenu>();
        private readonly Dictionary<Type, UIMenu> _uiMenusInstantiated = new Dictionary<Type, UIMenu>();

        #endregion
    
        protected override void Initialize()
        {
            base.Initialize();

            _uiMenus = MenuAssets.Where(menu => menu != null).ToList();
        }
    
        /// <summary>
        /// Saves all instantiated menus and hides them
        /// </summary>
        public void SaveStates()
        {
            var states = _uiMenusInstantiated.Where(menu => (!menu.Value.AlwaysOnTop) && menu.Value.IsOpen).Select(pair => pair.Value).ToList();
            SavedStates.Push(states);
            foreach (UIMenu menu in states) menu.HideSilent();
        }
    
        /// <summary>
        /// Shows the last saved menu states
        /// </summary>
        public void RestoreStates()
        {
            if (SavedStates.Count > 0)
            {
                var states = SavedStates.Pop();
                states.ForEach(menu => menu.ShowSilent());
            }
        }

        /// <summary>
        /// Close all shown menu's
        /// </summary>
        /// <param name="hideAlwaysOnTop">True if we also close all AlwaysOnTop marked menus</param>
        public void CloseAll(bool hideAlwaysOnTop)
        {
            _uiMenusInstantiated.ToList().ForEach(menu =>
            {
                if (menu.Value.IsOpen)
                {
                    if (!menu.Value.AlwaysOnTop)
                    {
                        menu.Value.Hide();
                    }
                    else if (hideAlwaysOnTop)
                    {
                        // Hide popups that are marked as AlwaysOnTop
                        menu.Value.Hide();
                    }
                }
            });
        }

        /// <summary>
        /// Gets a menu, if the menu is not found, instantiate it
        /// </summary>
        /// <typeparam name="T">The menu type to get</typeparam>
        /// <returns>The instantiated menu</returns>
        public T GetMenu<T>() where T : UIMenu
        {
            if (_uiMenusInstantiated.TryGetValue(typeof(T), out var instantiatedMenu)) return (T) instantiatedMenu;
            var menuToInstantiate = _uiMenus.Find(menu => menu != null && menu.GetType() == typeof(T)) as T;
            if (menuToInstantiate == null)
            {
                Debug.LogError($"The menu you are trying to instantiate is null ({typeof(T).Name})");
                return null;
            }
            return (T) InternalInstantiateMenu(menuToInstantiate);
        }

        /// <summary>
        /// Gets the list of all instantiated menu's
        /// </summary>
        /// <returns></returns>
        public List<UIMenu> GetInstantiatedMenus()
        {
            return _uiMenusInstantiated.Values.ToList();
        }

        /// <summary>
        /// Gets a menu by type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public UIMenu GetMenuByType(Type type)
        {
            if (type == null) return null;
            if (_uiMenusInstantiated.TryGetValue(type, out var instantiatedMenu)) return instantiatedMenu;
            var menuToInstantiate = _uiMenus.Find(menu => menu != null && menu.GetType() == type);
            if (menuToInstantiate == null)
            {
                Debug.LogError($"The menu you are trying to instantiate is null (name={type.Name})");
                return null;
            }
            return InternalInstantiateMenu(menuToInstantiate);
        }

        /// <summary>
        /// Gets a menu by it's name
        /// </summary>
        /// <param name="menuName"></param>
        /// <returns></returns>
        public UIMenu GetMenuByName(string menuName)
        {
            var type = Type.GetType(menuName, false);
            return type == null ? null : GetMenuByType(type);
        }
    
        /// <summary>
        /// Instantiates and inject the menu
        /// </summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        private UIMenu InternalInstantiateMenu(UIMenu menu)
        {
            var newMenu = Instantiate(menu, transform);
            if (newMenu != null)
            {
                newMenu.InternalSetManager(this);
                var scaler = newMenu.GetComponent<CanvasScaler>();
                scaler.matchWidthOrHeight = (float) _lastScreenSize.x / _lastScreenSize.y <= MatchScreenValue ? 0.0f : 1.0f;
            
                _uiMenusInstantiated[newMenu.GetType()] = newMenu;
            
                newMenu.GetComponentsInChildren<Button>(true).ToList().ForEach(button =>
                {
                    if (button != null) button.onClick.AddListener(PlayButtonAudio);
                });
                newMenu.GetComponentsInChildren<Toggle>(true).ToList().ForEach(toggle =>
                {
                    if (toggle != null) toggle.onValueChanged.AddListener((b) => PlayButtonAudio());
                });
                newMenu.gameObject.SetActive(false);
            
                newMenu.InternalInit();
                newMenu.InternalStart();
            }
            else
            {
                Debug.LogWarning(menu.name + " Has no UIMenu");
            }
            return newMenu;
        }
    
        /// <summary>
        /// Close everything and shows the target UI
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ShowOnly<T>() where T : UIMenu
        {
            T ui = GetMenu<T>();
            CloseAll(false);
            if (ui != null)
            {
                ui.Show();
                return ui;
            }
            else
            {
                return null;
            }
        }
    
        /// <summary>
        /// Invokes OnLogout() on all UIMenu's, even on non instantiated menus
        /// </summary>
        public void Logout()
        {
            var exceptList = new List<Type>();
            _uiMenusInstantiated.Values.ToList().ForEach(menu =>
            {
                menu.OnLogout();
                exceptList.Add(menu.GetType());
            });
            _uiMenus.Where(menu => !exceptList.Contains(menu.GetType())).ToList().ForEach(menu => menu.OnLogout());
        }

        /// <summary>
        /// Toggles (show or hide) the menu active state.
        /// </summary>
        public T ToggleMenu<T>() where T : UIMenu => ToggleMenu<T>(null, null);
    
        /// <summary>
        /// Toggles (show or hide) the menu active state.
        /// </summary>
        public T ToggleMenu<T>(Action<T> actionBefore, Action<T> actionAfter) where T : UIMenu
        {
            var menu = GetMenu<T>();
            actionBefore?.Invoke(menu);
            menu.Toggle();
            actionAfter?.Invoke(menu);
            return menu;
        }

        /// <summary>
        /// Tries to show if the menu is not Open
        /// </summary>
        public T TryShow<T>() where T : UIMenu => TryShow<T>(null);
        /// <summary>
        /// Tries to show if the menu is not Open
        /// </summary>
        public T TryShow<T>(Action<T> showAction) where T : UIMenu
        {
            var menu = GetMenu<T>();
            if (menu.IsOpen) return menu;
            menu.Show();
            showAction?.Invoke(menu);
            return menu;
        }
    
        /// <summary>
        /// Tries to hide if the menu is Open
        /// </summary>
        public T TryHide<T>() where T : UIMenu => TryHide<T>(null);
    
        /// <summary>
        /// Tries to hide if the menu is Open
        /// </summary>
        public T TryHide<T>(Action<T> hideAction) where T : UIMenu
        {
            var menu = GetMenu<T>();
            if (!menu.IsOpen) return menu;
            menu.Hide();
            hideAction?.Invoke(menu);
            return menu;
        }

        /// <summary>
        /// Plays a button audio
        /// </summary>
        public void PlayButtonAudio()
        {
            var time = Time.time;
            if (Mathf.Abs(time - LastTimeButtonClickAudioPlayed) < PlayAudioButtonPreventDelay) return;
            LastTimeButtonClickAudioPlayed = time;
            if (ButtonAudioSource != null) ButtonAudioSource.Play();
        }

        public void Update()
        {
            var screenSize = new Vector2Int(Screen.width,Screen.height);
            if (_lastScreenSize != screenSize)
            {
                _lastScreenSize = screenSize;
                ScreenSizeChangeEvent?.Invoke(screenSize.x, screenSize.y);
            }
        }
    }
}