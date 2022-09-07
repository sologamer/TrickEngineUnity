using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TrickCore
{
    public static class TransformExtensions
    {
        public static Transform FindChildContainsName(this Transform transform, string name)
        {
            foreach (Transform child in transform)
            {
                if (child.name.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) != -1) return child;
            }
            return null;
        }
        
        public static Bounds GetRendererBounds(this Renderer[] renderers)
        {
            Bounds bounds = new Bounds();

            if (renderers.Length > 0)
            {
                foreach (Renderer renderer in renderers)
                {
                    if (renderer.enabled)
                    {
                        bounds = renderer.bounds;
                        break;
                    }
                }

                //Encapsulate for all renderers
                foreach (Renderer renderer in renderers)
                {
                    if (renderer.enabled)
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
            }
            return bounds;
        }
        
        public static Bounds GetRendererBounds(this GameObject obj, bool includeInactive = true)
        {
            return GetRendererBounds(obj.GetComponentsInChildren<Renderer>(includeInactive));
        }

        public static Vector3 GetRandomPointInsideCollider(this BoxCollider boxCollider, IRandomizer randomizer)
        {
            Vector3 extents = boxCollider.size / 2f;
            Vector3 point = new Vector3(
                randomizer.Next(-extents.x, extents.x),
                randomizer.Next(-extents.y, extents.y),
                randomizer.Next(-extents.z, extents.z)
            ) + boxCollider.center;
            return boxCollider.transform.TransformPoint(point);
        }

        public static Bounds GetColliderBounds(this GameObject obj, bool includeInactive = true)
        {
            return GetColliderBounds(obj.GetComponentsInChildren<Collider>(includeInactive));
        }
        public static Bounds GetColliderBounds(this Collider[] obj)
        {
            Bounds bounds = new Bounds();
            if (obj.Length > 0)
            {
                bounds = obj.First().bounds;
                
                foreach (var collider in obj)
                {
                    bounds.Encapsulate(collider.bounds);
                }                
            }
            return bounds;
        }
        
        public static string GetTransformPath(this Transform current, Transform parent)
        {
            List<string> path = new List<string>();
            while (current != null || current != parent)
            {
                path.Add(current.name);
                current = current.parent;
            }
            return string.Join("/", path.AsEnumerable().Reverse());
        }

        public static void ClearChildTransforms(this Transform transform, bool destroyImmediate)
        {
            if (transform == null) return;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                if(destroyImmediate)
                    Object.DestroyImmediate(transform.GetChild(i).gameObject);
                else
                    Object.Destroy(transform.GetChild(i).gameObject);
            }
        }
    }
}