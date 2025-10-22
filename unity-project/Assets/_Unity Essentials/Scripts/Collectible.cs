using UnityEngine;

public class Collectible : MonoBehaviour
{
    public float x_speed;
    public float y_speed;
    public float z_speed;

    public float x_minAngle = -90f;
    public float x_maxAngle = 0f;
    public float y_minAngle = -90f;
    public float y_maxAngle = 0f;
    public float z_minAngle = -90f;
    public float z_maxAngle = 0f;

    private Vector3 currentAngles;
    private Vector3 rotationDirection = Vector3.one;

    void Start()
    {
        currentAngles = transform.localEulerAngles;
    }

    void Update()
    {
        currentAngles.x += x_speed * rotationDirection.x * Time.deltaTime;
        currentAngles.y += y_speed * rotationDirection.y * Time.deltaTime;
        currentAngles.z += z_speed * rotationDirection.z * Time.deltaTime;

        // X axis clamp & direction
        if (currentAngles.x > x_maxAngle)
        {
            currentAngles.x = x_maxAngle;
            rotationDirection.x *= -1;
        }
        else if (currentAngles.x < x_minAngle)
        {
            currentAngles.x = x_minAngle;
            rotationDirection.x *= -1;
        }

        // Y axis clamp & direction
        if (currentAngles.y > y_maxAngle)
        {
            currentAngles.y = y_maxAngle;
            rotationDirection.y *= -1;
        }
        else if (currentAngles.y < y_minAngle)
        {
            currentAngles.y = y_minAngle;
            rotationDirection.y *= -1;
        }

        // Z axis clamp & direction
        if (currentAngles.z > z_maxAngle)
        {
            currentAngles.z = z_maxAngle;
            rotationDirection.z *= -1;
        }
        else if (currentAngles.z < z_minAngle)
        {
            currentAngles.z = z_minAngle;
            rotationDirection.z *= -1;
        }

        transform.localEulerAngles = currentAngles;
    }
}
