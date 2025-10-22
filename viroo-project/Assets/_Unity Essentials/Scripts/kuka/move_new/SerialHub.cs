/*
using UnityEngine;
using System;
using System.Collections;
using System.Globalization;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.IO.Ports;
#endif

public class SerialHub : MonoBehaviour
{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    [Header("Serial Settings")]
    public string serialPortName = "COM2";
    public int baudRate = 115200;
    public bool enableHandshaking = false;
#endif

    public static SerialHub Instance { get; private set; }

    public readonly int[] raw = new int[8]; // A1..A0 + flag
    public readonly float[] scaled = new float[8];

    /// <summary>FALSE تا نخستین خطِ معتبر دریافت شود.</summary>
    public bool isDataReady { get; private set; } = false;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private SerialPort serial;
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private void Start()
    {
        try
        {
            serial = new SerialPort(serialPortName, baudRate)
            {
                NewLine = "\n",
                ReadTimeout = 100
            };
            if (enableHandshaking)
            {
                serial.DtrEnable = true;
                serial.RtsEnable = true;
            }
            serial.Open();
            Debug.Log($"✅ SerialHub opened {serialPortName} @ {baudRate}");
            StartCoroutine(ReadLoop());
        }
        catch (Exception e)
        {
            Debug.LogError("❌ SerialHub: " + e.Message);
        }
    }

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
            {
                var tok = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (tok.Length >= 8)
                {
                    bool allParsed = true;
                    for (int i = 0; i < 8; i++)
                    {
                        if (int.TryParse(tok[i], NumberStyles.Integer, culture, out int v))
                        {
                            raw[i] = v;
                            scaled[i] = v / 100f;
                        }
                        else allParsed = false;
                    }
                    if (allParsed && !isDataReady) isDataReady = true; // نخستین خطِ سالم
                }
            }
            yield return null;
        }
    }

    private void OnDestroy()
    {
        if (serial != null && serial.IsOpen) serial.Close();
    }
#else
    private void Start() =>
        Debug.LogError("System.IO.Ports فقط روی ویندوز دسکتاپ در دسترس است.");
#endif
}


*/ 


using UnityEngine;
using System;
using System.Collections;
using System.Globalization;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.IO.Ports;
#endif

public class SerialHub : MonoBehaviour
{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    [Header("Serial Settings")]
    public string serialPortName = "COM3";
    public int baudRate = 115200;
    public bool enableHandshaking = false;
#endif

    public static SerialHub Instance { get; private set; }

    public readonly int[] raw = new int[8];   // A1..A0 + flag
    public readonly float[] scaled = new float[8]; // مقیاس‌شده (÷100)

    /// <summary>پس از نخستین فریم سالم True می‌شود.</summary>
    public bool isDataReady { get; private set; } = false;
    /// <summary>زمان لحظهٔ ورود آخرین فریم (Time.time).</summary>
    public float lastPacketTime { get; private set; } = 0f;
    /// <summary>فاصلهٔ زمانی بین دو فریم پیاپی (ثانیه).</summary>
    public float deltaPacketTime { get; private set; } = 0.1f;   // مقدار پیش‌فرض

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private SerialPort serial;
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private void Start()
    {
        try
        {
            serial = new SerialPort(serialPortName, baudRate)
            {
                NewLine = "\n",
                ReadTimeout = 100
            };
            if (enableHandshaking)
            {
                serial.DtrEnable = true;
                serial.RtsEnable = true;
            }
            serial.Open();
            Debug.Log($"✅ SerialHub opened {serialPortName} @ {baudRate}");
            StartCoroutine(ReadLoop());
        }
        catch (Exception e)
        {
            Debug.LogError("❌ SerialHub: " + e.Message);
        }
    }

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
            {
                var tok = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (tok.Length >= 8)
                {
                    bool allParsed = true;
                    for (int i = 0; i < 8; i++)
                    {
                        if (int.TryParse(tok[i], NumberStyles.Integer, culture, out int v))
                        {
                            raw[i] = v;
                            scaled[i] = v / 100f;
                        }
                        else allParsed = false;
                    }

                    if (allParsed)
                    {
                        // محاسبهٔ زمان دریافت
                        float now = Time.time;
                        if (lastPacketTime > 0f)
                            deltaPacketTime = now - lastPacketTime;     // فاصله با فریم قبلی
                        lastPacketTime = now;

                        if (!isDataReady) isDataReady = true;          // نخستین فریم سالم
                    }
                }
            }
            yield return null;
        }
    }

    private void OnDestroy()
    {
        if (serial != null && serial.IsOpen) serial.Close();
    }
#else
    private void Start() =>
        Debug.LogError("System.IO.Ports فقط روی ویندوز دسکتاپ در دسترس است.");
#endif
}


