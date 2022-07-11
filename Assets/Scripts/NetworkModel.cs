// Adapted from the WinML MNIST sample and Rene Schulte's repo 
// https://github.com/microsoft/Windows-Machine-Learning/tree/master/Samples/MNIST
// https://github.com/reneschulte/WinMLExperiments/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if ENABLE_WINMD_SUPPORT
using Windows.AI.MachineLearning;
using Windows.Storage.Streams;
using Windows.Media;
using Windows.Storage;
using Windows.Media.Capture;
#endif

public class InferenceResult
{
    public List<float> boxes;
    public List<float> scores;
}


//https://github.com/takuya-takeuchi/UltraFaceDotNet/tree/9418a0a2ce31e844667212c09d7457ee451ba936/src/UltraFaceDotNet
/// <summary>
/// Describes the location of a face. This class cannot be inherited.
/// </summary>
public sealed class FaceInfo
{

    /// <summary>
    /// Gets the x-axis value of the left side of the rectangle of face.
    /// </summary>
    public float X1
    {
        get;
        internal set;
    }

    /// <summary>
    /// Gets the y-axis value of the top of the rectangle of face.
    /// </summary>
    public float Y1
    {
        get;
        internal set;
    }

    /// <summary>
    /// Gets the x-axis value of the right side of the rectangle of face.
    /// </summary>
    public float X2
    {
        get;
        internal set;
    }

    /// <summary>
    /// Gets the y-axis value of the bottom of the rectangle of face.
    /// </summary>
    public float Y2
    {
        get;
        internal set;
    }

    /// <summary>
    /// Gets the score of the rectangle of face.
    /// </summary>
    public float Score
    {
        get;
        internal set;
    }

}

public class NetworkModel
{

#if ENABLE_WINMD_SUPPORT
    private MediaCapture _media_capture;
    private LearningModel _model;
    private LearningModelSession _session;
    private LearningModelBinding _binding;

#endif



#if ENABLE_WINMD_SUPPORT


    public async Task InitModelAsync()
    {
        var model_file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets//version-RFB-640.onnx"));
        _model = await LearningModel.LoadFromStorageFileAsync(model_file);
       // var device = new LearningModelDevice(LearningModelDeviceKind.Cpu);
        _session = new LearningModelSession(_model);
        _binding = new LearningModelBinding(_session);

    }
    public async Task<InferenceResult> EvaluateVideoFrameAsync(VideoFrame inputFrame)
    {
        // Sometimes on HL RS4 the D3D surface returned is null, so simply skip those frames
        if (_model == null || inputFrame == null || (inputFrame.Direct3DSurface == null && inputFrame.SoftwareBitmap == null))
        {
            UnityEngine.Debug.Log("Frame thrown out");
            return null;
        }
        
        try{

            // Perform network model inference using the input data tensor, cache output and time operation
            InferenceResult result = await EvaluateFrame(inputFrame);


        return result;
        }

         catch (Exception ex)
        {
            throw;
            return null;
        }

    }

   private async Task<InferenceResult> EvaluateFrame(VideoFrame frame)
        {

            _binding.Clear();
            _binding.Bind("input", frame);

            var results = await _session.EvaluateAsync(_binding, "");

            TensorFloat boxes = results.Outputs["boxes"] as TensorFloat;
            TensorFloat scores = results.Outputs["scores"] as TensorFloat;
            //var shape = result.Shape;
            var boxesFloat = boxes.GetAsVectorView();
            var scoresFloat = scores.GetAsVectorView();

            InferenceResult result = new InferenceResult();;
            result.boxes = boxesFloat.ToList<float>();
            result.scores = scoresFloat.ToList<float>();
            return result;
        }


#endif


}