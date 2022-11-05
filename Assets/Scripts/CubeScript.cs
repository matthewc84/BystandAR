using Microsoft.MixedReality.Toolkit.Input;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class CubeScript : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback, IMixedRealityFocusHandler, IPunObservable
{

    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);

        this.gameObject.transform.SetParent(GameObject.Find("AnchorParent").transform, false);


    }

    public void OnFocusEnter(FocusEventData eventData)
    {
        // ask the photonview for permission
        var photonView = this.GetComponent<PhotonView>();

        photonView?.RequestOwnership();
    }

    public void OnFocusExit(FocusEventData eventData)
    {
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {


    }

    #region IPunObservable implementation

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {

    }

    #endregion
}
