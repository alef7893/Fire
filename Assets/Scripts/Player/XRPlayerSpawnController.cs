using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-11000)]
public sealed class XRPlayerSpawnController : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform trackedHead;
    [SerializeField] private string spawnPointName = "PlayerSpawnPoint";
    [SerializeField] private bool findSpawnPointByName = true;
    [SerializeField] private bool matchSpawnYaw = true;

    private void Awake()
    {
        ApplySpawn();
    }

    public void Configure(Transform spawn, Transform head)
    {
        spawnPoint = spawn;
        trackedHead = head;
    }

    public void ApplySpawn()
    {
        if (spawnPoint == null && findSpawnPointByName)
        {
            spawnPoint = FindObjectsOfType<Transform>(true)
                .FirstOrDefault(item => item.name == spawnPointName);
        }

        if (spawnPoint == null)
        {
            return;
        }

        if (trackedHead == null)
        {
            trackedHead = FindTrackedHead();
        }

        if (matchSpawnYaw)
        {
            transform.rotation = Quaternion.Euler(0.0f, spawnPoint.eulerAngles.y, 0.0f);
        }

        Vector3 targetPosition = spawnPoint.position;
        if (trackedHead != null)
        {
            Vector3 headOffset = trackedHead.position - transform.position;
            headOffset.y = 0.0f;
            targetPosition -= headOffset;
        }

        targetPosition.y = spawnPoint.position.y;
        transform.position = targetPosition;
    }

    private Transform FindTrackedHead()
    {
        Camera playerCamera = GetComponentsInChildren<Camera>(true).FirstOrDefault();
        if (playerCamera != null)
        {
            return playerCamera.transform;
        }

        return GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(child => child.name == "CenterEyeAnchor");
    }
}
