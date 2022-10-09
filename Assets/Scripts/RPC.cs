using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class RPC : MonoBehaviourPun
{

    [PunRPC]
    public void SendIdentifier(string indentifier)
    {
        Debug.Log("RPC Called");
        //photonView.RPC("SendACK", RpcTarget.OthersBuffered);
        //if (!GameObject.Find("AzureSpatialAnchors").GetComponent<AzureSpatialAnchorsScript>().retrievingAnchor)
        //{
            GameObject.Find("AzureSpatialAnchors").GetComponent<AzureSpatialAnchorsScript>().LocateAnchor(indentifier);
       // }
        
        
    }
}
