// ====================================================================
//  A0LogReplayerOffsets.cs
//  پخش زنده + مرور و پلی‌بک لاگ با ایندکس offset
//  نسخهٔ نهایی:  xu = a * xr + b      (a = scale,  b = offset)
// ====================================================================

using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using TMPro;   // TextMesh Pro

public class A0LogReplayerfinal : MonoBehaviour
{
    /*────────────[ Inspector ]────────────*/
    [Header("Log File")]
    public string logFile =
        @"C:\Users\LabTR\Desktop\unity\full_realtime_log.log";

    [Header("Motion")]
    public float smoothTime = 0.8f;

    [Header("Offline Control")]
    public int linesPerWheelStep = 50;
    public float playbackInterval = 0.016f;

    [Header("Index")]
    [Tooltip("حداکثر تعداد offset ذخیره‌شده (۸ بایت برای هر خط)")]
    public int indexCapacity = 2_000_000;

    [Header("UI")]
    [Tooltip("یک Text یا TMP_Text برای نمایش وضعیت. می‌تواند خالی بماند.")]
    public TMP_Text statusLabel;

    /*────────────[ Linear Transform ]────────────*/
    [Header("A0  →  Unity X")]
    [Tooltip("a = scale (مثلاً 0.001 برای mm→m)")]
    public float a_scale = 0.001f;      // ← a
    [Tooltip("b = offset")]
    public float b_offset = 20f;          // ← b

    /*────────────[ وضعیت متحرک ]────────────*/
    private float velocityX = 0f;
    private volatile float targetX;
    private bool firstValue = false;

    private readonly List<long> offsets = new();  // نشانی ابتدای هر خط
    private int baseDiscard = 0;                  // چند خط اول به‌دلیل پرشدن ظرفیت حذف شده

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
        tailCo = StartCoroutine(Tail());
        ShowStatus("Live");
    }

    /*────────────────── Update ─────────────────*/
    private void Update()
    {
        HandleKeys();
        HandleWheel();

        if (!firstValue) return;

        Vector3 p = transform.position;
        float nx = Mathf.SmoothDamp(p.x, targetX, ref velocityX, smoothTime);
        transform.position = new Vector3(nx, p.y, p.z);

        if (mode == Mode.OfflineSelecting)
            ApplySelection();
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

        int step = Mathf.RoundToInt(-d * linesPerWheelStep); // پایین = عقب
        selectorIdx = Mathf.Clamp(selectorIdx + step, 0, offlineEndIdx);
    }

    /*───────── Tail coroutine ─────────*/
    private IEnumerator Tail()
    {
        while (!File.Exists(logFile))
            yield return null;

        using var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(fs);

        fs.Seek(0, SeekOrigin.End);          // شروع از انتهای فایل
        long lastLen = fs.Length;

        while (true)
        {
            if (fs.Length < lastLen)         // فایل چرخش شده
            {
                fs.Seek(0, SeekOrigin.Begin);
                sr.DiscardBufferedData();
                offsets.Clear();
                baseDiscard = 0;
                Debug.LogWarning("[A0] Log rotated; index cleared.");
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

            if (mode == Mode.Live && TryParseA0(line, out float a0))
            {
                a0 = a0 * a_scale + b_offset;   // ★ xu = a*xr + b
                targetX = a0;
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

        ShowStatus($"Offline  ({baseDiscard + offsets.Count:n0} lines)");
    }

    private void ApplySelection()
    {
        if (TryReadA0(selectorIdx, out float a0))
        {
            a0 = a0 * a_scale + b_offset;       // ★ تبدیل
            targetX = a0;
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
        ShowStatus("Play ►  (Space = paused)");

        while (playIdx <= offlineEndIdx)
        {
            if (TryReadA0(playIdx, out float a0))
            {
                a0 = a0 * a_scale + b_offset;   // ★ تبدیل
                targetX = a0;
                firstValue = true;
            }
            playIdx++;

            yield return new WaitForSecondsRealtime(playbackInterval);
            if (mode != Mode.OfflinePlaying) yield break;
        }

        selectorIdx = offlineEndIdx;
        mode = Mode.OfflineSelecting;
        ShowStatus("Offline  (Ended)");
    }

    private void PausePlayback()
    {
        if (playCo != null) StopCoroutine(playCo);
        playCo = null;
        selectorIdx = Mathf.Clamp(playIdx, 0, offlineEndIdx);
        mode = Mode.OfflineSelecting;
        ShowStatus("Offline  (paused)");
    }

    /*───────── Return Live ─────────*/
    private void ReturnLive()
    {
        if (mode == Mode.Live) return;

        if (playCo != null) StopCoroutine(playCo);
        playCo = null;
        mode = Mode.Live;

        int lastIdx = offsets.Count - 1;
        if (lastIdx >= 0 && TryReadA0(lastIdx, out float a0))
        {
            a0 = a0 * a_scale + b_offset;       // ★ تبدیل
            targetX = a0;
        }

        ShowStatus("Live");
    }

    /*───────── Helper: read A0 at offset index ─────────*/
    private bool TryReadA0(int idx, out float a0)
    {
        a0 = default;
        if (idx < 0 || idx >= offsets.Count) return false;

        using var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        fs.Seek(offsets[idx], SeekOrigin.Begin);
        using var sr = new StreamReader(fs);
        string line = sr.ReadLine();
        return TryParseA0(line, out a0);
    }

    /*───────── Parse line ─────────*/
    private static bool TryParseA0(string raw, out float a0)
    {
        a0 = default;
        if (raw == null) return false;

        int sep = raw.IndexOf('>');
        if (sep < 0) return false;

        string data = raw[(sep + 1)..].TrimStart();
        string[] tok = data.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tok.Length > 6 &&
               float.TryParse(tok[6], NumberStyles.Float,
                              CultureInfo.InvariantCulture, out a0);
    }

    /*───────── UI Helper ─────────*/
    private void ShowStatus(string msg)
    {
        if (statusLabel) statusLabel.text = msg;
        Debug.Log($"[A0] {msg}");
    }
}
