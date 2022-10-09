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
    GameObject anchorParent;
    #endregion

    #region MonoBehaviour CallBacks


    #endregion


    #region Public Methods
    void Start()
    {
        anchorParent = GameObject.Find("AnchorParent");
    }


    public void spawnCube()
    {
        GameObject.Find("Launcher").GetComponent<Launcher>().spawnCube();
    }


    #endregion



}

