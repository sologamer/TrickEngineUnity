using System;
using UnityEngine;

namespace TrickCore
{
    public class TrickVisualRotate : MonoBehaviour
    {
        public Vector3 Axis = new Vector3(0, 0, 1);
        public float Speed = 5.0f;
        
        private Transform _tr;

        private void Awake()
        {
            _tr = transform;
        }

        private void Update()
        {
            _tr.localEulerAngles += Axis * (Speed * Time.deltaTime);
        }
    }
}