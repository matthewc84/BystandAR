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

        public int framesEyeContactMade = 0;
        public int secondsEyeContactMade = 0;
        public float bboxWidth = 0;
        public float bboxHeight = 0;
        public bool toObscure = true;
        private float initializationTime;
        private FrameSanitizer frameSanitizer;
        int staleCounter;
        public int eyegazeCounter;
        public int totalFrameCounter;
        public int voiceAndEyeGazeCounter;
        bool firstTimeEyeGazeContact = true;


        void Start()
        {
            staleCounter = 0;
            eyegazeCounter = 0;
            voiceAndEyeGazeCounter = 0;
            totalFrameCounter = 0;
            initializationTime = Time.realtimeSinceStartup;
            frameSanitizer = GameObject.Find("FrameSanitizer").GetComponent<FrameSanitizer>();
            

        }


        void RemoveDetection()
        {

            Destroy(gameObject);
        }

        void Update()
        {
            staleCounter += 1;
            totalFrameCounter += 1;

            //If object has existed for more than the given threshold without update, we treat it as stale and remove
            if (staleCounter > 60)
            {
                RemoveDetection();
            }

            float percentEyeAndVoiceContact = (float)voiceAndEyeGazeCounter / (float)totalFrameCounter;
            float percentEyeContact = (float)eyegazeCounter / (float)totalFrameCounter;
            //UnityEngine.Debug.Log(percentEyeContact);

            if (percentEyeAndVoiceContact > 0.77f || percentEyeContact > 0.88f)
            {
                toObscure = false;
            }
        }

        public void EyeContactMade()
        {
            if (firstTimeEyeGazeContact)
            {
                firstTimeEyeGazeContact = false;
                totalFrameCounter = 0;
            }

            if (frameSanitizer.userSpeaking)
            {
                voiceAndEyeGazeCounter += 1;

            }
            else
            {
                eyegazeCounter += 1;


            }


        }

        public void EyeContactLost()
        {


        }


        //If this gameobject is in the same physical space another bounding box, we compare the time they have existed and remove the older one
        //This allows for tracking the amount of eye contact over multiple detections.
        void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.tag == "BoundingBox")
            {
                //UnityEngine.Debug.Log("Collision");
                if (collision.gameObject.GetComponent<BoundingBoxScript>().initializationTime > this.initializationTime)
                {
                    //collision.gameObject.GetComponent<BoundingBoxScript>().toObscure = this.toObscure;
                    //collision.gameObject.GetComponent<BoundingBoxScript>().totalFrameCounter = this.totalFrameCounter;
                    //collision.gameObject.GetComponent<BoundingBoxScript>().voiceAndEyeGazeCounter = this.voiceAndEyeGazeCounter;
                    //collision.gameObject.GetComponent<BoundingBoxScript>().eyegazeCounter = this.eyegazeCounter;
                    staleCounter = 0;
                    this.gameObject.transform.position = collision.gameObject.transform.position;
                    this.bboxWidth = collision.gameObject.GetComponent<BoundingBoxScript>().bboxWidth;
                    this.bboxHeight = collision.gameObject.GetComponent<BoundingBoxScript>().bboxHeight;
                    collision.gameObject.GetComponent<BoundingBoxScript>().RemoveDetection();
                }
                else
                {
                    staleCounter = 0;
                }
            }


        }

    }
}