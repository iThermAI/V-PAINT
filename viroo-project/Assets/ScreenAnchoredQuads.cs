using UnityEngine;

public class ScreenAnchoredQuads : MonoBehaviour
{
    [Header("Assign your existing quads")]
    public Transform quadLeft;
    public Transform quadRight;

    [Header("Behavior")]
    public bool parentToCamera = true;
    public float distanceFromCamera = 1.2f;            // meters
    [Range(0.05f, 0.6f)] public float heightOfTilesRelativeToView = 0.25f;
    public Vector2 viewportMargin = new Vector2(0.08f, 0.08f);

    [Header("Video Aspect (width / height)")]
    public float aspectLeft = 16f / 9f;
    public float aspectRight = 16f / 9f;

    [Header("Rotation / Mirroring")]
    [Tooltip("Extra roll (twist) in degrees around the view forward axis.")]
    public float rollLeftDeg = 0f;
    public float rollRightDeg = 0f;

    [Tooltip("Mirror horizontally/vertically if your feed is flipped.")]
    public bool flipXLeft = false, flipYLeft = false;
    public bool flipXRight = false, flipYRight = false;

    Camera _cam;
    Transform _hudAnchor;
    float _camCheckTimer;

    void OnEnable()
    {
        FindActiveCamera(force: true);
        EnsureAnchor();
        ReparentIfNeeded();
    }

    void LateUpdate()
    {
        _camCheckTimer += Time.unscaledDeltaTime;
        if (_camCheckTimer > 0.5f) { FindActiveCamera(); _camCheckTimer = 0f; }

        if (!_cam) return;

        EnsureAnchor();
        ReparentIfNeeded();

        UpdateCorner(quadLeft, new Vector2(viewportMargin.x, viewportMargin.y),
                     aspectLeft, rollLeftDeg, flipXLeft, flipYLeft);

        UpdateCorner(quadRight, new Vector2(1f - viewportMargin.x, viewportMargin.y),
                     aspectRight, rollRightDeg, flipXRight, flipYRight);
    }

    void FindActiveCamera(bool force = false)
    {
        if (!force && _cam && _cam.isActiveAndEnabled && _cam.targetTexture == null) return;

        Camera best = null;
        if (Camera.main && Camera.main.isActiveAndEnabled && Camera.main.targetTexture == null)
            best = Camera.main;

        if (best == null)
        {
            foreach (var c in Camera.allCameras)
            {
                if (!c.isActiveAndEnabled) continue;
                if (c.targetTexture != null) continue;
                best = c; break;
            }
        }
        if (best != _cam) _cam = best;
    }

    void EnsureAnchor()
    {
        if (!parentToCamera || !_cam) return;

        if (_hudAnchor == null || _hudAnchor.parent != _cam.transform)
        {
            var go = new GameObject("HUDAnchor");
            go.hideFlags = HideFlags.DontSave;
            _hudAnchor = go.transform;
            _hudAnchor.SetParent(_cam.transform, false);
            _hudAnchor.localPosition = Vector3.zero;
            _hudAnchor.localRotation = Quaternion.identity;
            _hudAnchor.localScale = Vector3.one;
        }
    }

    void ReparentIfNeeded()
    {
        if (!parentToCamera || !_hudAnchor) return;
        if (quadLeft && quadLeft.parent != _hudAnchor) quadLeft.SetParent(_hudAnchor, true);
        if (quadRight && quadRight.parent != _hudAnchor) quadRight.SetParent(_hudAnchor, true);
    }

    void UpdateCorner(Transform quad, Vector2 vp, float aspect, float rollDeg, bool flipX, bool flipY)
    {
        if (!quad || !_cam) return;

        float safeDist = Mathf.Max(distanceFromCamera, _cam.nearClipPlane + 0.2f);
        Vector3 worldPos = _cam.ViewportToWorldPoint(new Vector3(vp.x, vp.y, safeDist));
        quad.position = worldPos;

        // Face camera then add extra roll around the camera forward
        Quaternion face = _cam.transform.rotation * Quaternion.AngleAxis(rollDeg, _cam.transform.forward);
        quad.rotation = face;

        // Size
        float viewH = 2f * safeDist * Mathf.Tan(_cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float h = viewH * Mathf.Clamp01(heightOfTilesRelativeToView);
        float w = h * Mathf.Max(0.01f, aspect);

        // Apply mirroring via sign of scale
        float sx = flipX ? -w : w;
        float sy = flipY ? -h : h;
        quad.localScale = new Vector3(sx, sy, 1f);

        var mr = quad.GetComponent<Renderer>();
        if (mr && !mr.enabled) mr.enabled = true;
    }
}
