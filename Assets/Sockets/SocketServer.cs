using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.WindowsMR;
using TMPro;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.MixedReality.OpenXR.ARFoundation;
using Microsoft.MixedReality.OpenXR;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Concurrent;


#if !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Perception.Spatial;
using Windows.Storage;
using Windows.System;
using Windows.Storage.Streams;


#else
using System.Net.Sockets;


#endif




[RequireComponent(typeof(ARAnchorManager))]

public class SocketServer : MonoBehaviour
{

    TrackableId myTrackableId;
    XRAnchorTransferBatch myAnchorTransferBatch = new XRAnchorTransferBatch();
    bool clientConnected = false;
    bool anchorSent = false;
    int counter = 0;
    MemoryStream anchorStream = new MemoryStream();

#if !UNITY_EDITOR
    StreamSocketListener listener = new StreamSocketListener();
    String port;
    StreamSocket client;

#endif

    // Use this for initialization
    async void Start()
    {

#if !UNITY_EDITOR

        port = "65432";
        listener.ConnectionReceived += Listener_ConnectionReceived;
        listener.Control.KeepAlive = true;
        await Listener_Start();

        anchorStream = await tryAddLocalAnchor();
        while (anchorStream == null)
        {
            anchorStream = await tryAddLocalAnchor();
        }

#else

#endif

    }

#if !UNITY_EDITOR
    private async Task<bool> Listener_Start()
    {
        Debug.Log("Listener started");
        try
        {
            await listener.BindServiceNameAsync(port);
        }
        catch (Exception e)
        {
            Debug.Log("Error: " + e.Message);
        }

        Debug.Log("Listening");

        return true;
    }

    private async void Listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
    {
        clientConnected = true;
        client = args.Socket;
    }

#else

#endif


    async void Update()
    {

#if !UNITY_EDITOR
        counter += 1;


        //once the first client connects, we begin to check is it/they have received the host's spatial anchor.  If not, we send them the host's anchor
        if (clientConnected && !anchorSent)
        {
            try
            {
                anchorSent = await trysendAnchor(anchorStream, client);
            }
            catch (Exception exception)
            {
                Debug.Log(exception);
            }

        }


#else

#endif
    }



    public void StopExchange()
    {

#if UNITY_EDITOR


#else

        listener.Dispose();

        listener = null;

#endif

    }

    public void OnDestroy()
    {
        StopExchange();
    }

#if !UNITY_EDITOR
    async Task<MemoryStream> tryAddLocalAnchor()
    {
        MemoryStream memoryStream;
        Debug.Log("Creating Export Anchor Batch in Socket Stream");
        try
        {
            myTrackableId = GameObject.Find("AnchorParent").GetComponent<ARAnchor>().trackableId;
            myAnchorTransferBatch.AddAnchor(myTrackableId, "ParentAnchor");
            memoryStream = (MemoryStream)await XRAnchorTransferBatch.ExportAsync(myAnchorTransferBatch);

            if (memoryStream.Length > 5000)
            {
                return memoryStream;
            }
            else
            {
                Debug.Log("Current Spatial Anchor invalid, please move around physical area with headset on and try this application again");
                return null;
            }

        }
        catch (Exception e)
        {
            Debug.Log("Anchor returned invalid");
            return null;
        }

    }

    async Task<bool> trysendAnchor(MemoryStream memoryStream, StreamSocket client)
    {
        try
        {
            long streamLength = memoryStream.Length;
            byte[] byteAnchorStreamTemp = memoryStream.ToArray();
            //int clientAnchorReceived = 0;
            byte[] lengthBytes = BitConverter.GetBytes(byteAnchorStreamTemp.Length);
            Stream dataWriter = client.OutputStream.AsStreamForWrite();
            await dataWriter.WriteAsync(lengthBytes, 0, lengthBytes.Length);
            await dataWriter.FlushAsync();
            await dataWriter.WriteAsync(byteAnchorStreamTemp, 0, byteAnchorStreamTemp.Length);
            await dataWriter.FlushAsync();
            Debug.Log("Anchor sent to client");
            return true;

        }
        catch (Exception e)
        {
            Debug.Log("Anchor transfer failed");
            return false;
            throw;
        }

    }

#endif

}
