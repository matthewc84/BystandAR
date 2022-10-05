using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Concurrent;

#if WINDOWS_UWP
using Windows.Storage;

#endif

public class LoggerScript : MonoBehaviour
{
    /// <summary>
    ///     Logging Script 
    /// </summary>
    /// 
    //define filePath
    #region Constants to modify
    private const string DataSuffix = "data";
    private const string SessionFolderRoot = "CSVLogger";
    #endregion

    #region private members
    private string m_sessionPath;
    private string m_filePath;
    private string m_recordingId;
    private string m_sessionId;
    Stopwatch clock;
    private int counter = 0;

    #endregion
    #region public members
    public string RecordingInstance => m_recordingId;
    public ConcurrentQueue<byte[]> inputFrames = new ConcurrentQueue<byte[]>();


    #endregion

    async void Start()
    {
        bool success = await MakeNewSession();

        Task thread = Task.Run(async () =>
        {
            while (true)
            {
                if (inputFrames.Count > 0)
                {
                    var filename = m_recordingId + "-" + counter + ".jpg";
                    m_filePath = Path.Combine(m_sessionPath, filename);
                    byte[] tempByte;
                    bool successDequeue = inputFrames.TryDequeue(out tempByte);
                    if (successDequeue)
                    {
                        File.WriteAllBytes(m_filePath, tempByte);
                        counter += 1;
                    }
                }
            }
        });
    }

    async Task<bool> MakeNewSession()
    {
        m_recordingId = DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
        m_sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string rootPath = "";
#if WINDOWS_UWP
            StorageFolder sessionParentFolder = await KnownFolders.PicturesLibrary
                .CreateFolderAsync(SessionFolderRoot,
                CreationCollisionOption.OpenIfExists);
            rootPath = sessionParentFolder.Path;

#endif
        m_sessionPath = Path.Combine(rootPath, m_sessionId);
        Directory.CreateDirectory(m_sessionPath);
        UnityEngine.Debug.Log("Logger logging data to " + m_sessionPath);

        return true;
    }


    public void startTimer()
    {

        clock = Stopwatch.StartNew();

    }


    public void stopTimer()
    {
        clock.Stop();

    }

}
