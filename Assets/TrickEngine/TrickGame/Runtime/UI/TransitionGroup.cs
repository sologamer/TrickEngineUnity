using System;
using BeauRoutine;
using UnityEngine;

namespace TrickCore
{
    [Serializable]
    public class TransitionGroup
    {
        /// <summary>
        /// The panel transform of the menu, used for smooth menu transitions
        /// </summary>
        public RectTransform TransitionPanelTransform;

        /// <summary>
        /// Delay for the fade
        /// </summary>
        public float Delay;
        
        /// <summary>
        /// Tween settings for transition IN
        /// </summary>
        [Header("Transition")]
        public TweenSettings TransitionInSettings;
    
        /// <summary>
        /// Tween settings for transition OUT
        /// </summary>
        public TweenSettings TransitionOutSettings;
            
        /// <summary>
        /// The way the menu is transitioned in
        /// </summary>
        public TrickTransitionDirection TransitionDirectionIn;
    
        /// <summary>
        /// The way the menu is transitioned out
        /// </summary>
        public TrickTransitionDirection TransitionDirectionOut;
    
        
        /// <summary>
        /// If enabled, we fade the transition panel. 
        /// </summary>
        [Header("Fading")]
        public FadeEnableType PanelFading = FadeEnableType.Off;

        /// <summary>
        /// Transition fade in
        /// </summary>
        public TweenSettings FadeInSettings;
    
        /// <summary>
        /// Transition fade out
        /// </summary>
        public TweenSettings FadeOutSettings;
    }
}