﻿// Adapted from the Window's MR Holographic face tracking sample
// https://docs.microsoft.com/en-us/samples/microsoft/windows-universal-samples/holographicfacetracking/
//And this repo by Mitchell Doughty
//https://github.com/doughtmw/YoloDetectionHoloLens-Unity
//using this Research mode port to C#
//https://github.com/petergu684/HoloLens2-ResearchMode-Unity

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

using HL2UnityPlugin;

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

    public static System.Numerics.Vector3 ToSystemWithoutConversion(UnityEngine.Vector3 vector) => new System.Numerics.Vector3(vector.x, vector.y, vector.z);
}

public class FrameSanitizer : MonoBehaviour
{
    // Public fields
    public GameObject objectOutlineCube;
    public GameObject imagePreviewPlane;
    public GameObject longDepthPreviewPlane;
    public int samplingInterval;
    public GameObject clientSocket;

    // Private fields
    private NetworkModel _networkModel;
    private MediaCaptureUtility _mediaCaptureUtility;
    private bool _isRunning = false;
    private byte[] depthData = null;
    byte[] sanitizedImageFrameByteArray = null;
    byte[] sanitizedDepthFrameByteArray = null;
    private Material imageMediaMaterial = null;
    private Texture2D imageMediaTexture = null;
    private Material longDepthMediaMaterial = null;
    private Texture2D longDepthMediaTexture = null;



#if ENABLE_WINMD_SUPPORT
    private Windows.Perception.Spatial.SpatialCoordinateSystem worldSpatialCoordinateSystem;
    HL2ResearchMode researchMode;
    private Frame returnFrame;

#endif

    int counter;
    Microsoft.MixedReality.Toolkit.Input.IMixedRealityEyeGazeProvider eyeGazeProvider;
    float averageFaceWidthInMeters = 0.15f;
    float averageHumanHeightToFaceRatio = 2.0f / 0.15f;

    #region UnityMethods

    private void Awake()
    {

    }

    async void Start()
    {
        eyeGazeProvider = CoreServices.InputSystem?.EyeGazeProvider;
        counter = samplingInterval;

#if ENABLE_WINMD_SUPPORT
        try
        {
            _networkModel = new NetworkModel();
        }
        catch (Exception ex)
        {
            Debug.Log("Error initializing inference model:" + ex.Message);
            Debug.Log($"Failed to start model inference: {ex}");
        }

        // Configure camera to return frames fitting the model input size
        try
        {
                Debug.Log("Creating MediaCaptureUtility and initializing frame reader.");
                _mediaCaptureUtility = new MediaCaptureUtility();
                await _mediaCaptureUtility.InitializeMediaFrameReaderAsync();
                //Debug.Log("Camera started. Running!");
                Debug.Log("Successfully initialized frame reader.");
        }
        catch (Exception ex)
        {
            Debug.Log("Failed to start camera: {ex.Message}. Using loaded/picked image.");
        }

        worldSpatialCoordinateSystem = PerceptionInterop.GetSceneCoordinateSystem(Pose.identity) as SpatialCoordinateSystem;

        try
        {
            researchMode = new HL2ResearchMode();

            researchMode.InitializeLongDepthSensor();      
            researchMode.InitializeSpatialCamerasFront();
            researchMode.SetReferenceCoordinateSystem(worldSpatialCoordinateSystem);
            researchMode.SetPointCloudDepthOffset(0);

            researchMode.StartLongDepthSensorLoop(true);
            researchMode.StartSpatialCamerasFrontLoop();

            if (longDepthPreviewPlane != null)
            {
                longDepthMediaMaterial = longDepthPreviewPlane.GetComponent<MeshRenderer>().material;
                longDepthMediaTexture = new Texture2D(320, 288, TextureFormat.Alpha8, false);
                longDepthMediaMaterial.mainTexture = longDepthMediaTexture;
            }

            if (imagePreviewPlane != null)
            {
                imageMediaMaterial = imagePreviewPlane.GetComponent<MeshRenderer>().material;
                imageMediaTexture = new Texture2D(1280, 720, TextureFormat.BGRA32, false);
                imageMediaMaterial.mainTexture = imageMediaTexture;
            }
        }
        catch (Exception ex)
        {
            Debug.Log("Error starting research mode:" + ex.Message);

        }


#endif
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
        if (_mediaCaptureUtility.IsCapturing)
        {
            //var clientSocketScript = clientSocket.GetComponent<SocketClient>();
            returnFrame = await _mediaCaptureUtility.GetLatestFrame();

                depthData = RetreiveDepthFrame();

                if (returnFrame != null)
                {
                    sanitizedImageFrameByteArray = ObscureFacesInImage(returnFrame);
                    //DisplayImageOnQuad(sanitizedImageFrameByteArray);

                    //var audioBuffer = returnFrame.audioFrame.LockBuffer(0);
                    //IMemoryBufferReference reference = audioBuffer.CreateReference();
                    //Debug.Log(reference);

                    if (depthData != null)
                    {
                        sanitizedDepthFrameByteArray = ObscureDepthInFrame(depthData, returnFrame);
                        //DisplayDepthOnQuad(sanitizedDepthFrameByteArray);
                    }

                    if (counter >= samplingInterval)
                    {
                        counter = 0;

                        Task thread = Task.Run(async () =>
                        {
                            try
                            {
                                // Get the prediction from the model
                                var result = await _networkModel.EvaluateVideoFrameAsync(returnFrame.bitmap);
                                //If faces exist in frame, identify in 3D
                                if (result.Faces.Any())
                                {
                                    UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                                    {
                                        //Visualize the detections in 3D to create GameObjects for eye gaze to interact with
                                        RGBDetectionToWorldspace(result, returnFrame);
                                    }, true);
                                }

                                //sanitizedImageFrameByteArray = ObscureFacesInImage(returnFrame);
                                //var sendResult = await clientSocketScript.trySendSanitizedImage(sanitizedImageFrameByteArray);

                            }
                            catch (Exception ex)
                            {
                                Debug.Log("Exception:" + ex.Message);
                            }

                        });
                    }
                }
        }
#endif
    }

    #endregion



