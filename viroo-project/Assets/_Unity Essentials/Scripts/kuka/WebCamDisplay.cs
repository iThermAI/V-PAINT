using UnityEngine;

public class WebCamDisplay : MonoBehaviour
{
    private WebCamTexture webcamTexture;

    void Start()
    {
        // دریافت لیست دستگاه‌های وب‌کم متصل
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length > 0)
        {
            // انتخاب اولین وب‌کم موجود
            webcamTexture = new WebCamTexture(devices[0].name);

            // اعمال تصویر وب‌کم به ماده (Material) شیء
            Renderer renderer = GetComponent<Renderer>();
            renderer.material.mainTexture = webcamTexture;

            // شروع پخش تصویر وب‌کم
            webcamTexture.Play();
        }
        else
        {
            Debug.LogWarning("هیچ وب‌کمی یافت نشد.");
        }
    }

    void OnDisable()
    {
        // توقف پخش تصویر وب‌کم هنگام غیرفعال شدن شیء
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }
    }
}
