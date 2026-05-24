using UnityEngine;
using Unity.Netcode;

public class WeaponPickup : NetworkBehaviour
{
    public string weaponName = "Gun";
    public bool spinWhileAvailable = true;
    public float spinSpeed = 90f;
    public Vector3 spinAxis = Vector3.up;
    public float pickupRadius = 0.85f;

    private bool available = true;

    private WeaponSpawner ownerSpawner;
    private WeaponSpawner.SpawnPointData ownerSpawnPoint;

    void Awake()
    {
        gameObject.name = weaponName;
        EnsurePickupPhysics();
    }

    void Update()
    {
        if (!available || !spinWhileAvailable)
            return;

        transform.Rotate(spinAxis.normalized, spinSpeed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!available)
            return;

        ThirdPersonPlayer player = other.GetComponentInParent<ThirdPersonPlayer>();

        if (player == null)
            return;

        if (!player.TryPickUpWeapon(this))
            return;

        Collect();
    }

    public void SetSpawner(WeaponSpawner spawner, WeaponSpawner.SpawnPointData spawnPoint)
    {
        ownerSpawner = spawner;
        ownerSpawnPoint = spawnPoint;
    }

    public void Collect()
    {
        if (!available)
            return;

        available = false;

        if (IsSpawned)
        {
            if (IsServer)
            {
                ServerCollect();
            }
            else
            {
                CollectServerRpc();
                gameObject.SetActive(false);
            }
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void CollectServerRpc()
    {
        if (!available)
            return;

        available = false;
        ServerCollect();
    }

    void ServerCollect()
    {
        if (ownerSpawner != null)
            ownerSpawner.MakePointAvailable(ownerSpawnPoint);

        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public static void ConfigureScenePickups(Transform playerRoot)
    {
        foreach (Transform sceneTransform in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            GameObject gun = sceneTransform.gameObject;

            if (!gun.name.StartsWith("Gun") || sceneTransform.IsChildOf(playerRoot))
                continue;

            if (gun.GetComponentInParent<ThirdPersonPlayer>() != null)
                continue;

            if (gun.GetComponent<WeaponPickup>() == null)
                gun.AddComponent<WeaponPickup>();
        }
    }

    public static void HideClosestAvailableScenePickup(Vector3 pickupPosition)
    {
        WeaponPickup closest = null;
        float closestDistance = 1.5f * 1.5f;

        foreach (WeaponPickup pickup in FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None))
        {
            if (!pickup.available)
                continue;

            float distance = (pickup.transform.position - pickupPosition).sqrMagnitude;

            if (distance >= closestDistance)
                continue;

            closest = pickup;
            closestDistance = distance;
        }

        if (closest != null)
            closest.Collect();
    }

    void EnsurePickupPhysics()
    {
        Collider pickupCollider = GetComponent<Collider>();

        if (pickupCollider == null)
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = pickupRadius;
            pickupCollider = sphere;
        }

        pickupCollider.isTrigger = true;

        Rigidbody body = GetComponent<Rigidbody>();

        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();

        body.useGravity = false;
        body.isKinematic = true;
    }
}