using UnityEngine;
using System.Collections;

public class A3_RotateObject : MonoBehaviour
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

        // 2. 0 degrees to -70 degrees: 5 seconds
        yield return RotateToAngle(-70f, 5f);

        // 3. -70 degrees to 75 degrees: 7 seconds
        yield return RotateToAngle(75f, 7f);

        // 4. 75 degrees to 0 degrees: 5 seconds
        yield return RotateToAngle(0f, 5f);

        // 5. Delay 3 seconds
        yield return new WaitForSeconds(3f);

        // 6. 0 degrees to -75 degrees: 5 seconds
        yield return RotateToAngle(-75f, 5f);

        // 7. -75 degrees to 50 degrees: 7 seconds
        yield return RotateToAngle(50f, 7f);

        // 8. Delay 3 seconds
        yield return new WaitForSeconds(3f);

        // 9. 50 degrees to -75 degrees: 7 seconds
        yield return RotateToAngle(-75f, 7f);

        // 10. Delay 3 seconds
        yield return new WaitForSeconds(3f);

        // 11. -75 degrees to 50 degrees: 7 seconds
        yield return RotateToAngle(50f, 7f);

        // 12. 50 degrees to 0 degrees: 5 seconds
        yield return RotateToAngle(0f, 5f);

        // 13. Delay 3 seconds
        yield return new WaitForSeconds(3f);

        // 14. 0 degrees to -70 degrees: 5 seconds
        yield return RotateToAngle(-70f, 5f);

        // 15. -70 degrees to 75 degrees: 7 seconds
        yield return RotateToAngle(75f, 7f);

        // 16. 75 degrees to 0 degrees: 5 seconds
        yield return RotateToAngle(0f, 5f);
    }

    // Function to rotate to a specific angle over a given duration
    IEnumerator RotateToAngle(float targetAngle, float duration)
    {
        // Start angle on the X axis, ignoring parent transforms
        float startAngle = transform.localRotation.eulerAngles.x;

        // Handle negative direction rotation when values are greater than 180 degrees
        if (targetAngle < 0 && startAngle > 180) startAngle -= 360;

        float elapsedTime = 0f;

        // Perform rotation toward the target angle
        while (elapsedTime < duration)
        {
            float angle = Mathf.LerpAngle(startAngle, targetAngle, elapsedTime / duration);
            transform.localRotation = Quaternion.Euler(angle, transform.localRotation.eulerAngles.y, transform.localRotation.eulerAngles.z); // Rotate on the X axis locally
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the object lands exactly at the target angle
        transform.localRotation = Quaternion.Euler(targetAngle, transform.localRotation.eulerAngles.y, transform.localRotation.eulerAngles.z);
    }
}
