using UnityEngine;

namespace TrickCore
{
    public class TrickDontDestroyOnLoad : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}