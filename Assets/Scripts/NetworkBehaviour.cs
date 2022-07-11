// Adapted from the WinML MNIST sample and Rene Schulte's repo 
// https://github.com/microsoft/Windows-Machine-Learning/tree/master/Samples/MNIST
// https://github.com/reneschulte/WinMLExperiments/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;


#if ENABLE_WINMD_SUPPORT
using Windows.Media;
#endif

public class NetworkBehaviour : MonoBehaviour
{
    // Public fields
    public Vector2 InputFeatureSize = new Vector2(640, 480);
    public GameObject objectOutlineCube;
    public int samplingInterval = 300;
    int counter = 0;

    // Private fields
    private NetworkModel _networkModel;
    private MediaCaptureUtility _mediaCaptureUtility;
    private bool _isRunning = false;
    private Camera cam;
    public int currentState;
    private enum SystemStates
    {
        Initializing,
        DetectingObjects,
        Waiting
    }

    #region UnityMethods

    private void Awake()
    {

    }
    async void Start()
    {

        cam = Camera.main;

        try
        {
            // Create a new instance of the network model class
            // and asynchronously load the onnx model
            _networkModel = new NetworkModel();

        #if ENABLE_WINMD_SUPPORT
            await _networkModel.InitModelAsync();
        #endif

            Debug.Log("Loaded model. Starting camera...");

        #if ENABLE_WINMD_SUPPORT
            // Configure camera to return frames fitting the model input size
            try
            {
                Debug.Log("Creating MediaCaptureUtility and initializing frame reader.");
                _mediaCaptureUtility = new MediaCaptureUtility();
                await _mediaCaptureUtility.InitializeMediaFrameReaderAsync((uint)InputFeatureSize.x, (uint)InputFeatureSize.y);
                Debug.Log("Camera started. Running!");

                Debug.Log("Successfully initialized frame reader.");

                currentState = (int)SystemStates.Waiting;
            }
            catch (Exception ex)
            {
                Debug.Log("Failed to start camera: {ex.Message}. Using loaded/picked image.");

            }

        #endif
        }
        catch (Exception ex)
        {
            Debug.Log("Error init:" + ex.Message);
            Debug.Log($"Failed to start model inference: {ex}");
        }

    }
    private async void OnDestroy()
    {
        _isRunning = false;
        if (_mediaCaptureUtility != null)
        {
            await _mediaCaptureUtility.StopMediaFrameReaderAsync();
        }
    }

    async void Update()
    {
#if ENABLE_WINMD_SUPPORT
    counter += 1; 
        if (counter >= samplingInterval && currentState == 2)
        {
            counter = 0;
            currentState = (int)SystemStates.DetectingObjects;
            bool inferenceSuccess = await RunInferenceOnFrame();
            
        }
#endif
    }

    #endregion




    public async Task<bool> RunInferenceOnFrame() {

        try
        {
#if ENABLE_WINMD_SUPPORT
            Task thread = Task.Run(async () =>
            {

                InferenceResult result = await RunDetectionModel();

                UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                {
                    //bool successObjVisualization = RunDetectionVisualization(result, InputFeatureSize);
                }, true);

            });
#endif
            return true;

        }
        catch (Exception e)
        {
            throw;
            return false;
        }

    }

#if ENABLE_WINMD_SUPPORT
    private async Task<InferenceResult> RunDetectionModel()
    {
        
        //currentState = (int)SystemStates.DetectingObjects;
        InferenceResult result = new InferenceResult();;


        if (_mediaCaptureUtility.IsCapturing)
        {
            using (var videoFrame = _mediaCaptureUtility.GetLatestFrame())
            {

                    try
                    {
                        // Get the current network prediction from model and input frame

                        result = await _networkModel.EvaluateVideoFrameAsync(videoFrame);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        Debug.Log("Exception:" + ex.Message);
                        return result;
                    
                    }

            }

        }
        else{
            Debug.Log("Media Capture Utility not capturing frames!");
            return result;
        }
    }

   /* private bool RunDetectionVisualization(InferenceResult result, Vector2 InputFeatureSize)
    {
        if (result?.Any() == true)
        {
            foreach (NetworkResult detection in result)
            {
                Debug.Log("***********************************************************************");
                Debug.Log("BBox x coord is: " + detection.bbox[0] + " and BBox y coord is :" + detection.bbox[1] + " and the width is: " + detection.bbox[2] +
                 " and the height is: " + detection.bbox[3]);
                 Debug.Log("***********************************************************************");
                 GameObject newObject = Instantiate(objectOutlineCube, tempLocation, Quaternion.identity);
                 Vector3 localScale = new Vector3((detection.bbox[2] / InputFeatureSize.x) * hit.distance, (detection.bbox[3] / InputFeatureSize.y) * hit.distance, depths[detection.label]);
            }
            
            currentState = (int)SystemStates.Waiting;
            return true;
        }
        else
        {
            //Debug.Log("No Detections found in frame");
            currentState = (int)SystemStates.Waiting;
            result.Clear();
            return false;
        }

    }*/


#endif

}