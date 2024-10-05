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
using Unity.Mathematics;
using Unity.Collections;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.InteropServices;
using System.Threading;
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

#region ConversionExtension
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

#endregion

namespace BystandAR
{
    public class SanitizedFrames
    {
        public byte[] sanitizedDepthFrame { get; set; }
        public Texture2D sanitizedImageFrame { get; set; }

    }

    [RequireComponent(typeof(AudioSource))]
    public class FrameSanitizer : MonoBehaviour
    {
        [SerializeField]
        private string ReadGazeOriginFromCSVName = "GazeOrigin2024-18-6--19-45-33.csv";
        private string GazeOriginFile;
        [SerializeField]
        private string ReadGazeDirFromCSVName = "GazeDir2024-18-6--19-45-33.csv";
        private string GazeDirFile;
        [SerializeField]
        private string ReadQRDirFromCSVName = "QRDir2024-18-6--19-45-33.csv";
        private string QRDirFile;
        [SerializeField]
        private string ReadQRDistFromCSVName = "QRDist2024-18-6--19-45-33.csv";
        private string QRDistFile;

        private Queue<Vector3> HardcodedGazeOriginPath = new Queue<Vector3>();
        private Queue<Vector3> HardcodedGazeDirPath = new Queue<Vector3>();
        private Queue<Vector3> HardcodedQRDirPath = new Queue<Vector3>();
        private Queue<float> HardcodedQRDistPath = new Queue<float>();

        Vector3 direction = new Vector3(0, 0, 0);
        Vector3 origin = new Vector3(0, 0, 0);

        // Public fields
        public GameObject objectOutlineCube;
        public int samplingInterval;
        [SerializeField]
        private int LoggingFrameStep = 1;
        public bool SanitizeFrames;
        [SerializeField] // false: turns off debug cube position update, debug cube renderer, texture updates 
        private bool DebugCubeMode = true;
        public bool userSpeaking;
        public GameObject imagePreviewPlane;
        public GameObject longDepthPreviewPlane;
        public GameObject debugWindow;

        // Private fields
        private NetworkModel _networkModel;
        private byte[] depthData = null;
        private Texture2D tempImageTexture;
        private MediaCaptureUtility _mediaCaptureUtility;
        private float averageAmplitude = 0.0f;
        private Color32[] eyeColors;
        private static Mutex mut = new Mutex();
        private Material imageMediaMaterial = null;
        private Texture2D imageMediaTexture = null;
        private Material longDepthMediaMaterial = null;
        private Texture2D longDepthMediaTexture = null;

        [SerializeField]
        private GameObject EyeGazeVisualizer;
        private Renderer EyeGazeVisualizerRenderer;
        [SerializeField]
        private bool VisualizationToggle = false;

        [SerializeField]
        private GameObject Logger;
        private CSVLogger CSVLoggerScript;

        [SerializeField]
        private float frameRateFrequency;

        private int faceCounter = 0;
        private int faceCubeCounter = 1;

        private DateTime lastCallToDrawPred = DateTime.Now;

        private bool toggleButtonState = false;
        [SerializeField]
        private GameObject ToggleButton;
        // private Renderer ToggleButtonRenderer;
        [SerializeField]
        private Vector3 toggleButtonOffset = new Vector3(0.2f, -0.3f, 0.01f);

        [SerializeField]
        private GameObject ArucoDetectionObject;
        private ArUcoMarker ArucoDetctionScript;

        private float QRDistMin;
        private float QRDistMax;
        private float QRDistMid;

        [SerializeField]
        private float distScalingFactor = 1.0f;

        private float QRDirMinX;
        private float QRDirMaxX;
        private float QRDirMidX;

        private float QRDirMinY;
        private float QRDirMaxY;
        private float QRDirMidY;

        private float QRDirMinZ;
        private float QRDirMaxZ;
        private float QRDirMidZ;

        /*
        public float boundsBuffer = 0.01f;
        public float DistBoundsBuffer = 0.1f;
        */

