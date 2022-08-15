using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;


#if !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

#else
using System.Net.Sockets;

#endif


//Able to act as a reciever 
public class SocketClient : MonoBehaviour
{

#if !UNITY_EDITOR

#endif
    // Use this for initialization
    void Start()
    {
#if !UNITY_EDITOR
        //socket.Control.KeepAlive = true;
#endif
    }


#if !UNITY_EDITOR

    public async Task<bool> trySendSanitizedImage(byte[] image)
    {
        StreamSocket socket = new Windows.Networking.Sockets.StreamSocket();
        HostName serverHost = new HostName("192.168.0.40");
        String port = "65432";
        try
        {

            await socket.ConnectAsync(serverHost, port);
            Debug.Log("Connected to Host");

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

        try
        {
            using (Stream dataWriter = socket.OutputStream.AsStreamForWrite())
            {
                await Task.WhenAll(dataWriter.WriteAsync(image, 0, image.Length), dataWriter.FlushAsync());
            }
            socket.Dispose();
            Debug.Log("Image Frame sent to Server");

            return true;

        }
        catch (Exception ex)
        {
            Debug.Log("Failed to send Image Frame to Server");
            Debug.Log(ex);
            return false;
            throw;
        }

    }

#endif


    void Update()
    {

    }
}
