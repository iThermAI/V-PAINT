/*

// ============ A3RotateFromSerial.cs ============
using UnityEngine;

/// <summary>
/// چرخش نرم حول محور X بر اساس A3 (scaled[2])؛
/// تا دریافت داده، زاویهٔ اولیه ثابت می‌ماند.
/// </summary>
public class A3RotateFromSerial : MonoBehaviour
{
    [Range(0.01f, 1f)] public float smoothTime = 0.15f;
    private float velocityX;

    void Update()
    {
        if (SerialHub.Instance == null || !SerialHub.Instance.isDataReady) return;

        float target = SerialHub.Instance.scaled[2];          // A3
        float current = GetSignedAngleX();
        float newAng = Mathf.SmoothDampAngle(current, target,
                                              ref velocityX, smoothTime);

        Vector3 eul = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(newAng, eul.y, eul.z);
    }

    float GetSignedAngleX()
    {
        float a = transform.rotation.eulerAngles.x;
        return (a > 180f) ? a - 360f : a;
    }
}


*/
 
// ============ A3RotateSmoothFromSerial.cs ============
using UnityEngine;

/// <summary>
/// چرخش اسموس ایزینگ حول X با لحاظ مدت‌زمان واقعی بین دو بستهٔ سریال (A3).
/// </summary>
public class A3RotateSmoothFromSerial : MonoBehaviour
{
    public float minDuration = 0.05f;   // حداقل زمان حرکت

    float startAngle, targetAngle;
    float startTime, duration;
    bool  first = true;

    void Update()
    {
        var hub = SerialHub.Instance;
        if (hub == null || !hub.isDataReady) return;

        float incoming = hub.scaled[2];                       // A3

        // هنگام دریافت مقدار تازه
        if (first || !Mathf.Approximately(incoming, targetAngle))
        {
            startAngle  = GetSignedAngleX();
            targetAngle = incoming;
            duration    = Mathf.Max(hub.deltaPacketTime, minDuration);
            startTime   = Time.time;
            first       = false;
        }

        // اینتِرپولیشن اسموس (SmoothStep)
        float t       = Mathf.Clamp01((Time.time - startTime) / duration);
        float easedT  = t * t * (3f - 2f * t);
        float newAng  = Mathf.LerpAngle(startAngle, targetAngle, easedT);

        Vector3 eul = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(newAng, eul.y, eul.z);
    }

    float GetSignedAngleX()
    {
        float a = transform.rotation.eulerAngles.x;
        return (a > 180f) ? a - 360f : a;
    }
}


