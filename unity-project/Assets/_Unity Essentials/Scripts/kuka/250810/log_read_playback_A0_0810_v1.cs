// ====================================================================
//  A0LogReplayerOffsets.cs  –  نسخهٔ اصلاح‌شده
//  نمایش همهٔ مقادیر A1..A6, A0, flag در همهٔ حالت‌ها
//  پخش زنده + مرور و پلی‌بک لاگ با ایندکس offset
//  تبدیل خطی:  xu = a * xr + b     (a = scale,  b = offset)
//  اکنون فقط محور X تغییر می‌کند؛ Y و Z اولیه ثابت می‌مانند
// ====================================================================

using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using TMPro;   // TextMesh Pro

public class A0LogReplayer0810_v1 : MonoBehaviour
{
    /*────────────[ Inspector ]────────────*/
    [Header("Log File")]
    public string logFile =
        @"C:\Users\hojat\OneDrive\Desktop\unity\full_realtime_log.log";

    [Header("Motion")]
    public float smoothTime = 0.8f;

    [Header("Offline Control")]
    public int linesPerWheelStep = 50;
    public float playbackInterval = 0.016f;

    [Header("Index")]
    public int indexCapacity = 2_000_000;

    [Header("UI")]
    public TMP_Text statusLabel;
    public TMP_Text dataLabel;   // ← برای نمایش A1..A6, A0, flag

    /*────────────[ Linear Transform ]────────────*/
    [Header("A0  →  Unity X")]
    public float a_scale = 0.001f;   // a
    public float b_offset = 20f;     // b

    /*────────────[ وضعیت متحرک ]────────────*/
    private float currentX;          // مقدار هموارشدهٔ داخلی محور X
    private float velocityX = 0f;
    private volatile float targetX;
    private bool firstValue = false;

    // محورهای اولیهٔ Y و Z ثابت می‌مانند
    private float initialY;
    private float initialZ;

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

    // آخرین مقادیر پارس‌شده برای نمایش UI
    private readonly float[] lastA = new float[7]; // A1..A6, A0
    private int lastFlag = 0;
    private bool hasParsed = false;

    /*────────────────── Start ──────────────────*/
    private void Start()
    {
        Vector3 init = transform.position;
        currentX = init.x;
        initialY = init.y;
        initialZ = init.z;

        tailCo = StartCoroutine(Tail());
        ShowStatus("Live");
        UpdateDataUI(null); // پاکسازی اولیه
    }

    /*────────────────── Update ─────────────────*/
    private void Update()
    {
        HandleKeys();
        HandleWheel();

        if (!firstValue) return;

        // هموارسازی فقط روی محور X
        currentX = Mathf.SmoothDamp(
            currentX,
            targetX,
            ref velocityX,
            smoothTime);

        // اعمال موقعیت با حفظ Y و Z اولیه
        transform.position = new Vector3(
            currentX,
            initialY,
            initialZ);

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
        // ApplySelection در Update فراخوانی می‌شود و UI را هم به‌روز می‌کند
    }

    /*───────── Tail coroutine ─────────*/
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

            if (mode == Mode.Live && TryParseAll(line, out float[] a, out int flag))
            {
                // a[0..5] = A1..A6, a[6] = A0
                CacheParsedAndUpdateUI(a, flag);
                targetX = a[6] * a_scale + b_offset;
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
        ApplySelection(); // برای نمایش فوری
    }

