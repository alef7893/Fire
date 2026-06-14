using UnityEngine;

public class ClickWaterBeamSpawner : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Collider validClickArea;
    [SerializeField] private GameObject waterBeamPrefab;
    [SerializeField] private float effectLifetime = 3f;
    [SerializeField] private Vector3 spawnRotationEuler = Vector3.zero;
    [SerializeField] private float spawnHeightOffset = 0.03f;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0) || targetCamera == null || validClickArea == null || waterBeamPrefab == null)
        {
            return;
        }

        Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
        if (!validClickArea.Raycast(ray, out RaycastHit hit, 100f))
        {
            return;
        }

        Vector3 spawnPosition = hit.point + hit.normal * spawnHeightOffset;
        Quaternion spawnRotation = Quaternion.Euler(spawnRotationEuler);
        GameObject instance = Instantiate(waterBeamPrefab, spawnPosition, spawnRotation);
        Destroy(instance, effectLifetime);
    }
}
