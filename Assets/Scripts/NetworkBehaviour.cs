// Adapted from the Window's MR Holographic face tracking sample
// https://docs.microsoft.com/en-us/samples/microsoft/windows-universal-samples/holographicfacetracking/
//And this repo by Mitchell Doughty
//https://github.com/doughtmw/YoloDetectionHoloLens-Unity

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.OpenXR;



#if ENABLE_WINMD_SUPPORT
using Windows.Media;
using Windows.Graphics.Imaging;
using Windows.Foundation;
using Windows.Media.Devices.Core;
using Windows.Perception.Spatial;
using Windows.Perception.Spatial.Preview;

//using HL2UnityPlugin;

#endif

//https://docs.microsoft.com/en-us/windows/mixed-reality/develop/unity/unity-xrdevice-advanced?tabs=mrtk
public static class NumericsConversionExtensions
{
    public static UnityEngine.Vector3 ToUnity(this System.Numerics.Vector3 v) => new UnityEngine.Vector3(v.X, v.Y, -v.Z);
    public static UnityEngine.Quaternion ToUnity(this System.Numerics.Quaternion q) => new UnityEngine.Quaternion(q.X, q.Y, -q.Z, -q.W);
    public static UnityEngine.Matrix4x4 ToUnity(this System.Numerics.Matrix4x4 m) => new UnityEngine.Matrix4x4(
        new Vector4(m.M11, m.M12, -m.M13, m.M14),
        new Vector4(m.M21, m.M22, -m.M23, m.M24),
        new Vector4(-m.M31, -m.M32, m.M33, -m.M34),
        new Vector4(m.M41, m.M42, -m.M43, m.M44));

    public static System.Numerics.Vector3 ToSystem(this UnityEngine.Vector3 v) => new System.Numerics.Vector3(v.x, v.y, -v.z);
    public static System.Numerics.Quaternion ToSystem(this UnityEngine.Quaternion q) => new System.Numerics.Quaternion(q.x, q.y, -q.z, -q.w);
    public static System.Numerics.Matrix4x4 ToSystem(this UnityEngine.Matrix4x4 m) => new System.Numerics.Matrix4x4(
        m.m00, m.m10, -m.m20, m.m30,
        m.m01, m.m11, -m.m21, m.m31,
       -m.m02, -m.m12, m.m22, -m.m32,
        m.m03, m.m13, -m.m23, m.m33);
}

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

#if ENABLE_WINMD_SUPPORT
    private Windows.Perception.Spatial.SpatialCoordinateSystem worldSpatialCoordinateSystem;
    Renderer quadRenderer;
#endif

    int counter = 0;
    Microsoft.MixedReality.Toolkit.Input.IMixedRealityEyeGazeProvider eyeGazeProvider;

    float averageFaceWidthInMeters = 0.15f;

    #region UnityMethods

    private void Awake()
    {

    }

    async void Start()
    {
        eyeGazeProvider = CoreServices.InputSystem?.EyeGazeProvider;


        try
        {
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
               quadRenderer = quad.GetComponent<Renderer>() as Renderer;

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
        Frame returnFrame = _mediaCaptureUtility.GetLatestFrame();
        if (counter >= samplingInterval)
        {
            counter = 0;

            //Task thread = Task.Run(async () =>
            //{

                if (_mediaCaptureUtility.IsCapturing)
                {
                        try
                        {
                            // Get the current network prediction from model and input frame
                            var result = await _networkModel.EvaluateVideoFrameAsync(returnFrame.videoFrame);
                            //UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                            // {
                            RunDetectionVisualization(result, returnFrame);
                            //}, false);
                        }
                        catch (Exception ex)
                        {
                            Debug.Log("Exception:" + ex.Message);
                        }
                }
                else{
                    Debug.Log("Media Capture Utility not capturing frames!");
                }

            //});


        }

#endif

    }

    #endregion



#if ENABLE_WINMD_SUPPORT

   private void RunDetectionVisualization(DetectedFaces result, Frame returnFrame)
    {

        RaycastHit hit;
        // Bit shift the index of the layer (31) to get a bit mask
        int layerMask = 1 << 31;
        //Debug.Log("Number of faces: " + result.Faces.Count());
        worldSpatialCoordinateSystem = PerceptionInterop.GetSceneCoordinateSystem(Pose.identity) as SpatialCoordinateSystem;
        var cameraToWorld = (System.Numerics.Matrix4x4)returnFrame.spatialCoordinateSystem.TryGetTransformTo(worldSpatialCoordinateSystem);
        UnityEngine.Matrix4x4 unityCameraToWorld = NumericsConversionExtensions.ToUnity(cameraToWorld);
        var pixelsPerMeterAlongX = returnFrame.cameraIntrinsics.FocalLength.X;
        var averagePixelsForFaceAt1Meter = pixelsPerMeterAlongX * averageFaceWidthInMeters;
        
        foreach (Rect face in result.Faces)
        {
            double xCoord = (double)face.X + ((double)face.Width / 2.0F);
            double yCoord = (double)face.Y + ((double)face.Height / 2.0F);
            
            System.Numerics.Vector2 projectedVector = returnFrame.cameraIntrinsics.UnprojectAtUnitDepth(new Point(xCoord, yCoord));
            UnityEngine.Vector3 normalizedVector = NumericsConversionExtensions.ToUnity(new System.Numerics.Vector3(projectedVector.X, projectedVector.Y, -1.0f));
            normalizedVector.Normalize();

            //I tested this program using face images found on google. Problem is that small, thumbnail images are thought to be farther away than they actually are using a pixel/width method
            //Here, we use a raycast to judge if there is a physical object between the headset and the expected depth, likely a monitor.  If so, we set the depth to that reading.
            float estimatedFaceDepth = averagePixelsForFaceAt1Meter / (float)face.Width;
            
            Vector3 targetPositionInCameraSpace = normalizedVector * estimatedFaceDepth;
            Vector3 bestRectPositionInWorldspace = unityCameraToWorld.MultiplyPoint(targetPositionInCameraSpace);
            var newObject = Instantiate(objectOutlineCube, bestRectPositionInWorldspace, Quaternion.identity);
        }
 
    }

    private void AnalyzeFrame(Frame returnFrame)
    {
        byte[] imageBytes = new byte[8 * 1280 * 720];
	    result.originalImageBitmap.CopyToBuffer(imageBytes.AsBuffer());
        Texture2D photoTexture = new Texture2D(result.originalImageBitmap.PixelWidth, result.originalImageBitmap.PixelHeight);
	    photoTexture.LoadRawTextureData(imageBytes);
        photoTexture.Apply();

        // Create a GameObject to which the texture can be applied
        quadRenderer.material.SetTexture("_MainTex", photoTexture);
    }



#endif

    private void ObscureFaces()
    {
        foreach (GameObject face in GameObject.FindGameObjectsWithTag("BoundingBox"))
        {
            Debug.Log("1");
        }
    }

}