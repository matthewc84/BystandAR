using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class RPC : MonoBehaviourPun
{

    [PunRPC]
    public void SendIdentifier(string indentifier)
    {
        Debug.Log("RPC Called");
            GameObject.Find("AzureSpatialAnchors").GetComponent<AzureSpatialAnchorsScript>().LocateAnchor(indentifier);
        
        
    }
}
