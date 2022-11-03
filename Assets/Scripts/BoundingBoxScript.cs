using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System;
using System.Diagnostics;

namespace BystandAR
{
    public class BoundingBoxScript : MonoBehaviour
    {

        public float bboxWidth = 0;
        public float bboxHeight = 0;
        public bool toObscure = true;
        public bool isSubject = false;
        private FrameSanitizer frameSanitizer;
        public int staleCounter;
        
        Stopwatch eyeGazeStopwatch;
        Stopwatch staleSubjectStopwatch;
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
            staleSubjectStopwatch = new Stopwatch();
            staleCounter = 0;
            voiceAndEyeGazeCounter = 0;
            totalEyeGazeTime = 0;
            totalVoiceAndEyeGazeTime = 0;
            eyeGazeCounter = 0;
            percentEyeAndVoiceContact = 0;
            percentEyeContact = 0;
            detectionStopwatch.Start();
            frameSanitizer = GameObject.Find("FrameSanitizer").GetComponent<FrameSanitizer>();
            

        }


        public void RemoveDetection()
        {

            Destroy(this.gameObject);
        }

        void Update()
        {
            staleCounter += 1;

            //If object has existed for more than the given threshold without update, we treat it as stale and remove
            if (staleCounter > 90)
            {
                RemoveDetection();
            }

            if(detectionStopwatch.ElapsedMilliseconds > 0)
            {
                percentEyeAndVoiceContact = (float)(totalVoiceAndEyeGazeTime + voiceAndEyeGazeCounter) / (float)detectionStopwatch.ElapsedMilliseconds;
                percentEyeContact = (float)(totalEyeGazeTime + eyeGazeCounter) / (float)detectionStopwatch.ElapsedMilliseconds;
                //UnityEngine.Debug.Log(percentEyeContact.ToString("F6"));
            }

            if (percentEyeAndVoiceContact > 0.30f || percentEyeContact > 0.50f)
            {
                isSubject = true;
            }

            if(staleSubjectStopwatch.ElapsedMilliseconds > 10000)
            {
                isSubject = false;
            }

        }

        public void EyeContactStarted()
        {
            eyeGazeStopwatch.Restart();
            staleSubjectStopwatch.Reset();

            if (isSubject)
            {
                toObscure = false;
            }

        }

        public void EyeContactMaintained()
        {
            if (isSubject)
            {
                toObscure = false;
            }

            if (!eyeGazeStopwatch.IsRunning)
            {
                eyeGazeStopwatch.Restart();
            }

            if (frameSanitizer.userSpeaking)
            {
                voiceAndEyeGazeCounter = eyeGazeStopwatch.ElapsedMilliseconds;
                eyeGazeCounter = eyeGazeStopwatch.ElapsedMilliseconds;
                //UnityEngine.Debug.Log("Eye Gaze Continues");

            }
            else
            {
                eyeGazeCounter = eyeGazeStopwatch.ElapsedMilliseconds;

            }


        }

        public void EyeContactLost()
        {
            toObscure = true;
            eyeGazeStopwatch.Stop();
            staleSubjectStopwatch.Restart();
            voiceAndEyeGazeCounter = eyeGazeStopwatch.ElapsedMilliseconds;
            eyeGazeCounter = eyeGazeStopwatch.ElapsedMilliseconds;
            totalVoiceAndEyeGazeTime += voiceAndEyeGazeCounter;
            totalEyeGazeTime += eyeGazeCounter;
            eyeGazeCounter = 0;
            voiceAndEyeGazeCounter = 0;

        }

        public void onDwell()
        {
            isSubject = true;
        }


        //If this gameobject is in the same physical space another bounding box, we compare the time they have existed and remove the older one
        //This allows for tracking the amount of eye contact over multiple detections.
        /*void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.tag == "BoundingBox")
            {
                //UnityEngine.Debug.Log("Collision");
                if (collision.gameObject.GetComponent<BoundingBoxScript>().detectionStopwatch.ElapsedMilliseconds < this.detectionStopwatch.ElapsedMilliseconds)
                {
                    staleCounter = 0;
                    this.gameObject.transform.position = collision.gameObject.transform.position;
                    this.bboxWidth = collision.gameObject.GetComponent<BoundingBoxScript>().bboxWidth;
                    this.bboxHeight = collision.gameObject.GetComponent<BoundingBoxScript>().bboxHeight;
                    Destroy(collision.gameObject);
                }
                else
                {
                    //staleCounter = 0;
                }
            }


        }*/

    }
}