
using UnityEngine;
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.IO.Ports;
#endif

public class SerialLoggerReal : MonoBehaviour
{
    // مسیر ثابت لاگ
    //private const string logFilePath = @"C:\Users\hojat\OneDrive\Desktop\unity\full_realtime_log.log";
    private const string logFilePath = @"C:\Users\LabTR\Desktop\unity\full_realtime_log.log";

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    [Header("Serial Settings")]
    public string serialPortName = "COM3";
    public int baudRate = 115200;
    public bool enableHandshaking = false;

    private SerialPort serial;
#endif

    private StreamWriter logWriter;
    private FileStream logFileStream;  // ← برای Flush تا سطح OS
    private const int FileBufferSize = 4096;

    private void Start()
    {
        InitLogFile();

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        try
        {
            serial = new SerialPort(serialPortName, baudRate)
            {
                NewLine = "\r",
                ReadTimeout = 100
            };
            if (enableHandshaking)
            {
                serial.DtrEnable = true;
                serial.RtsEnable = true;
            }
            serial.Open();
            serial.ReadLine();             // ← خط اول را بخوان و دور بینداز
            Debug.Log($"✅ SerialLogger opened {serialPortName} @ {baudRate}");

            StartCoroutine(ReadLoop());
        }
        catch (Exception e) { Debug.LogError("❌ Serial open: " + e.Message); }
#else
        Debug.LogError("System.IO.Ports فقط روی ویندوز در دسترس است؛ فقط لاگ فایل ایجاد می‌شود.");
#endif
    }

    private void InitLogFile()
    {
        try
        {
            string dir = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            logFileStream = new FileStream(
                logFilePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite,     // اجازهٔ خواندن هم‌زمان
                FileBufferSize,
                FileOptions.WriteThrough // کم‌ترین تأخیرِ سیستمی
            );

            // بافر کوچک، چون هر خط را بلافاصله Flush می‌کنیم
            logWriter = new StreamWriter(logFileStream, Encoding.UTF8, 128);
            Debug.Log($"📄 Serial log file: {logFilePath}");
        }
        catch (Exception ex) { Debug.LogError("❌ Log init: " + ex.Message); }
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private IEnumerator ReadLoop()
    {
        var culture = CultureInfo.InvariantCulture;

        while (true)
        {
            string line = null;
            try { line = serial.ReadLine(); }
            catch (TimeoutException) { }
            catch (Exception ex) { Debug.LogWarning("Serial read: " + ex.Message); }

            if (!string.IsNullOrWhiteSpace(line))
                WriteParsedLine(line.Trim(), culture);

            yield return null;   // یک فریم صبر
        }
    }
#endif

    private void WriteParsedLine(string incoming, CultureInfo culture)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff", culture);

        var tok = incoming.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tok.Length >= 8)
        {
            float[] vals = new float[8];
            bool ok = true;

            for (int i = 0; i < 8; i++)
            {
                if (int.TryParse(tok[i], NumberStyles.Integer, culture, out int v))
                    vals[i] = v / 100f;
                else { ok = false; break; }
            }

            if (ok)
            {
                var sb = new StringBuilder(128);
                sb.Append(timestamp).Append("> ");
                for (int i = 0; i < 8; i++)
                {
                    sb.Append(vals[i].ToString("0.00", culture));
                    if (i < 7) sb.Append(' ');
                }
                SafeWriteLine(sb.ToString());
                return;
            }
        }

        // اگر پارس نشد، همان خط خام را ذخیره کن
        SafeWriteLine($"{timestamp}> {incoming}");
    }

    private void SafeWriteLine(string text)
    {
        if (logWriter == null) return;

        try
        {
            logWriter.WriteLine(text);
            logWriter.Flush();           // خالی‌کردن بافر StreamWriter
            logFileStream.Flush();       // خالی‌کردن بافر FileStream/OS
        }
        catch (Exception ex) { Debug.LogWarning("Log write: " + ex.Message); }
    }

    private void OnApplicationQuit() => Cleanup();
    private void OnDestroy() => Cleanup();

    private void Cleanup()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (serial != null)
        {
            try { if (serial.IsOpen) serial.Close(); }
            catch (Exception ex) { Debug.LogWarning("Serial close: " + ex.Message); }
            serial = null;
        }
#endif
        CloseLog();
    }

    private void CloseLog()
    {
        if (logWriter != null)
        {
            try
            {
                logWriter.Flush();
                logFileStream.Flush();
                logWriter.Close();
                logFileStream.Close();
            }
            catch (Exception ex) { Debug.LogWarning("Log close: " + ex.Message); }
            logWriter = null;
            logFileStream = null;
        }
    }
}

