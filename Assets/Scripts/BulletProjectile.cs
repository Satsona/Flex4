using UnityEngine;

public class BulletProjectile : MonoBehaviour
{
    private Vector3 direction = Vector3.forward;
    private float speed = 35f;
    private float lifetime = 3f;
    private float spawnedAt;

    public void Initialize(Vector3 shootDirection, float shootSpeed, float lifeSeconds)
    {
        if (shootDirection.sqrMagnitude > 0.0001f)
            direction = shootDirection.normalized;

        speed = Mathf.Max(0f, shootSpeed);
        lifetime = Mathf.Max(0.01f, lifeSeconds);
        spawnedAt = Time.time;
    }

    void Awake()
    {
        spawnedAt = Time.time;
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;

        if (Time.time - spawnedAt >= lifetime)
            Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger)
            return;

        ThirdPersonPlayer player = other.GetComponentInParent<ThirdPersonPlayer>();

        if (player != null)
        {
            player.KillAndRespawn();
        }

        Destroy(gameObject);
    }
}