using UnityEngine;
using System.Collections;

public class A5_RotateObject : MonoBehaviour
{
    public float rotationSpeed = 10f; // Rotation speed (in degrees per second)

    void Start()
    {
        StartCoroutine(RotateSequence());
    }

    IEnumerator RotateSequence()
    {
        // 1. Delay 5 seconds
        yield return new WaitForSeconds(5f);

        // 2. 0 degrees to -60 degrees: 5 seconds
        yield return RotateToAngle(-60f, 5f);

        // 3. -60 degrees to 0 degrees: 7 seconds
        yield return RotateToAngle(0f, 7f);

        // 4. 0 degrees to 0 degrees: 5 seconds
        yield return RotateToAngle(0f, 5f);

        // 5. Delay 3 seconds
        yield return new WaitForSeconds(3f);

        // 6. 0 degrees to -40 degrees: 5 seconds
        yield return RotateToAngle(-40f, 5f);

        // 7. -40 degrees to 0 degrees: 7 seconds
        yield return RotateToAngle(0f, 7f);

        // 8. Delay 3 seconds
        yield return new WaitForSeconds(3f);

        // 9. 0 degrees to -40 degrees: 7 seconds
        yield return RotateToAngle(-40f, 7f);

        // 10. Delay 3 seconds
        yield return new WaitForSeconds(3f);

        // 11. -40 degrees to 0 degrees: 7 seconds
        yield return RotateToAngle(0f, 7f);

        // 12. 0 degrees to 0 degrees: 5 seconds
        yield return RotateToAngle(0f, 5f);

        // 13. Delay 3 seconds
        yield return new WaitForSeconds(3f);

        // 14. 0 degrees to -60 degrees: 5 seconds
        yield return RotateToAngle(-60f, 5f);

        // 15. -60 degrees to 0 degrees: 7 seconds
        yield return RotateToAngle(0f, 7f);

        // 16. 0 degrees to 0 degrees: 5 seconds
        yield return RotateToAngle(0f, 5f);
    }

    // Function to rotate to a specific angle over a defined duration
    IEnumerator RotateToAngle(float targetAngle, float duration)
    {
        // Starting angle on the X axis, ignoring the parent transform
        float startAngle = transform.localRotation.eulerAngles.x;

        // Handle negative rotation when start angle is greater than 180
        if (targetAngle < 0 && startAngle > 180) startAngle -= 360;

        float elapsedTime = 0f;

        // Rotate towards the target angle
        while (elapsedTime < duration)
        {
            float angle = Mathf.LerpAngle(startAngle, targetAngle, elapsedTime / duration);
            transform.localRotation = Quaternion.Euler(angle, transform.localRotation.eulerAngles.y, transform.localRotation.eulerAngles.z); // Rotate on local X axis
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the final angle is exactly the target
        transform.localRotation = Quaternion.Euler(targetAngle, transform.localRotation.eulerAngles.y, transform.localRotation.eulerAngles.z);
    }
}
