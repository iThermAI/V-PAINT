using UnityEngine;
using System.Collections;

public class A1_RotateObject : MonoBehaviour
{
    public float delay = 10f; // Initial setup duration (smooth rotation in 10 seconds)
    public float duration = 5f; // Duration of each forward or backward rotation (seconds)
    public int repeatCount = 3; // Number of rotation repetitions

    void Start()
    {
        StartCoroutine(RotateSequence());
    }

    IEnumerator RotateSequence()
    {
        // Initial rotation over 10 seconds
        yield return StartCoroutine(RotateToAngle(75f, delay));

        // After the delay, the main rotations start immediately
        for (int i = 0; i < repeatCount; i++)
        {
            yield return StartCoroutine(RotateToAngle(110f, duration)); // Rotate to 110 degrees
            yield return StartCoroutine(RotateToAngle(70f, duration));  // Return to 70 degrees
        }
    }

    IEnumerator RotateToAngle(float targetY, float duration)
    {
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, targetY, transform.rotation.eulerAngles.z);

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t);

            // Rotate all child objects
            foreach (Transform child in transform)
            {
                child.rotation = Quaternion.Lerp(startRotation, targetRotation, t);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Finalize the rotation to the exact target value
        transform.rotation = targetRotation;
        foreach (Transform child in transform)
        {
            child.rotation = targetRotation;
        }
    }
}
