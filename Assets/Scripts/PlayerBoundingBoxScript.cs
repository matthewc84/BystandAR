using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System;
using System.Diagnostics;

namespace BystandAR
{
    public class PlayerBoundingBoxScript : MonoBehaviour
    {

        public float bboxWidth = 50;
        public float bboxHeight = 50;
        public bool toObscure = true;
        private FrameSanitizer frameSanitizer;
        
        bool firstTimeEyeGazeContact = true;
        Stopwatch eyeGazeStopwatch;
        public Stopwatch detectionStopwatch;
        long voiceAndEyeGazeCounter;
        long totalEyeGazeTime;
        long totalVoiceAndEyeGazeTime;
        long eyeGazeCounter;
        float percentEyeAndVoiceContact;
        float percentEyeContact;


        void Start()
        {

            eyeGazeStopwatch = new Stopwatch();
            detectionStopwatch = new Stopwatch();
            voiceAndEyeGazeCounter = 0;
            totalEyeGazeTime = 0;
            totalVoiceAndEyeGazeTime = 0;
            eyeGazeCounter = 0;
            percentEyeAndVoiceContact = 0;
            percentEyeContact = 0;
            detectionStopwatch.Start();
            eyeGazeStopwatch.Start();
            frameSanitizer = GameObject.Find("FrameSanitizer").GetComponent<FrameSanitizer>();
            

        }


        public void RemoveDetection()
        {

            Destroy(this.gameObject);
        }

        void Update()
        {

            //If object has existed for more than the given threshold without update, we treat it as stale and remove


            if(detectionStopwatch.ElapsedMilliseconds > 0)
            {
                percentEyeAndVoiceContact = (float)(totalVoiceAndEyeGazeTime + voiceAndEyeGazeCounter) / (float)detectionStopwatch.ElapsedMilliseconds;
                percentEyeContact = (float)(totalEyeGazeTime + eyeGazeCounter) / (float)detectionStopwatch.ElapsedMilliseconds;
                //UnityEngine.Debug.Log(percentEyeContact.ToString("F6"));
            }

            if (percentEyeAndVoiceContact > 0.30f || percentEyeContact > 0.50f)
            {
                toObscure = false;
            }
            else
            {
                toObscure = true;
            }
        }

        public void EyeContactStarted()
        {
            eyeGazeStopwatch.Restart();
            //UnityEngine.Debug.Log("Eye Gaze Started");

        }

        public void EyeContactMaintained()
        {

            if (frameSanitizer.userSpeaking)
            {
                //voiceAndEyeGazeCounter += 1;
                voiceAndEyeGazeCounter = eyeGazeStopwatch.ElapsedMilliseconds;
                eyeGazeCounter = eyeGazeStopwatch.ElapsedMilliseconds;
                //UnityEngine.Debug.Log("Eye Gaze Continues");

            }
            else
            {
                //eyegazeCounter += 1;
                eyeGazeCounter = eyeGazeStopwatch.ElapsedMilliseconds;
                //UnityEngine.Debug.Log(eyeGazeCounter);

            }


        }

        public void EyeContactLost()
        {
            eyeGazeStopwatch.Stop();
            voiceAndEyeGazeCounter = eyeGazeStopwatch.ElapsedMilliseconds;
            eyeGazeCounter = eyeGazeStopwatch.ElapsedMilliseconds;
            totalVoiceAndEyeGazeTime += voiceAndEyeGazeCounter;
            totalEyeGazeTime += eyeGazeCounter;
            eyeGazeCounter = 0;
            voiceAndEyeGazeCounter = 0;
            //UnityEngine.Debug.Log("Eye Gaze Stops");
        }


    }
}