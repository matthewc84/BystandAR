using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Microsoft.MixedReality.OpenXR;
using TMPro;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Microsoft.MixedReality.OpenXR.ARFoundation;
using System.Threading.Tasks;

#if !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

#endif

[RequireComponent(typeof(ARAnchorManager))]

//Able to act as a reciever 
public class SocketClientAnchor : MonoBehaviour
{
    XRAnchorTransferBatch myAnchorTransferBatch = new XRAnchorTransferBatch();
    public bool anchorReceived = false;
    bool connectedToServer = false;
    MemoryStream tempStream = new MemoryStream();
    int counter = 0;

#if !UNITY_EDITOR
    StreamSocket socket = new Windows.Networking.Sockets.StreamSocket();
    HostName serverHost = new HostName("192.168.0.162");
    String port = "15462";
    bool _Connected = false;

#endif

    // Use this for initialization
    void Start()
    {
#if !UNITY_EDITOR
        socket.Control.KeepAlive = true;
        Client_Start();
#endif
    }


#if !UNITY_EDITOR
    private async void Client_Start()
    {
        Debug.Log("Client Started");

        try
        {
            await socket.ConnectAsync(serverHost, port);
            _Connected = true;
            Debug.Log("Connected to Host");

        }
        catch (Exception exception)
        {
            // If this is an unknown status it means that the error is fatal and retry will likely fail.
            if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
            {
                throw;
                Debug.Log("Connection Attempt failed, trying again");
                Client_Start();
            }
        }

    }

    private async void attemptReceiveSpatialAnchor()
    {
        // Buffer to store the response bytes.
        byte[] lengthBuffer = new byte[4];
        int bytesRead = 0;
        int totalBytes = 0;
        //int bufferSize = 8192;
        int bufferSize = 32768;
        double progress = 0;
        int counter = 0;
        int streamLength;
        MemoryStream tempMemStream;

        try
        {
            Stream dataReader = socket.InputStream.AsStreamForRead();
            await dataReader.ReadAsync(lengthBuffer, 0, lengthBuffer.Length);
            streamLength = BitConverter.ToInt32(lengthBuffer, 0);
            byte[] myReadBuffer = new byte[bufferSize];
            byte[] tempByteArray = new byte[streamLength];
            Debug.Log("Attempting to read anchor of size: " + streamLength + " bytes");
            Debug.Log("Waiting....");
            // Incoming message may be larger than the buffer size.
            do
            {
                if (bufferSize > (streamLength - totalBytes))
                {
                    bufferSize = streamLength - totalBytes;
                }
                bytesRead = await dataReader.ReadAsync(myReadBuffer, 0, bufferSize);
                Array.Copy(myReadBuffer, 0, tempByteArray, totalBytes, bytesRead);
                totalBytes += bytesRead;
                counter += 1;
                if (counter == 120)
                {
                    progress = (Convert.ToDouble(totalBytes) / Convert.ToDouble(streamLength)) * 100;
                    Debug.Log("Recv'd " + progress + "% of Spatial Anchor");
                    counter = 0;
                }

            } while (totalBytes < streamLength);

            tempMemStream = new MemoryStream(tempByteArray);

            Debug.Log("Anchor Received");
            if (tempMemStream.CanRead)
            {
                Debug.Log("Attempting to import anchor locally...");
                myAnchorTransferBatch = await XRAnchorTransferBatch.ImportAsync(tempMemStream);
                if (myAnchorTransferBatch != null)
                {
                    myAnchorTransferBatch.LoadAndReplaceAnchor(myAnchorTransferBatch.AnchorNames[0], GameObject.Find("AnchorParent").GetComponent<ARAnchor>().trackableId);
                    Debug.Log("Host Anchor Imported to Local System");
                    byte[] bytes = Encoding.ASCII.GetBytes("Done");
                    Stream dataWriter = socket.OutputStream.AsStreamForWrite();
                    await dataWriter.WriteAsync(bytes, 0, bytes.Length);
                    anchorReceived = true;
                }



            }
            else
            {
                Debug.Log("tempStream not readable");
                anchorReceived = false;

            }


        }
        catch (Exception exception)
        {
            throw;
        }

        //tempMemStream.Close();
    }


#else

#endif


    async void Update()
    {
        counter += 1;
#if !UNITY_EDITOR
        if (_Connected && counter >= 60 && !anchorReceived)
        {
            counter = 0;
            try
            {
                attemptReceiveSpatialAnchor();
            }
            catch (Exception exception)
            {
                Debug.Log("Exception receiving anchor: " + exception);
            }
        }

#else
      
    
#endif
    }
}

