using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Azure.SpatialAnchors.Unity;
using Microsoft.Azure.SpatialAnchors;
using System.Threading.Tasks;
using Photon.Pun;
using Photon.Realtime;

public class AzureSpatialAnchorsScript : MonoBehaviour
{

    /// <summary>
    /// Main interface to anything Spatial Anchors related
    /// </summary>
    private SpatialAnchorManager _spatialAnchorManager = null;
    private GameObject anchorParent;
    public string anchorIdentifier = null;
    public bool retrievingAnchor = true;


    // Start is called before the first frame update
    async void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        anchorParent = GameObject.Find("AnchorParent");

        _spatialAnchorManager = GetComponent<SpatialAnchorManager>();
        _spatialAnchorManager.LogDebug += (sender, args) => Debug.Log($"ASA - Debug: {args.Message}");
        _spatialAnchorManager.Error += (sender, args) => Debug.LogError($"ASA - Error: {args.ErrorMessage}");
        _spatialAnchorManager.AnchorLocated += SpatialAnchorManager_AnchorLocated;

        await _spatialAnchorManager.StartSessionAsync();


    }

    public async Task<string> createAnchor()
    {
        CloudNativeAnchor cloudNativeAnchor = anchorParent.AddComponent<CloudNativeAnchor>();
        await cloudNativeAnchor.NativeToCloud();
        CloudSpatialAnchor cloudSpatialAnchor = cloudNativeAnchor.CloudAnchor;
        cloudSpatialAnchor.Expiration = DateTimeOffset.Now.AddMinutes(20);

        //Collect Environment Data
        while (!_spatialAnchorManager.IsReadyForCreate)
        {
            float createProgress = _spatialAnchorManager.SessionStatus.RecommendedForCreateProgress;
            Debug.Log($"ASA - Move your device to capture more environment data: {createProgress:0%}");
        }

        Debug.Log($"ASA - Saving cloud anchor... ");

        try
        {
            // Now that the cloud spatial anchor has been prepared, we can try the actual save here.
            await _spatialAnchorManager.CreateAnchorAsync(cloudSpatialAnchor);

            bool saveSucceeded = cloudSpatialAnchor != null;
            if (!saveSucceeded)
            {
                Debug.LogError("ASA - Failed to save, but no exception was thrown.");
                return null;
            }

            Debug.Log($"ASA - Saved cloud anchor with ID: {cloudSpatialAnchor.Identifier}");
            retrievingAnchor = false;
            return cloudSpatialAnchor.Identifier;
        }
        catch (Exception exception)
        {
            Debug.Log("ASA - Failed to save anchor: " + exception.ToString());
            Debug.LogException(exception);
            return null;
        }

        
    }

    /// <summary>
    /// Looking for anchors with ID in _createdAnchorIDs
    /// </summary>
    public void LocateAnchor(string identifier)
    {
        
        string[] identifiers = new string[1];
        identifiers[0] = identifier;
        //Create watcher to look for all stored anchor IDs
        Debug.Log($"ASA - Creating watcher to look for {identifier} spatial anchor");
        AnchorLocateCriteria anchorLocateCriteria = new AnchorLocateCriteria();
        anchorLocateCriteria.Identifiers = identifiers;
        _spatialAnchorManager.Session.CreateWatcher(anchorLocateCriteria);
        Debug.Log($"ASA - Watcher created!");

    }

    /// <summary>
    /// Callback when an anchor is located
    /// </summary>
    /// <param name="sender">Callback sender</param>
    /// <param name="args">Callback AnchorLocatedEventArgs</param>
    private void SpatialAnchorManager_AnchorLocated(object sender, AnchorLocatedEventArgs args)
    {
        Debug.Log($"ASA - Anchor recognized as a possible anchor {args.Identifier} {args.Status}");

        if (args.Status == LocateAnchorStatus.Located)
        {
            //Creating and adjusting GameObjects have to run on the main thread. We are using the UnityDispatcher to make sure this happens.
            UnityDispatcher.InvokeOnAppThread(() =>
            {
                var clientAnchorParent = GameObject.Find("AnchorParent");
                // Read out Cloud Anchor values
                CloudSpatialAnchor cloudSpatialAnchor = args.Anchor;

                // Link to Cloud Anchor
                clientAnchorParent.AddComponent<CloudNativeAnchor>().CloudToNative(cloudSpatialAnchor);
            });
        }

        retrievingAnchor = false;
    }
}
