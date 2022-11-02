using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Microsoft.MixedReality.OpenXR;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Microsoft.MixedReality.OpenXR.ARFoundation;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;



#if !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

#endif


[RequireComponent(typeof(ARAnchorManager))]
//Able to act as a reciever 
public class SocketClientImages : MonoBehaviour
{

#if !UNITY_EDITOR
    StreamSocket socket;
    HostName serverHost;
    Stream dataWriter;
    

#endif

    XRAnchorTransferBatch myAnchorTransferBatch = new XRAnchorTransferBatch();
    bool anchorReceived = false;
    public bool connectedToServer = false;
    MemoryStream tempStream = new MemoryStream();
    int counter = 0;
    public bool queueOpen = true;
    public ConcurrentQueue<byte[]> inputFrames = new ConcurrentQueue<byte[]>();

    // Use this for initialization
    async void Start()
    {
#if !UNITY_EDITOR
        socket = new Windows.Networking.Sockets.StreamSocket();
        //serverHost = new HostName("128.173.239.212");
        serverHost = new HostName("192.168.0.40");
        String port = "65432";
        
        try
        {

            await socket.ConnectAsync(serverHost, port);
            Debug.Log("Connected to Image Server");
            connectedToServer = true;

        }
        catch (Exception exception)
        {
            // If this is an unknown status it means that the error is fatal and retry will likely fail.
            if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
            {
                throw;
                Debug.Log("Connection Attempt failed, trying again");
            }
        }

        dataWriter = socket.OutputStream.AsStreamForWrite();

        Task thread = Task.Run(async () =>
        {
            while (true)
            {
                if(inputFrames.Count > 0)
                {
                    byte[] tempByte;
                    inputFrames.TryDequeue(out tempByte);
                    try
                    {
                        bool sendSuccess = await trySendSanitizedImage(tempByte);
                    }
                    catch
                    {
                        Debug.Log("Image Send failed!");
                    }
                }
            }
        });

#endif
    }


#if !UNITY_EDITOR

    public async Task<bool> trySendSanitizedImage(byte[] image)
    {

        try
        {
            byte[] imageLength = BitConverter.GetBytes(image.Length);
            //Debug.Log("Sending picture of size " + image.Length);
            await dataWriter.WriteAsync(imageLength, 0, imageLength.Length);
            await dataWriter.FlushAsync();
            await dataWriter.WriteAsync(image, 0, image.Length);
            await dataWriter.FlushAsync();
            return true;

        }
        catch (Exception ex)
        {
            Debug.Log("Failed to send Image Frame to Server");
            Debug.Log(ex);
            return false;
        }

    }
 
#endif

}
