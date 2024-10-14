using System;
using System.IO;
// using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class CSVLogger : MonoBehaviour
{
    // public variables //

    // private variables //
    private string currentDateTime = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");
    
    private string CSVDirPath;
    private string CSVFilePath;
    private TextWriter tw;
    private string CSVFilePathDrawPredTime;
    private TextWriter twDrawPredTime;
    private string GazeOriginCSVFilePath;
    private TextWriter GazeOrigintw;
    private string GazeDirCSVFilePath;
    private TextWriter GazeDirtw;

    [SerializeField]
    private int FPSCSVWriteFrequency;   // enter -1 to write once only at the end otherwise every x number of lines will be written
    private int writeAtEndNumber = -1;

    private List<string> FPSList = new List<string>();
    private List<string> DrawPredTimeList = new List<string>();
    private List<Tuple<string, Vector3>> GazeOriginList = new List<Tuple<string, Vector3>>();
    private List<Tuple<string, Vector3>> GazeDirList = new List<Tuple<string, Vector3>>();

    [SerializeField]
    private bool ReadGazeFromCSV = false;

    private Queue<Vector3> gazeOriginArray = new Queue<Vector3>();
    private List<Tuple<int, Vector3>> gazeOriginArrayList = new List<Tuple<int, Vector3>>();
    private Queue<Vector3> gazeDirArray = new Queue<Vector3>();
    private List<Tuple<int, Vector3>> gazeDirArrayList = new List<Tuple<int, Vector3>>();
    private Queue<float> QRDistArray = new Queue<float>();
    private Queue<Vector3> QRDirArray = new Queue<Vector3>();

    private List<float> gazeOriginXArray = new List<float>();
    private List<float> gazeOriginYArray = new List<float>();
    private List<float> gazeOriginZArray = new List<float>();

    private float gazeOriginMinX = 10000;
    private float gazeOriginMaxX = -10000;

    private float gazeOriginMinY = 10000;
    private float gazeOriginMaxY = -10000;

    private float gazeOriginMinZ = 10000;
    private float gazeOriginMaxZ = -10000;

    private List<float> QRDistXArray = new List<float>();

    private float QRDistMin = 10000;
    private float QRDistMax = -10000;

    private List<float> QRDirXArray = new List<float>();
    private List<float> QRDirYArray = new List<float>();
    private List<float> QRDirZArray = new List<float>();

    private float QRDirMinX = 10000;
    private float QRDirMaxX = -10000;

    private float QRDirMinY = 10000;
    private float QRDirMaxY = -10000;

    private float QRDirMinZ = 10000;
    private float QRDirMaxZ = -10000;

    // functions //

    void Awake()
    {
        CSVDirPath = Application.persistentDataPath + "/Logs/";

        // checking for csv directory 
        if (!Directory.Exists(CSVDirPath))
        {
            Directory.CreateDirectory(CSVDirPath);
        }

        //  Performance metrics logger
        // create new csv filepath
        CSVFilePath = Application.persistentDataPath + "/Logs/" + currentDateTime + ".csv";
        // initiate columns of CSV
        tw = new StreamWriter(CSVFilePath, false); // false indicates overwriting the file
        tw.WriteLine("TimeStamp, FPS, # Faces, frame #, QRDetection Status, Hardcoded Eye Gaze, DetectedFaces Info"); // edit this to update coloumns of CSV
        tw.Close();

        /*
        // Time between drawPredictions logger
        CSVFilePathDrawPredTime = Application.persistentDataPath + "/Logs/DrawPredTimes" + currentDateTime + ".csv";
        twDrawPredTime = new StreamWriter(CSVFilePathDrawPredTime, false);
        twDrawPredTime.WriteLine("TimeStamp, DateTime");
        twDrawPredTime.Close();
        */
    }

    void Update()
    {
    }

    public void addFPStoList(string fpsLine)
    {
        // FPSList.Add(fpsLine);

        if (FPSCSVWriteFrequency == writeAtEndNumber)
        {
            FPSList.Add(fpsLine);
        }

        if (FPSCSVWriteFrequency == 1)
        {
            Debug.Log("Writing every line directly without lists");

            tw = new StreamWriter(CSVFilePath, true); // true to indicate no overwriting, just append
            tw.WriteLine(fpsLine);
            tw.Close();
        }
        else
        {
            if (FPSList.Count >= FPSCSVWriteFrequency)
            {
                batchWriteToFiles();

                FPSList = new List<string>();
            }

            FPSList.Add(fpsLine);
        }
    }

    public void addDrawPredTimetoList(string drawPredTimeList)
    {
        DrawPredTimeList.Add(drawPredTimeList);
    }

    public void addGazeOriginToList(string timestamp, Vector3 gazeOriginLine)
    {
        var timeAndGazeOriginTupel = new Tuple<string, Vector3>(timestamp, gazeOriginLine);
        GazeOriginList.Add(timeAndGazeOriginTupel);
    }

    public void addGazeDirToList(string timestamp, Vector3 gazeDirLine)
    {
        var timeAndGazeDirTupel = new Tuple<string, Vector3>(timestamp, gazeDirLine);
        GazeDirList.Add(timeAndGazeDirTupel);
    }

    void OnApplicationPause()
    {
        batchWriteToFiles();

        FPSList = new List<string>();
        // DrawPredTimeList = new List<string>();
        GazeOriginList = new List<Tuple<string, Vector3>>();
        GazeDirList = new List<Tuple<string, Vector3>>();
    }

    public void batchWriteToFiles()
    {
        // Performance metrics logger
        tw = new StreamWriter(CSVFilePath, true); // true to indicate no overwriting, just append
        for (int i = 0; i < FPSList.Count; i++)
        {
            tw.WriteLine(FPSList[i]);
        }

        tw.Close();

        /*
        // Time between drawPredictions logger
        twDrawPredTime = new StreamWriter(CSVFilePathDrawPredTime, true); // true to indicate no overwriting, just append
        for (int i = 0; i < DrawPredTimeList.Count; i++)
        {
            twDrawPredTime.WriteLine(DrawPredTimeList[i]);
        }

        twDrawPredTime.Close();
        */
    }

    public bool getReadGazeFromCSV()
    {
        return ReadGazeFromCSV;
    }

    public Queue<Vector3> loadGazeOriginDataCSV(string GazeOriginFile)
    {
        if (File.Exists(GazeOriginFile))
        {

            StreamReader strReader = new StreamReader(GazeOriginFile);
            bool eof = false;

            while (!eof)
            {
                string line = strReader.ReadLine();

                if (line == null)
                {
                    eof = true;
                    break;
                }

                var data_values = line.Split(',');

                // remove the leading ( and trailing ) from the line
                line = data_values[1] + "," + data_values[2] + "," + data_values[3];
                line = line.Substring(1);
                line = line.Remove(line.Length - 1);

                data_values = line.Split(',');

                Vector3 GazeOriginVector = new Vector3(float.Parse(data_values[0]), float.Parse(data_values[1]), float.Parse(data_values[2]));

                gazeOriginArray.Enqueue(GazeOriginVector);
            }
        }

        return gazeOriginArray;
    }

    public List<Tuple<int, Vector3>> loadListFromGazeOriginDataCSV(string GazeOriginFile)
    {
        if (File.Exists(GazeOriginFile))
        {

            StreamReader strReader = new StreamReader(GazeOriginFile);
            bool eof = false;

            while (!eof)
            {
                string line = strReader.ReadLine();

                if (line == null)
                {
                    eof = true;
                    break;
                }

                var data_values = line.Split(',');

                // get time stamp
                int elapsedTime = Int32.Parse(data_values[0]);

                // remove the leading ( and trailing ) from the line
                line = data_values[1] + "," + data_values[2] + "," + data_values[3];
                line = line.Substring(1);
                line = line.Remove(line.Length - 1);

                data_values = line.Split(',');

                Vector3 GazeOriginVector = new Vector3(float.Parse(data_values[0]), float.Parse(data_values[1]), float.Parse(data_values[2]));

                // gazeOriginArray.Enqueue(GazeOriginVector);
                var timeAndGazeOriginTupel = new Tuple<int, Vector3>(elapsedTime, GazeOriginVector);
                gazeOriginArrayList.Add(timeAndGazeOriginTupel);

                /*
                gazeOriginXArray.Add(GazeOriginVector.x);
                gazeOriginYArray.Add(GazeOriginVector.y);
                gazeOriginZArray.Add(GazeOriginVector.z);
                */
            }
        }

        /*
        var gazeOriginXActualArray = gazeOriginXArray.ToArray();
        var gazeOriginYActualArray = gazeOriginYArray.ToArray();
        var gazeOriginZActualArray = gazeOriginZArray.ToArray();

        gazeOriginMinX = gazeOriginXActualArray.Min();
        gazeOriginMaxX = gazeOriginXActualArray.Max();

        gazeOriginMinY = gazeOriginYActualArray.Min();
        gazeOriginMaxY = gazeOriginYActualArray.Max();

        gazeOriginMinZ = gazeOriginZActualArray.Min();
        gazeOriginMaxZ = gazeOriginZActualArray.Max();
        */

        return gazeOriginArrayList;
    }

    public float getgazeOriginMinX()
    {
        return gazeOriginMinX;
    }

    public float getgazeOriginMaxX()
    {
        return gazeOriginMaxX;
    }

    public float getgazeOriginMinY()
    {
        return gazeOriginMinY;
    }

    public float getgazeOriginMaxY()
    {
        return gazeOriginMaxY;
    }

    public float getgazeOriginMinZ()
    {
        return gazeOriginMinZ;
    }

    public float getgazeOriginMaxZ()
    {
        return gazeOriginMaxZ;
    }

    public Queue<Vector3> loadGazeDirDataCSV(string GazeDirFile)
    {
        if (File.Exists(GazeDirFile))
        {

            StreamReader strReader = new StreamReader(GazeDirFile);
            bool eof = false;

            while (!eof)
            {
                string line = strReader.ReadLine();

                if (line == null)
                {
                    eof = true;
                    break;
                }

                var data_values = line.Split(',');

                // remove the leading ( and trailing ) from the line
                line = data_values[1] + "," + data_values[2] + "," + data_values[3];
                line = line.Substring(1);
                line = line.Remove(line.Length - 1);

                data_values = line.Split(',');

                Vector3 GazeDirVector = new Vector3(float.Parse(data_values[0]), float.Parse(data_values[1]), float.Parse(data_values[2]));

                gazeDirArray.Enqueue(GazeDirVector);
            }
        }

        return gazeDirArray;
    }

    public List<Tuple<int, Vector3>> loadListFromGazeDirDataCSV(string GazeDirFile)
    {
        if (File.Exists(GazeDirFile))
        {

            StreamReader strReader = new StreamReader(GazeDirFile);
            bool eof = false;

            while (!eof)
            {
                string line = strReader.ReadLine();

                if (line == null)
                {
                    eof = true;
                    break;
                }

                var data_values = line.Split(',');

                // get time stamp
                int elapsedTime = Int32.Parse(data_values[0]);

                // remove the leading ( and trailing ) from the line
                line = data_values[1] + "," + data_values[2] + "," + data_values[3];
                line = line.Substring(1);
                line = line.Remove(line.Length - 1);

                data_values = line.Split(',');

                Vector3 GazeDirVector = new Vector3(float.Parse(data_values[0]), float.Parse(data_values[1]), float.Parse(data_values[2]));

                // gazeDirArray.Enqueue(GazeDirVector);
                var timeAndGazeOriginTupel = new Tuple<int, Vector3>(elapsedTime, GazeDirVector);
                gazeDirArrayList.Add(timeAndGazeOriginTupel);
            }
        }

        return gazeDirArrayList;
    }

    public Queue<float> loadQRDistDataCSV(string QRDistFile)
    {
        if (File.Exists(QRDistFile))
        {
            StreamReader strReader = new StreamReader(QRDistFile);
            bool eof = false;

            while (!eof)
            {
                string line = strReader.ReadLine();

                if (line == null)
                {
                    eof = true;
                    break;
                }

                var data_values = line.Split(',');

                float QRDistFloat = float.Parse(data_values[1]);

                QRDistArray.Enqueue(QRDistFloat);

                if (QRDistFloat < QRDistMin)
                {
                    QRDistMin = QRDistFloat;
                }
                else if (QRDistFloat > QRDistMax)
                {
                    QRDistMax = QRDistFloat;
                }

            }
        }

        /*
        var QRDistActualArray = QRDistArray.ToArray();

        QRDistMin = QRDistActualArray.Min();
        QRDistMax = QRDistActualArray.Max();
        */

        return QRDistArray;
    }

    public float getQRDistMin()
    {
        return QRDistMin;
    }

    public float getQRDistMax()
    {
        return QRDistMax;
    }

    public Queue<Vector3> loadQRDirDataCSV(string QRDirFile)
    {
        if (File.Exists(QRDirFile))
        {
            StreamReader strReader = new StreamReader(QRDirFile);
            bool eof = false;

            while (!eof)
            {
                string line = strReader.ReadLine();

                if (line == null)
                {
                    eof = true;
                    break;
                }

                var data_values = line.Split(',');

                // remove the leading ( and trailing ) from the line
                line = data_values[1] + "," + data_values[2] + "," + data_values[3];
                line = line.Substring(1);
                line = line.Remove(line.Length - 1);

                data_values = line.Split(',');

                Vector3 QRDirVector = new Vector3(float.Parse(data_values[0]), float.Parse(data_values[1]), float.Parse(data_values[2]));

                QRDirArray.Enqueue(QRDirVector);

                /*
                QRDirXArray.Add(QRDirVector.x);
                QRDirYArray.Add(QRDirVector.y);
                QRDirZArray.Add(QRDirVector.z);
                */

                if (QRDirVector.x < QRDirMinX)
                {
                    QRDirMinX = QRDirVector.x;
                }
                else if (QRDirVector.x > QRDirMaxX)
                {
                    QRDirMaxX = QRDirVector.x;
                }

                if (QRDirVector.y < QRDirMinY)
                {
                    QRDirMinY = QRDirVector.y;
                }
                else if (QRDirVector.y > QRDirMaxY)
                {
                    QRDirMaxY = QRDirVector.y;
                }

                if (QRDirVector.z < QRDirMinZ)
                {
                    QRDirMinZ = QRDirVector.z;
                }
                else if (QRDirVector.z > QRDirMaxZ)
                {
                    QRDirMaxZ = QRDirVector.z;
                }

            }
        }

        /*
        var QRDirXActualArray = QRDirXArray.ToArray();
        var QRDirYActualArray = QRDirYArray.ToArray();
        var QRDirZActualArray = QRDirZArray.ToArray();

        QRDirMinX = QRDirXActualArray.Min();
        QRDirMaxX = QRDirXActualArray.Max();

        QRDirMinY = QRDirYActualArray.Min();
        QRDirMaxY = QRDirYActualArray.Max();

        QRDirMinZ = QRDirZActualArray.Min();
        QRDirMaxZ = QRDirZActualArray.Max();
        */

        return QRDirArray;
    }

    public float getQRDirMinX()
    {
        return QRDirMinX;
    }

    public float getQRDirMaxX()
    {
        return QRDirMaxX;
    }

    public float getQRDirMinY()
    {
        return QRDirMinY;
    }

    public float getQRDirMaxY()
    {
        return QRDirMaxY;
    }

    public float getQRDirMinZ()
    {
        return QRDirMinZ;
    }

    public float getQRDirMaxZ()
    {
        return QRDirMaxZ;
    }
}
