using UnityEngine;

public class AlignObjects : MonoBehaviour
{
    public GameObject referenceObject;  // شی مرجع
    public GameObject objectToAlign;    // شیی که میخواهید فیکس شود

    void Update()
    {
        // موقعیت شی objectToAlign را با موقعیت شی referenceObject یکسان کنید
        objectToAlign.transform.position = referenceObject.transform.position;

        // در صورت نیاز، می‌توانید چرخش و مقیاس را نیز هم‌راستا کنید
        objectToAlign.transform.rotation = referenceObject.transform.rotation;
        objectToAlign.transform.localScale = referenceObject.transform.localScale;
    }
}