        private float currDistance;
        private Vector3 currDirection;

        [SerializeField]
        private GameObject PositioningCube;
        private Renderer PositioningCubeRenderer;
        private Collider PositioningCubeCollider;

        Vector3 currQRPosition;
        Quaternion currQRRotation;
        private bool qrDetectionStatus = false;

        private GameObject[] faceCubesInScene;

#if ENABLE_WINMD_SUPPORT
    private Windows.Perception.Spatial.SpatialCoordinateSystem worldSpatialCoordinateSystem;
    HL2ResearchMode researchMode;

#endif
        private int frameCounter = 0;
        int samplingCounter;
        int frameCaptureCounter = 0;
        Microsoft.MixedReality.Toolkit.Input.IMixedRealityEyeGazeProvider eyeGazeProvider;
        float averageFaceWidthInMeters = 0.15f;
        float averageHumanHeightToFaceRatio = 2.0f / 0.15f;

        #region UnityMethods

        async void Awake()
        {
            //  CSV Logger Script
            CSVLoggerScript = Logger.GetComponent<CSVLogger>();

            ArucoDetctionScript = ArucoDetectionObject.GetComponent<ArUcoMarker>();

            if (CSVLoggerScript.getReadGazeFromCSV())
            {
                GazeOriginFile = Application.persistentDataPath + "/Logs/" + ReadGazeOriginFromCSVName;
                HardcodedGazeOriginPath = CSVLoggerScript.loadGazeOriginDataCSV(GazeOriginFile);

                GazeDirFile = Application.persistentDataPath + "/Logs/" + ReadGazeDirFromCSVName;
                HardcodedGazeDirPath = CSVLoggerScript.loadGazeDirDataCSV(GazeDirFile);

                QRDirFile = Application.persistentDataPath + "/Logs/" + ReadQRDirFromCSVName;
                
                /*
                if (File.Exists(QRDirFile))
                {
                    Debug.Log("QRDirFile found!");
                }
                else
                {
                    Debug.Log("QRDirFile not found");
                }
                */

                HardcodedQRDirPath = CSVLoggerScript.loadQRDirDataCSV(QRDirFile);

                QRDirMinX = CSVLoggerScript.getQRDirMinX();
                QRDirMaxX = CSVLoggerScript.getQRDirMaxX();
                QRDirMidX = (QRDirMinX + QRDirMaxX) / 2;

                QRDirMinY = CSVLoggerScript.getQRDirMinY();
                QRDirMaxY = CSVLoggerScript.getQRDirMaxY();
                QRDirMidY = (QRDirMinY + QRDirMaxY) / 2;

                QRDirMinZ = CSVLoggerScript.getQRDirMinZ();
                QRDirMaxZ = CSVLoggerScript.getQRDirMaxZ();
                QRDirMidZ = (QRDirMinZ + QRDirMaxZ) / 2;

                /*
                Debug.Log("QRDirMinX: " + QRDirMinX);
                Debug.Log("QRDirMaxX: " + QRDirMaxX);
                Debug.Log("QRDirMinY: " + QRDirMinY);
                Debug.Log("QRDirMaxY: " + QRDirMaxY);
                Debug.Log("QRDirMinZ: " + QRDirMinZ);
                Debug.Log("QRDirMaxZ: " + QRDirMaxZ);
                */

                QRDistFile = Application.persistentDataPath + "/Logs/" + ReadQRDistFromCSVName;
                
                HardcodedQRDistPath = CSVLoggerScript.loadQRDistDataCSV(QRDistFile);

                QRDistMin = CSVLoggerScript.getQRDistMin();
                QRDistMax = CSVLoggerScript.getQRDistMax();
                QRDistMid = (QRDistMax + QRDistMin) / 2;
            }
        }

