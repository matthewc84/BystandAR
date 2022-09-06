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
    StreamSocket socket;
    HostName serverHost;
    Stream dataWriter;
    public Queue<byte[]> inputFrames = new Queue<byte[]>();
#endif

    // Use this for initialization
    async void Start()
    {
#if !UNITY_EDITOR
        socket = new Windows.Networking.Sockets.StreamSocket();
        serverHost = new HostName("192.168.0.40");
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

        dataWriter = socket.OutputStream.AsStreamForWrite();
#endif
    }

    async void Update()
    {
#if !UNITY_EDITOR
        if(inputFrames.Count > 0)
        {
            try
            {

                StartCoroutine(trySendSanitizedImage(inputFrames.Dequeue()));

            }
            catch (Exception exception)
            {

            }
        }

#endif
    }


#if !UNITY_EDITOR

    IEnumerator trySendSanitizedImage(byte[] image)
    {
        try
        {
            byte[] imageLength = BitConverter.GetBytes(image.Length);
            //Debug.Log("Sending picture of size " + image.Length);
            dataWriter.WriteAsync(imageLength, 0, imageLength.Length);

            dataWriter.WriteAsync(image, 0, image.Length);
            dataWriter.FlushAsync();

        }
        catch (Exception ex)
        {
            Debug.Log("Failed to send Image Frame to Server");
            Debug.Log(ex);
            throw;
        }
        yield return null;
    }

#endif

}
