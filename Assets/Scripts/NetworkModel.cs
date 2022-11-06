// Adapted from the WinML MNIST sample and Rene Schulte's repo 
// https://github.com/microsoft/Windows-Machine-Learning/tree/master/Samples/MNIST
// https://github.com/reneschulte/WinMLExperiments/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;

#if ENABLE_WINMD_SUPPORT
using Windows.AI.MachineLearning;
using Windows.Storage.Streams;
using Windows.Media;
using Windows.Storage;
using Windows.Media.Capture;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Foundation;
using Windows.Media.FaceAnalysis;

#endif



namespace BystandAR
{

    public class DetectedFaces
    {
        public Rect[] Faces { get; set; }
    }

    public class Rect
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
    }

    public class NetworkModel : MonoBehaviour
    {
        int detectorInstance = 1;
#if ENABLE_WINMD_SUPPORT
    FaceDetector detector1;
    FaceDetector detector2;
#endif

#if ENABLE_WINMD_SUPPORT
    public async Task InitFaceDetector()
    {
        DetectedFaces result = new DetectedFaces();
      
        try{

            if (detector1 == null)
            {
                detector1 = await FaceDetector.CreateAsync();
                //detector.MinDetectableFaceSize = new BitmapSize(){Height = 20, Width = 20};
            }
            if (detector2 == null)
            {
                detector2 = await FaceDetector.CreateAsync();
                //detector.MinDetectableFaceSize = new BitmapSize(){Height = 20, Width = 20};
            }
        }

         catch (Exception ex)
        {
            throw;
        }

    }

    public async Task<DetectedFaces> EvaluateVideoFrameAsync(SoftwareBitmap bitmap)
    {
        DetectedFaces result = new DetectedFaces();
      
        try{

            // Perform network model inference using the input data tensor, cache output and time operation
            result = await EvaluateFrame(bitmap);

            return result;
        }

         catch (Exception ex)
        {
            throw;
            return result;
        }

    }

    private async Task<DetectedFaces> EvaluateFrame(SoftwareBitmap bitmap)
    {
            IList<DetectedFace> detectedFaces = null;

            var allFormats = FaceDetector.GetSupportedBitmapPixelFormats();
			BitmapPixelFormat faceDetectionPixelFormat = allFormats.FirstOrDefault();
            SoftwareBitmap convertedBitmap;
            if (bitmap.BitmapPixelFormat != faceDetectionPixelFormat)
            {
                convertedBitmap = SoftwareBitmap.Convert(bitmap, faceDetectionPixelFormat);
            }
            else
            {
                convertedBitmap = bitmap;
            }

            //var stopwatch = Stopwatch.StartNew();
            try
            {
                if(detectorInstance == 1)
                {
                    detectedFaces = await detector1.DetectFacesAsync(convertedBitmap);
                    detectorInstance = 2;
                }
                if (detectorInstance == 2)
                {
                    detectedFaces = await detector2.DetectFacesAsync(convertedBitmap);
                    detectorInstance = 1;
                }

                return new DetectedFaces
			    {
			    Faces = detectedFaces.Select(f => 
			        new Rect {X = f.FaceBox.X, Y = f.FaceBox.Y, Width = f.FaceBox.Width, Height = f.FaceBox.Height}).ToArray()
			    };
            }
            catch (Exception ex){
                return null;
            }
            //stopwatch.Stop();

            //UnityEngine.Debug.Log($"Elapsed time for inference (in ms): {stopwatch.ElapsedMilliseconds.ToString("F4")}");



    }


#endif

    }
}