        async void Start()
        { 
            userSpeaking = false;
            eyeGazeProvider = CoreServices.InputSystem?.EyeGazeProvider;
            samplingCounter = samplingInterval;
            // StartCoroutine(FramerateCountLoop());

            EyeGazeVisualizerRenderer = EyeGazeVisualizer.GetComponent<Renderer>();

            PositioningCubeRenderer = PositioningCube.GetComponent<Renderer>();
            PositioningCubeCollider = PositioningCube.GetComponent<Collider>();

            // ToggleButtonRenderer = ToggleButton.GetComponent<Renderer>();

            //create temp texture to apply SoftwareBitmap to, in order to sanitize
            tempImageTexture = new Texture2D(1920, 1080, TextureFormat.BGRA32, false);

            if (DebugCubeMode)
            {
                if (imagePreviewPlane != null)
                {
                    imageMediaMaterial = imagePreviewPlane.GetComponent<MeshRenderer>().material;
                    imageMediaTexture = new Texture2D(1920, 1080, TextureFormat.BGRA32, false);
                    imageMediaMaterial.mainTexture = imageMediaTexture;
                }

                if (longDepthPreviewPlane != null)
                {
                    longDepthMediaMaterial = longDepthPreviewPlane.GetComponent<MeshRenderer>().material;
                    longDepthMediaTexture = new Texture2D(320, 288, TextureFormat.Alpha8, false);
                    longDepthMediaMaterial.mainTexture = longDepthMediaTexture;
                }
            }
            else
            {
                // disable quad (image), depth quad, debug window
                imagePreviewPlane.SetActive(false);
                longDepthPreviewPlane.SetActive(false);
                debugWindow.SetActive(false);
            }

            /*
            Debug.Log("QRDirMinX: " + QRDirMinX);
            Debug.Log("QRDirMaxX: " + QRDirMaxX);
            Debug.Log("QRDirMinY: " + QRDirMinY);
            Debug.Log("QRDirMaxY: " + QRDirMaxY);
            Debug.Log("QRDirMinZ: " + QRDirMinZ);
            Debug.Log("QRDirMaxZ: " + QRDirMaxZ);

            Debug.Log("QRDistMin: " + QRDistMin);
            Debug.Log("QRDistMax: " + QRDistMax);
            */

#if ENABLE_WINMD_SUPPORT
        try
        {
            _networkModel = new NetworkModel();
            _networkModel.InitFaceDetector();
        }
        catch (Exception ex)
        {
            // Debug.Log("Error initializing inference model:" + ex.Message);
        }


        // Create Media Capture instance
        try
        {
                // Debug.Log("Creating MediaCaptureUtility and initializing frame reader.");
                _mediaCaptureUtility = new MediaCaptureUtility();
                await _mediaCaptureUtility.InitializeMediaFrameReaderAsync();
        }
        catch (Exception ex)
        {
            // Debug.Log("Failed to start camera: {ex.Message}. Using loaded/picked image.");
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

        }
        catch (Exception ex)
        {
            // Debug.Log("Error starting research mode:" + ex.Message);
        }
#endif
        }

