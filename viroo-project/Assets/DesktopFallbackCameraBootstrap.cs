using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(10000)]
public class DesktopFallbackCameraBootstrap : MonoBehaviour
{
    [Header("Assign your start pose")]
    public Transform singlePlayerStart;   // drag: Viroo → Scenes → Single Player Start

    [Header("Enable / Disable")]
    public bool allowRotation = true;
    public bool allowMovement = true;

    [Header("Movement Settings")]
    public float moveRadiusMeters = 3f;   // allowed distance from start
    public float moveSpeed = 2f;          // m/s (Shift ×3, Ctrl ×0.35)

    [Header("Rotation Settings")]
    public float yawLimitDegrees = 30f;   // ± yaw
    public float pitchDownLimit = 15f;    // look down
    public float pitchUpLimit = 20f;      // look up

    [Header("Enforcement")]
    public bool enforceAsOnlyCamera = true;   // disable any other non-RT cameras

    Camera _cam;
    GentleCameraDriver _driver;
    Vector3 _startPos;
    Quaternion _startRot;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += (_, __) => { _cam = null; _driver = null; };
    }

    void Start() { StartCoroutine(EnsureCamera()); }

    System.Collections.IEnumerator EnsureCamera()
    {
        // let runtime systems initialize a couple frames
        yield return null; yield return null;

        // Reuse an existing non-RT camera if present
        foreach (var c in Camera.allCameras)
            if (c.isActiveAndEnabled && c.targetTexture == null) { _cam = c; break; }

        // Otherwise create fallback
        if (_cam == null)
        {
            var go = new GameObject("FallbackCamera");
            go.tag = "MainCamera";
            _cam = go.AddComponent<Camera>();
            _cam.clearFlags = CameraClearFlags.Skybox;
            _cam.nearClipPlane = 0.1f;
            _cam.farClipPlane = 1000f;
            _cam.cullingMask = ~0;
        }

        // Place at start
        if (singlePlayerStart != null)
            _cam.transform.SetPositionAndRotation(singlePlayerStart.position, singlePlayerStart.rotation);
        else
            _cam.transform.SetPositionAndRotation(new Vector3(0, 1.7f, -5), Quaternion.LookRotation(Vector3.forward));

        _startPos = _cam.transform.position;
        _startRot = _cam.transform.rotation;

        // Attach & configure driver
        _driver = _cam.GetComponent<GentleCameraDriver>();
        if (_driver == null) _driver = _cam.gameObject.AddComponent<GentleCameraDriver>();

        _driver.allowRotation = allowRotation;
        _driver.allowMovement = allowMovement;
        _driver.moveRadiusMeters = moveRadiusMeters;
        _driver.moveSpeed = moveSpeed;
        _driver.yawLimitDegrees = yawLimitDegrees;
        _driver.pitchDownLimit = pitchDownLimit;
        _driver.pitchUpLimit = pitchUpLimit;
        _driver.SetStart(singlePlayerStart != null ? singlePlayerStart : _cam.transform);

        _cam.depth = 9999; // render on top if others enable themselves
    }

    void LateUpdate()
    {
        if (_cam == null) return;

        // Keep our camera as the only active non-RT camera
        if (enforceAsOnlyCamera)
        {
            foreach (var c in Camera.allCameras)
            {
                if (c == _cam) continue;
                if (!c.isActiveAndEnabled) continue;
                if (c.targetTexture != null) continue; // ignore RT feeds
                c.enabled = false;
            }
            _cam.depth = 9999;
        }

        // If disabled, hard-lock to start pose
        var t = _cam.transform;
        if (!allowRotation) t.rotation = _startRot;

        if (!allowMovement)
        {
            t.position = _startPos;
        }
        else
        {
            // Even when moving, keep height fixed and clamp radius
            Vector3 p = t.position;
            p.y = _startPos.y;

            Vector3 deltaXZ = new Vector3(p.x - _startPos.x, 0f, p.z - _startPos.z);
            float r = Mathf.Max(0.0001f, moveRadiusMeters);
            if (deltaXZ.magnitude > r)
                p = new Vector3(_startPos.x, _startPos.y, _startPos.z) + deltaXZ.normalized * r;

            t.position = p;
        }
    }
}
