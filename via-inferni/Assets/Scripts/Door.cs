using UnityEngine;

public class Door : MonoBehaviour
{
    public void SetDoorPrefab(GameObject doorPrefab)
    {
        if (doorPrefab != null)
        {
            var doorInstance = Instantiate(doorPrefab, transform);
            doorInstance.transform.localPosition = Vector3.zero;
            doorInstance.transform.localRotation = Quaternion.identity;
        }
    }
}
