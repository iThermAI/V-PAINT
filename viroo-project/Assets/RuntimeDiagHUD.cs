using UnityEngine;
using System.Text;

public class RuntimeDiagHUD : MonoBehaviour
{
    public bool show = true;
    public KeyCode toggleKey = KeyCode.F10;

    Transform rigRoot;
    Transform centerEye;

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) show = !show;

        if (rigRoot == null)
        {
            rigRoot = FindObj("CameraRig") ?? FindObj("PlayerRoot") ?? FindObj("LocalPlayerSpace");
            if (rigRoot == null)
            {
                var eye = FindObj("CenterEyeAnchor");
                if (eye != null) rigRoot = eye.parent;
            }
        }
        if (centerEye == null) centerEye = FindObj("CenterEyeAnchor");
    }

    void OnGUI()
    {
        if (!show) return;

        var sb = new StringBuilder();
        sb.AppendLine("<b>RUNTIME DIAG</b>");
        sb.AppendLine($"Time: {Time.time:0.00}s");
        sb.AppendLine($"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        sb.AppendLine("");

        // Cameras
        sb.AppendLine("<b>Active Cameras</b>");
        int i = 0;
        foreach (var cam in Camera.allCameras)
        {
            if (!cam.isActiveAndEnabled) continue;
            sb.AppendLine($"[{i++}] {cam.name}  RT={(cam.targetTexture ? cam.targetTexture.name : "None")}  Mask={cam.cullingMask}");
        }
        if (i == 0) sb.AppendLine("(none)");

        // Rig + eye
        sb.AppendLine("");
        sb.AppendLine("<b>Rig</b>");
        if (rigRoot != null)
            sb.AppendLine($"RigRoot: {rigRoot.name}  P={rigRoot.position:F2}  R={rigRoot.eulerAngles:F1}");
        else
            sb.AppendLine("RigRoot: (not found)");

        if (centerEye != null)
            sb.AppendLine($"CenterEye: {centerEye.position:F2}  {centerEye.eulerAngles:F1}");
        else
            sb.AppendLine("CenterEye: (not found)");

        // Fallback cam present?
        var fb = GameObject.Find("FallbackCamera");
        sb.AppendLine("");
        sb.AppendLine($"FallbackCamera: {(fb ? "YES" : "no")}");

        // Draw
        var style = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 16, alignment = TextAnchor.UpperLeft };
        GUI.Box(new Rect(10, 10, 520, 240), GUIContent.none);
        GUI.Label(new Rect(20, 20, 500, 220), sb.ToString(), style);
    }

    Transform FindObj(string name)
    {
        var go = GameObject.Find(name);
        return go ? go.transform : null;
    }
}
