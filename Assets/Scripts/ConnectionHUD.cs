using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Object = UnityEngine.Object;
using Microsoft.MixedReality.Toolkit;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
#endif

/// <summary>
///     Handles the start of the inference model
/// </summary>
/// 
public class ConnectionHUD : MonoBehaviour
{
    //private NetworkManager netManager;

    public GameObject buttonParent;

    private Launcher _launcher;


    void Start()
    {
        _launcher = GameObject.Find("Launcher").GetComponent<Launcher>();
    }

        public void spawnCubePressed()
    {
        _launcher.spawnCube();
    }






}
