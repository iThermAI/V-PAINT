/*

using UnityEngine;

/// <summary>چرخش نرم حول محور X با A2؛ تا دریافت داده، زاویهٔ اولیه حفظ می‌شود.</summary>
public class RotateFromSerial : MonoBehaviour
{
    [Range(0.01f, 1f)] public float smoothTime = 0.15f;
    private float velocityX;

    private void Update()
    {
        if (SerialHub.Instance == null || !SerialHub.Instance.isDataReady) return;

        float target = SerialHub.Instance.scaled[1];          // A2
        float current = GetSignedAngleX();
        float newAng = Mathf.SmoothDampAngle(current, target, ref velocityX, smoothTime);

        Vector3 eul = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(newAng, eul.y, eul.z);
    }

    private float GetSignedAngleX()
    {
        float a = transform.rotation.eulerAngles.x;
        return (a > 180f) ? a - 360f : a;
    }
}


*/


// ============ RotateSmoothFromSerial.cs ============
using UnityEngine;

/// <summary>چرخش اسموس ایزینگ حول X با لحاظ مدت‌زمان واقعی بین فریم‌ها (A2).</summary>
public class RotateSmoothFromSerial : MonoBehaviour
{
    public float minDuration = 0.05f;       // جلوگیری از صفر شدن مدت

    float startAngle, targetAngle;
    float startTime, duration;
    bool first = true;

    void Update()
    {
        var hub = SerialHub.Instance;
        if (hub == null || !hub.isDataReady) return;

        float incoming = hub.scaled[1];                  // A2

        // اگر بستهٔ جدید آمد
        if (first || !Mathf.Approximately(incoming, targetAngle))
        {
            startAngle = GetSignedAngleX();
            targetAngle = incoming;
            duration = Mathf.Max(hub.deltaPacketTime, minDuration);
            startTime = Time.time;
            first = false;
        }

        // t نرمالایز [0..1] سپس SmoothStep
        float t = Mathf.Clamp01((Time.time - startTime) / duration);
        float easedT = t * t * (3f - 2f * t);            // SmoothStep
        float newA = Mathf.LerpAngle(startAngle, targetAngle, easedT);

        Vector3 eul = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(newA, eul.y, eul.z);
    }

    float GetSignedAngleX()
    {
        float a = transform.rotation.eulerAngles.x;
        return a > 180f ? a - 360f : a;
    }
}
