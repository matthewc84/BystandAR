using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;
using Photon.Realtime;


public class PlayerManager : MonoBehaviourPunCallbacks, IPunObservable
{

    [Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
    public static GameObject LocalPlayerInstance;
    string identifier = null;

    // Start is called before the first frame update
    #region MonoBehaviour CallBacks

    /// <summary>
    /// MonoBehaviour method called on GameObject by Unity during early initialization phase.
    /// </summary>
    async public void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        // #Important
        // used in GameManager.cs: we keep track of the localPlayer instance to prevent instanciation when levels are synchronized
        if (photonView.IsMine)
        {
            LocalPlayerInstance = gameObject;
        }

      /*  if (PhotonNetwork.IsMasterClient && photonView.IsMine)
        {
            identifier = await GameObject.Find("AzureSpatialAnchors").GetComponent<AzureSpatialAnchorsScript>().createAnchor();
            while (identifier == null)
            {
                identifier = await GameObject.Find("AzureSpatialAnchors").GetComponent<AzureSpatialAnchorsScript>().createAnchor();
            }
            photonView.RPC("SendIdentifier", RpcTarget.OthersBuffered, identifier);

            Debug.Log("Anchor Created");

        }*/


        



        // #Critical
        // we flag as don't destroy on load so that instance survives level synchronization, thus giving a seamless experience when levels load.

    }

    #endregion

    // Update is called once per frame
    void Update()
    {
        if (photonView.IsMine)
        {
            this.gameObject.transform.position = Camera.main.transform.position;

        }

    }

    #region IPunObservable implementation

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {

    }

    #endregion
}