        async void Update()
        {
            samplingCounter += 1;

            faceCubesInScene = GameObject.FindGameObjectsWithTag("BoundingBox");

            PositioningCubePosUpdate();

            bool isInBounds = BoundsCheck();
            if (isInBounds)
            {
                EyeGazeVisualizerRenderer.material.color = Color.green;
            }
            else
            {
                EyeGazeVisualizerRenderer.material.color = Color.red;
            }

            if (toggleButtonState && CSVLoggerScript.getReadGazeFromCSV())
            {
                if (HardcodedGazeOriginPath.Count > 0 && HardcodedGazeDirPath.Count > 0)
                {
                    origin = HardcodedGazeOriginPath.Dequeue();
                    // Debug.Log("origin (from file): " + origin);
                    direction = HardcodedGazeDirPath.Dequeue();
                    direction = (direction).normalized;
                }
                else
                {
                    // Debug.Log("Gaze Points ended!!");
                    origin = new Vector3(0, 0, 0);
                    direction = new Vector3(0, 0, 0);
                }
            }
            else
            {
                origin = eyeGazeProvider.GazeOrigin;
                direction = eyeGazeProvider.GazeDirection;
                direction = (direction).normalized;
            }

            eyeGazeFixationVisualizer();

            UpdateButtonsPosition();

#if ENABLE_WINMD_SUPPORT

        if (_mediaCaptureUtility.IsCapturing)
        {
                Frame returnFrame = null;
                if (samplingCounter >= samplingInterval || SanitizeFrames)
                {
                    returnFrame = await _mediaCaptureUtility.GetLatestVideoFrame();
                    frameCounter++;
                }
  
                //evaluate the average amplitude of the collected voice input mic
                /*
                if (averageAmplitude > 0.005f)
                {
                    userSpeaking = true;
                }
                else
                {
                    userSpeaking = false;
                }
                */

                if (returnFrame != null)
                {
                    if (SanitizeFrames)
                    {
                        depthData = RetreiveDepthFrame();
                        SanitizedFrames sanitizedFrame = SanitizeFrame(returnFrame, depthData);

                        if (DebugCubeMode)
                        {
                            Graphics.CopyTexture(sanitizedFrame.sanitizedImageFrame,imageMediaTexture);
                            imageMediaTexture.Apply();
                            if (sanitizedFrame.sanitizedDepthFrame != null)
                            {
                                longDepthMediaTexture.LoadRawTextureData(sanitizedFrame.sanitizedDepthFrame);
                                longDepthMediaTexture.Apply();
                            }
                        }
                    }

                    if (samplingCounter >= samplingInterval)
                    {
                        samplingCounter = 0;

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
                                        RGBDetectionToWorldspace(ref result, ref returnFrame);
                                    }, false);
                                }

                            }
                            catch (Exception ex)
                            {
                                // Debug.Log("Exception:" + ex.Message);
                            }

                        });
                    }
                }

            }
