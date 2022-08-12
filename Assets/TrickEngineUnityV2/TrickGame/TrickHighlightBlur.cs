using System.Collections;
using BeauRoutine;
using UnityEngine;
using UnityEngine.UI;

namespace TrickCore
{
    public class TrickHighlightBlur : MonoBehaviour
    {
        public float TweenTimeZero;
        public float TweenTimeOne;
        public Vector2 TweenFadeRange;
        public Curve TweenCurve;
        public float TweenPause;
        public CanvasGroup CanvasGroup;
        public Image BlurImage;

        private Routine _routine;
    
        private void OnEnable()
        {
            _routine = _routine.Replace(this, Fader());
        }

        private IEnumerator Fader()
        {
            while (this != null)
            {
                yield return CanvasGroup.FadeTo(TweenFadeRange.x, new TweenSettings(TweenTimeZero, TweenCurve));
                yield return Routine.WaitSeconds(TweenPause);
                yield return CanvasGroup.FadeTo(TweenFadeRange.y, new TweenSettings(TweenTimeOne, TweenCurve));
            }
        }

        private void OnDisable()
        {
            _routine.Stop();
        }
    }
}