// ====================================================================
//  A3LogRotator.cs
//  خواندن مقدار A3 از لاگ و تنظیم Rotation X
//      xu = a * xr + b
// ====================================================================

using UnityEngine;
using System;
using System.IO;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;

public class A3LogRotator : MonoBehaviour
{
    [Header("Log File")]
    public string logFile = @"C:\Users\LabTR\Desktop\unity\full_realtime_log.log";

    [Header("Rotation Smoothing")]
    public float smoothTime = 0.8f;               // هموارسازی زاویه

    [Header("Offline Control")]
    public int linesPerWheelStep = 50;
    public float playbackInterval = 0.016f;       // ≈60 Hz

    [Header("Index")]
    public int indexCapacity = 2_000_000;

    [Header("A3  →  Rotation X")]
    [Tooltip("a = scale (مثلاً 0.1 برای تبدیل به درجه)")]
    public float a_scale = 0.1f;
    [Tooltip("b = offset (درجه)")]
    public float b_offset = 0f;

    /*────────── وضعیت داخلی ─────────*/
    private float angleVelocity = 0f;
    private volatile float targetAngleX;
    private bool firstValue = false;

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
    private void Start() => tailCo = StartCoroutine(Tail());

    /*───────────────── Update ─────────────────*/
    private void Update()
    {
        HandleKeys();
        HandleWheel();

        if (!firstValue) return;

        Vector3 eul = transform.eulerAngles;
        float nx = Mathf.SmoothDampAngle(eul.x, targetAngleX, ref angleVelocity, smoothTime);
        transform.rotation = Quaternion.Euler(nx, eul.y, eul.z);

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
            if (fs.Length < lastLen)            // فایل روتیت شده
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

            if (mode == Mode.Live && TryParseA3(line, out float a3))
            {
                a3 = a3 * a_scale + b_offset;
                targetAngleX = a3;
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
        if (TryReadA3(selectorIdx, out float a3))
        {
            a3 = a3 * a_scale + b_offset;
            targetAngleX = a3;
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
            if (TryReadA3(playIdx, out float a3))
            {
                a3 = a3 * a_scale + b_offset;
                targetAngleX = a3;
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
        if (lastIdx >= 0 && TryReadA3(lastIdx, out float a3))
        {
            a3 = a3 * a_scale + b_offset;
            targetAngleX = a3;
        }
    }

    /*───────── Helper: read A3 ─────────*/
    private bool TryReadA3(int idx, out float a3)
    {
        a3 = default;
        if (idx < 0 || idx >= offsets.Count) return false;

        using var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        fs.Seek(offsets[idx], SeekOrigin.Begin);
        using var sr = new StreamReader(fs);
        string line = sr.ReadLine();
        return TryParseA3(line, out a3);
    }

    /*───────── Parse line ─────────*/
    private static bool TryParseA3(string raw, out float a3)
    {
        a3 = default;
        if (raw == null) return false;

        int sep = raw.IndexOf('>');
        if (sep < 0) return false;

        string data = raw[(sep + 1)..].TrimStart();
        string[] tok = data.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tok.Length > 2 &&                        // A3 سومین مقدار
               float.TryParse(tok[2], NumberStyles.Float,
                              CultureInfo.InvariantCulture, out a3);
    }
}
