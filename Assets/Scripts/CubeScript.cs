using Microsoft.MixedReality.Toolkit.Input;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class CubeScript : MonoBehaviour, IPunInstantiateMagicCallback, IMixedRealityFocusHandler
{
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
}
