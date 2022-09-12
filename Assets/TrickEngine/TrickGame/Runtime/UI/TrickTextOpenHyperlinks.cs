using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace TrickCore
{
    /// <summary>
    /// Helper script for handling hyperlinks in text
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class TrickTextOpenHyperlinks : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        public bool DoesColorChangeOnHover = true;
        public Color HoverColor = new Color(60f / 255f, 120f / 255f, 1f);

        private TextMeshProUGUI _textMeshPro;
        private Canvas _canvas;
        private Camera _camera;

        public bool IsLinkHighlighted => _currentLink != -1;

        private int _currentLink = -1;
        private List<Color32[]> _originalVertexColors = new List<Color32[]>();

        /// <summary>
        /// Event invoked whenever the user clicks on a link, do your implementation in this event
        /// </summary>
        public static UnityEvent<string> OnLinkClicked { get; } = new UnityEvent<string>();

        private void Awake()
        {
            _textMeshPro = GetComponent<TextMeshProUGUI>();
            _canvas = GetComponentInParent<Canvas>();

            // Get a reference to the camera if Canvas Render Mode is not ScreenSpace Overlay.
            _camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        }

        private void LateUpdate()
        {
            if (!DoesColorChangeOnHover) return;
            // is the cursor in the correct region (above the text area) and furthermore, in the link region?
            var isHoveringOver =
                TMP_TextUtilities.IsIntersectingRectTransform(_textMeshPro.rectTransform, Input.mousePosition, _camera);
            int linkIndex = isHoveringOver
                ? TMP_TextUtilities.FindIntersectingLink(_textMeshPro, Input.mousePosition, _camera)
                : -1;

            // Clear previous link selection if one existed.
            if (_currentLink != -1 && linkIndex != _currentLink)
            {
                // Debug.Log("Clear old selection");
                SetLinkToColor(_currentLink, (linkIdx, vertIdx) => _originalVertexColors[linkIdx][vertIdx]);
                _originalVertexColors.Clear();
                _currentLink = -1;
            }

            // Handle new link selection.
            if (linkIndex != -1 && linkIndex != _currentLink)
            {
                // Debug.Log("New selection");
                _currentLink = linkIndex;
                if (DoesColorChangeOnHover)
                    _originalVertexColors = SetLinkToColor(linkIndex, (_linkIdx, _vertIdx) => HoverColor);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(_textMeshPro, Input.mousePosition, _camera);
            if (linkIndex != -1)
            {
                // was a link clicked?
                TMP_LinkInfo linkInfo = _textMeshPro.textInfo.linkInfo[linkIndex];

                var link = linkInfo.GetLinkID().Trim('\'', '\"');
                Debug.Log(string.Format("id: {0}, text: {1}", link, linkInfo.GetLinkText()));

                // Make generic, so we can handle things
                OnLinkClicked?.Invoke(link);
            }
        }

        List<Color32[]> SetLinkToColor(int linkIndex, Func<int, int, Color32> colorForLinkAndVert)
        {
            TMP_LinkInfo linkInfo = _textMeshPro.textInfo.linkInfo[linkIndex];

            var oldVertColors = new List<Color32[]>(); // store the old character colors

            for (int i = 0; i < linkInfo.linkTextLength; i++)
            {
                // for each character in the link string
                int characterIndex =
                    linkInfo.linkTextfirstCharacterIndex + i; // the character index into the entire text
                var charInfo = _textMeshPro.textInfo.characterInfo[characterIndex];
                int meshIndex =
                    charInfo
                        .materialReferenceIndex; // Get the index of the material / sub text object used by this character.
                int vertexIndex = charInfo.vertexIndex; // Get the index of the first vertex of this character.

                Color32[] vertexColors =
                    _textMeshPro.textInfo.meshInfo[meshIndex].colors32; // the colors for this character
                oldVertColors.Add(vertexColors.ToArray());

                if (charInfo.isVisible)
                {
                    vertexColors[vertexIndex + 0] = colorForLinkAndVert(i, vertexIndex + 0);
                    vertexColors[vertexIndex + 1] = colorForLinkAndVert(i, vertexIndex + 1);
                    vertexColors[vertexIndex + 2] = colorForLinkAndVert(i, vertexIndex + 2);
                    vertexColors[vertexIndex + 3] = colorForLinkAndVert(i, vertexIndex + 3);
                }
            }

            // Update Geometry
            _textMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.All);

            return oldVertColors;
        }

        public void OnPointerDown(PointerEventData eventData)
        {

        }

        public void OnPointerUp(PointerEventData eventData)
        {

        }
    }
}