#if ENABLE_WINMD_SUPPORT

   private void RGBDetectionToWorldspace(DetectedFaces result, Frame returnFrame)
   {

        //Debug.Log("Number of faces: " + result.Faces.Count());
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
            float estimatedFaceDepth = averagePixelsForFaceAt1Meter / (float)face.Width;
            Vector3 targetPositionInCameraSpace = normalizedVector * estimatedFaceDepth;
            Vector3 bestRectPositionInWorldspace = unityCameraToWorld.MultiplyPoint(targetPositionInCameraSpace);
            var newObject = Instantiate(objectOutlineCube, bestRectPositionInWorldspace, Quaternion.identity);
            newObject.GetComponent<BoundingBoxScript>().box.X = face.X;
            newObject.GetComponent<BoundingBoxScript>().box.Y = face.Y;
            newObject.GetComponent<BoundingBoxScript>().box.Width = (uint)(face.Width);
            newObject.GetComponent<BoundingBoxScript>().box.Height = (uint)(face.Height);
        }
 
    }

    private void DisplayImageOnQuad(byte[] imageBytes)
    {
        imageMediaTexture.LoadRawTextureData(imageBytes);
        imageMediaTexture.Apply();
    }

    private void DisplayDepthOnQuad(byte[] longDepthFrameData)
    {
        longDepthMediaTexture.LoadRawTextureData(longDepthFrameData);
        longDepthMediaTexture.Apply();
    }


    private byte[] RetreiveDepthFrame()
    {
        byte[] longDepthFrameData = null;
        // update long depth map texture
        if (longDepthPreviewPlane != null && researchMode.LongDepthMapTextureUpdated())
        {
            byte[] frameTexture = researchMode.GetLongDepthMapTextureBuffer();
            if (frameTexture.Length > 0)
            {
                if (longDepthFrameData == null)
                {
                    longDepthFrameData = frameTexture;
                }
                else
                {
                    System.Buffer.BlockCopy(frameTexture, 0, longDepthFrameData, 0, longDepthFrameData.Length);
                }


            }
        }

        return longDepthFrameData;
    }

    private byte[] ObscureDepthInFrame(byte[] depthFrame, Frame returnFrame){

        byte[] depthBytesWithoutBystanders = new byte[Buffer.ByteLength(depthFrame)];
        depthFrame.CopyTo(depthBytesWithoutBystanders,0);
        //used to convert detection X,Y to depth frame X,Y
        float depthToImageWidthRatio = 320.0F / (float)returnFrame.bitmap.PixelWidth;
        float depthToImageHeightRatio = 288.0F / (float)returnFrame.bitmap.PixelHeight;

        //for each detection GameObject, we convert the current 3D position to to 2D position of the current depth frame
        foreach (GameObject face in GameObject.FindGameObjectsWithTag("BoundingBox"))
        {
            var boundingBoxScript = face.GetComponent<BoundingBoxScript>();
            Rect tempbox = boundingBoxScript.box;
            Vector3 reletiveNormalizedPos = (face.transform.position - Camera.main.transform.position).normalized;
            float dot = Vector3.Dot(reletiveNormalizedPos, Camera.main.transform.forward);

            //angle difference between looking direction and direction to item (radians), if larger than HL2 RGB FOV, don't attempt to obscure
            float angle = Mathf.Acos(dot);
            if (boundingBoxScript.toObscure && angle < 0.60F)
            {
                var worldToCamera = (System.Numerics.Matrix4x4)worldSpatialCoordinateSystem.TryGetTransformTo(returnFrame.spatialCoordinateSystem);
                UnityEngine.Matrix4x4 unityWorldToCamera = NumericsConversionExtensions.ToUnity(worldToCamera);
                Vector3 cameraSpaceCoordinate = unityWorldToCamera.MultiplyPoint(face.transform.position);
                Point projected2DPoint = returnFrame.cameraIntrinsics.ProjectOntoFrame(NumericsConversionExtensions.ToSystemWithoutConversion(cameraSpaceCoordinate));

                var faceToHeightConversion = (double)(tempbox.Height * depthToImageHeightRatio) - (tempbox.Height * averageHumanHeightToFaceRatio);
                var xCoord = (double)(projected2DPoint.X * depthToImageWidthRatio) - ((double)(tempbox.Width * depthToImageWidthRatio) / 2.0F);
                var yCoord = (double)(projected2DPoint.Y * depthToImageHeightRatio) - ((double)(tempbox.Height * depthToImageHeightRatio) / 2.0F);
                //var yCoord = (double)((projected2DPoint.Y * depthToImageHeightRatio) / 2.0F) - faceToHeightConversion;
                //yCoord = (yCoord < 0) ? yCoord = 0 : yCoord;

                for (uint rows = (uint)yCoord; rows < yCoord + (tempbox.Height * depthToImageHeightRatio); rows++)
                {
                    for (uint columns = (uint)xCoord; columns < xCoord + (tempbox.Width * depthToImageWidthRatio); columns++)
                    {
                        depthBytesWithoutBystanders[(rows * 320) + columns] = depthBytesWithoutBystanders[(rows * 320) + (uint)xCoord];
                    }
                }
            }
        }

        return depthBytesWithoutBystanders;
    }

    private byte[] ObscureFacesInImage(Frame returnFrame)
    {


        byte[] imageBytesWithoutBystanders = new byte[8 * returnFrame.bitmap.PixelWidth * returnFrame.bitmap.PixelHeight];
        returnFrame.bitmap.CopyToBuffer(imageBytesWithoutBystanders.AsBuffer());
        //for each detection GameObject, we convert the current 3D position to to 2D position of the current depth frame
        foreach (GameObject face in GameObject.FindGameObjectsWithTag("BoundingBox"))
        {
            var boundingBoxScript = face.GetComponent<BoundingBoxScript>();
            Rect tempbox = boundingBoxScript.box;
            Vector3 reletiveNormalizedPos = (face.transform.position - Camera.main.transform.position).normalized;
            float dot = Vector3.Dot(reletiveNormalizedPos, Camera.main.transform.forward);

            //angle difference between looking direction and direction to item (radians), if larger than HL2 RGB FOV, don't attempt to obscure
            float angle = Mathf.Acos(dot);
            if (boundingBoxScript.toObscure && angle < 0.60F)
            {
                var worldToCamera = (System.Numerics.Matrix4x4)worldSpatialCoordinateSystem.TryGetTransformTo(returnFrame.spatialCoordinateSystem);
                UnityEngine.Matrix4x4 unityWorldToCamera = NumericsConversionExtensions.ToUnity(worldToCamera);
                Vector3 cameraSpaceCoordinate = unityWorldToCamera.MultiplyPoint(face.transform.position);
                Point projected2DPoint = returnFrame.cameraIntrinsics.ProjectOntoFrame(NumericsConversionExtensions.ToSystemWithoutConversion(cameraSpaceCoordinate));

                var xCoord = (double)projected2DPoint.X - ((double)tempbox.Width / 2.0F);
                //xCoord = (xCoord-20 > 0) ? xCoord -= 20 : xCoord;
                var yCoord = (double)projected2DPoint.Y - ((double)tempbox.Height / 2.0F);
                //yCoord = (yCoord-20 > 0) ? yCoord -= 20 : yCoord;

                for (uint rows = (uint)yCoord; rows <= yCoord + tempbox.Height; rows++)
                {
                    for (uint columns = (uint)xCoord; columns < xCoord + tempbox.Width; columns++)
                    {
                        imageBytesWithoutBystanders[(rows * returnFrame.bitmap.PixelWidth * 4) + columns * 4] = Byte.MaxValue;
                        imageBytesWithoutBystanders[(rows * returnFrame.bitmap.PixelWidth * 4) + columns * 4 + 1] = Byte.MaxValue;
                        imageBytesWithoutBystanders[(rows * returnFrame.bitmap.PixelWidth * 4) + columns * 4 + 2] = Byte.MaxValue;
                    }
                }
            }

        }
        return imageBytesWithoutBystanders;
    }
#endif
}