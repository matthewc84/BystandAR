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
    public GameObject clientPrefabImages;
    public GameObject clientPrefabDepth;
    string identifier = null;

    // Start is called before the first frame update
    #region MonoBehaviour CallBacks

    /// <summary>
    /// MonoBehaviour method called on GameObject by Unity during early initialization phase.
    /// </summary>
   void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        // #Important
        // used in GameManager.cs: we keep track of the localPlayer instance to prevent instanciation when levels are synchronized
        if (photonView.IsMine)
        {
            LocalPlayerInstance = gameObject;
        }


    }

    async void Start()
    {

        if (PhotonNetwork.IsMasterClient && photonView.IsMine)
        {
            identifier = await GameObject.Find("AzureSpatialAnchors").GetComponent<AzureSpatialAnchorsScript>().createAnchor();
            while (identifier == null)
            {
                identifier = await GameObject.Find("AzureSpatialAnchors").GetComponent<AzureSpatialAnchorsScript>().createAnchor();
            }
            photonView.RPC("SendIdentifier", RpcTarget.OthersBuffered, identifier);

            Debug.Log("Anchor Created");


            Instantiate(clientPrefabImages);
            Instantiate(clientPrefabDepth);
        }
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
