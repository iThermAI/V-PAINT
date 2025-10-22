using UnityEngine;

public class GentleCameraDriver : MonoBehaviour
{
    [HideInInspector] public bool allowRotation = true;
    [HideInInspector] public bool allowMovement = true;
    [HideInInspector] public float moveRadiusMeters = 3f;
    [HideInInspector] public float moveSpeed = 2f;
    [HideInInspector] public float yawLimitDegrees = 30f;
    [HideInInspector] public float pitchDownLimit = 15f;
    [HideInInspector] public float pitchUpLimit = 20f;

    Vector3 _startPos;
    Quaternion _startRot;
    float _yawOffset, _pitchOffset;

    public void SetStart(Transform start)
    {
        _startPos = start.position;
        _startRot = start.rotation;
        _yawOffset = 0f;
        _pitchOffset = 0f;
        transform.SetPositionAndRotation(_startPos, _startRot);
    }

    void Update()
    {
        // --- Rotation with Arrow Keys ---
        if (allowRotation)
        {
            float yawInput = (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            float pitchInput = (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);

            float rotSpeed = 60f; // deg/s
            _yawOffset += yawInput * rotSpeed * Time.deltaTime;
            _pitchOffset += pitchInput * rotSpeed * Time.deltaTime;

            _yawOffset = Mathf.Clamp(_yawOffset, -yawLimitDegrees, yawLimitDegrees);
            _pitchOffset = Mathf.Clamp(_pitchOffset, -pitchDownLimit, pitchUpLimit);
        }

        // --- Movement with WASD ---
        if (allowMovement)
        {
            int h = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
            int v = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
            Vector3 input = new Vector3(h, 0f, v);

            if (input.sqrMagnitude > 0f)
            {
                float mult = 1f;
                if (Input.GetKey(KeyCode.LeftShift)) mult = 3f;     // fast
                if (Input.GetKey(KeyCode.LeftControl)) mult = 0.35f;  // slow

                Vector3 dirLocal = input.normalized * moveSpeed * mult * Time.deltaTime;
                // Move relative to current yaw (so turning changes forward)
                Quaternion yawRot = Quaternion.Euler(0f, _yawOffset, 0f) * _startRot;
                Vector3 delta = yawRot * dirLocal;
                Vector3 pos = transform.position + delta;

                // Clamp within radius from start (Y clamped by bootstrap)
                Vector3 fromStart = pos - _startPos;
                if (fromStart.magnitude > moveRadiusMeters)
                    pos = _startPos + fromStart.normalized * moveRadiusMeters;

                transform.position = pos;
            }
        }

        // --- Apply rotation every frame ---
        transform.rotation = _startRot * Quaternion.Euler(_pitchOffset, _yawOffset, 0f);
    }
}
