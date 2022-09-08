using System;
using BeauRoutine;
using UnityEngine;

namespace TrickCore
{
    [Serializable]
    public class TransitionGroup
    {
        public TweenSettings TransitionInSettings;
    
        /// <summary>
        /// Tween settings for fading in when hiding the menu
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
        /// The time/curve of the transition
        /// </summary>
        public TweenSettings TransitionTweenSettings;
    
        /// <summary>
        /// The panel transform of the menu, used for smooth menu transitions
        /// </summary>
        public RectTransform TransitionPanelTransform;

        /// <summary>
        /// If enabled, we fade the transition panel. 
        /// </summary>
        public FadeEnableType TransitionPanelFading = FadeEnableType.Off;

        /// <summary>
        /// Transition fade in
        /// </summary>
        public TweenSettings FadeInSettings;
    
        /// <summary>
        /// Transition fade out
        /// </summary>
        public TweenSettings FadeOutSettings;

        /// <summary>
        /// Delay for the fade
        /// </summary>
        public float Delay;
    }
}