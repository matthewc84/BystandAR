# BystandAR
A prototype created to explore the potential of improved protection of bystander visual data in AR devices
<b>To deploy the prototype:</b>
- Ensure you have Unity 2021.3.11f1 or higher build
- clone the repo
- open the project in Unity
- navigate to the "Scenes" folder under "Assets" in the Project View window and open the "FacialDetection" scene
- Click File -> Build Settings -> and proceed as shown below

![image](https://user-images.githubusercontent.com/87574595/215337299-3de0d7a5-3507-4795-b57f-7414e20a7801.png)

<b>Build the prototype as shown above</b>

![image](https://user-images.githubusercontent.com/87574595/215337598-80915934-72b7-4d7a-b6d5-c5817be9fd0b.png)

Open BystandAR.sln in Visual Studio (ensure VS installed, and configured with packages as shown in https://learn.microsoft.com/en-us/windows/mixed-reality/develop/install-the-tools)

![image](https://user-images.githubusercontent.com/87574595/215337661-34552939-3670-41ca-8d09-6a06756c710f.png)

Deploy to your HoloLens 2

<b>NOTE: You MUST have developer mode enabled for the prototype to run. See this link for more info: https://learn.microsoft.com/en-us/windows/mixed-reality/develop/advanced-concepts/using-the-windows-device-portal</b>

<b> NOTE: 
If you want to offload sanitized frames for testing, please see below:</b> 

![image](https://user-images.githubusercontent.com/87574595/215337801-4f496138-60a1-44c0-af5b-b654db246c82.png)

The "Frame Sanitizer" class has all options related to inference interval, frame sanitization, and debugging outputs (see above). The solution will need to be build with these set before deployment.
- "Sampling Interval" is how often the prototype uses a raw RGB frame to find faces
- "Frame Capture Interval" is how often the sanatized frames are offloaded to the Socket server (must be configured with the right IP and running)
- "Offload Sanitized Frames..." is the option to allow the offload of sanatized frames to the server (boolean) 
- "Sanintize Frames" This option will simulate having a third party application request frames, and incur the frame rate hit that is mentioned in the paper.
- "Record Eye Gaze" shows green dots on the sanaitized images to debug the user's eye gaze location. Will be included in frames offloaded to socket server if selected

<b>To use the custom Socket servers for offloading sanitized frames, see below:</b>
- Open ../BystandAR/PythonSocketServer/
- Open "Socket_Server_Images_Multithreaded" and "Socket_Server_Depth_Multithreaded" and run (have these running before startign the prototype)
- Ensure the IP address for these servers is correct in Unity (Assets/Sockets/SocketClientDepth.cs (line 55) and Assets/Sockets/SocketClientImages.cs (line 56))


<b>If using Azure Spatial Anchors (ASA) as part of the multi-user evaluation, see below:</b>

Update the ASA account info (example here: https://learn.microsoft.com/en-us/azure/spatial-anchors/quickstarts/get-started-unity-hololens?tabs=azure-portal) before attempting to use this feature/testing method.

![image](https://user-images.githubusercontent.com/87574595/215338131-c5234df4-52a8-4a8d-b792-9fa470c3388d.png)
