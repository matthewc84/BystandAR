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


    void Start()
    {
        counter = 0;

    }

    
    void RemoveDetection()
    {

        Destroy(gameObject);
    }

    void Update()
    {
        counter += 1;
        if (counter > 60)
        {
            counter = 0;
            RemoveDetection();
        }

        if(framesEyeContactMade > 10)
        {
            
            UnityEngine.Debug.Log("20 frames eye contact, resetting....");
            framesEyeContactMade = 0;
        }


    }

    public void EyeContactMade()
    {
        this.GetComponent<Renderer>().material.SetColor("_Color", Color.red);
        framesEyeContactMade += 1;
    }

}