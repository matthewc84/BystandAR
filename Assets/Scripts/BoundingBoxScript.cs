using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System;
using System.Diagnostics;

public class BoundingBoxScript : MonoBehaviour
{
    int counter;
    public int framesEyeContactMade = 0;
    public Rect box;
    public bool toObscure = true;
    private float initializationTime;
    public float timeSinceInitialization;
    private bool colorSet = false;

    void Start()
    {
        counter = 0;
        initializationTime = Time.realtimeSinceStartup;

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

        if(framesEyeContactMade > 10 && !colorSet )
        {
            this.GetComponent<Renderer>().material.SetColor("_Color", Color.red);
            toObscure = false;
            colorSet = true;
        }

        //timeSinceInitialization = Time.realtimeSinceStartup - initializationTime;


    }

    public void EyeContactMade()
    {

        framesEyeContactMade += 1;
    }

    //If this gameobject is in the same physical space another bounding box, we compare the time they have existed and remove the younger one
    //This allows for tracking the amount of eye contact over multiple detections.
    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "BoundingBox")
        {
            //UnityEngine.Debug.Log("Collision");
            if (collision.gameObject.GetComponent<BoundingBoxScript>().initializationTime > this.initializationTime)
            {
                counter = 0;
            }
            else
            {
               RemoveDetection();
            }
        }


    }

}