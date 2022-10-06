using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// 
/// </summary>
/// 

public class GameFunctionality : MonoBehaviourPun
{

    #region Private Serializable Fields

    #endregion


    #region Private Fields

    #endregion

    #region MonoBehaviour CallBacks


    #endregion


    #region Public Methods



    public void spawnCube()
    {
        PhotonNetwork.Instantiate("Cube", new Vector3(0f, 0f, 0f), Quaternion.identity, 0);
    }


    #endregion



}

