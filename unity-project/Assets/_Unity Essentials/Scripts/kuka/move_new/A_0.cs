using UnityEngine;
using System;
using System.IO;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;

public class MoveFromLog : MonoBehaviour
{
    [Tooltip("مسیر فایل لاگ (ستون A0)")]
    public string logFile =
        @"C:\\Users\\hojat\\OneDrive\\Desktop\\unity\\full_realtime_log.log";

    struct Record
    {
        public float time;   // ثانیه از شروع
        public float a0;     // موقعیت هدف روی محور X
    }

    private readonly List<Record> records = new();

    /* ───────────────────────────── Start ───────────────────────────── */
    private void Start()
    {
        if (!ParseLog())
        {
            Debug.LogError("Log parse failed – فایل پیدا نشد یا داده نامعتبر بود.");
            return;
        }

        // شیء را روی اولین مقدار A0 می‌گذاریم و پخش لاگ را استارت می‌کنیم
        transform.position = new Vector3(records[0].a0,
                                         transform.position.y,
                                         transform.position.z);

        StartCoroutine(ReplayLog());
    }

    /* ──────────────── خواندن و تبدیل لاگ به لیست رکورد ─────────────── */
    private bool ParseLog()
    {
        if (!File.Exists(logFile))
            return false;

        DateTime? baseTime = null;

        foreach (var raw in File.ReadLines(logFile))
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;

            int sep = raw.IndexOf('>');
            if (sep < 0) continue;               // «>» پیدا نشد

            // ❶ بخش زمان  (قبل از >)
            string timeStr = raw[..sep].Trim();

            // ❷ بعد از «>» یک فاصله دارد؛ TrimStart فضای اضافی را حذف می‌کند
            string dataStr = raw[(sep + 1)..].TrimStart();

            // زمان را بخوان
            if (!DateTime.TryParseExact(timeStr,
                                        "HH:mm:ss.fff",
                                        CultureInfo.InvariantCulture,
                                        DateTimeStyles.None,
                                        out var ts))
                continue;                        // فرمت غلط

            // ستون‌ها را جدا کن
            var toks = dataStr.Split(' ',
                         StringSplitOptions.RemoveEmptyEntries);

            if (toks.Length == 0) continue;

            // ستون اول بعد از زمان  ↔  A0
            if (!float.TryParse(toks[6],
                                NumberStyles.Float,
                                CultureInfo.InvariantCulture,
                                out var a0))
                continue;                        // مقدار غیرعددی

            baseTime ??= ts;                     // زمان صفر لاگ

            float seconds = (float)(ts - baseTime.Value).TotalSeconds;
            records.Add(new Record { time = seconds, a0 = a0 });
        }

        // حداقل دو رکورد برای حرکت لازم است
        return records.Count > 1;
    }


    /* ─────────────── اجرای لاگ: حرکت یا صبر بر اساس رکوردها ────────────── */
    private IEnumerator ReplayLog()
    {
        for (int i = 1; i < records.Count; i++)
        {
            float duration = records[i].time - records[i - 1].time;
            float targetX = records[i].a0;

            // اگر مقدار A0 همان قبلی است فقط معطل می‌شویم
            if (Mathf.Approximately(targetX, transform.position.x))
            {
                if (duration > 0f) yield return new WaitForSeconds(duration);
            }
            else
            {
                // در مدت مشخص بین دو مقدار A0، خطی جابجا می‌شویم
                yield return MoveToPosition(targetX, Mathf.Max(0.01f, duration));
            }
        }
    }

    /* ───── عین متد اصلی اما بدون «speed» چون زمان در لاگ تعیین شده ───── */
    private IEnumerator MoveToPosition(float targetX, float duration)
    {
        float startX = transform.position.x;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float newX = Mathf.Lerp(startX, targetX, t);
            transform.position = new Vector3(newX,
                                             transform.position.y,
                                             transform.position.z);
            yield return null;
        }

        // اطمینان از رسیدن دقیق به مقصد
        transform.position = new Vector3(targetX,
                                         transform.position.y,
                                         transform.position.z);
    }
}
