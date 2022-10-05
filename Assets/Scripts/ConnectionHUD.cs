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
public class ConnectionHUD : MonoBehaviour
{
    //private NetworkManager netManager;

    public GameObject buttonParent;
    public GameObject _socketClient;
    public GameObject _socketServer;


    void Start()
    {
        //netManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
    }

        public void serverButtonPressed()
    {
        //if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsClient)
       // {
            //When Button pressed, start host
            //netManager.StartHost();
            Instantiate(_socketServer);
            //startHostButton.GetComponentInChildren<TextMeshPro>().SetText("End Client Discovery");
            buttonParent.SetActive(false);

        //}

    }


    public void clientButtonPressed()
    {
        //if (!NetworkManager.Singleton.IsClient)
        //{

            //netManager.StartClient();
            Instantiate(_socketClient);
            buttonParent.SetActive(false);
        //}


    }




}
