using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Microsoft.Azure.SpatialAnchors.Unity;
using Microsoft.Azure.SpatialAnchors;
using Photon.Pun;
using Photon.Realtime;

namespace BystandAR
{
    public class PlayerManager : MonoBehaviourPunCallbacks, IPunObservable
    {

        [Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
        public static GameObject LocalPlayerInstance;
        public GameObject clientPrefabImages;
        public GameObject clientPrefabDepth;
        string identifier = null;
        bool RPCSent = false;

        // Start is called before the first frame update
        #region MonoBehaviour CallBacks

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during early initialization phase.
        /// </summary>
        async void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
            // #Important
            // used in GameManager.cs: we keep track of the localPlayer instance to prevent instanciation when levels are synchronized
            if (photonView.IsMine)
            {
                PlayerManager.LocalPlayerInstance = this.gameObject;
            }
            if (PhotonNetwork.IsMasterClient && photonView.IsMine)
            {
                identifier = await GameObject.Find("AzureSpatialAnchors").GetComponent<AzureSpatialAnchorsScript>().createAnchor();
            }

            this.gameObject.transform.SetParent(GameObject.Find("AnchorParent").transform, false);
            
        }


        #endregion

        // Update is called once per frame
        async void Update()
        {
            if (photonView.IsMine)
            {
                this.gameObject.transform.position = Camera.main.transform.position;

            }

            if (PhotonNetwork.IsMasterClient && photonView.IsMine && PhotonNetwork.InRoom && identifier != null && !RPCSent)
            {
                
                photonView.RPC("SendIdentifier", RpcTarget.OthersBuffered, identifier);
                RPCSent = true;
                Debug.Log("Anchor Created");
                GameObject.Find("FrameSanitizer").GetComponent<FrameSanitizer>().clientSocketImagesInstance.SetActive(true);
                GameObject.Find("FrameSanitizer").GetComponent<FrameSanitizer>().clientSocketDepthInstance.SetActive(true);
            }

        }


        #region IPunObservable implementation

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {

        }

        #endregion
    }
}