/* 
using UnityEngine;

/// <summary>جابجایی نرم روی محور X با A0؛ تا دریافت داده، حالت اولیهٔ صحنه حفظ می‌شود.</summary>
public class MoveFromSerial : MonoBehaviour
{
    [Range(0.01f, 1f)] public float smoothTime = 0.15f;
    private float velocityX;

    private void Update()
    {
        if (SerialHub.Instance == null || !SerialHub.Instance.isDataReady) return;

        float target = SerialHub.Instance.scaled[6];   // A0
        Vector3 pos = transform.position;
        float newX = Mathf.SmoothDamp(pos.x, target, ref velocityX, smoothTime);

        transform.position = new Vector3(newX, pos.y, pos.z);
    }
}


*/


// ============ MoveSmoothFromSerial.cs ============
using UnityEngine;

/// <summary>جابجایی اسموس ایزینگ روی محور X با لحاظ مدت‌زمان واقعی بین فریم‌ها (A0).</summary>
public class MoveSmoothFromSerial : MonoBehaviour
{
    public float minDuration = 0.05f;

    float startX, targetX;
    float startTime, duration;
    bool first = true;

    void Update()
    {
        var hub = SerialHub.Instance;
        if (hub == null || !hub.isDataReady) return;

        float incoming = hub.scaled[6];                  // A0

        if (first || !Mathf.Approximately(incoming, targetX))
        {
            startX = transform.position.x;
            targetX = incoming;
            duration = Mathf.Max(hub.deltaPacketTime, minDuration);
            startTime = Time.time;
            first = false;
        }

        float t = Mathf.Clamp01((Time.time - startTime) / duration);
        float easedT = t * t * (3f - 2f * t);            // SmoothStep
        float newX = Mathf.Lerp(startX, targetX, easedT);

        Vector3 p = transform.position;
        transform.position = new Vector3(newX, p.y, p.z);
    }
}
