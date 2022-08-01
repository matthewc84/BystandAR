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


    public GameObject buttonParent;
    public GameObject objectDetection;

    void Start()
    {
        //CoreServices.DiagnosticsSystem.ShowDiagnostics = false;
    }

    void Update()
    {

    }


    public void serverButtonPressed()
    {
        buttonParent.SetActive(false);
        objectDetection.SetActive(true);

    }




}
