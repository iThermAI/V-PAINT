using UnityEngine;

public class DayCycle : MonoBehaviour
{
    [Tooltip("Real-time duration in seconds for a full day cycle.")]
    public float secondsPerDay = 120f;

    // Update is called once per frame
    void Update()
    {
        // Rotate based on elapsed time
        float anglePerSecond = 360f / secondsPerDay;
        transform.Rotate(Vector3.right, anglePerSecond * Time.deltaTime);
    }
}