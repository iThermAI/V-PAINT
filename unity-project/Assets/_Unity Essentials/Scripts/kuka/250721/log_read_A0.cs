// ============ A0LiveSmoothDamp.cs ============
using UnityEngine;
using System;
using System.IO;
using System.Globalization;
using System.Collections;

/// <summary>
/// Tail‑کردن زندهٔ فایل لاگ و حرکت فوق‌العاده نرم با SmoothDamp.
/// فقط آخرین A0 را نگه می‌دارد و در Update به سمتش حرکت می‌کند.
/// </summary>
public class A0LiveSmoothDamp : MonoBehaviour
{
    [Header("Log File")]
    public string logFile =
        @"C:\Users\hojat\OneDrive\Desktop\unity\full_realtime_log.log";

    [Header("Smoothing")]
    [Tooltip("زمان رسیدن تقریبی به هدف (ثانیه)")]
    public float smoothTime = 0.8f;      // هرچه بزرگ‌تر → حرکت نرم‌تر
    private float velocityX = 0f;

    /* آخرین مقصدی که از فایل گرفتیم */
    private volatile float targetX;
    private bool firstValue = false;

    /* ───────────────────────────── Start ───────────────────────────── */
    private void Start()
    {
        StartCoroutine(TailFileAndUpdateTarget());
    }

    /* ─────────────── Coroutine Tail ─────────────── */
    private IEnumerator TailFileAndUpdateTarget()
    {
        CultureInfo cul = CultureInfo.InvariantCulture;

        /* صبر تا فایل ساخته شود */
        while (!File.Exists(logFile))
            yield return null;

        using FileStream fs = new FileStream(
            logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using StreamReader sr = new StreamReader(fs);

        fs.Seek(0, SeekOrigin.End); // فقط خطوط جدید

        while (true)
        {
            string line = sr.ReadLine();
            if (line == null)
            {
                yield return null;
                continue;
            }

            if (TryParseA0(line, cul, out float a0))
            {
                targetX = a0;
                firstValue = true;
            }
        }
    }

    /* ─────────────── خواندن A0 از یک خط لاگ ─────────────── */
    private static bool TryParseA0(string raw, CultureInfo cul, out float a0)
    {
        a0 = default;
        int sep = raw.IndexOf('>');
        if (sep < 0) return false;

        string dataStr = raw[(sep + 1)..].TrimStart();
        string[] tok = dataStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tok.Length > 6 &&
               float.TryParse(tok[6], NumberStyles.Float, cul, out a0);
    }

    /* ─────────────── حرکت نرم در هر فریم ─────────────── */
    private void Update()
    {
        if (!firstValue) return; // هنوز مقداری نیامده

        Vector3 pos = transform.position;
        float newX = Mathf.SmoothDamp(pos.x, targetX, ref velocityX, smoothTime);
        transform.position = new Vector3(newX, pos.y, pos.z);
    }
}
