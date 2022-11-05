using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Object = UnityEngine.Object;
using Microsoft.MixedReality.Toolkit;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
#endif
namespace BystandAR
{
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

            _launcher.SetActive(true);
            _gameManager.SetActive(true);
            _spawnCube.SetActive(true);
            buttonParent.SetActive(false);

        }

        public void startInterviewLoggingPressed()
        {
            _interviewQuestions.SetActive(true);
            GameObject.Find("FrameSanitizer").GetComponent<FrameSanitizer>().clientSocketImagesInstance.SetActive(true);
            GameObject.Find("FrameSanitizer").GetComponent<FrameSanitizer>().clientSocketDepthInstance.SetActive(true);
            buttonParent.SetActive(false);
        }

        public void startBlocksLoggingPressed()
        {


            _launcher.SetActive(true);
            _gameManager.SetActive(true);
            _spawnCube.SetActive(true);
            buttonParent.SetActive(false);

        }

    }
}