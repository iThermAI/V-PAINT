// ====================================================================
//  A6LogRotator.cs  –  نسخهٔ اصلاح‌شده بدون پرش و بدون تغییر Scale
//  خواندن مقدار A6 از لاگ و تنظیم Rotation Z
//      zu = a * zr + b
// ====================================================================

using UnityEngine;
using System;
using System.IO;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;

public class A6LogRotator0805 : MonoBehaviour
{
    /*───────────[ Inspector ]───────────*/
    [Header("Log File")]
    private string logFile = @"C:\Users\LabTR\Desktop\unity\full_realtime_log.log";

    [Header("Rotation Smoothing")]
    public float smoothTime = 0.8f;

    [Header("Offline Control")]
    public int linesPerWheelStep = 50;
    public float playbackInterval = 0.016f;   // ≈60 Hz

    [Header("Index")]
    public int indexCapacity = 2_000_000;

    [Header("A6  →  Rotation Z")]
    public float a_scale = 0.1f;              // ضریب تبدیل
    public float b_offset = 0f;               // افست (درجه)

    /*───────────[ State ]───────────*/
    private float currentAngleZ;              // زاویهٔ هموارشدهٔ داخلی
    private float angleVelocity;
    private volatile float targetAngleZ;
    private bool firstValue;

    // محورهای اولیهٔ X و Y را نگه می‌داریم
    private float initialLocalX;
    private float initialLocalY;

    private readonly List<long> offsets = new();
    private int baseDiscard;

    private int offlineEndIdx;
    private int selectorIdx;
    private int playIdx;

    private Coroutine tailCo;
    private Coroutine playCo;

    private enum Mode { Live, OfflineSelecting, OfflinePlaying }
    private Mode mode = Mode.Live;

    /*────────────────── Start ──────────────────*/
    private void Start()
    {
        Vector3 init = transform.localEulerAngles;
        initialLocalX = init.x;
        initialLocalY = init.y;
        currentAngleZ = init.z;

        tailCo = StartCoroutine(Tail());
    }

    /*───────────────── Update ─────────────────*/
    private void Update()
    {
        HandleKeys();
        HandleWheel();

        if (!firstValue) return;

        // هموارسازی فقط روی محور Z
        currentAngleZ = Mathf.SmoothDampAngle(
            currentAngleZ,
            targetAngleZ,
            ref angleVelocity,
            smoothTime);

        // حفظ X و Y اولیه
        transform.localRotation = Quaternion.Euler(
            initialLocalX,
            initialLocalY,
            currentAngleZ);

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
            if (fs.Length < lastLen)                     // log rotated
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
            TrimIndex();

            if (mode == Mode.Live && TryParseA6(line, out float a6))
            {
                targetAngleZ = a6 * a_scale + b_offset;
                firstValue = true;
            }

            lastLen = fs.Length;
        }
    }

    private void TrimIndex()
    {
        if (offsets.Count <= indexCapacity) return;

        int remove = offsets.Count - indexCapacity;
        offsets.RemoveRange(0, remove);
        baseDiscard += remove;
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
        if (TryReadA6(selectorIdx, out float a6))
        {
            targetAngleZ = a6 * a_scale + b_offset;
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
            if (TryReadA6(playIdx, out float a6))
            {
                targetAngleZ = a6 * a_scale + b_offset;
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

        int last = offsets.Count - 1;
        if (last >= 0 && TryReadA6(last, out float a6))
            targetAngleZ = a6 * a_scale + b_offset;
    }

    /*───────── Helpers ─────────*/
    private bool TryReadA6(int idx, out float a6)
    {
        a6 = default;
        if (idx < 0 || idx >= offsets.Count) return false;

        using var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        fs.Seek(offsets[idx], SeekOrigin.Begin);
        using var sr = new StreamReader(fs);
        string line = sr.ReadLine();
        return TryParseA6(line, out a6);
    }

    private static bool TryParseA6(string raw, out float a6)
    {
        a6 = default;
        if (raw == null) return false;

        int sep = raw.IndexOf('>');
        if (sep < 0) return false;

        string data = raw[(sep + 1)..].TrimStart();
        string[] tok = data.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tok.Length > 5 &&                        // A6 ششمین مقدار
               float.TryParse(tok[5], NumberStyles.Float,
                              CultureInfo.InvariantCulture, out a6);
    }
}
