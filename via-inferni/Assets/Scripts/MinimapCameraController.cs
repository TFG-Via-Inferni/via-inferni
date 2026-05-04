using UnityEngine;

[ExecuteAlways]
public class MinimapCameraController : MonoBehaviour
{
    [SerializeField] private Camera minimapCamera;
    [SerializeField] private float followLerp = 0.2f;
    [SerializeField] private bool smooth = true;
    [SerializeField] private float zOffset = -10f;

    private void Reset()
    {
        if (minimapCamera == null)
            minimapCamera = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (MapGenerator.instance == null)
            return;

        var cell = MapGenerator.instance.CurrentRoomCell;
        if (cell == null)
            return;

        Vector3 target = cell.transform.position;
        target.z = zOffset;

        if (minimapCamera == null)
        {
            if (Camera.main != null)
                minimapCamera = Camera.main; // fallback
            else
                return;
        }

        if (smooth)
        {
            minimapCamera.transform.position = Vector3.Lerp(minimapCamera.transform.position, target, followLerp);
        }
        else
        {
            minimapCamera.transform.position = target;
        }
    }
}
