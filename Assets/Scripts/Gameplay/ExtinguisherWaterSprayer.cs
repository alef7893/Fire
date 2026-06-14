using System.Collections.Generic;
using UnityEngine;

public class ExtinguisherWaterSprayer : MonoBehaviour
{
    [SerializeField] private GrabbableExtinguisher grabbableExtinguisher;
    [SerializeField] private Transform nozzlePoint;
    [SerializeField] private GameObject waterBeamPrefab;
    [SerializeField] private int fireMouseButton = 0;
    [SerializeField] private float spawnInterval = 0.7f;
    [SerializeField] private float effectLifetime = 2.5f;
    [SerializeField] private float stopCleanupDelay = 0.75f;
    [SerializeField] private Vector3 effectLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 effectLocalEulerAngles = new Vector3(90f, 0f, 0f);
    [SerializeField] private Vector3 effectLocalScale = Vector3.one;

    [Header("Fire Node Interaction")]
    [SerializeField] private float waterRange = 12.0f;
    [SerializeField] private float waterRadius = 0.35f;
    [SerializeField] private float waterPower = 1.0f;
    [SerializeField] private LayerMask fireNodeLayers = ~0;

    private readonly List<GameObject> activeEffects = new List<GameObject>();
    private readonly List<Component> wateredTargets = new List<Component>();
    private float nextSpawnTime;
    private bool wasFiring;

    private void Awake()
    {
        if (grabbableExtinguisher == null)
        {
            grabbableExtinguisher = GetComponent<GrabbableExtinguisher>();
        }
    }

    private void Update()
    {
        bool canFire = grabbableExtinguisher != null
            && grabbableExtinguisher.IsHeld
            && nozzlePoint != null
            && waterBeamPrefab != null;

        bool isFiring = canFire && Input.GetMouseButton(fireMouseButton);

        if (isFiring && Time.time >= nextSpawnTime)
        {
            SpawnWaterBeam();
            nextSpawnTime = Time.time + spawnInterval;
        }

        if (isFiring)
        {
            ApplyWaterToFireNodes();
        }

        if (wasFiring && !isFiring)
        {
            StopActiveEffects();
        }

        wasFiring = isFiring;
        activeEffects.RemoveAll(effect => effect == null);
    }

    private void OnDisable()
    {
        StopActiveEffects();
        wasFiring = false;
    }

    private void SpawnWaterBeam()
    {
        GameObject instance = Instantiate(waterBeamPrefab, nozzlePoint);
        instance.transform.localPosition = effectLocalPosition;
        instance.transform.localRotation = Quaternion.Euler(effectLocalEulerAngles);
        instance.transform.localScale = effectLocalScale;
        activeEffects.Add(instance);
        Destroy(instance, effectLifetime);
    }

    private void ApplyWaterToFireNodes()
    {
        wateredTargets.Clear();

        RaycastHit[] hits = Physics.SphereCastAll(
            nozzlePoint.position,
            Mathf.Max(0.01f, waterRadius),
            nozzlePoint.forward,
            Mathf.Max(0.01f, waterRange),
            fireNodeLayers,
            QueryTriggerInteraction.Collide);

        foreach (RaycastHit hit in hits)
        {
            IFireWaterTarget waterTarget = hit.collider.GetComponentInParent<IFireWaterTarget>();
            Component targetComponent = waterTarget as Component;
            if (waterTarget == null || targetComponent == null || wateredTargets.Contains(targetComponent))
            {
                continue;
            }

            waterTarget.ApplyWater(waterPower);
            wateredTargets.Add(targetComponent);
        }
    }

    private void StopActiveEffects()
    {
        foreach (GameObject effect in activeEffects)
        {
            if (effect == null)
            {
                continue;
            }

            ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            Destroy(effect, stopCleanupDelay);
        }

        activeEffects.Clear();
    }
}
