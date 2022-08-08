// Adapted from the Window's MR Holographic face tracking sample
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
    public GameObject quad;
    public GameObject longDepthPreviewPlane;
    public int samplingInterval;

    // Private fields
    private NetworkModel _networkModel;
    private MediaCaptureUtility _mediaCaptureUtility;
    private bool _isRunning = false;
    private byte[] depthData = null;
    byte[] sanitizedImageFrameByteArray = null;
    byte[] sanitizedDepthFrameByteArray = null;
    private Material longDepthMediaMaterial = null;
    private Texture2D longDepthMediaTexture = null;


#if ENABLE_WINMD_SUPPORT
    private Windows.Perception.Spatial.SpatialCoordinateSystem worldSpatialCoordinateSystem;
    Renderer quadRenderer;
    Renderer depthQuadRenderer;
    HL2ResearchMode researchMode;
    private Frame returnFrame;

#endif

    int counter;
    Microsoft.MixedReality.Toolkit.Input.IMixedRealityEyeGazeProvider eyeGazeProvider;
    float averageFaceWidthInMeters = 0.15f;
    float averageHumanHeightInMeters = 2.00f;

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
        quadRenderer = quad.GetComponent<Renderer>() as Renderer;

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

            returnFrame = await _mediaCaptureUtility.GetLatestFrame();
            depthData = RetreiveDepthFrame();

            if (returnFrame != null)
            {
                byte[] sanitizedImageFrameByteArray = ObscureFacesInImage(returnFrame);

                if (depthData != null)
                {
                    sanitizedDepthFrameByteArray = ObscureDepthInFrame(depthData, returnFrame);
                    DisplayDepthOnQuad(sanitizedDepthFrameByteArray);
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
                                    //Visualize the detections in 3D to create GameObejcts for eye gaze to interact with
                                    RGBDetectionToWorldspace(result, returnFrame);
                                }, true);
                            }

                            //sanitizedImageFrameByteArray = SanitizeImageFrame(returnFrame);

                            UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                            {
                                //Use the 3D bounding boxes to remove "bystanders" and keep "subjects"
                                DisplayImageOnQuad(sanitizedImageFrameByteArray, result.originalImageBitmap.PixelWidth, result.originalImageBitmap.PixelHeight);
                                
                            }, true);
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
            newObject.GetComponent<BoundingBoxScript>().box.Width = (uint)(face.Width + face.Width * 0.1);
            newObject.GetComponent<BoundingBoxScript>().box.Height = (uint)(face.Height + face.Height * 0.1);
        }
 
    }

    private void DisplayImageOnQuad(byte[] imageBytes, int width, int height)
    {
        Texture2D photoTexture = new Texture2D(width, height);
	    photoTexture.LoadRawTextureData(imageBytes);
        photoTexture.Apply();

        // Create a GameObject to which the texture can be applied
        quadRenderer.material.SetTexture("_MainTex", photoTexture);
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
        var depthToImageWidthRatio = 320 / returnFrame.bitmap.PixelWidth;
        var depthToImageHeightRatio = 288 / returnFrame.bitmap.PixelHeight;

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

                var xCoord = (double)(projected2DPoint.X * depthToImageWidthRatio) - ((double)(tempbox.Width * depthToImageWidthRatio) / 2.0F);
                var yCoord = (double)(projected2DPoint.Y * depthToImageHeightRatio) - ((double)(tempbox.Height * depthToImageHeightRatio) / 2.0F);

                for (uint rows = (uint)yCoord; rows <= yCoord + (tempbox.Height * depthToImageHeightRatio); rows++)
                {
                    for (uint columns = (uint)xCoord; columns < xCoord + (tempbox.Width * depthToImageWidthRatio); columns++)
                    {
                        depthBytesWithoutBystanders[(rows * 320) + columns] = Byte.MinValue;
                    }
                }
            }
        }

        return depthBytesWithoutBystanders;
    }

    private byte[] ObscureFacesInImage(Frame returnFrame)
    {

        //byte[] imageBytes = new byte[8 * returnFrame.bitmap.PixelWidth * returnFrame.bitmap.PixelHeight];
	    //returnFrame.bitmap.CopyToBuffer(imageBytes.AsBuffer());

        byte[] imageBytesWithoutBystanders = new byte[8 * returnFrame.bitmap.PixelWidth * returnFrame.bitmap.PixelHeight];
        //inputRawImage.CopyTo(imageBytesWithoutBystanders,0);
        returnFrame.bitmap.CopyToBuffer(imageBytesWithoutBystanders.AsBuffer());
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
                //Debug.Log(cameraSpaceCoordinate);
                Point projected2DPoint = returnFrame.cameraIntrinsics.ProjectOntoFrame(NumericsConversionExtensions.ToSystemWithoutConversion(cameraSpaceCoordinate));
                //Debug.Log(projected2DPoint);

                var xCoord = (double)projected2DPoint.X - ((double)tempbox.Width / 2.0F);
                var yCoord = (double)projected2DPoint.Y - ((double)tempbox.Height / 2.0F);

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