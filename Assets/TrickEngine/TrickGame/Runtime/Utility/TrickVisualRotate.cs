using System;
using UnityEngine;

namespace TrickCore
{
    /// <summary>
    /// Simple rotation script, rotation is applied in the Update loop and scales with the speed*deltaTime.
    /// </summary>
    public class TrickVisualRotate : MonoBehaviour
    {
        /// <summary>
        /// The rotation axis (euler)
        /// </summary>
        public Vector3 Axis = new Vector3(0, 0, 1);
        
        /// <summary>
        /// The speed of the rotation
        /// </summary>
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