using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

public class LogRecorder
{
    public readonly StreamWriter writer;
    public readonly string name;
    public readonly bool isCSV;

    public LogRecorder(string name, string ext = "log")
    {
        this.name = name;
        isCSV = ext == "csv";

        Debug.Log(LogRecorderCtrl.instance.userDataPath + "/" + name + "." + ext);

        writer = new StreamWriter(LogRecorderCtrl.instance.userDataPath + "/" + name + "." + ext);
    }

    ~LogRecorder()
    {
        Close();
    }

    public void Write(string txt, bool debug = true)
    {
        if (debug)
            Debug.Log(txt);

        if (writer.BaseStream.CanWrite)
        {
            if (!isCSV)
            {
                txt = $"{Utils.GetTimeStamp()}: {txt}";
            }
            writer.WriteLine(txt);
        }
    }

    public void Flush()
    {
        if (writer.BaseStream.CanWrite)
        {
            writer.Flush();
        }
    }

    public void Close()
    {
        writer.Flush();
        writer.Close();
    }
}

public class LogRecorderCtrl
{
    public static LogRecorderCtrl instance { private set; get; }

    private static string m_basePath;
    private static string m_userdataPath;

    private ProtocolManager _protocolMngr;
    private UserInteract _userInteract;

    public string userDataPath => m_userdataPath;
    public static int userId = -1;

    public Dictionary<string, LogRecorder> writerDict;

    public LogRecorderCtrl()
    {
        instance = this;
        _protocolMngr = ProtocolManager.instance;

        // Hardcoded because master node instance is actually in a cache folder
        m_basePath = "./build/SubjectData";
        if (!Directory.Exists(m_basePath)) Directory.CreateDirectory(m_basePath);

        // userId = 10; // Hard setting user number in case of crash, to restart

        // Get last user number
        if (userId < 0)
        {
            for (int iSubj = 0; iSubj < 150; iSubj++)
            {
                if (!Directory.Exists($"{m_basePath}/Subj_{iSubj}"))
                {
                    userId = iSubj;
                    break;
                }
            }
        }

        m_userdataPath = m_basePath + "/Subj_" + userId;

        // Create new folder with subject ID
        Directory.CreateDirectory(m_userdataPath);

        writerDict = new Dictionary<string, LogRecorder>();
    }

    ~LogRecorderCtrl()
    {
        List<string> keys = writerDict.Keys.ToList();
        for (int ik = 0; ik < keys.Count; ik++)
        {
            string key = keys[ik];
            writerDict[key].Close();
            writerDict.Remove(key);
        }
    }

    public void FlushAll()
    {
        foreach (var kv in writerDict)
        {
            // Debug.Log(kv.Key);
            if (kv.Value == null || kv.Value.writer == null)
            {
                continue;
            }

            kv.Value.Flush();
        }
    }

    public void CloseAll()
    {
        foreach (var kv in writerDict)
        {
            // Debug.Log(kv.Key);
            if (kv.Value == null || kv.Value.writer == null)
            {
                continue;
            }

            kv.Value.Close();
        }
    }

    public void SetSubjFolder()
    {
        // If this user already exists: start after last trial
        if (Directory.Exists(m_userdataPath))
        {
            int count = Directory.GetFiles(m_userdataPath, "*.log", SearchOption.AllDirectories).Length;

            // Rename userdata file before creating a new one
            if (File.Exists(m_userdataPath + "/UserData.txt"))
            {
                File.Move(m_userdataPath + "/UserData.txt", m_userdataPath + $"/UserData_{Utils.GetTimeStamp()}.txt");
            }

            if (count > 1)
            {
                // ?? recoverSession ??
            }
        }
    }

    public void NewDatarecord(string name, string type)
    {
        if (writerDict.ContainsKey(type))
        {
            writerDict[type].Close();
        }

        writerDict[type] = new LogRecorder(name + "_" + type, "csv");
    }

    public void Newrecord(string name)
    {
        if (writerDict.ContainsKey(name))
        {
            writerDict[name].Close();
        }

        writerDict[name] = new LogRecorder(name);
    }

    public void StartNewRecord(string trialName = "")
    {
        if (!_userInteract) { _userInteract = UserInteract.instance; }

        if (trialName == string.Empty)
        {
            trialName = _protocolMngr.GetCurrentTrialId();
        }
        Debug.Log($"StartNewRecord called \"{trialName}\"");

        NewDatarecord(trialName, "ET");
        writerDict["ET"].Write(
            "ETtimestamp,UnityTimeStamp," +
            "cameraPosition.x,cameraPosition.y,cameraPosition.z," +
            "cameraRotation.x,cameraRotation.y,cameraRotation.z,cameraRotation.w," +
            "meanBasePoint.x,meanBasePoint.y,meanBasePoint.z," +
            "meanGazeDirection.x,meanGazeDirection.y,meanGazeDirection.z," +
            "leftBasePoint.x,leftBasePoint.y,leftBasePoint.z," +
            "rightBasePoint.x,rightBasePoint.y,rightBasePoint.z," +
            "leftGazeDirection.x,leftGazeDirection.y,leftGazeDirection.z," +
            "rightGazeDirection.x,rightGazeDirection.y,rightGazeDirection.z," +
            "vergencePt.pos.x,vergencePt.pos.y,vergencePt.pos.z," +
            "vergenceDistL,vergenceDistR," +
            "valC,valL,valR", false);

        NewDatarecord(trialName, "PD");
        writerDict["PD"].Write("OcutimeStamp,leftPupilDiam,righttPupilDiam", false);

        NewDatarecord(trialName, "HMD");
        string HMDheader = "OcutimeStamp,UnityTimeStamp,";

        string name;
        foreach (Transform trans in new[] {
            ProtocolManager.userHead,
            _userInteract.Torso, _userInteract.Leg, _userInteract.Hand,
            ProtocolManager.instance.currentTestRunner.GetTargetTransform() })
        {
            name = trans.name;
            HMDheader +=
                $"{name}.pos.x,{name}.pos.y,{name}.pos.z," +
                $"{name}.rot.x,{name}.rot.y,{name}.rot.z,{name}.rot.w,";
        }

        writerDict["HMD"].Write(HMDheader, false);

        EyetrackerCtrl.instance.isSampling = true;

        ProtocolManager.InfoLogger.Write("[LRC] Started new record: " + trialName);
    }

    public void StopRecord(long elapsedtime)
    {
        EyetrackerCtrl.instance.isSampling = false;

        ProtocolManager.InfoLogger.Write("[LRC] Stopped record: " + writerDict["ET"].name);
        ProtocolManager.InfoLogger.Write($"[LRC] Elapsed time: {ProtocolManager.instance.currentTestRunner.GetElapsedTime()}");

        writerDict["ET"].Close();
        writerDict["PD"].Close();
        writerDict["HMD"].Close();

        writerDict.Remove("ET");
        writerDict.Remove("PD");
        writerDict.Remove("HMD");
    }
}
