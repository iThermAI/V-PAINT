using UnityEngine;
using System;
using System.IO;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// محور X مدل را مطابق ستون A2 فایل لاگ می‌چرخاند
/// </summary>
public class A2_RotateFromLog : MonoBehaviour
{
    [Tooltip("مسیر فایل لاگ (ستون A2)")]
    public string logFile =
        @"C:\\Users\\hojat\\OneDrive\\Desktop\\unity\\full_realtime_log.log";

    struct Record
    {
        public float time;   // ثانیه از شروع
        public float angle;  // درجهٔ هدف روی محور X (از ستون A2)
    }

    private readonly List<Record> records = new();

    /* ─────────────────────────────ــ Start ــ──────────────────────────── */
    private void Start()
    {
        if (!ParseLog())
        {
            Debug.LogError("Log parse failed – فایل پیدا نشد یا داده نامعتبر بود.");
            return;
        }

        // Rotation را روی اولین زاویه تنظیم می‌کنیم
        transform.rotation = Quaternion.Euler(records[0].angle,
                                              transform.rotation.eulerAngles.y,
                                              transform.rotation.eulerAngles.z);

        StartCoroutine(ReplayLog());
    }

    /* ─────────────── خواندن فایل و استخراج ستون A2 ─────────────── */
    private bool ParseLog()
    {
        if (!File.Exists(logFile)) return false;

        DateTime? baseTime = null;

        foreach (var raw in File.ReadLines(logFile))
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;

            int sep = raw.IndexOf('>');
            if (sep < 0) continue;                     // «>» وجود ندارد

            string timeStr = raw[..sep].Trim();        // قسمت ساعت
            string dataStr = raw[(sep + 1)..].TrimStart(); // بعد از «>» (ستون‌ها)

            if (!DateTime.TryParseExact(timeStr,
                                        "HH:mm:ss.fff",
                                        CultureInfo.InvariantCulture,
                                        DateTimeStyles.None,
                                        out var ts))
                continue;                              // ساعت نامعتبر

            string[] toks = dataStr.Split(' ',
                                  StringSplitOptions.RemoveEmptyEntries);

            if (toks.Length < 3) continue;            // حداقل تا ستون A2 لازم است

            if (!float.TryParse(toks[1],               // ستون 0=A0, 1=A1, 2=A2
                                NumberStyles.Float,
                                CultureInfo.InvariantCulture,
                                out var a2))
                continue;                              // مقدار غیرعددی

            baseTime ??= ts;                           // زمان مبنا

            float seconds = (float)(ts - baseTime.Value).TotalSeconds;
            records.Add(new Record { time = seconds, angle = a2 });
        }

        return records.Count > 1;
    }

    /* ─────────────── پخش لاگ و چرخش بر اساس زمان‌بندی ─────────────── */
    private IEnumerator ReplayLog()
    {
        for (int i = 1; i < records.Count; i++)
        {
            float duration = records[i].time - records[i - 1].time;
            float targetAngle = records[i].angle;

            // اگر زاویه عوض نشده فقط صبر می‌کنیم
            if (Mathf.Approximately(targetAngle, GetSignedAngleX()))
            {
                if (duration > 0f) yield return new WaitForSeconds(duration);
            }
            else
            {
                yield return RotateToAngle(targetAngle,
                                           Mathf.Max(0.01f, duration));
            }
        }
    }

    /* ───── چرخش نرم به زاویهٔ هدف طی مدت مشخص ───── */
    private IEnumerator RotateToAngle(float targetAngle, float duration)
    {
        float startAngle = GetSignedAngleX();         // در بازهٔ ‎[-180,180]‎
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float angle = Mathf.LerpAngle(startAngle, targetAngle, t);
            transform.rotation = Quaternion.Euler(angle,
                                                  transform.rotation.eulerAngles.y,
                                                  transform.rotation.eulerAngles.z);
            yield return null;
        }

        transform.rotation = Quaternion.Euler(targetAngle,
                                              transform.rotation.eulerAngles.y,
                                              transform.rotation.eulerAngles.z);
    }

    /* کمک: زاویهٔ فعلی محور X را در محدودهٔ ‎[-180,180]‎ برمی‌گرداند */
    private float GetSignedAngleX()
    {
        float a = transform.rotation.eulerAngles.x;
        return (a > 180f) ? a - 360f : a;
    }
}
