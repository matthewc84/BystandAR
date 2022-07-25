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



public struct DetectedFaces
{
    public SoftwareBitmap originalImageBitmap { get; set; }
    public int FrameWidth { get; set; }
    public int FrameHeight { get; set; }
    public Rect[] Faces { get; set; }
}

#endif

public struct Rect
{
    public uint X { get; set; }
    public uint Y { get; set; }
    public uint Width { get; set; }
    public uint Height { get; set; }
}

public class NetworkModel
{

#if ENABLE_WINMD_SUPPORT
    private MediaCapture _media_capture;
    FaceDetector detector;
    IList<DetectedFace> detectedFaces;

#endif



#if ENABLE_WINMD_SUPPORT

    public async Task<DetectedFaces> EvaluateVideoFrameAsync(VideoFrame inputFrame)
    {
        DetectedFaces result = new DetectedFaces();
        // Sometimes on HL RS4 the D3D surface returned is null, so simply skip those frames
        if (inputFrame == null || (inputFrame.Direct3DSurface == null && inputFrame.SoftwareBitmap == null))
        {
            UnityEngine.Debug.Log("Frame thrown out");
            return result;
        }
        
        try{

            // Perform network model inference using the input data tensor, cache output and time operation
            result = await EvaluateFrame(inputFrame);


        return result;
        }

         catch (Exception ex)
        {
            throw;
            return result;
        }

    }

   private async Task<DetectedFaces> EvaluateFrame(VideoFrame frame)
   {
            SoftwareBitmap bitmap;
			if (detector == null)
            {
                detector = await FaceDetector.CreateAsync();
            }
            if (frame.Direct3DSurface != null && frame.SoftwareBitmap == null)
            {
                bitmap  = await SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Direct3DSurface);
            }
            else{
                bitmap = frame.SoftwareBitmap;
            }

			const BitmapPixelFormat faceDetectionPixelFormat = BitmapPixelFormat.Nv12;
            SoftwareBitmap convertedBitmap;
            if (bitmap.BitmapPixelFormat != faceDetectionPixelFormat)
            {
                convertedBitmap = SoftwareBitmap.Convert(bitmap, faceDetectionPixelFormat);
            }
            else
            {
                convertedBitmap = bitmap;
            }
			detectedFaces = await detector.DetectFacesAsync(convertedBitmap);
       
            return new DetectedFaces
			{
                originalImageBitmap = bitmap,
			    FrameWidth = convertedBitmap.PixelWidth,
			    FrameHeight = convertedBitmap.PixelHeight,
			    Faces = detectedFaces.Select(f => 
			        new Rect {X = f.FaceBox.X, Y = f.FaceBox.Y, Width = f.FaceBox.Width, Height = f.FaceBox.Height}).ToArray()
			};

   }


#endif

}