using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System;
using System.Diagnostics;

public class BoundingBoxScript : MonoBehaviour
{
    TextMeshPro textComponent;
    public string label;
    int counter;


    void Start()
    {
        textComponent = this.GetComponentInChildren<TextMeshPro>();
        counter = 0;

    }

    
    void RemoveDetection()
    {

        Destroy(gameObject);
    }

    void Update()
    {

        if (counter > 30)
        {
            counter = 0;
            RemoveDetection();
        }




    }

}