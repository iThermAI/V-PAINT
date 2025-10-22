// ====================================================================
//  A4LogRotator.cs
//  خواندن مقدار A4 از لاگ و تنظیم Rotation Z
//      zu = a * zr + b
// ====================================================================

using UnityEngine;
using System;
using System.IO;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;

public class A4LogRotator : MonoBehaviour
{
    /*────────────[ Settings ]────────────*/
    [Header("Log File")]
    public string logFile = @"C:\Users\LabTR\Desktop\unity\full_realtime_log.log";

    [Header("Rotation Smoothing")]
    public float smoothTime = 0.8f;           // زمان هموارسازی زاویه

    [Header("Offline Control")]
    public int linesPerWheelStep = 50;        // چند خط به ازای هر اسکرول
    public float playbackInterval = 0.016f;   // ≈۶۰ هرتز

    [Header("Index")]
    public int indexCapacity = 2_000_000;     // حداکثر خطوط نگه‌داری‌شده

    [Header("A4  →  Rotation Z")]
    [Tooltip("a = scale (مثلاً 0.1 برای تبدیل به درجه)")]
    public float a_scale = 0.1f;
    [Tooltip("b = offset (درجه)")]
    public float b_offset = 0f;

    /*────────────[ Internal state ]────────────*/
    private float angleVelocity = 0f;
    private volatile float targetAngleZ;
    private bool firstValue = false;

    private readonly List<long> offsets = new();
    private int baseDiscard = 0;              // تعداد خطوطی که برای حفظ ظرفیت حذف شده‌اند

    private int offlineEndIdx;
    private int selectorIdx;
    private int playIdx;

    private Coroutine tailCo;
    private Coroutine playCo;

    private enum Mode { Live, OfflineSelecting, OfflinePlaying }
    private Mode mode = Mode.Live;

    private readonly CultureInfo cul = CultureInfo.InvariantCulture;

    /*────────────────── Start ──────────────────*/
    private void Start() => tailCo = StartCoroutine(Tail());

    /*───────────────── Update ─────────────────*/
    private void Update()
    {
        HandleKeys();
        HandleWheel();

        if (!firstValue) return;

        Vector3 eul = transform.eulerAngles;
        float nz = Mathf.SmoothDampAngle(eul.z, targetAngleZ, ref angleVelocity, smoothTime);
        transform.rotation = Quaternion.Euler(eul.x, eul.y, nz);

        if (mode == Mode.OfflineSelecting) ApplySelection();
    }

    /*───────── Input: Keys ─────────*/
    private void HandleKeys()
    {
        if (Input.GetKeyDown(KeyCode.O) && mode == Mode.Live)
            EnterOffline();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (mode == Mode.OfflineSelecting) StartPlayback();
            else if (mode == Mode.OfflinePlaying) PausePlayback();
        }

        if (Input.GetKeyDown(KeyCode.L))
            ReturnLive();
    }

    /*───────── Input: Wheel ─────────*/
    private void HandleWheel()
    {
        if (mode != Mode.OfflineSelecting) return;

        float d = Input.mouseScrollDelta.y;
        if (Mathf.Abs(d) < 0.01f) return;

        int step = Mathf.RoundToInt(-d * linesPerWheelStep);  // پایین = عقب
        selectorIdx = Mathf.Clamp(selectorIdx + step, 0, offlineEndIdx);
    }

    /*───────── Tail coroutine (live reading) ─────────*/
    private IEnumerator Tail()
    {
        while (!File.Exists(logFile))
            yield return null;

        using var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(fs);

        fs.Seek(0, SeekOrigin.End);
        long lastLen = fs.Length;

        while (true)
        {
            // اگر فایل روتیت یا پاک شد
            if (fs.Length < lastLen)
            {
                fs.Seek(0, SeekOrigin.Begin);
                sr.DiscardBufferedData();
                offsets.Clear();
                baseDiscard = 0;
            }

            long pos = fs.Position;
            string line = sr.ReadLine();
            if (line == null)
            {
                yield return null;
                lastLen = fs.Length;
                continue;
            }

            offsets.Add(pos);
            TrimIndexIfNeeded();

            if (mode == Mode.Live && TryParseA4(line, out float a4))
            {
                a4 = a4 * a_scale + b_offset;      // ★ xu = a*xr + b
                targetAngleZ = a4;
                firstValue = true;
            }

            lastLen = fs.Length;
        }
    }

    /*───────── Index trimming ─────────*/
    private void TrimIndexIfNeeded()
    {
        if (offsets.Count <= indexCapacity) return;

        int removeCount = offsets.Count - indexCapacity;
        offsets.RemoveRange(0, removeCount);
        baseDiscard += removeCount;
    }

    /*───────── Offline Selecting ─────────*/
    private void EnterOffline()
    {
        if (offsets.Count == 0) return;

        mode = Mode.OfflineSelecting;
        offlineEndIdx = offsets.Count - 1;
        selectorIdx = offlineEndIdx;
    }

    private void ApplySelection()
    {
        if (TryReadA4(selectorIdx, out float a4))
        {
            a4 = a4 * a_scale + b_offset;
            targetAngleZ = a4;
            firstValue = true;
        }
    }

    /*───────── Playback ─────────*/
    private void StartPlayback()
    {
        if (playCo != null) StopCoroutine(playCo);
        playIdx = selectorIdx;
        playCo = StartCoroutine(Playback());
    }

    private IEnumerator Playback()
    {
        mode = Mode.OfflinePlaying;

        while (playIdx <= offlineEndIdx)
        {
            if (TryReadA4(playIdx, out float a4))
            {
                a4 = a4 * a_scale + b_offset;
                targetAngleZ = a4;
                firstValue = true;
            }
            playIdx++;

            yield return new WaitForSecondsRealtime(playbackInterval);
            if (mode != Mode.OfflinePlaying) yield break;
        }

        selectorIdx = offlineEndIdx;
        mode = Mode.OfflineSelecting;
    }

    private void PausePlayback()
    {
        if (playCo != null) StopCoroutine(playCo);
        playCo = null;
        selectorIdx = Mathf.Clamp(playIdx, 0, offlineEndIdx);
        mode = Mode.OfflineSelecting;
    }

    /*───────── Return Live ─────────*/
    private void ReturnLive()
    {
        if (mode == Mode.Live) return;

        if (playCo != null) StopCoroutine(playCo);
        playCo = null;
        mode = Mode.Live;

        int lastIdx = offsets.Count - 1;
        if (lastIdx >= 0 && TryReadA4(lastIdx, out float a4))
        {
            a4 = a4 * a_scale + b_offset;
            targetAngleZ = a4;
        }
    }

    /*───────── Helpers: reading & parsing ─────────*/
    private bool TryReadA4(int idx, out float a4)
    {
        a4 = default;
        if (idx < 0 || idx >= offsets.Count) return false;

        using var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        fs.Seek(offsets[idx], SeekOrigin.Begin);
        using var sr = new StreamReader(fs);
        string line = sr.ReadLine();
        return TryParseA4(line, out a4);
    }

    private static bool TryParseA4(string raw, out float a4)
    {
        a4 = default;
        if (raw == null) return false;

        int sep = raw.IndexOf('>');
        if (sep < 0) return false;

        string data = raw[(sep + 1)..].TrimStart();
        string[] tok = data.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tok.Length > 3 &&                        // A4 چهارمین مقدار
               float.TryParse(tok[3], NumberStyles.Float,
                              CultureInfo.InvariantCulture, out a4);
    }
}
