using System.Collections;
using BeauRoutine;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

namespace TrickCore
{
    public static class RectTransformExtensions
    {
        private static readonly Vector3[] WorldCorners = new Vector3[4];

        public static void ScrollViewSnapToHorizontal(this ScrollRect scrollRect, RectTransform target)
        {
            if (target == null) return;
            
            var layoutGroup = scrollRect.content.GetComponent<LayoutGroup>();

            if (layoutGroup != null)
            {
                var targetPos = target.position;
                targetPos.y -= layoutGroup.padding.top;
                targetPos.x -= layoutGroup.padding.left;
                
                var v = (Vector2) scrollRect.transform.InverseTransformPoint(scrollRect.content.position)
                        - (Vector2) scrollRect.transform.InverseTransformPoint(targetPos);
                scrollRect.content.anchoredPosition = new Vector2(v.x, scrollRect.content.anchoredPosition.y);
            }
            else
            {
                var v = (Vector2) scrollRect.transform.InverseTransformPoint(scrollRect.content.position)
                        - (Vector2) scrollRect.transform.InverseTransformPoint(target.position);
                scrollRect.content.anchoredPosition = new Vector2(v.x, scrollRect.content.anchoredPosition.y);
            }
        }
        
        public static void ScrollViewSnapToVertical(this ScrollRect scrollRect, RectTransform target, Vector3 offset = default)
        {
            if (target == null) return;
            
            var layoutGroup = scrollRect.content.GetComponent<LayoutGroup>();

            if (layoutGroup != null)
            {
                var targetPos = target.position + offset;
                targetPos.y -= layoutGroup.padding.top;
                targetPos.x -= layoutGroup.padding.left;
                
                var v = (Vector2) scrollRect.transform.InverseTransformPoint(scrollRect.content.position)
                        - (Vector2) scrollRect.transform.InverseTransformPoint(targetPos);
                scrollRect.content.anchoredPosition = new Vector2(scrollRect.content.anchoredPosition.x, v.y);
            }
            else
            {
                var v = (Vector2) scrollRect.transform.InverseTransformPoint(scrollRect.content.position)
                        - (Vector2) scrollRect.transform.InverseTransformPoint(target.position + offset);
                scrollRect.content.anchoredPosition = new Vector2(scrollRect.content.anchoredPosition.x, v.y);
            }
        }
        
        public static void FillToParent(this RectTransform transform, RectTransform parent = null)
        {
            if (transform == null) return;
            if (parent != null && transform.parent != parent) transform.SetParent(parent);
            
            transform.anchorMin = Vector2.zero;
            transform.anchorMax = Vector2.one;
            transform.offsetMin = Vector2.zero;
            transform.offsetMax = Vector2.zero;
        }
        
        public static Bounds GetRectTransformBounds(RectTransform transform)
        {
            transform.GetWorldCorners(WorldCorners);
            Bounds bounds = new Bounds(WorldCorners[0], Vector3.zero);
            for (int i = 1; i < 4; ++i)
            {
                bounds.Encapsulate(WorldCorners[i]);
            }

            return bounds;
        }

        public static bool Overlaps(this RectTransform a, Rect rect)
        {
            return a.WorldRect().Overlaps(rect);
        }

        public static bool Overlaps(this RectTransform a, RectTransform b)
        {
            return a.WorldRect().Overlaps(b.WorldRect());
        }

        public static bool Overlaps(this RectTransform a, RectTransform b, bool allowInverse)
        {
            return a.WorldRect().Overlaps(b.WorldRect(), allowInverse);
        }

        public static Rect WorldRect(this RectTransform rectTransform)
        {
            Vector2 sizeDelta = rectTransform.sizeDelta;
            float rectTransformWidth = sizeDelta.x * rectTransform.lossyScale.x;
            float rectTransformHeight = sizeDelta.y * rectTransform.lossyScale.y;

            Vector3 position = rectTransform.position;
            return new Rect(position.x - rectTransformWidth / 2f, position.y - rectTransformHeight / 2f,
                rectTransformWidth, rectTransformHeight);
        }

        /// <summary>
        /// https://forum.unity.com/threads/test-if-ui-element-is-visible-on-screen.276549/
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <param name="camera"></param>
        /// <returns></returns>
        private static int CountCornersVisibleFrom(this RectTransform rectTransform, Camera camera)
        {
            Rect screenBounds =
                new Rect(0f, 0f, Screen.width,
                    Screen.height); // Screen space bounds (assumes camera renders across the entire screen)
            Vector3[] objectCorners = new Vector3[4];
            rectTransform.GetWorldCorners(objectCorners);

            int visibleCorners = 0;

            if (camera != null)
            {
                Vector3 tempScreenSpaceCorner; // Cached
                for (var i = 0; i < objectCorners.Length; i++) // For each corner in rectTransform
                {
                    tempScreenSpaceCorner =
                        camera.WorldToScreenPoint(
                            objectCorners[i]); // Transform world space position of corner to screen space

                    if (screenBounds.Contains(tempScreenSpaceCorner)) // If the corner is inside the screen
                    {
                        visibleCorners++;
                    }
                }
            }
            else
            {
                if (Overlaps(rectTransform, screenBounds))
                    visibleCorners = 4;
            }

            return visibleCorners;
        }

        /// <summary>
        /// Determines if this RectTransform is fully visible from the specified camera.
        /// Works by checking if each bounding box corner of this RectTransform is inside the cameras screen space view frustrum.
        /// </summary>
        /// <returns><c>true</c> if is fully visible from the specified camera; otherwise, <c>false</c>.</returns>
        /// <param name="rectTransform">Rect transform.</param>
        /// <param name="camera">Camera.</param>
        public static bool IsFullyVisibleFrom(this RectTransform rectTransform, Camera camera = null)
        {
            return CountCornersVisibleFrom(rectTransform, camera) == 4; // True if all 4 corners are visible
        }

        /// <summary>
        /// Determines if this RectTransform is at least partially visible from the specified camera.
        /// Works by checking if any bounding box corner of this RectTransform is inside the cameras screen space view frustrum.
        /// </summary>
        /// <returns><c>true</c> if is at least partially visible from the specified camera; otherwise, <c>false</c>.</returns>
        /// <param name="rectTransform">Rect transform.</param>
        /// <param name="camera">Camera.</param>
        public static bool IsVisibleFrom(this RectTransform rectTransform, Camera camera = null)
        {
            return CountCornersVisibleFrom(rectTransform, camera) > 0; // True if any corners are visible
        }
        
        public static Vector3 GetGUIElementOffset(this RectTransform rect)
        {
            Rect screenBounds = new Rect(0f, 0f, Screen.width, Screen.height);
            Vector3[] objectCorners = new Vector3[4];
            rect.GetWorldCorners(objectCorners);
            var xNew = 0f;
            var yNew = 0f;
            var zNew = 0f;
            for (int i = 0; i < objectCorners.Length; i++)
            {
                if (objectCorners[i].x < screenBounds.xMin) xNew = screenBounds.xMin - objectCorners[i].x;
                if (objectCorners[i].x > screenBounds.xMax) xNew = screenBounds.xMax - objectCorners[i].x;
                if (objectCorners[i].y < screenBounds.yMin) yNew = screenBounds.yMin - objectCorners[i].y;
                if (objectCorners[i].y > screenBounds.yMax) yNew = screenBounds.yMax - objectCorners[i].y;
            }
            return new Vector3(xNew, yNew, zNew);
 
        }
    }
}