using UnityEngine;

namespace TrickCore
{
    /// <summary>
    /// Simple script to mark a gameObject as DontDestroyOnLoad
    /// </summary>
    public class TrickDontDestroyOnLoad : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}