#endif
            if (frameCounter % LoggingFrameStep == 0)
            {
                // CSV Logger
                FramerateCountLoop();
            }
        }

        #endregion

    private void FramerateCountLoop() 
    {
        int FPS = (int)(1.0f / Time.smoothDeltaTime);
        // Debug.Log("Current Frame Rate: " + FPS);

        string finalJSONString = "{";
        //  create JSON like data structure that stores all info regarding all faceCubesInScene
        for (int i = 0; i < faceCubesInScene.Length; i++)
        {
            var faceCubeScript = faceCubesInScene[i].GetComponent<BoundingBoxScriptManual>();

            string faceCubeName = "\"" + faceCubesInScene[i].name + "\": {";

            Vector3 faceCubeWorldPos = faceCubesInScene[i].transform.position;
            string faceCubeWorldPosString = "\"3D Position\": \"<" + faceCubeWorldPos.x + " | " + faceCubeWorldPos.y + " | " + faceCubeWorldPos.z + ">\" |";

            bool faceCubeIsSubject = faceCubeScript.getIsSubject();
            string faceCubeIsSubjectString = "\"isSubject\": \"" + faceCubeIsSubject + "\" |";

            double faceCube2DXCenter = faceCubeScript.xCenter;
            double faceCube2DYCenter = faceCubeScript.yCenter;
            string faceCube2DPosString = "\"2D Position\": \"(" + faceCube2DXCenter + " | " + faceCube2DYCenter + ")\" }";

            string faceCubeJSONString = faceCubeName + faceCubeWorldPosString + faceCubeIsSubjectString + faceCube2DPosString + " |";

            finalJSONString += faceCubeJSONString;
        }
        finalJSONString += '}';


        // log FPS to CSV
        string writeDateTime = DateTime.Now.ToString("HH-mm-ss.ffff");
        string FPSFileLine = writeDateTime + "," + FPS.ToString() + "," + faceCubesInScene.Length.ToString() + "," + frameCounter + "," + !qrDetectionStatus + "," + toggleButtonState + "," + finalJSONString;

        CSVLoggerScript.addFPStoList(FPSFileLine);

        /*
        Debug.Log("currDistance: " + currDistance);
        Debug.Log("currDirection: " + currDirection);
        */

    }

    public void PositioningCubePosUpdate()
    {
        // get latest position and rotation of detected QR code 
        if (!qrDetectionStatus)
        {
            currQRPosition = ArucoDetctionScript.getLatestPosition();
            currQRRotation = ArucoDetctionScript.getLatestRotation();
        }

        // place PositioningCube at certain distance and direction w.r.t. the detected QR code
        /*
        // this works but depth seems a bit off an y axis is totally in negative
        Vector3 angle = (currQRPosition - PositioningCubeRenderer.transform.position).normalized;
        PositioningCubeRenderer.transform.position = currQRPosition + (angle * QRDistMid);
        */

        Vector3 midDir = new Vector3(QRDirMidX, QRDirMidY, QRDirMidZ);
        // PositioningCubeRenderer.transform.position = currQRPosition - (midDir.normalized * (QRDistMid));
        PositioningCubeRenderer.transform.position = currQRPosition - (midDir.normalized * (QRDistMid * distScalingFactor));


        // Debug.Log("PositioningCubeRenderer.transform.position: " + PositioningCubeRenderer.transform.position);
        }

    private void UpdateButtonsPosition()
    {
        ToggleButton.transform.position = Camera.main.transform.position + Camera.main.transform.forward + toggleButtonOffset;
        ToggleButton.transform.rotation = Camera.main.transform.rotation;
    }

    public bool BoundsCheck()
    {
        if (PositioningCubeCollider.bounds.Contains(Camera.main.transform.position))
        {
            if (!qrDetectionStatus)
            {
                // Debug.Log("qrDetectionStatus set to true now -- should not detect QR anymore");
                qrDetectionStatus = true;
                // ArucoDetctionScript.OnDisable();
                ArucoDetectionObject.SetActive(false);
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    public void toggleButtonStateController()
    {
        toggleButtonState = !toggleButtonState;
    }

    private void eyeGazeFixationVisualizer()
    {
        if (VisualizationToggle == true)
        {
            EyeGazeVisualizerRenderer.transform.position = origin + direction * 2;
        }
        else
        {
            EyeGazeVisualizerRenderer.enabled = false;
        }
    }

#if ENABLE_WINMD_SUPPORT

    private void RGBDetectionToWorldspace(ref DetectedFaces result, ref Frame returnFrame)
    {

        //Debug.Log("Number of faces: " + result.Faces.Count());
        faceCounter = result.Faces.Count();

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
                var overlapBoxes = Physics.OverlapBox(bestRectPositionInWorldspace, objectOutlineCube.transform.localScale / 2, Quaternion.identity);
                if(overlapBoxes.Length > 0 && overlapBoxes[0].gameObject.tag == "BoundingBox")
                {
                    overlapBoxes[0].gameObject.transform.position = bestRectPositionInWorldspace;
                    var bboxScript = overlapBoxes[0].gameObject.GetComponent<BoundingBoxScriptManual>();
                    bboxScript.staleCounter = 0;
                    bboxScript.bboxWidth = (float)face.Width * 1.25f;
                    bboxScript.bboxHeight = (float)face.Height * 1.25f;

                    bboxScript.xCenter = xCoord;
                    bboxScript.yCenter = yCoord;
                }
                else
                {
                    var newObject = Instantiate(objectOutlineCube, bestRectPositionInWorldspace, Quaternion.identity);
                    
                    newObject.name = "FaceCube-" + faceCubeCounter.ToString();
                    faceCubeCounter++;

                    var bboxScript = newObject.GetComponent<BoundingBoxScriptManual>();
                    bboxScript.bboxWidth = (float)face.Width * 1.25f;
                    bboxScript.bboxHeight = (float)face.Height * 1.25f;

                    bboxScript.xCenter = xCoord;
                    bboxScript.yCenter = yCoord;
                }
        }
    }

    private byte[] RetreiveDepthFrame()
    {
 
        if (researchMode.LongDepthMapTextureUpdated())
        {
            byte[] frameTexture = researchMode.GetLongDepthMapTextureBuffer();
            if (frameTexture.Length > 0)
            {
                return frameTexture;
            }
            else
            {
            return null;
            }
        }

        return null;
    }

        private SanitizedFrames SanitizeFrame(Frame returnFrame, byte[] depthFrame)
        {

            /*
            Debug.Log("GazeOrigin: " + eyeGazeProvider.GazeOrigin);
            Debug.Log("GazeDirection: " + eyeGazeProvider.GazeDirection);
            */

            if (Physics.Raycast(origin, direction, out RaycastHit hit, 100))
            {
                GameObject targetGameObject = hit.transform.gameObject;
                
                if (targetGameObject.tag == "BoundingBox")
                {
                    // Debug.Log("Gaze collision with FaceCube name: " + targetGameObject.name);
                    
                    var targetFaceCubeScript = targetGameObject.GetComponent<BoundingBoxScriptManual>();
                    
                    if (targetFaceCubeScript.getIslooking() == false)
                    {
                        targetFaceCubeScript.setIslooking(true);
                        targetFaceCubeScript.EyeContactStarted();
                    }
                    else
                    {
                        targetFaceCubeScript.setIslooking(true);
                        targetFaceCubeScript.EyeContactMaintained();
                    }

                    // isLooking for all other facecube's should be false

                    foreach (GameObject face in faceCubesInScene)
                    {
                        if (face.name != targetGameObject.name)
                        {
                            var faceCubeScript = face.GetComponent<BoundingBoxScriptManual>();
                        
                            if (faceCubeScript.getIslooking() == true)
                            {
                                faceCubeScript.EyeContactLost();
                                faceCubeScript.setIslooking(false);
                            }   
                        }
                    }
                }
                else
                {
                    // Debug.Log("Eye Gaze not colliding with Face cube");
                    
                    foreach (GameObject face in faceCubesInScene)
                    {
                        var faceCubeScript = face.GetComponent<BoundingBoxScriptManual>();
                        
                        if (faceCubeScript.getIslooking() == true)
                        {
                            faceCubeScript.EyeContactLost();
                            faceCubeScript.setIslooking(false);
                        }   
                    }
                }
            }


            byte[] depthBytesWithoutBystanders = null;
            byte[] imageBytesWithoutBystanders = new byte[8 * returnFrame.bitmap.PixelWidth * returnFrame.bitmap.PixelHeight];

            System.Numerics.Matrix4x4 worldToCamera = (System.Numerics.Matrix4x4)worldSpatialCoordinateSystem.TryGetTransformTo(returnFrame.spatialCoordinateSystem);
            UnityEngine.Matrix4x4 unityWorldToCamera = NumericsConversionExtensions.ToUnity(worldToCamera);
            returnFrame.bitmap.CopyToBuffer(imageBytesWithoutBystanders.AsBuffer());
            tempImageTexture.LoadRawTextureData(imageBytesWithoutBystanders);

            if (depthFrame != null)
            {
                depthBytesWithoutBystanders = new byte[Buffer.ByteLength(depthFrame)];
                depthFrame.CopyTo(depthBytesWithoutBystanders, 0);
            }


            SanitizedFrames tempSanitizedFrames = new SanitizedFrames();

            //used to convert detection X,Y to depth frame X,Y
            float depthToImageWidthRatio = 320.0F / (float)returnFrame.bitmap.PixelWidth;
            float depthToImageHeightRatio = 288.0F / (float)returnFrame.bitmap.PixelHeight;

            //for each detection GameObject, we convert the current 3D position to to 2D position of the current depth frame
            foreach (GameObject face in faceCubesInScene)
            {
                var boundingBoxScript = face.GetComponent<BoundingBoxScriptManual>();
                Vector3 relativeNormalizedPos = (face.transform.position - Camera.main.transform.position).normalized;
                float dot = Vector3.Dot(relativeNormalizedPos, Camera.main.transform.forward);
                float angle = Mathf.Acos(dot);

                // if (boundingBoxScript.toObscure && angle < 0.62F)
                // {
                if (!boundingBoxScript.isSubject && angle < 0.62F)
                {
                    var lengthOfTime = DateTime.Now - lastCallToDrawPred;
                    // Debug.Log("Time gap between two drawPred calls: " + lengthOfTime);
                    lastCallToDrawPred = DateTime.Now;

                    // log drawPredTime to CSV
                    string writeDateTime = DateTime.Now.ToString("HH-mm-ss.ffff");
                    string DrawPredTimeFileLine = writeDateTime + "," + lengthOfTime.TotalSeconds.ToString();;

                    CSVLoggerScript.addDrawPredTimetoList(DrawPredTimeFileLine);

                    Vector3 cameraSpaceCoordinate = unityWorldToCamera.MultiplyPoint(face.transform.position);
                    Point projected2DPoint = returnFrame.cameraIntrinsics.ProjectOntoFrame(NumericsConversionExtensions.ToSystemWithoutConversion(cameraSpaceCoordinate));

                    var xCoordDepth = Mathf.Max((float)(projected2DPoint.X * depthToImageWidthRatio) - (float)((boundingBoxScript.bboxWidth * depthToImageWidthRatio) / 2.0F), 0);
                    var yCoordDepth = Mathf.Max(((float)(projected2DPoint.Y * depthToImageHeightRatio) - (float)((boundingBoxScript.bboxHeight * depthToImageHeightRatio) / 2.0F) - 30), 0);
                    xCoordDepth = Mathf.Clamp(xCoordDepth, 0f, 320f);
                    yCoordDepth = Mathf.Clamp(yCoordDepth, 0f, 288f);

                    var scaledDepthBoxWidth = Mathf.Min(xCoordDepth + (boundingBoxScript.bboxWidth * depthToImageWidthRatio)* 2, 320f);
                    var scaledDepthBoxHeight = Mathf.Min(yCoordDepth + (boundingBoxScript.bboxHeight * depthToImageHeightRatio) * 3, 288f);

                    float scaledImageHeight;
                    float scaledImageWidth;

                    var xCoordImage = Mathf.Max((float)projected2DPoint.X - (boundingBoxScript.bboxWidth / 2.0F), 0);
                    var yCoordImage = Mathf.Max((float)projected2DPoint.Y - (boundingBoxScript.bboxHeight / 2.0F), 0);
                    xCoordImage = Mathf.Clamp(xCoordImage, 0f, returnFrame.bitmap.PixelWidth-1);
                    yCoordImage = Mathf.Clamp(yCoordImage, 0f, returnFrame.bitmap.PixelHeight-1);
                    if ((xCoordImage + boundingBoxScript.bboxWidth) >= returnFrame.bitmap.PixelWidth)
                    {
                        scaledImageWidth = returnFrame.bitmap.PixelWidth - xCoordImage;
                    }
                    else
                    {
                        scaledImageWidth = boundingBoxScript.bboxWidth;
                    }

                    if ((yCoordImage + boundingBoxScript.bboxHeight) >= returnFrame.bitmap.PixelHeight)
                    {

                        scaledImageHeight = returnFrame.bitmap.PixelHeight - yCoordImage;
                    }
                    else
                    {
                        scaledImageHeight = boundingBoxScript.bboxHeight;
                    }

                    if (depthFrame != null)
                    {

                        for (uint rows = (uint)yCoordDepth; rows < scaledDepthBoxHeight; rows++)
                        {
                            for (uint columns = (uint)xCoordDepth; columns < scaledDepthBoxWidth; columns++)
                            {
                                depthBytesWithoutBystanders[(rows * 320) + columns] = depthBytesWithoutBystanders[(rows * 320) + (uint)xCoordDepth];
                            }
                        }

                    }

                    Color32[] colors = new Color32[(int)scaledImageWidth * (int)scaledImageHeight];
                    tempImageTexture.SetPixels32((int)xCoordImage, (int)yCoordImage, (int)scaledImageWidth, (int)scaledImageHeight, colors, 0);

                }
            }

            tempSanitizedFrames.sanitizedImageFrame = tempImageTexture;
            tempSanitizedFrames.sanitizedDepthFrame = depthBytesWithoutBystanders;
            return tempSanitizedFrames;
        }

#endif
        /*
        IEnumerator FramerateCountLoop()
        {
            while (true)
            {
                // yield return new WaitForSeconds(15);
                yield return new WaitForSeconds(frameRateFrequency);

                int FPS = (int)(1.0f / Time.smoothDeltaTime);
                // Debug.Log("Current Frame Rate: " + FPS);

                string finalJSONString = "{";
                //  create JSON like data structure that stores all info regarding all faceCubesInScene
                for (int i = 0; i < faceCubesInScene.Length; i++)
                {
                    var faceCubeScript = faceCubesInScene[i].GetComponent<BoundingBoxScriptManual>();

                    string faceCubeName = "\"" + faceCubesInScene[i].name + "\": {";

                    Vector3 faceCubeWorldPos = faceCubesInScene[i].transform.position;
                    string faceCubeWorldPosString = "\"3D Position\": \"<" + faceCubeWorldPos.x + " | " + faceCubeWorldPos.y + " | " + faceCubeWorldPos.z + ">\" |";

                    bool faceCubeIsSubject = faceCubeScript.getIsSubject();
                    string faceCubeIsSubjectString = "\"isSubject\": \"" + faceCubeIsSubject + "\" |";

                    double faceCube2DXCenter = faceCubeScript.xCenter;
                    double faceCube2DYCenter = faceCubeScript.yCenter;
                    string faceCube2DPosString = "\"2D Position\": \"(" + faceCube2DXCenter + " | " + faceCube2DYCenter + ")\" }";

                    string faceCubeJSONString = faceCubeName + faceCubeWorldPosString + faceCubeIsSubjectString + faceCube2DPosString + " |";

                    finalJSONString += faceCubeJSONString;
                }
                finalJSONString += '}';


                // log FPS to CSV
                string writeDateTime = DateTime.Now.ToString("HH-mm-ss.ffff");
                string FPSFileLine = writeDateTime + "," + FPS.ToString() + "," + faceCubesInScene.Length.ToString() + "," + frameCounter + "," + !qrDetectionStatus + "," + toggleButtonState + "," + finalJSONString;

                CSVLoggerScript.addFPStoList(FPSFileLine);

                
                Debug.Log("currDistance: " + currDistance);
                Debug.Log("currDirection: " + currDirection);
                
            }
        }
        */


#if ENABLE_WINMD_SUPPORT
    private void OnAudioFilterRead(float[] buffer, int numChannels)
    {

        // Read the microphone stream data.
        var returnFloat = _mediaCaptureUtility.GetLatestAudioFrame(buffer, numChannels);

        float sumOfValues = 0;

        // Calculate this frame's average amplitude.
        /*for (int i = 0; i < buffer.Length; i++)
        {
            if (float.IsNaN(buffer[i]))
            {
                buffer[i] = 0;
            }

            buffer[i] = Mathf.Clamp(buffer[i], -1.0f, 1.0f);
            sumOfValues += Mathf.Clamp01(Mathf.Abs(buffer[i]));
        }*/

        //averageAmplitude = sumOfValues / buffer.Length;
        averageAmplitude = Mathf.Abs(Mathf.Clamp(returnFloat, -1.0f, 1.0f));
    }
#endif
    }
}

 