    private void ApplySelection()
    {
        if (TryReadParsed(selectorIdx, out float[] a, out int flag))
        {
            CacheParsedAndUpdateUI(a, flag);
            targetX = a[6] * a_scale + b_offset;
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
            if (TryReadParsed(playIdx, out float[] a, out int flag))
            {
                CacheParsedAndUpdateUI(a, flag);
                targetX = a[6] * a_scale + b_offset;
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
        ApplySelection(); // نمایش آخرین خط روی UI
    }

    /*───────── Return Live ─────────*/
    private void ReturnLive()
    {
        if (mode == Mode.Live) return;

        if (playCo != null) StopCoroutine(playCo);
        playCo = null;
        mode = Mode.Live;

        int lastIdx = offsets.Count - 1;
        if (lastIdx >= 0 && TryReadParsed(lastIdx, out float[] a, out int flag))
        {
            CacheParsedAndUpdateUI(a, flag);
            targetX = a[6] * a_scale + b_offset;
            firstValue = true;
        }

        ShowStatus("Live");
    }

    /*───────── Helper: read parsed at offset index ─────────*/
    private bool TryReadParsed(int idx, out float[] a, out int flag)
    {
        a = null;
        flag = 0;
        if (idx < 0 || idx >= offsets.Count) return false;

        using var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        fs.Seek(offsets[idx], SeekOrigin.Begin);
        using var sr = new StreamReader(fs);
        string line = sr.ReadLine();
        return TryParseAll(line, out a, out flag);
    }

    /*───────── Parse line: A1..A6, A0, flag ─────────*/
    // فرض: پس از '>' هشت توکن به‌ترتیب A1..A6, A0, flag هستند.
    private bool TryParseAll(string raw, out float[] a, out int flag)
    {
        a = null;
        flag = 0;
        if (string.IsNullOrEmpty(raw)) return false;

        int sep = raw.IndexOf('>');
        if (sep < 0) return false;

        string data = raw[(sep + 1)..].TrimStart();
        string[] tok = data.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // حداقل به 8 مقدار نیاز داریم
        if (tok.Length < 8) return false;

        float[] vals = new float[7];
        for (int i = 0; i < 7; i++)
        {
            if (!float.TryParse(tok[i], NumberStyles.Float, CultureInfo.InvariantCulture, out vals[i]))
                return false;
        }

        // flag می‌تواند int یا float باشد؛ اگر float بود، رُند می‌کنیم
        if (!int.TryParse(tok[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out flag))
        {
            if (float.TryParse(tok[7], NumberStyles.Float, CultureInfo.InvariantCulture, out float fflag))
                flag = Mathf.RoundToInt(fflag);
            else
                return false;
        }

        a = vals;
        return true;
    }

    /*───────── UI Helpers ─────────*/
    private void CacheParsedAndUpdateUI(float[] a, int flag)
    {
        if (a == null || a.Length < 7) return;
        for (int i = 0; i < 7; i++) lastA[i] = a[i];
        lastFlag = flag;
        hasParsed = true;
        UpdateDataUI(a);
    }

    private void UpdateDataUI(float[] aOrNull)
    {
        if (!dataLabel) return;

        if (!hasParsed)
        {
            dataLabel.text = "A1: –\nA2: –\nA3: –\nA4: –\nA5: –\nA6: –\nA0: –\nSpray: –";
            return;
        }

        // قالب‌بندی جمع‌وجور و خوانا
        dataLabel.text =
            $"A1: {lastA[0].ToString("0.#####", cul)}\n" +
            $"A2: {lastA[1].ToString("0.#####", cul)}\n" +
            $"A3: {lastA[2].ToString("0.#####", cul)}\n" +
            $"A4: {lastA[3].ToString("0.#####", cul)}\n" +
            $"A5: {lastA[4].ToString("0.#####", cul)}\n" +
            $"A6: {lastA[5].ToString("0.#####", cul)}\n" +
            $"A0: {lastA[6].ToString("0.#####", cul)}\n" +
            $"Spray: {lastFlag}";
    }


    /*
    private void UpdateDataUI(float[] aOrNull)
    {
        if (!dataLabel) return;
        if (!hasParsed)
        {
            dataLabel.text = "";
            return;
        }

        // همه مقادیر A1..A6, A0 و flag را در یک خط با کاما جدا می‌کنیم
        string[] parts = new string[8];
        for (int i = 0; i < 7; i++)
            parts[i] = lastA[i].ToString("0.#####", cul);
        parts[7] = lastFlag.ToString(cul);

        dataLabel.text = string.Join(", ", parts);
    }

    */


    private void ShowStatus(string msg)
    {
        if (statusLabel) statusLabel.text = msg;
        Debug.Log($"[A0] {msg}");
    }
}
