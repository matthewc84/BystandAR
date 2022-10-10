using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Object = UnityEngine.Object;
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

    public GameObject buttonParent;
    public GameObject _interviewQuestions;
    public GameObject _socketClientImages;
    public GameObject _socketClientDepth;
    public GameObject _spawnCube;
    public GameObject _launcher;
    public GameObject _gameManager;
    public GameObject _anchorManager;



    void Start()
    {
        
    }

    public void startInterviewNoLoggingPressed()
    {
        _interviewQuestions.SetActive(true);
        buttonParent.SetActive(false);
    }

    public void startBlocksNoLoggingPressed()
    {
        _spawnCube.SetActive(true);
        _launcher.SetActive(true);
        //_anchorManager.SetActive(true);
        _gameManager.SetActive(true);
        buttonParent.SetActive(false);

    }

    public void startInterviewLoggingPressed()
    {
        _interviewQuestions.SetActive(true);
        _socketClientImages.SetActive(true);
        _socketClientDepth.SetActive(true);
        buttonParent.SetActive(false);
    }

    public void startBlocksLoggingPressed()
    {

       // _anchorManager.SetActive(true);
        _spawnCube.SetActive(true);
        _launcher.SetActive(true);
        _gameManager.SetActive(true);
        buttonParent.SetActive(false);


        //_socketClientImages.SetActive(true);
        //_socketClientDepth.SetActive(true);
    }






}
