// Script taken directly from Rene Schulte's repo: https://github.com/reneschulte/WinMLExperiments/blob/master/HoloVision20/Assets/Scripts/MediaCapturer.cs

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using System.IO;

#if ENABLE_WINMD_SUPPORT
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Devices.Core;
using Windows.Foundation;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.Media.Devices;
using Windows.Graphics.Imaging;
using Windows.Devices.Enumeration;
using Windows.System;
using Windows.Perception.Spatial;


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
    private MediaFrameReader _audioMediaFrameReader;
    private Frame _videoFrame = null;
#endif

    public async Task InitializeMediaFrameReaderAsync()
    {
#if ENABLE_WINMD_SUPPORT
        // Check state of media capture object 
        if (_mediaCapture == null || _mediaCapture.CameraStreamState == CameraStreamState.Shutdown || _mediaCapture.CameraStreamState == CameraStreamState.NotStreaming)
        {
            if (_mediaCapture != null)
            {
                _mediaCapture.Dispose();
            }

            // Find right camera settings and prefer back camera
            MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
            MediaCaptureInitializationSettings audiosettings = new MediaCaptureInitializationSettings();

            var allCameras = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            var selectedCamera = allCameras.FirstOrDefault(c => c.EnclosureLocation?.Panel == Panel.Back) ?? allCameras.FirstOrDefault();


            if (selectedCamera != null)
            {
                settings.VideoDeviceId = selectedCamera.Id;
                settings.StreamingCaptureMode = StreamingCaptureMode.AudioAndVideo;

            }

            // Init capturer and Frame reader
            _mediaCapture = new MediaCapture();
            Debug.Log("InitializeMediaFrameReaderAsync: Successfully created media capture object.");
            await _mediaCapture.InitializeAsync(settings);

            Debug.Log("InitializeMediaFrameReaderAsync: Successfully initialized media capture object.");

            var imageFrameSourcePair = _mediaCapture.FrameSources.Where(source => source.Value.Info.SourceKind == MediaFrameSourceKind.Color).First();
            var audioFrameSources = _mediaCapture.FrameSources.Where(x => x.Value.Info.MediaStreamType == MediaStreamType.Audio);

            if (audioFrameSources.Count() == 0)
            {
                Debug.Log("No audio frame source was found.");
            }

            MediaFrameSource frameSource = audioFrameSources.FirstOrDefault().Value;
            MediaFrameFormat format = frameSource.CurrentFormat;
            if (format.Subtype != MediaEncodingSubtypes.Float)
            {
                Debug.Log("Incorrect audio media subtype.");
            }

            // Convert the pixel formats
            //var subtype = MediaEncodingSubtypes.Bgra8;
            var subtype = MediaEncodingSubtypes.Rgb32;

            // The overloads of CreateFrameReaderAsync with the format arguments will actually make a copy in FrameArrived
            BitmapSize outputSize = new BitmapSize { Width = 1280, Height = 720};

            _imageMediaFrameReader = await _mediaCapture.CreateFrameReaderAsync(imageFrameSourcePair.Value, subtype, outputSize);
            _audioMediaFrameReader = await _mediaCapture.CreateFrameReaderAsync(frameSource);

            Debug.Log("InitializeMediaFrameReaderAsync: Successfully created media frame reader.");

            _imageMediaFrameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;
            _audioMediaFrameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;

            await _imageMediaFrameReader.StartAsync();
            var status = await _audioMediaFrameReader.StartAsync();

            Debug.Log("InitializeMediaFrameReaderAsync: Successfully started media frame reader.");

            IsCapturing = true;
        }
#endif
    }

#if ENABLE_WINMD_SUPPORT

    /// <summary>
    /// Retrieve the latest video frame from the media frame reader
    /// </summary>
    /// <returns>VideoFrame object with current frame's software bitmap</returns>
    public async Task<Frame> GetLatestVideoFrame()
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

    /// <summary>
    /// Retrieve the latest video frame from the media frame reader
    /// </summary>
    /// <returns>VideoFrame object with current frame's software bitmap</returns>
    public float GetLatestAudioFrame(float[] buffer, int numChannels)
    {
        try
        {
            using (MediaFrameReference reference = _audioMediaFrameReader.TryAcquireLatestFrame())
            {
                if (reference != null)
                {
                   var returnFloat = ProcessAudioFrame(reference.AudioMediaFrame, buffer, buffer.Length, numChannels);
                   return returnFloat;
                }
                else
                {
                    return 0.0f;
                }
            }
        }
        catch (Exception ex)
        {
            return 0.0f;
        }
    }

#endif

    /// <summary>
    /// Asynchronously stop media capture and dispose of resources
    /// </summary>
    /// <returns></returns>
    public async Task OnDestroy()
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

#if ENABLE_WINMD_SUPPORT
    unsafe private float ProcessAudioFrame(AudioMediaFrame audioMediaFrame, float[] buffer, int length, int numChannels)
    {
        int indexInFrame = 0;
        using (AudioFrame audioFrame = audioMediaFrame.GetAudioFrame())
        using (AudioBuffer audioBuffer = audioFrame.LockBuffer(AudioBufferAccessMode.Read))
        using (IMemoryBufferReference reference = audioBuffer.CreateReference())
        {
            byte* dataInBytes;
            uint capacityInBytes;
            float* dataInFloat;


            ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);

            // The requested format was float
            dataInFloat = (float*)dataInBytes;

            //Debug.Log(*dataInFloat);

            // Get the number of samples by multiplying the duration by sampling rate: 
            // duration [s] x sampling rate [samples/s] = # samples 

            // Duration can be gotten off the frame reference OR the audioFrame
            TimeSpan duration = audioMediaFrame.FrameReference.Duration;

            // frameDurMs is in milliseconds, while SampleRate is given per second.
            uint frameDurMs = (uint)duration.TotalMilliseconds;
            uint sampleRate = audioMediaFrame.AudioEncodingProperties.SampleRate;
            uint sampleCount = (frameDurMs * sampleRate) / 1000;

            int framesize = (int)sampleCount * numChannels;

            /*for (int i = 0; i < length; i++)
            {

                if (capacityInBytes > 0)
                {
                    buffer[i] = dataInFloat[indexInFrame];
                    ++indexInFrame; 
                }
            }*/

            return *dataInFloat;

        }

    }

    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }
#endif

}