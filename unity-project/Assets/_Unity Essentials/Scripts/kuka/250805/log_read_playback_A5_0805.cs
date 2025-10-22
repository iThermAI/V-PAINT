// ====================================================================
//  A5LogRotator.cs  –  نسخهٔ نهایی بدون پرش و بدون تغییر Scale
//      xu = a * xr + b
// ====================================================================

using UnityEngine;
using System;
using System.IO;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;

public class A5LogRotator0805 : MonoBehaviour
{
    /*──────── تنظیمات ────────*/
    [Header("Log File")]
    private string logFile = @"C:\Users\LabTR\Desktop\unity\full_realtime_log.log";

    [Header("Rotation Smoothing")]
    public float smoothTime = 0.8f;           // زمان هموارسازی زاویه

    [Header("Offline Control")]
    public int linesPerWheelStep = 50;        // چند خط به ازای هر اسکرول
    public float playbackInterval = 0.016f;   // ≈۶۰ هرتز

    [Header("Index")]
    public int indexCapacity = 2_000_000;     // حداکثر خطوط نگه‌داری‌شده

    [Header("A5  →  Rotation X")]
    public float a_scale = 1f;                // ضریب تبدیل
    public float b_offset = 0f;               // افست (درجه)

    /*──────── وضعیت داخلی ────────*/
    private float currentAngleX;              // زاویهٔ هموارشدهٔ داخلی
    private float angleVelocity = 0f;
    private volatile float targetAngleX;
    private bool firstValue = false;

    // محورهای اولیهٔ Y و Z را نگه می‌داریم تا دست نخورند
    private float initialLocalY;
    private float initialLocalZ;

    private readonly List<long> offsets = new();
    private int baseDiscard = 0;

    private int offlineEndIdx;
    private int selectorIdx;
    private int playIdx;

    private Coroutine tailCo;
    private Coroutine playCo;

    private enum Mode { Live, OfflineSelecting, OfflinePlaying }
    private Mode mode = Mode.Live;

    private readonly CultureInfo cul = CultureInfo.InvariantCulture;

    /*────────────────── Start ──────────────────*/
    private void Start()
    {
        // مقدار اولیهٔ اویلر محلی
        Vector3 init = transform.localEulerAngles;
        currentAngleX = init.x;
        initialLocalY = init.y;
        initialLocalZ = init.z;

        tailCo = StartCoroutine(Tail());
    }

    /*───────────────── Update ─────────────────*/
    private void Update()
    {
        HandleKeys();
        HandleWheel();

        if (!firstValue) return;

        // هموارسازی فقط روی محور X
        currentAngleX = Mathf.SmoothDampAngle(
            currentAngleX,
            targetAngleX,
            ref angleVelocity,
            smoothTime);

        // حفظِ Y و Z اولیه
        transform.localRotation = Quaternion.Euler(
            currentAngleX,
            initialLocalY,
            initialLocalZ);

        if (mode == Mode.OfflineSelecting) ApplySelection();
    }

    /*───────── Input: Keys ─────────*/
    private void HandleKeys()
    {
        if (Input.GetKeyDown(KeyCode.O) && mode == Mode.Live) EnterOffline();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (mode == Mode.OfflineSelecting) StartPlayback();
            else if (mode == Mode.OfflinePlaying) PausePlayback();
        }

        if (Input.GetKeyDown(KeyCode.L)) ReturnLive();
    }

    /*───────── Input: Wheel ─────────*/
    private void HandleWheel()
    {
        if (mode != Mode.OfflineSelecting) return;

        float d = Input.mouseScrollDelta.y;
        if (Mathf.Abs(d) < 0.01f) return;

        int step = Mathf.RoundToInt(-d * linesPerWheelStep);
        selectorIdx = Mathf.Clamp(selectorIdx + step, 0, offlineEndIdx);
    }

    /*───────── Tail coroutine ─────────*/
    private IEnumerator Tail()
    {
        while (!File.Exists(logFile)) yield return null;

        using var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(fs);

        fs.Seek(0, SeekOrigin.End);
        long lastLen = fs.Length;

        while (true)
        {
            if (fs.Length < lastLen)            // چرخش یا پاک‌شدن فایل
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

            if (mode == Mode.Live && TryParseA5(line, out float a5))
            {
                a5 = a5 * a_scale + b_offset;
                targetAngleX = a5;
                firstValue = true;
            }

            lastLen = fs.Length;
        }
    }

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
        if (TryReadA5(selectorIdx, out float a5))
        {
            a5 = a5 * a_scale + b_offset;
            targetAngleX = a5;
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
            if (TryReadA5(playIdx, out float a5))
            {
                a5 = a5 * a_scale + b_offset;
                targetAngleX = a5;
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
        if (lastIdx >= 0 && TryReadA5(lastIdx, out float a5))
        {
            a5 = a5 * a_scale + b_offset;
            targetAngleX = a5;
        }
    }

    /*───────── Helper: read A5 ─────────*/
    private bool TryReadA5(int idx, out float a5)
    {
        a5 = default;
        if (idx < 0 || idx >= offsets.Count) return false;

        using var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        fs.Seek(offsets[idx], SeekOrigin.Begin);
        using var sr = new StreamReader(fs);
        string line = sr.ReadLine();
        return TryParseA5(line, out a5);
    }

    /*───────── Parse line ─────────*/
    private static bool TryParseA5(string raw, out float a5)
    {
        a5 = default;
        if (raw == null) return false;

        int sep = raw.IndexOf('>');
        if (sep < 0) return false;

        string data = raw[(sep + 1)..].TrimStart();
        string[] tok = data.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tok.Length > 4 &&                        // A5 پنجمین مقدار
               float.TryParse(tok[4], NumberStyles.Float,
                              CultureInfo.InvariantCulture, out a5);
    }
}
