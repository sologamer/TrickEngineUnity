using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TrickCore
{
    public class TrickPointerClickAudio : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            UIManager.Instance.PlayButtonAudio();
        }
    }
}
