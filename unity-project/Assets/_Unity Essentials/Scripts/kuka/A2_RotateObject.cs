using UnityEngine;
using System.Collections;

public class A2_RotateObject : MonoBehaviour
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

        // 2. 0 degrees to -15 degrees: 5 seconds
        yield return RotateToAngle(-15f, 5f);

        // 3. -15 degrees to -90 degrees: 7 seconds
        yield return RotateToAngle(-90f, 7f);

        // 4. -90 degrees to 0 degrees: 5 seconds
        yield return RotateToAngle(0f, 5f);

        // 5. Delay 3 second
        yield return new WaitForSeconds(3f);

        // 6. 0 degrees to 25 degrees: 5 seconds
        yield return RotateToAngle(25f, 5f);

        // 7. 25 degrees to -65 degrees: 7 seconds
        yield return RotateToAngle(-65f, 7f);

        // 8. Delay 3 second
        yield return new WaitForSeconds(3f);

        // 9. -65 degrees to 25 degrees: 7 seconds
        yield return RotateToAngle(25f, 7f);

        // 10. Delay 3 second
        yield return new WaitForSeconds(3f);

        // 11. 25 degrees to -65 degrees: 7 seconds
        yield return RotateToAngle(-65f, 7f);

        // 12. -65 degrees to 0 degrees: 5 seconds
        yield return RotateToAngle(0f, 5f);

        // 13. Delay 3 second
        yield return new WaitForSeconds(3f);

        // 14. 0 degrees to -15 degrees: 5 seconds
        yield return RotateToAngle(-15f, 5f);

        // 15. -15 degrees to -90 degrees: 7 seconds
        yield return RotateToAngle(-90f, 7f);

        // 16. -90 degrees to 0 degrees: 5 seconds
        yield return RotateToAngle(0f, 5f);
    }

    // Function to rotate to a specific angle over a defined duration
    IEnumerator RotateToAngle(float targetAngle, float duration)
    {
        float startAngle = transform.rotation.eulerAngles.x; // X axis
        if (targetAngle < 0 && startAngle > 180) startAngle -= 360; // Handle negative rotation from 0 to 360 degrees

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float angle = Mathf.LerpAngle(startAngle, targetAngle, elapsedTime / duration);
            transform.rotation = Quaternion.Euler(angle, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z); // Rotate on the X axis
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure it ends exactly at the target angle
        transform.rotation = Quaternion.Euler(targetAngle, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
    }
}
