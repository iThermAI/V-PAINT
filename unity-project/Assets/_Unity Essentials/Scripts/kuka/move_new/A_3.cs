using UnityEngine;
using System;
using System.IO;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// محور X مدل را مطابق ستون A3 فایل لاگ می‌چرخاند
/// </summary>
public class A3_RotateFromLog : MonoBehaviour
{
    [Tooltip("مسیر فایل لاگ (ستون A3)")]
    public string logFile =
        @"C:\\Users\\hojat\\OneDrive\\Desktop\\unity\\full_realtime_log.log";

    struct Record
    {
        public float time;    // ثانیه از شروع
        public float angle;   // درجهٔ هدف روی محور X (ستون A3)
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

        // چرخش اولیه
        transform.localRotation = Quaternion.Euler(records[0].angle,
                                                   transform.localRotation.eulerAngles.y,
                                                   transform.localRotation.eulerAngles.z);

        StartCoroutine(ReplayLog());
    }

    /* ──────────────── خواندن و تبدیل لاگ به لیست رکورد ─────────────── */
    private bool ParseLog()
    {
        if (!File.Exists(logFile)) return false;

        DateTime? baseTime = null;

        foreach (var raw in File.ReadLines(logFile))
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;

            int sep = raw.IndexOf('>');
            if (sep < 0) continue;

            string timeStr = raw[..sep].Trim();
            string dataStr = raw[(sep + 1)..].TrimStart();

            if (!DateTime.TryParseExact(timeStr,
                                        "HH:mm:ss.fff",
                                        CultureInfo.InvariantCulture,
                                        DateTimeStyles.None,
                                        out var ts))
                continue;

            string[] toks = dataStr.Split(' ',
                                  StringSplitOptions.RemoveEmptyEntries);

            if (toks.Length < 4) continue;            // ستون A3 وجود ندارد

            // ستون 3 = A3
            if (!float.TryParse(toks[3],
                                NumberStyles.Float,
                                CultureInfo.InvariantCulture,
                                out var a3))
                continue;

            baseTime ??= ts;
            float seconds = (float)(ts - baseTime.Value).TotalSeconds;
            records.Add(new Record { time = seconds, angle = a3 });
        }

        return records.Count > 1;
    }

    /* ─────────────── اجرای لاگ: چرخش یا صبر بر اساس رکوردها ────────────── */
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

    /* ───── چرخش نرم به زاویهٔ هدف طی زمان مشخص ───── */
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

    /* ــ زاویهٔ فعلی محور X را در بازهٔ ‎[-180°, 180°]‎ برمی‌گرداند */
    private float GetSignedLocalAngleX()
    {
        float a = transform.localRotation.eulerAngles.x;
        return (a > 180f) ? a - 360f : a;
    }
}
