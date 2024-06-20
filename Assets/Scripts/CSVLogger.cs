using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class CSVLogger : MonoBehaviour
{
    // public variables //

    // private variables //
    private string currentDateTime = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");
    private string CSVDirPath;
    private string CSVFilePathDrawPredTime;

    private string CSVFilePath;
    private TextWriter tw;
    private TextWriter twDrawPredTime;

    private List<string> FPSList = new List<string>();
    private List<string> DrawPredTimeList = new List<string>();

    // functions //

    void Awake()
    {
        CSVDirPath = Application.persistentDataPath + "/Logs/";
    }

    void Start()
    {
        // checking for csv directory 
        if (!Directory.Exists(CSVDirPath))
        {
            Directory.CreateDirectory(CSVDirPath);
        }

        // create new csv filepath
        CSVFilePath = Application.persistentDataPath + "/Logs/" + currentDateTime + ".csv";
        CSVFilePathDrawPredTime = Application.persistentDataPath + "/Logs/DrawPredTimes" + currentDateTime + ".csv";

        // initiate columns of CSV
        tw = new StreamWriter(CSVFilePath, false); // false indicates overwriting the file
        tw.WriteLine("TimeStamp, FPS, # Faces"); // edit this to update coloumns of CSV
        tw.Close();

        twDrawPredTime = new StreamWriter(CSVFilePathDrawPredTime, false); // false indicates overwriting the file
        twDrawPredTime.WriteLine("TimeStamp, DateTime"); // edit this to update coloumns of CSV
        twDrawPredTime.Close();
    }

    void Update()
    {
    }

    public void addFPStoList(string fpsLine)
    {
        FPSList.Add(fpsLine);
    }

    public void addDrawPredTimetoList(string drawPredTimeList)
    {
        DrawPredTimeList.Add(drawPredTimeList);
    }

    void OnApplicationPause()
    {
        batchWriteFPS();

        FPSList = new List<string>();
        DrawPredTimeList = new List<string>();
    }

    public void batchWriteFPS()
    {
        tw = new StreamWriter(CSVFilePath, true); // true to indicate no overwriting, just append
        for (int i = 0; i < FPSList.Count; i++)
        {
            tw.WriteLine(FPSList[i]);
        }

        tw.Close();

        twDrawPredTime = new StreamWriter(CSVFilePathDrawPredTime, true); // true to indicate no overwriting, just append
        for (int i = 0; i < DrawPredTimeList.Count; i++)
        {
            twDrawPredTime.WriteLine(DrawPredTimeList[i]);
        }

        twDrawPredTime.Close();
    }
}
