﻿using BeauRoutine;
using UnityEngine;

namespace TrickCore
{
    /// <summary>
    /// Simple scaler script using BeauRoutine to scale the object
    /// </summary>
    public class TrickVisualScale : MonoBehaviour
    {
        public TweenSettings TweenSettings;
        public Vector3 ScaleFrom = Vector3.one;
        public Vector3 ScaleTarget = Vector3.one;
        
        private Transform _tr;
        private Routine _scaleRoutine;

        private void OnEnable()
        {
            _tr = transform;
            Play();
        }

        private void OnDisable()
        {
            _tr = transform;
            _tr.localScale = ScaleFrom;
            _scaleRoutine.Stop();
        }

        public void Play()
        {
            _tr.localScale = ScaleFrom;
            _scaleRoutine.Replace(_tr.ScaleTo(ScaleTarget, TweenSettings).YoyoLoop().Play());
        }
    }
}