using UnityEngine;

public class FreeFlyCamera : MonoBehaviour
{
    public float speed = 10f;         // سرعت حرکت دوربین
    public float sensitivity = 2f;    // حساسیت موس برای نگاه کردن

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Update()
    {
        // گرفتن ورودی حرکت از کلیدهای جهت کیبورد یا WASD
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // کنترل حرکت به جلو و عقب (W و S یا کلید بالا/پایین)
        Vector3 forward = transform.forward * vertical;

        // کنترل حرکت به راست و چپ (A و D یا کلیدهای چپ و راست)
        Vector3 right = transform.right * horizontal;

        // مجموع حرکت‌ها
        Vector3 moveDirection = (forward + right).normalized;

        // اعمال حرکت
        transform.position += moveDirection * speed * Time.deltaTime;

        // کنترل جهت نگاه دوربین با موس (اختیاری، اما توصیه می‌شود)
        rotationX += Input.GetAxis("Mouse X") * sensitivity;
        rotationY += Input.GetAxis("Mouse Y") * sensitivity;

        rotationY = Mathf.Clamp(rotationY, -90f, 90f); // محدود کردن حرکت عمودی نگاه

        transform.localRotation = Quaternion.Euler(-rotationY, rotationX, 0f);
    }
}
