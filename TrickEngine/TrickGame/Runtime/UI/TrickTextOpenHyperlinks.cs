using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TrickCore
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TrickTextOpenHyperlinks : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        public bool doesColorChangeOnHover = true;
        public Color hoverColor = new Color(60f / 255f, 120f / 255f, 1f);

        private TextMeshProUGUI pTextMeshPro;
        private Canvas pCanvas;
        private Camera pCamera;

        public bool isLinkHighlighted
        {
            get { return pCurrentLink != -1; }
        }

        private int pCurrentLink = -1;
        private List<Color32[]> pOriginalVertexColors = new List<Color32[]>();

        protected virtual void Awake()
        {
            pTextMeshPro = GetComponent<TextMeshProUGUI>();
            pCanvas = GetComponentInParent<Canvas>();

            // Get a reference to the camera if Canvas Render Mode is not ScreenSpace Overlay.
            pCamera = pCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : pCanvas.worldCamera;
        }

        private void LateUpdate()
        {
            if (!doesColorChangeOnHover) return;
            // is the cursor in the correct region (above the text area) and furthermore, in the link region?
            var isHoveringOver =
                TMP_TextUtilities.IsIntersectingRectTransform(pTextMeshPro.rectTransform, Input.mousePosition, pCamera);
            int linkIndex = isHoveringOver
                ? TMP_TextUtilities.FindIntersectingLink(pTextMeshPro, Input.mousePosition, pCamera)
                : -1;

            // Clear previous link selection if one existed.
            if (pCurrentLink != -1 && linkIndex != pCurrentLink)
            {
                // Debug.Log("Clear old selection");
                SetLinkToColor(pCurrentLink, (linkIdx, vertIdx) => pOriginalVertexColors[linkIdx][vertIdx]);
                pOriginalVertexColors.Clear();
                pCurrentLink = -1;
            }

            // Handle new link selection.
            if (linkIndex != -1 && linkIndex != pCurrentLink)
            {
                // Debug.Log("New selection");
                pCurrentLink = linkIndex;
                if (doesColorChangeOnHover)
                    pOriginalVertexColors = SetLinkToColor(linkIndex, (_linkIdx, _vertIdx) => hoverColor);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(pTextMeshPro, Input.mousePosition, pCamera);
            if (linkIndex != -1)
            {
                // was a link clicked?
                TMP_LinkInfo linkInfo = pTextMeshPro.textInfo.linkInfo[linkIndex];

                var link = linkInfo.GetLinkID().Trim('\'', '\"');
                Debug.Log(string.Format("id: {0}, text: {1}", link, linkInfo.GetLinkText()));

                if (link.StartsWith("privacy"))
                {

                }
                else if (link.StartsWith("tos"))
                {

                }
                else if (link.StartsWith("player:{id}"))
                {

                }
            }
        }

        List<Color32[]> SetLinkToColor(int linkIndex, Func<int, int, Color32> colorForLinkAndVert)
        {
            TMP_LinkInfo linkInfo = pTextMeshPro.textInfo.linkInfo[linkIndex];

            var oldVertColors = new List<Color32[]>(); // store the old character colors

            for (int i = 0; i < linkInfo.linkTextLength; i++)
            {
                // for each character in the link string
                int characterIndex =
                    linkInfo.linkTextfirstCharacterIndex + i; // the character index into the entire text
                var charInfo = pTextMeshPro.textInfo.characterInfo[characterIndex];
                int meshIndex =
                    charInfo
                        .materialReferenceIndex; // Get the index of the material / sub text object used by this character.
                int vertexIndex = charInfo.vertexIndex; // Get the index of the first vertex of this character.

                Color32[] vertexColors =
                    pTextMeshPro.textInfo.meshInfo[meshIndex].colors32; // the colors for this character
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
            pTextMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.All);

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
