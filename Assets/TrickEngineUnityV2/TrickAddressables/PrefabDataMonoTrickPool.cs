using System;
using UnityEngine;

/// <summary>
/// Use this to store initial values of an object, required for pooled object to reset correctly
/// </summary>
public class PrefabDataMonoTrickPool : MonoBehaviour
{
    public Vector3? InitialLocalScale { get; set; }
    public Vector3? InitialEuler { get; set; }

    private void Awake()
    {
        var tr = transform;
        InitialLocalScale ??= tr.localScale;
        InitialEuler ??= tr.localEulerAngles;
    }
}