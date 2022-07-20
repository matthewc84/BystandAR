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
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;



#if ENABLE_WINMD_SUPPORT
using Windows.Media;
using Windows.Graphics.Imaging;


#endif

public class NetworkBehaviour : MonoBehaviour
{
    // Public fields
    public GameObject objectOutlineCube;
    public GameObject quad;
    public int samplingInterval = 60;

    // Private fields
    private NetworkModel _networkModel;
    private MediaCaptureUtility _mediaCaptureUtility;
    private bool _isRunning = false;
    private Camera cam;
    public int currentState;
    int counter = 0;
    GameObject cameraGameObject;
    Camera newCamera;
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
        cameraGameObject = new GameObject();
        newCamera = cameraGameObject.AddComponent<Camera>();
        newCamera.enabled = false;

        try
        {
            // Create a new instance of the network model class
            // and asynchronously load the onnx model
            _networkModel = new NetworkModel();

            //Debug.Log("Starting camera...");

        #if ENABLE_WINMD_SUPPORT
            // Configure camera to return frames fitting the model input size
            try
            {
                Debug.Log("Creating MediaCaptureUtility and initializing frame reader.");
                _mediaCaptureUtility = new MediaCaptureUtility();
                await _mediaCaptureUtility.InitializeMediaFrameReaderAsync();
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
        if (counter >= samplingInterval)
        {
            counter = 0;
            //currentState = (int)SystemStates.DetectingObjects;
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
                UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                {
               Debug.Log("Setting Temp Camera");
                    //take "snapshot" of current camera position and rotation, so if user moves while model is working on detection, the resulting location wont be skewed
                    newCamera.transform.position = cam.transform.position;
                    newCamera.transform.rotation = cam.transform.rotation;
                }, false);
               Debug.Log("Running Model");
                var result = await RunDetectionModel();

                UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                {
               Debug.Log("Running Visualization");
                    RunDetectionVisualization(result, newCamera);
                }, false);

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
    private async Task<DetectedFaces> RunDetectionModel()
    {
        
        //currentState = (int)SystemStates.DetectingObjects;
        DetectedFaces result = new DetectedFaces();


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

   private void RunDetectionVisualization(DetectedFaces result, Camera tempCamera)
    {

        RaycastHit hit;
        // Bit shift the index of the layer (31) to get a bit mask
        int layerMask = 1 << 31;

        Debug.Log("Number of face: " + result.Faces.Count());

        byte[] imageBytes = new byte[8 * result.originalImageBitmap.PixelWidth * result.originalImageBitmap.PixelHeight];
	    result.originalImageBitmap.CopyToBuffer(imageBytes.AsBuffer());

        foreach (Rect face in result.Faces)
        {
            Debug.Log("***********************************************************************");
            Debug.Log("BBox x coord is: " + face.X + " and BBox y coord is :" + face.Y + " and the width is: " + face.Width +
             " and the height is: " + face.Height);
            Debug.Log("***********************************************************************");
            uint faceIndex= (face.Y-1)*4 + (face.X-1)*4;
            Ray ray = tempCamera.ViewportPointToRay(new Vector3(face.X / result.FrameWidth, face.Y / result.FrameHeight, 0));

            if (Physics.Raycast(ray, out hit, 10, layerMask))
            {

                Vector3 tempLocation = hit.point;
                GameObject newObject = Instantiate(objectOutlineCube, tempLocation, Quaternion.identity);
                //Vector3 localScale = new Vector3((face.Width / result.FrameWidth), (face.Height / result.FrameHeight), .05F);
                //newObject.transform.localScale = localScale;
                //BoxCollider boxCollider = newObject.AddComponent<BoxCollider>();

            }

        }
 

	    Texture2D photoTexture = new Texture2D(result.originalImageBitmap.PixelWidth, result.originalImageBitmap.PixelHeight);
	    photoTexture.LoadRawTextureData(imageBytes);
        photoTexture.Apply();

        // Create a GameObject to which the texture can be applied
        Renderer quadRenderer = quad.GetComponent<Renderer>() as Renderer;
        quadRenderer.material.SetTexture("_MainTex", photoTexture);



    }


#endif



}