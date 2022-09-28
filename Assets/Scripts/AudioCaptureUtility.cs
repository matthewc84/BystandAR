using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

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


#endif

public class AudioCaptureUtility : MonoBehaviour
{

#if ENABLE_WINMD_SUPPORT
    MediaCapture mediaCapture;
    MediaFrameReader mediaFrameReader;
#endif

    // Start is called before the first frame update
    async void Start()
    {
#if ENABLE_WINMD_SUPPORT
        mediaCapture = new MediaCapture();
        MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings()
        {
            StreamingCaptureMode = StreamingCaptureMode.Audio,
        };
        await mediaCapture.InitializeAsync(settings);

        var audioFrameSources = mediaCapture.FrameSources.Where(x => x.Value.Info.MediaStreamType == MediaStreamType.Audio);

        if (audioFrameSources.Count() == 0)
        {
            Debug.Log("No audio frame source was found.");
            return;
        }

        MediaFrameSource frameSource = audioFrameSources.FirstOrDefault().Value;

        MediaFrameFormat format = frameSource.CurrentFormat;
        if (format.Subtype != MediaEncodingSubtypes.Float)
        {
            return;
        }

        //if (format.AudioEncodingProperties.ChannelCount != 1
        //    || format.AudioEncodingProperties.SampleRate != 48000)
        //{
        //    return;
        //}

        mediaFrameReader = await mediaCapture.CreateFrameReaderAsync(frameSource);


        Debug.Log("Created audio FrameReader");

        // Optionally set acquisition mode. Buffered is the default mode for audio.
        mediaFrameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;

        mediaFrameReader.FrameArrived += MediaFrameReader_AudioFrameArrived;

        var status = await mediaFrameReader.StartAsync();

        Debug.Log("Audio FrameReader Started");

        if (status != MediaFrameReaderStartStatus.Success)
        {
            Debug.Log("The MediaFrameReader couldn't start.");
        }

#endif
    }

    // Update is called once per frame
    void Update()
    {
#if ENABLE_WINMD_SUPPORT
        using (MediaFrameReference reference = mediaFrameReader.TryAcquireLatestFrame())
        {
            if (reference != null)
            {
                ProcessAudioFrame(reference.AudioMediaFrame);
            }
        }
#endif
    }
#if ENABLE_WINMD_SUPPORT
    private void MediaFrameReader_AudioFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        using (MediaFrameReference reference = sender.TryAcquireLatestFrame())
        {
            if (reference != null)
            {
                ProcessAudioFrame(reference.AudioMediaFrame);
            }
        }
    }

    unsafe private void ProcessAudioFrame(AudioMediaFrame audioMediaFrame)
    {

        using (AudioFrame audioFrame = audioMediaFrame.GetAudioFrame())
        using (AudioBuffer buffer = audioFrame.LockBuffer(AudioBufferAccessMode.Read))
        using (IMemoryBufferReference reference = buffer.CreateReference())
        {
            byte* dataInBytes;
            uint capacityInBytes;
            float* dataInFloat;


            ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);

            // The requested format was float
            dataInFloat = (float*)dataInBytes;

            Debug.Log(*dataInFloat);

            // Get the number of samples by multiplying the duration by sampling rate: 
            // duration [s] x sampling rate [samples/s] = # samples 

            // Duration can be gotten off the frame reference OR the audioFrame
            TimeSpan duration = audioMediaFrame.FrameReference.Duration;

            // frameDurMs is in milliseconds, while SampleRate is given per second.
            uint frameDurMs = (uint)duration.TotalMilliseconds;
            uint sampleRate = audioMediaFrame.AudioEncodingProperties.SampleRate;
            uint sampleCount = (frameDurMs * sampleRate) / 1000;

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
