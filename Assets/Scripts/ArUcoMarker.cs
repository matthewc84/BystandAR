using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Microsoft.MixedReality.OpenXR;

public class ArUcoMarker : MonoBehaviour
{
    private float latestDistance;
    private Vector3 latestDirection;

    private Vector3 latestPosition = new Vector3(0,0,0);
    private Quaternion latestRotation = Quaternion.identity;

    private void Awake()
    {
        var m_arMarkerManager = GetComponent<ARMarkerManager>();
        m_arMarkerManager.markersChanged += OnQRCodesChanged;
    }

    void OnQRCodesChanged(ARMarkersChangedEventArgs args)
    {
        // Debug.Log("OnQRCodesChanged");

        /*
        foreach (ARMarker qrCode in args.added)
            Debug.Log($"QR code with the ID {qrCode.trackableId} added.");
            
        foreach (ARMarker qrCode in args.removed)
            Debug.Log($"QR code with the ID {qrCode.trackableId} removed.");
        */

        foreach (ARMarker qrCode in args.updated)
        {
            // Debug.Log($"QR code with the ID {qrCode.trackableId} updated.");
            // Debug.Log($"Pos:{qrCode.transform.position} Rot:{qrCode.transform.rotation} Size:{qrCode.size}");

            float distance = Vector3.Distance(Camera.main.transform.position, qrCode.transform.position);
            // Debug.Log("Distance from QR code with id " + id + " : " + distance);

            Vector3 direction = qrCode.transform.position - Camera.main.transform.position;
            // Debug.Log("Direction from QR code with id " + id + " : " + direction);

            latestDirection = direction;
            latestDistance = distance;

            latestPosition = qrCode.transform.position;
            latestRotation = qrCode.transform.rotation;
        }
    }

    public float getLatestDistance()
    {
        return latestDistance;
    }

    public Vector3 getLatestDirection()
    {
        return latestDirection;
    }

    public Vector3 getLatestPosition()
    {
        return latestPosition;
    }

    public Quaternion getLatestRotation()
    {
        return latestRotation;
    }
}