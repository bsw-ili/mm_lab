using System.IO;
using UnityEngine;

public class UnityLogToFile : MonoBehaviour
{
    private string logPath;
    private StreamWriter writer;

    void OnEnable()
    {
        logPath = Path.Combine(Application.dataPath, "unityLog.txt");
        writer = new StreamWriter(logPath, true); // ×·¼ÓÄ£Ê½
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
        writer.Close();
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        writer.WriteLine(System.DateTime.Now.ToString("HH:mm:ss") + " [" + type + "] " + logString);
        writer.Flush();
    }
}
