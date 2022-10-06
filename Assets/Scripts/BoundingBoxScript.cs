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
        int counter;
        public int framesEyeContactMade = 0;
        public float bboxWidth = 0;
        public float bboxHeight = 0;
        public bool toObscure = true;
        private float initializationTime;
        private FrameSanitizer frameSanitizer;

        void Start()
        {
            counter = 0;
            initializationTime = Time.realtimeSinceStartup;
            frameSanitizer = GameObject.Find("FrameSanitizer").GetComponent<FrameSanitizer>();

        }


        void RemoveDetection()
        {

            Destroy(gameObject);
        }

        void Update()
        {
            counter += 1;
            //If object has existed for more than the given threshold without update, we treat it as stale and remove
            if (counter > 60)
            {
                RemoveDetection();
            }

            if (framesEyeContactMade > 100)
            {
                toObscure = false;
            }
        }

        public void EyeContactMade()
        {
            if (frameSanitizer.userSpeaking)
            {
                framesEyeContactMade += 5;
                UnityEngine.Debug.Log("Eye and Voice contact!");
            }
            else
            {
                framesEyeContactMade += 1;
            }


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
                    collision.gameObject.GetComponent<BoundingBoxScript>().toObscure = this.toObscure;
                    collision.gameObject.GetComponent<BoundingBoxScript>().framesEyeContactMade = this.framesEyeContactMade;
                    RemoveDetection();
                }
                else
                {
                    counter = 0;
                }
            }


        }

    }
}