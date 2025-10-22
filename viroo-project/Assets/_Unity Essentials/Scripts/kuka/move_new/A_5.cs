using UnityEngine;
using System;
using System.IO;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// چرخش لوکال حول محور X بر اساس ستون A5 فایل لاگ
/// ستون‌ها:  A0 A1 A2 A3 A4 A5 A6 flag   →   A5 = ایندکس 5 از صفر
/// </summary>
public class A5_RotateFromLog : MonoBehaviour
{
    [Tooltip("مسیر فایل لاگ (ستون A5)")]
    public string logFile =
        @"C:\\Users\\hojat\\OneDrive\\Desktop\\unity\\full_realtime_log.log";

    struct Record
    {
        public float time;   // ثانیه از شروع
        public float angle;  // زاویه هدف روی X (A5)
    }

    private readonly List<Record> records = new();

    /* ───────────────────────────── Start ───────────────────────────── */
    private void Start()
    {
        if (!ParseLog())
        {
            Debug.LogError("Log parse failed – فایل پیدا نشد یا داده نامعتبر بود.");
            return;
        }

        // چرخش آغازین
        transform.localRotation = Quaternion.Euler(records[0].angle,
                                                   transform.localRotation.eulerAngles.y,
                                                   transform.localRotation.eulerAngles.z);

        StartCoroutine(ReplayLog());
    }

    /* ──────────────── خواندن فایل و استخراج ستون A5 ─────────────── */
    private bool ParseLog()
    {
        if (!File.Exists(logFile)) return false;

        DateTime? baseTime = null;

        foreach (var raw in File.ReadLines(logFile))
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;

            int sep = raw.IndexOf('>');
            if (sep < 0) continue;                             // «>» نیافتیم

            string timeStr = raw[..sep].Trim();
            string dataStr = raw[(sep + 1)..].TrimStart();     // حذف فاصله بعد >

            if (!DateTime.TryParseExact(timeStr,
                                        "HH:mm:ss.fff",
                                        CultureInfo.InvariantCulture,
                                        DateTimeStyles.None,
                                        out var ts))
                continue;                                      // ساعت نامعتبر

            var toks = dataStr.Split(' ',
                         StringSplitOptions.RemoveEmptyEntries);

            if (toks.Length < 6) continue;                    // A5 موجود نیست

            if (!float.TryParse(toks[5],                       // ایندکس 5 = A5
                                NumberStyles.Float,
                                CultureInfo.InvariantCulture,
                                out var a5))
                continue;

            baseTime ??= ts;

            float seconds = (float)(ts - baseTime.Value).TotalSeconds;
            records.Add(new Record { time = seconds, angle = a5 });
        }

        return records.Count > 1;
    }

    /* ─────────────── اجرای لاگ؛ چرخش یا صبر ─────────────── */
    private IEnumerator ReplayLog()
    {
        for (int i = 1; i < records.Count; i++)
        {
            float duration = records[i].time - records[i - 1].time;
            float targetAngle = records[i].angle;

            if (Mathf.Approximately(targetAngle, GetSignedLocalAngleX()))
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
        float startAngle = GetSignedLocalAngleX();
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float angle = Mathf.LerpAngle(startAngle, targetAngle, t);
            transform.localRotation = Quaternion.Euler(angle,
                                                       transform.localRotation.eulerAngles.y,
                                                       transform.localRotation.eulerAngles.z);
            yield return null;
        }

        transform.localRotation = Quaternion.Euler(targetAngle,
                                                   transform.localRotation.eulerAngles.y,
                                                   transform.localRotation.eulerAngles.z);
    }

    /* زاویهٔ فعلی محور X در بازهٔ ‎[-180°,180°]‎ */
    private float GetSignedLocalAngleX()
    {
        float a = transform.localRotation.eulerAngles.x;
        return (a > 180f) ? a - 360f : a;
    }
}
