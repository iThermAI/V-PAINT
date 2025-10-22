using UnityEngine;
using System.Collections;

public class MoveObject : MonoBehaviour
{
    public float speed = 10f; // Movement speed (meters per second)

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
        StartCoroutine(MoveThroughWaypoints());
    }

    IEnumerator MoveThroughWaypoints()
    {
        // 1. Move from 0 to 50 meters in 5 seconds
        yield return MoveToPosition(0f, 5f);
        yield return new WaitForSeconds(17f); // Wait for 17 seconds

        // 2. Move from 50 to 55.5 meters in 1 second
        yield return MoveToPosition(-5.5f, 3f);
        yield return new WaitForSeconds(12f); // Wait for 12 seconds

        // 3. Move from 55.5 to 59 meters in 1 second
        yield return MoveToPosition(-9f, 3f);
        yield return new WaitForSeconds(7f); // Wait for 7 seconds

        // 4. Move from 59 to 62 meters in 1 second
        yield return MoveToPosition(-12f, 3f);
        yield return new WaitForSeconds(12f); // Wait for 12 seconds

        // 5. Move from 62 to 68 meters in 1 second
        yield return MoveToPosition(-18f, 3f);
        yield return new WaitForSeconds(17f); // Wait for 17 seconds

        // 6. Move from 68 to 75 meters in 2 seconds
        yield return MoveToPosition(-25f, 2f);

        // Finished and stop at the final position
    }

    // Function to move to a specific position over a given duration
    IEnumerator MoveToPosition(float targetX, float duration)
    {
        float startX = transform.position.x;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float newX = Mathf.Lerp(startX, targetX, elapsedTime / duration);
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the object lands exactly at the target position
        transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
    }
}
