// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;

//namespace Microsoft.MixedReality.Toolkit.Examples
//{
namespace Microsoft.MixedReality.Toolkit.Audio
{
    /// <summary>
    /// Demonstration class using WindowsMicrophoneStream (from com.microsoft.mixedreality.toolkit.micstream) to select the 
    /// voice microphone and adjust the spatial awareness mesh based on the amplitude of the user's voice.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MicrophoneAmplitudeDemo : MonoBehaviour
    {
//#if MICSTREAM_PRESENT

        [SerializeField]
        [Tooltip("Gain to apply to the microphone input.")]
        [Range(0, 10)]
        private float inputGain = 1.0f;

        [SerializeField]
        [Tooltip("Factor by which to boost the microphone amplitude when changing the mesh display.")]
        [Range(0, 50)]
        private int amplitudeBoostFactor = 10;


        /// <summary>
        /// Class providing microphone stream management support on Microsoft Windows based devices.
        /// </summary>
        private WindowsMicrophoneStream micStream = null;

        /// <summary>
        /// The average amplitude of the sound captured during the most recent microphone update.
        /// </summary>
        private float averageAmplitude = 0.0f;


        private void Awake()
        {
            // We do not wish to play the ambient room sound from the audio source.
            gameObject.GetComponent<AudioSource>().volume = 0.0f;

            micStream = new WindowsMicrophoneStream();
            if (micStream == null)
            {
                Debug.Log("Failed to create the Windows Microphone Stream object");
            }

            micStream.Gain = inputGain;

            // Initialize the microphone stream.
            WindowsMicrophoneStreamErrorCode result = micStream.Initialize(WindowsMicrophoneStreamType.LowQualityVoice);
            Debug.Log("Init result: " + result);
            if (result != WindowsMicrophoneStreamErrorCode.Success && result != WindowsMicrophoneStreamErrorCode.AlreadyRunning)
            {
                Debug.Log($"Failed to initialize the microphone stream. {result}");
                return;
            }

            // Start the microphone stream.
            // Do not keep the data and do not preview.
            result = micStream.StartStream(false, false);
            Debug.Log("StartStream result: " + result);
            if (result != WindowsMicrophoneStreamErrorCode.Success)
            {
                Debug.Log($"Failed to start the microphone stream. {result}");
            }
        }

        private void OnDestroy()
        {
            if (micStream == null) { return; }

            // Stop the microphone stream.
            WindowsMicrophoneStreamErrorCode result = micStream.StopStream();
            if (result != WindowsMicrophoneStreamErrorCode.Success)
            {
                Debug.Log($"Failed to stop the microphone stream. {result}");
            }

            // Uninitialize the microphone stream.
            micStream.Uninitialize();
            micStream = null;

        }

        private void OnDisable()
        {
            if (micStream == null) { return; }

            // Pause the microphone stream.
            WindowsMicrophoneStreamErrorCode result = micStream.Pause();
            if (result != WindowsMicrophoneStreamErrorCode.Success)
            {
                Debug.Log($"Failed to pause the microphone stream. {result}");
            }
        }

        private void OnEnable()
        {
            if (micStream == null) { return; }

            // Resume the microphone stream.
            WindowsMicrophoneStreamErrorCode result = micStream.Resume();
            if (result != WindowsMicrophoneStreamErrorCode.Success)
            {
                Debug.Log($"Failed to resume the microphone stream. {result}");
            }
        }


        private void Update()
        {

        }

        private void OnAudioFilterRead(float[] buffer, int numChannels)
        {
            if (micStream == null) { return; }

            // Read the microphone stream data.
            WindowsMicrophoneStreamErrorCode result = micStream.ReadAudioFrame(buffer, numChannels);
            if (result != WindowsMicrophoneStreamErrorCode.Success)
            {
                Debug.Log($"Failed to read the microphone stream data. {result}");
            }

            float sumOfValues = 0;

            // Calculate this frame's average amplitude.
            for (int i = 0; i < buffer.Length; i++)
            {
                if (float.IsNaN(buffer[i]))
                {
                    buffer[i] = 0;
                }

                buffer[i] = Mathf.Clamp(buffer[i], -1.0f, 1.0f);
                sumOfValues += Mathf.Clamp01(Mathf.Abs(buffer[i]));
            }

            averageAmplitude = sumOfValues / buffer.Length;
        }

//#endif // MICSTREAM_PRESENT
    }
}
