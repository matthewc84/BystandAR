using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Object = UnityEngine.Object;
//using Unity.Netcode;
using Microsoft.MixedReality.Toolkit;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
#endif

/// <summary>
///     Handles the start of the inference model
/// </summary>
/// 

namespace BystandAR
{
    public class QuitButtonFunctionality : MonoBehaviour
    {
        //private NetworkManager netManager;

        FrameSanitizer sanitizerScript;


        void Start()
        {
            sanitizerScript = GameObject.Find("FrameSanitizer").GetComponent<FrameSanitizer>();
        }

        public void quitButtonPressed()
        {
            sanitizerScript.logData = false;
            //Application.Quit();

        }


    }
}
