using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TrickCore
{
    public static class GameObjectExtensions
    {
        private static BindingFlags CloneMemberFlags = BindingFlags.Instance
                                            | BindingFlags.GetProperty
                                            | BindingFlags.SetProperty
                                            | BindingFlags.GetField
                                            | BindingFlags.SetField
                                            | BindingFlags.NonPublic
                                            | BindingFlags.Public
                                            | BindingFlags.Static
                                            | BindingFlags.DeclaredOnly
                                            | BindingFlags.Default
                                            | BindingFlags.InvokeMethod;
        
        private class NoAllocHelper<T>
        {
            public static readonly List<T> List = new List<T>() {default};
        }

        public static T CloneComponent<T>(this Component comp, T other) where T : Component
        {
            Type type = comp.GetType();
            if (type != other.GetType()) return null;
            
            foreach (MemberInfo info in type.TrickGetMemberInfoFromType(true, CloneMemberFlags))
                info.SetValueToMember(comp, info.GetValueFromMember(other));
            
            return comp as T;
        }
    
        public static T GetComponentNonAlloc<T>(this GameObject gameObject) where T : Component
        {
            if (gameObject == null) return null;
            var list = NoAllocHelper<Component>.List;
            gameObject.GetComponents(typeof(T), list);
            return list.FirstOrDefault() as T;
        }
    
        public static T GetComponentNonAlloc<T>(this Transform transform) where T : Component
        {
            if (transform == null) return null;
            var list = NoAllocHelper<Component>.List;
            transform.GetComponents(typeof(T), list);
            return list.FirstOrDefault() as T;
        }    
    
        public static T GetComponentInParentNonAlloc<T>(this GameObject gameObject) where T : Component
        {
            if (gameObject == null) return null;
            var list = NoAllocHelper<T>.List;
            gameObject.GetComponentsInParent(true, list);
            return list.FirstOrDefault();
        }
    
        public static T GetComponentInParentNonAlloc<T>(this Transform transform) where T : Component
        {
            if (transform == null) return null;
            var list = NoAllocHelper<T>.List;
            transform.GetComponentsInParent(true, list);
            return list.FirstOrDefault();
        }
    
        public static void SetLayer(this GameObject gameObject, int layer)
        {
            if (gameObject == null) return;
            gameObject.layer = layer;
            var trs = gameObject.GetComponentsInChildren<Transform>(true);
            foreach (var tr in trs) tr.gameObject.layer = layer;
        }
        
        public static int? GetIndexOfLowestTrueBit(this int flag) {
            for (var index = 0; index < sizeof(int) * 8; index++)
                if ((flag & 1) == 1) return index;
                else flag >>= 1;
 
            return null;
        }
    }
}