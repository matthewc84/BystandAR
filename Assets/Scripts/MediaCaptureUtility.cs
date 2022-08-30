﻿// Script taken directly from Rene Schulte's repo: https://github.com/reneschulte/WinMLExperiments/blob/master/HoloVision20/Assets/Scripts/MediaCapturer.cs

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

#if ENABLE_WINMD_SUPPORT
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Devices.Core;
//using Windows.Storage.Streams;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.Media.Devices;
using Windows.Graphics.Imaging;
using Windows.Devices.Enumeration;
//using Windows.Storage;
using Windows.System;
using Windows.Perception.Spatial;
//using System.Threading.Tasks;


public class Frame : IDisposable
{
    public SpatialCoordinateSystem spatialCoordinateSystem;
    public CameraIntrinsics cameraIntrinsics;
    public SoftwareBitmap bitmap;

    public void Dispose()
    {
        bitmap.Dispose();
        GC.SuppressFinalize(this);
    }

}
#endif

public class MediaCaptureUtility
{
    public bool IsCapturing { get; set; }

#if ENABLE_WINMD_SUPPORT
    private MediaCapture _mediaCapture;
    private MediaFrameReader _imageMediaFrameReader;
    private Frame _videoFrame = null;


    /// <summary>
    /// Method to start capturing camera frames at desired resolution.
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public async Task InitializeMediaFrameReaderAsync()
    {
        // Check state of media capture object 
        if (_mediaCapture == null || _mediaCapture.CameraStreamState == CameraStreamState.Shutdown || _mediaCapture.CameraStreamState == CameraStreamState.NotStreaming)
        {
            if (_mediaCapture != null)
            {
                _mediaCapture.Dispose();
            }

            // Find right camera settings and prefer back camera
            MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
            var allCameras = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            var selectedCamera = allCameras.FirstOrDefault(c => c.EnclosureLocation?.Panel == Panel.Back) ?? allCameras.FirstOrDefault();

            if (selectedCamera != null)
            {
                settings.VideoDeviceId = selectedCamera.Id;
                //settings.MemoryPreference = MediaCaptureMemoryPreference.Auto;
                //Debug.Log($"InitializeMediaFrameReaderAsync: settings.VideoDeviceId: {settings.VideoDeviceId}");

            }

            // Init capturer and Frame reader
            _mediaCapture = new MediaCapture();
            Debug.Log("InitializeMediaFrameReaderAsync: Successfully created media capture object.");

            await _mediaCapture.InitializeAsync(settings);
            Debug.Log("InitializeMediaFrameReaderAsync: Successfully initialized media capture object.");

            var imageFrameSourcePair = _mediaCapture.FrameSources.Where(source => source.Value.Info.SourceKind == MediaFrameSourceKind.Color).First();

            // Convert the pixel formats
            //var subtype = MediaEncodingSubtypes.Bgra8;
            var subtype = MediaEncodingSubtypes.Rgb32;

            // The overloads of CreateFrameReaderAsync with the format arguments will actually make a copy in FrameArrived
            BitmapSize outputSize = new BitmapSize { Width = 1280, Height = 720};
            _imageMediaFrameReader = await _mediaCapture.CreateFrameReaderAsync(imageFrameSourcePair.Value, subtype, outputSize);
            Debug.Log("InitializeMediaFrameReaderAsync: Successfully created media frame reader.");
            _imageMediaFrameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;

            await _imageMediaFrameReader.StartAsync();
            Debug.Log("InitializeMediaFrameReaderAsync: Successfully started media frame reader.");

            IsCapturing = true;
        }
    }

    /// <summary>
    /// Retrieve the latest video frame from the media frame reader
    /// </summary>
    /// <returns>VideoFrame object with current frame's software bitmap</returns>
    public async Task<Frame> GetLatestFrame()
    {
        SoftwareBitmap bitmap;
        try{
            // The overloads of CreateFrameReaderAsync with the format arguments will actually return a copy so we don't have to copy again
            var imageMediaFrameReference = _imageMediaFrameReader.TryAcquireLatestFrame();
            VideoFrame videoFrame = imageMediaFrameReference?.VideoMediaFrame?.GetVideoFrame();
            var spatialCoordinateSystem = imageMediaFrameReference?.CoordinateSystem;
            var cameraIntrinsics = imageMediaFrameReference?.VideoMediaFrame?.CameraIntrinsics;

             // Sometimes on HL RS4 the D3D surface returned is null, so simply skip those frames
            if (videoFrame == null || cameraIntrinsics == null || spatialCoordinateSystem == null || (videoFrame.Direct3DSurface == null && videoFrame.SoftwareBitmap == null))
            {
                //UnityEngine.Debug.Log("Frame thrown out");
                return _videoFrame;
            }


            if (videoFrame.Direct3DSurface != null && videoFrame.SoftwareBitmap == null)
            {
                bitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(videoFrame.Direct3DSurface);
            }
            else
            {
                bitmap = SoftwareBitmap.Copy(videoFrame.SoftwareBitmap);
            }

            videoFrame.Dispose();
            imageMediaFrameReference.Dispose();

            Frame returnFrame = new Frame{
                spatialCoordinateSystem = spatialCoordinateSystem,
                cameraIntrinsics = cameraIntrinsics,
                bitmap = bitmap
                };

            return returnFrame;
        }
        catch (Exception ex){
        Debug.Log("Caught exception grabbing frame");
        Debug.Log(ex.Message);
        return _videoFrame;
        }
    }

#endif

    /// <summary>
    /// Asynchronously stop media capture and dispose of resources
    /// </summary>
    /// <returns></returns>
    public async Task StopMediaFrameReaderAsync()
    {
#if ENABLE_WINMD_SUPPORT
        if (_mediaCapture != null && _mediaCapture.CameraStreamState != CameraStreamState.Shutdown)
        {
            await _imageMediaFrameReader.StopAsync();
            _imageMediaFrameReader.Dispose();
            _mediaCapture.Dispose();
            _mediaCapture = null;
        }
        IsCapturing = false;
#endif
    }

}