using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class WeaponSpawner : NetworkBehaviour
{
    [System.Serializable]
    public class SpawnPointData
    {
        public Transform point;
        public NetworkObject currentWeapon;
    }

    [Header("Weapon Spawn Settings")]
    public GameObject weaponPrefab;
    public List<SpawnPointData> spawnPoints = new List<SpawnPointData>();
    public float spawnInterval = 15f;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        StartCoroutine(SpawnWeaponRoutine());
    }

    IEnumerator SpawnWeaponRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            SpawnWeapon();
        }
    }

    void SpawnWeapon()
    {
        if (weaponPrefab == null)
        {
            Debug.LogWarning("Weapon prefab is missing!");
            return;
        }

        List<SpawnPointData> availablePoints = new List<SpawnPointData>();

        foreach (SpawnPointData spawnPoint in spawnPoints)
        {
            if (spawnPoint.point == null)
                continue;

            if (spawnPoint.currentWeapon == null || !spawnPoint.currentWeapon.IsSpawned)
            {
                spawnPoint.currentWeapon = null;
                availablePoints.Add(spawnPoint);
            }
        }

        if (availablePoints.Count == 0)
            return;

        SpawnPointData selectedPoint = availablePoints[Random.Range(0, availablePoints.Count)];

        GameObject weaponInstance = Instantiate(
            weaponPrefab,
            selectedPoint.point.position,
            selectedPoint.point.rotation
        );

        NetworkObject networkObject = weaponInstance.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Debug.LogError("Weapon prefab needs NetworkObject component!");
            Destroy(weaponInstance);
            return;
        }

        WeaponPickup pickup = weaponInstance.GetComponent<WeaponPickup>();
        if (pickup != null)
        {
            pickup.SetSpawner(this, selectedPoint);
        }

        networkObject.Spawn();

        selectedPoint.currentWeapon = networkObject;
    }

    public void MakePointAvailable(SpawnPointData spawnPoint)
    {
        if (!IsServer)
            return;

        if (spawnPoint != null)
            spawnPoint.currentWeapon = null;
    }
}