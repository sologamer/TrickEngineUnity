using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TrickCore;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using ReorderableList = TrickCore.ReorderableList;

namespace TrickCore
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(Object), true)]
	public class DropTableEditor : Editor
	{

		private Dictionary<SerializedProperty, ReorderableList> dict =
			new Dictionary<SerializedProperty, ReorderableList>();

		void OnEnable()
		{
			var it = serializedObject.GetIterator();
			int index = 0;
			while (it.NextVisible(index++ == 0))
			{
				var itCopy = it.Copy();
				var instance = GetTargetObjectOfProperty(itCopy);
				var type = instance != null ? instance.GetType() : null;
				if (type != null && typeof(BaseUnityDropTable).IsAssignableFrom(type))
				{
					var spItems = itCopy.FindPropertyRelative("Items");
					var list = new ReorderableList(spItems);
					list.elementDisplayType = ReorderableList.ElementDisplayType.Auto;
					list.getElementNameCallback += element => GetElementName(list, itCopy, element);
					var genericTypes = instance.GetType().GetGenericArguments();
					if (genericTypes.Length >= 1 && genericTypes.Any(t => typeof(Object).IsAssignableFrom(t)))
					{
						var genType = genericTypes.FirstOrDefault(t => typeof(Object).IsAssignableFrom(t));
						LocalSurrogateCallback callback = new LocalSurrogateCallback("Object");
						list.surrogate = new ReorderableList.Surrogate(genType, callback.SetReference);
					}

					dict.Add(itCopy, list);
				}
				else
				{
					dict.Add(itCopy, null);
				}
			}
		}

		private struct LocalSurrogateCallback
		{

			private string property;

			internal LocalSurrogateCallback(string property)
			{

				this.property = property;
			}

			internal void SetReference(SerializedProperty element, Object objectReference, ReorderableList list)
			{

				SerializedProperty prop = !string.IsNullOrEmpty(property)
					? element.FindPropertyRelative(property)
					: null;

				if (prop != null && prop.propertyType == SerializedPropertyType.ObjectReference)
				{

					prop.objectReferenceValue = objectReference;
				}
			}
		}

		private string GetElementName(ReorderableList list, SerializedProperty parent, SerializedProperty element)
		{
			//.Replace("@Items", GetTargetObjectOfProperty(element).ToString())
			if (GetTargetObjectOfProperty(parent) is BaseUnityDropTable parentInstance)
			{
				return parentInstance.EditorGetElementHeader(GetTargetObjectOfProperty(element));
			}

			return GetTargetObjectOfProperty(element).ToString();
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			foreach (KeyValuePair<SerializedProperty, ReorderableList> pair in dict)
			{
				if (pair.Value != null)
				{
					EditorGUILayout.PropertyField(pair.Key, false);
					if (pair.Key.isExpanded)
					{
						pair.Value.DoLayoutList();
						GUILayout.BeginHorizontal();
						if (GUILayout.Button("OrderBy ASC"))
						{
							if (GetTargetObjectOfProperty(pair.Key) is BaseUnityDropTable baseUnityDropTable)
							{
								baseUnityDropTable.EditorOrderByNormalizedWeight(true);
							}
						}

						if (GUILayout.Button("OrderBy DESC"))
						{
							if (GetTargetObjectOfProperty(pair.Key) is BaseUnityDropTable baseUnityDropTable)
							{
								baseUnityDropTable.EditorOrderByNormalizedWeight(false);
							}
						}

						if (GUILayout.Button("Test Generate"))
						{
							if (GetTargetObjectOfProperty(pair.Key) is BaseUnityDropTable baseUnityDropTable)
							{
								Debug.Log("[Test Generate]: " + baseUnityDropTable.RandomItem());
							}
						}

						GUILayout.EndHorizontal();
					}
				}
				else
				{
					EditorGUILayout.PropertyField(pair.Key, false);
				}
			}

			UpdateIfNeeded(false);

			serializedObject.ApplyModifiedProperties();
		}

		private void UpdateIfNeeded(bool forced)
		{
			// If we have any modified fields
			if (!forced && !serializedObject.hasModifiedProperties) return;

			if (GUIUtility.hotControl == 0)
			{
				if (forced) serializedObject.Update();

				// Update
				foreach (KeyValuePair<SerializedProperty, ReorderableList> pair in dict)
				{
					if (pair.Value == null) continue;
					if (GetTargetObjectOfProperty(pair.Key) is BaseUnityDropTable baseUnityDropTable)
					{
						if (GUIUtility.hotControl == 0)
						{
							baseUnityDropTable.IsDirty = true;
						}
					}
				}

				if (forced) serializedObject.ApplyModifiedProperties();

				_registered = false;
				_nextCheckTime = 0.0f;
				// ReSharper disable once DelegateSubtraction
				EditorApplication.update -= EditorUpdate;
			}
			else
			{
				// Wait until
				if (!_registered)
				{
					_registered = true;
					EditorApplication.update += EditorUpdate;
				}
			}
		}

		private bool _registered = false;
		private double _nextCheckTime = 0.0f;

		private void EditorUpdate()
		{
			if (EditorApplication.timeSinceStartup >= _nextCheckTime)
			{
				if (GUIUtility.hotControl == 0)
				{
					UpdateIfNeeded(true);
				}

				_nextCheckTime = EditorApplication.timeSinceStartup + 0.1f;
			}
		}


		/// <summary>
		/// Gets the object the property represents.
		/// </summary>
		/// <param name="prop"></param>
		/// <returns></returns>
		public static object GetTargetObjectOfProperty(SerializedProperty prop)
		{
			if (prop == null) return null;

			var path = prop.propertyPath.Replace(".Array.data[", "[");
			object obj = prop.serializedObject.targetObject;
			var elements = path.Split('.');
			foreach (var element in elements)
			{
				if (element.Contains("["))
				{
					var elementName = element.Substring(0, element.IndexOf("["));
					var index =
						System.Convert.ToInt32(
							element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
					obj = GetValue_Imp(obj, elementName, index);
				}
				else
				{
					obj = GetValue_Imp(obj, element);
				}
			}

			return obj;
		}

		private static object GetValue_Imp(object source, string name)
		{
			if (source == null)
				return null;
			var type = source.GetType();

			while (type != null)
			{
				var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				if (f != null)
					return f.GetValue(source);

				var p =
					type.GetProperty(name,
						BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
				if (p != null)
					return p.GetValue(source, null);

				type = type.BaseType;
			}

			return null;
		}

		private static object GetValue_Imp(object source, string name, int index)
		{
			var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
			if (enumerable == null) return null;
			var enm = enumerable.GetEnumerator();
			//while (index-- >= 0)
			//    enm.MoveNext();
			//return enm.Current;

			for (int i = 0; i <= index; i++)
			{
				if (!enm.MoveNext()) return null;
			}

			return enm.Current;
		}
	}
}
