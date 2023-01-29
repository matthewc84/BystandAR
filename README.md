# BystandAR
A prototype created to explore the potential of improved protection of bystander visual data in AR devices
To deploy the prototype:
- Ensure you have Unity 2021.3.11f1 or higher build
- clone the repo
- install MRTK using the Mixed Reality Feature Tool - https://learn.microsoft.com/en-us/windows/mixed-reality/develop/unity/welcome-to-mr-feature-tool

![image](https://user-images.githubusercontent.com/87574595/215337299-3de0d7a5-3507-4795-b57f-7414e20a7801.png)

Build the prototype as shown above

![image](https://user-images.githubusercontent.com/87574595/215337598-80915934-72b7-4d7a-b6d5-c5817be9fd0b.png)

Open BystandAR.sln in Visual Studio (ensure VS installed)

![image](https://user-images.githubusercontent.com/87574595/215337661-34552939-3670-41ca-8d09-6a06756c710f.png)

Deploy to your HoloLens 2

NOTE:
If you want to offload sanitized frames for testing, please see below:

![image](https://user-images.githubusercontent.com/87574595/215337801-4f496138-60a1-44c0-af5b-b654db246c82.png)

The "Frame Sanitizer" class has all options related to inference interval, and debugging outputs (see above). The solution will need to be build with these set before deployment.
- "Sampling Interval" is how often the prototype uses a raw RGB frame to find faces
- "Frame Capture Interval" is how often the sanatized frames are offloaded to the Socket server (must be configured with the right IP and running)
- "Offload Sanitized Frames..." is the option to allow the offload of sanatized frames to the server (boolean)
- "Record Eye Gaze" shows green dots on the sanaitized images to debug the user's eye gaze location. Will be included in frames offloaded to socket server if selected


If using Azure Spatial Anchors (ASA) as part of the multi-user evaluation, see below

Update the ASA account info (example here: https://learn.microsoft.com/en-us/azure/spatial-anchors/quickstarts/get-started-unity-hololens?tabs=azure-portal) before attempting to use this feature/testing method.

![image](https://user-images.githubusercontent.com/87574595/215338131-c5234df4-52a8-4a8d-b792-9fa470c3388d.png)
