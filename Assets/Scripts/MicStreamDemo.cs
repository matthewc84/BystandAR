// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace HoloToolkit.Unity.InputModule.Tests
{
    [RequireComponent(typeof(AudioSource))]
    public class MicStreamDemo : MonoBehaviour
    {
        /// <summary>
        /// Which type of microphone/quality to access.
        /// </summary>
        public MicStream.StreamCategory StreamType = MicStream.StreamCategory.HIGH_QUALITY_VOICE;

        /// <summary>
        /// Can boost volume here as desired. 1 is default.
        /// <remarks>Can be updated at runtime.</remarks> 
        /// </summary>
        public float InputGain = 1;

        /// <summary>
        /// if keepAllData==false, you'll always get the newest data no matter how long the program hangs for any reason,
        /// but will lose some data if the program does hang.
        /// <remarks>Can only be set on initialization.</remarks>
        /// </summary>
        public bool KeepAllData;

        /// <summary>
        /// If true, Starts the mic stream automatically when this component is enabled.
        /// </summary>
        public bool AutomaticallyStartStream = true;

        /// <summary>
        /// Plays back the microphone audio source though default audio device.
        /// </summary>
        public bool PlaybackMicrophoneAudioSource = true;

        /// <summary>
        /// Records estimation of volume from the microphone to affect other elements of the game object.
        /// </summary>
        private float averageAmplitude;
        private float[] tempAudioBuffer;

        private bool isRunning;

        public bool IsRunning
        {
            get { return isRunning; }
            private set
            {
                isRunning = value;
                CheckForErrorOnCall(isRunning ? MicStream.MicPause() : MicStream.MicResume());
            }
        }

        #region Unity Methods

        private void OnAudioFilterRead(float[] buffer, int numChannels)
        {
            // this is where we call into the DLL and let it fill our audio buffer for us
            CheckForErrorOnCall(MicStream.MicGetFrame(buffer, buffer.Length, numChannels));

            float sumOfValues = 0;

            // figure out the average amplitude from this new data
            for (int i = 0; i < buffer.Length; i++)
            {
                if (float.IsNaN(buffer[i]))
                {
                    buffer[i] = 0;
                }

                buffer[i] = Mathf.Clamp(buffer[i], -1f, 1f);
                sumOfValues += Mathf.Clamp01(Mathf.Abs(buffer[i]));
            }
            averageAmplitude = sumOfValues / buffer.Length;

            
        }

        private void OnEnable()
        {
            IsRunning = true;
        }

        private void Awake()
        {
            CheckForErrorOnCall(MicStream.MicInitializeCustomRate((int)StreamType, AudioSettings.outputSampleRate));
            CheckForErrorOnCall(MicStream.MicSetGain(InputGain));

            if (!PlaybackMicrophoneAudioSource)
            {
                gameObject.GetComponent<AudioSource>().volume = 0; // can set to zero to mute mic monitoring
            }

            if (AutomaticallyStartStream)
            {
                CheckForErrorOnCall(MicStream.MicStartStream(KeepAllData, false));
            }

            tempAudioBuffer = new float[5];
            isRunning = true;
        }

        private void Update()
        {
            CheckForErrorOnCall(MicStream.MicSetGain(InputGain));

            Debug.Log(averageAmplitude.ToString("F4"));


        }

        private void OnApplicationPause(bool pause)
        {
            IsRunning = !pause;
        }

        private void OnDisable()
        {
            IsRunning = false;
        }

        private void OnDestroy()
        {
            CheckForErrorOnCall(MicStream.MicDestroy());
        }

#if !UNITY_EDITOR
        private void OnApplicationFocus(bool focused)
        {
            IsRunning = focused;
        }
#endif
        #endregion

        private static void CheckForErrorOnCall(int returnCode)
        {
            MicStream.CheckForErrorOnCall(returnCode);
        }

        public void Enable()
        {
            IsRunning = true;
        }

        public void Disable()
        {
            IsRunning = false;
        }
    }
}
