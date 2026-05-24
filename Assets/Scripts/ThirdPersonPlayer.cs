using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonPlayer : NetworkBehaviour
{
    static readonly int RunningParam = Animator.StringToHash("Running");
    static readonly int JumpingParam = Animator.StringToHash("Jumping");
    static readonly int AimingParam = Animator.StringToHash("Aiming");
    static readonly int ShootingParam = Animator.StringToHash("Shooting");
    static readonly int GroundedParam = Animator.StringToHash("Grounded");
    static readonly int PickingUpParam = Animator.StringToHash("PickingUp");

    [Header("Respawn")]
    public float respawnDelay = 3f;
    public Vector3 randomRespawnMin = new Vector3(-5f, 1f, -5f);
    public Vector3 randomRespawnMax = new Vector3(5f, 1f, 5f);

    private bool isDead;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float gravity = -20f;
    public float jumpHeight = 1.25f;
    public float rotationSpeed = 540f;

    [Header("Camera orbit")]
    public float mouseSensitivity = 2f;
    public float cameraDistance = 2.75f;
    public float pivotHeight = 1.35f;
    public float minPitch = -8f;
    public float maxPitch = 48f;

    [Header("Look target")]
    public float lookAtHeight = 1.35f;

    [Header("Animation")]
    public Animator animator;
    public float remoteMoveThreshold = 0.0001f;

    [Header("Weapon pickup")]
    public string heldWeaponObjectName = "Gun";
    public GameObject heldWeapon;
    public float equipWeaponDelay = 0.35f;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 35f;
    public float bulletLifetime = 3f;
    public float fireCooldown = 0.35f;
    public float aimBeforeShotDelay = 0.45f;
    public float postShotAimTime = 0f;
    public float consumeGunAfterShotDelay = 0.85f;
    public float droppedGunLifetime = 1.5f;
    public float droppedGunForwardForce = 1.5f;
    public float bulletSpawnDistance = 0.65f;
    public float fallbackBulletRadius = 0.06f;

    [Header("Crosshair (temporary — disable or remove later)")]
    public bool showCrosshair = true;
    public Color crosshairColor = new Color(1f, 1f, 1f, 0.9f);
    public float crosshairArmLength = 10f;
    public float crosshairThickness = 2f;
    public float crosshairGap = 4f;

    private CharacterController controller;
    private Camera mainCamera;

    private float yaw;
    private float pitch;
    private bool hasJumpTrigger;
    private bool hasAimingBool;
    private bool hasShootingTrigger;
    private bool hasGroundedBool;
    private bool hasPickingUpTrigger;
    private bool localAiming;
    private bool pickupInProgress;
    private bool gunShotSpent;
    private bool shotQueued;
    private float queuedShotTime;
    private float nextShotTime;
    private float aimUntilTime;
    private Vector3 pendingPickupPosition;
    private Vector3 lastPosition;

    // Server tarafında tutulacak düşey hız
    private readonly NetworkVariable<float> netVerticalVelocity = new NetworkVariable<float>(0f);
    private readonly NetworkVariable<bool> netAiming = new NetworkVariable<bool>(false);
    private readonly NetworkVariable<bool> netGrounded = new NetworkVariable<bool>(false);
    private readonly NetworkVariable<bool> netHasGun = new NetworkVariable<bool>(false);

    static Texture2D s_whitePixel;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (animator != null)
        {
            animator.applyRootMotion = false;
            hasJumpTrigger = AnimatorHasTrigger(animator, "Jumping");
            hasAimingBool = AnimatorHasBool(animator, "Aiming");
            hasShootingTrigger = AnimatorHasTrigger(animator, "Shooting");
            hasGroundedBool = AnimatorHasBool(animator, "Grounded");
            hasPickingUpTrigger = AnimatorHasTrigger(animator, "PickingUp");
        }

        if (heldWeapon == null)
            heldWeapon = FindChildByName(transform, heldWeaponObjectName);

        SetHeldWeapon(false);

        lastPosition = transform.position;
    }

    public override void OnNetworkSpawn()
    {
        netAiming.OnValueChanged += OnAimingChanged;
        netGrounded.OnValueChanged += OnGroundedChanged;
        netHasGun.OnValueChanged += OnHasGunChanged;
        SetAiming(netAiming.Value);
        SetGrounded(netGrounded.Value);
        SetHeldWeapon(netHasGun.Value);
    }

    public override void OnNetworkDespawn()
    {
        netAiming.OnValueChanged -= OnAimingChanged;
        netGrounded.OnValueChanged -= OnGroundedChanged;
        netHasGun.OnValueChanged -= OnHasGunChanged;
    }

    void Start()
    {
        mainCamera = Camera.main;

        if (mainCamera != null)
        {
            Vector3 toPlayer = transform.position - mainCamera.transform.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude > 0.01f)
                yaw = Quaternion.LookRotation(toPlayer.normalized).eulerAngles.y;

            pitch = 12f;
        }

        if (IsOwner)
            Cursor.lockState = CursorLockMode.Locked;

        WeaponPickup.ConfigureScenePickups(transform);

        lastPosition = transform.position;
    }

    void Update()
    {
        if (IsOwner)
            UpdateOwnerInputAndCamera();

        UpdateAnimation();

        lastPosition = transform.position;
    }

    void UpdateOwnerInputAndCamera()
    {
        bool lockedCursorThisFrame = false;

        if (Input.GetKeyDown(KeyCode.Escape))
            Cursor.lockState = CursorLockMode.None;

        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            lockedCursorThisFrame = true;
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch = Mathf.Clamp(pitch - mouseY, minPitch, maxPitch);

        UpdateCamera();

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool jumpPressed = Input.GetButtonDown("Jump");
        bool wantsAim = Input.GetMouseButton(1);
        bool shootPressed = UpdateShootingInput(wantsAim, lockedCursorThisFrame, out Vector3 shotDirection);

        wantsAim = wantsAim || shotQueued || shootPressed || Time.time < aimUntilTime;
        localAiming = wantsAim;
        SetAiming(wantsAim);

        SubmitMovementServerRpc(h, v, yaw, jumpPressed, wantsAim, shootPressed, shotDirection);
    }

    [ServerRpc]
    void SubmitMovementServerRpc(
        float h,
        float v,
        float ownerYaw,
        bool jumpPressed,
        bool wantsAim,
        bool shootPressed,
        Vector3 shotDirection)
    {
        if (isDead)
            return;

        Quaternion yawRot = Quaternion.Euler(0f, ownerYaw, 0f);
        Vector3 forward = yawRot * Vector3.forward;
        Vector3 right = yawRot * Vector3.right;
        Vector3 move = forward * v + right * h;

        if (move.sqrMagnitude > 1f)
            move.Normalize();

        bool grounded = controller.isGrounded;

        if (grounded)
        {
            if (netVerticalVelocity.Value < 0f)
                netVerticalVelocity.Value = -2f;

            if (jumpPressed)
            {
                netVerticalVelocity.Value = Mathf.Sqrt(jumpHeight * -2f * gravity);
                SetGrounded(false);
                netGrounded.Value = false;

                if (animator != null && hasJumpTrigger)
                    animator.SetTrigger(JumpingParam);

                PlayJumpTriggerClientRpc();
            }
        }

        netVerticalVelocity.Value += gravity * Time.deltaTime;

        Vector3 face = wantsAim ? new Vector3(forward.x, 0f, forward.z) : new Vector3(move.x, 0f, move.z);
        if (face.sqrMagnitude > 1e-6f)
        {
            Quaternion target = Quaternion.LookRotation(face.normalized);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                target,
                rotationSpeed * Time.deltaTime
            );
        }

        Vector3 velocity = move * moveSpeed + Vector3.up * netVerticalVelocity.Value;
        controller.Move(velocity * Time.deltaTime);

        bool groundedAfterMove = controller.isGrounded;
        bool wantsRunAnim = groundedAfterMove && (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f);
        if (animator != null)
            animator.SetBool(RunningParam, wantsRunAnim);

        if (netGrounded.Value != groundedAfterMove)
            netGrounded.Value = groundedAfterMove;

        SetGrounded(groundedAfterMove);

        if (netAiming.Value != wantsAim)
            netAiming.Value = wantsAim;

        SetAiming(wantsAim);

        if (shootPressed)
            FireOnServer(shotDirection);
    }

    void UpdateAnimation()
    {
        if (animator == null)
            return;

        if (IsServer)
            return; // server owner zaten animi movement sırasında set ediyor

        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;

        bool isRunning = delta.sqrMagnitude > remoteMoveThreshold;
        animator.SetBool(RunningParam, isRunning);
    }

    bool UpdateShootingInput(bool aimHeld, bool lockedCursorThisFrame, out Vector3 shotDirection)
    {
        shotDirection = GetAimDirection();

        if (!HasGun)
            return false;

        bool canQueueShot =
            Cursor.lockState == CursorLockMode.Locked &&
            !lockedCursorThisFrame &&
            Input.GetMouseButtonDown(0) &&
            Time.time >= nextShotTime;

        if (canQueueShot)
        {
            bool alreadyAiming = localAiming || aimHeld;
            float delay = alreadyAiming ? 0f : aimBeforeShotDelay;

            shotQueued = true;
            queuedShotTime = Time.time + Mathf.Max(0f, delay);
            nextShotTime = Time.time + Mathf.Max(0.01f, fireCooldown);
            aimUntilTime = queuedShotTime + Mathf.Max(0f, postShotAimTime);
        }

        if (!shotQueued || Time.time < queuedShotTime)
            return false;

        shotQueued = false;
        return true;
    }

    Vector3 GetAimDirection()
    {
        if (mainCamera != null)
            return mainCamera.transform.forward.normalized;

        return transform.forward;
    }

    void FireOnServer(Vector3 shotDirection)
    {
        if (!HasGun)
            return;

        gunShotSpent = true;

        if (shotDirection.sqrMagnitude < 0.0001f)
            shotDirection = transform.forward;

        shotDirection.Normalize();

        SetShootingTrigger();
        PlayShootingTriggerClientRpc();

        Vector3 origin = ResolveBulletOrigin(shotDirection);
        SpawnBulletClientRpc(origin, shotDirection);
        StartCoroutine(ConsumeGunAfterShot());
    }

    Vector3 ResolveBulletOrigin(Vector3 shotDirection)
    {
        if (firePoint != null)
            return firePoint.position;

        return transform.position + Vector3.up * lookAtHeight + shotDirection * bulletSpawnDistance;
    }

    void SpawnLocalBullet(Vector3 origin, Vector3 direction)
    {
        GameObject instance;

        if (bulletPrefab != null)
        {
            instance = Instantiate(bulletPrefab, origin, Quaternion.LookRotation(direction));
        }
        else
        {
            instance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            instance.name = "Bullet";
            instance.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction));
            instance.transform.localScale = Vector3.one * fallbackBulletRadius * 2f;
        }

        foreach (Collider collider in instance.GetComponentsInChildren<Collider>())
            collider.isTrigger = true;

        BulletProjectile projectile = instance.GetComponent<BulletProjectile>();
        if (projectile == null)
            projectile = instance.AddComponent<BulletProjectile>();

        Rigidbody body = instance.GetComponent<Rigidbody>();
        if (body == null)
            body = instance.AddComponent<Rigidbody>();

        body.useGravity = false;
        body.isKinematic = true;

        projectile.Initialize(direction, bulletSpeed, bulletLifetime);
    }

    public bool TryPickUpWeapon(WeaponPickup pickup)
    {
        if (HasGun || pickupInProgress)
            return false;

        if (!IsServer && !IsOwner)
            return false;

        pickupInProgress = true;
        pendingPickupPosition = pickup.transform.position;
        SetPickingUpTrigger();

        if (IsServer)
        {
            PlayPickingUpTriggerClientRpc();
            StartCoroutine(EquipGunAfterDelay());
        }
        else
        {
            StartCoroutine(EquipGunAfterDelay());
            RequestGunPickupServerRpc(pendingPickupPosition);
        }

        return true;
    }

    System.Collections.IEnumerator EquipGunAfterDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, equipWeaponDelay));
        CompleteGunPickup();
    }

    void CompleteGunPickup()
    {
        pickupInProgress = false;
        gunShotSpent = false;
        SetHeldWeapon(true);

        if (IsServer && !netHasGun.Value)
        {
            netHasGun.Value = true;
            HideWeaponPickupClientRpc(pendingPickupPosition);
        }
    }

    System.Collections.IEnumerator ConsumeGunAfterShot()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, consumeGunAfterShotDelay));

        Vector3 dropPosition = heldWeapon != null ? heldWeapon.transform.position : transform.position + Vector3.up;
        Quaternion dropRotation = heldWeapon != null ? heldWeapon.transform.rotation : transform.rotation;
        Vector3 dropVelocity = transform.forward * droppedGunForwardForce + Vector3.up * 1.25f;

        DropHeldGunClientRpc(dropPosition, dropRotation, dropVelocity);

        if (netHasGun.Value)
            netHasGun.Value = false;

        SetHeldWeapon(false);
        gunShotSpent = false;
        SetAiming(false);

        if (netAiming.Value)
            netAiming.Value = false;
    }

    [ServerRpc]
    void RequestGunPickupServerRpc(Vector3 pickupPosition)
    {
        if (netHasGun.Value || pickupInProgress)
            return;

        pickupInProgress = true;
        pendingPickupPosition = pickupPosition;
        SetPickingUpTrigger();
        PlayPickingUpTriggerClientRpc();
        StartCoroutine(EquipGunAfterDelay());
    }

    void SetAiming(bool isAiming)
    {
        if (animator != null && hasAimingBool)
            animator.SetBool(AimingParam, isAiming);
    }

    void SetShootingTrigger()
    {
        if (animator != null && hasShootingTrigger)
            animator.SetTrigger(ShootingParam);
    }

    void SetGrounded(bool isGrounded)
    {
        if (animator != null && hasGroundedBool)
            animator.SetBool(GroundedParam, isGrounded);
    }

    void SetPickingUpTrigger()
    {
        if (animator != null && hasPickingUpTrigger)
            animator.SetTrigger(PickingUpParam);
    }

    void SetHeldWeapon(bool isEquipped)
    {
        if (heldWeapon != null)
            heldWeapon.SetActive(isEquipped);
    }

    bool HasGun
    {
        get
        {
            return !gunShotSpent && (netHasGun.Value || (heldWeapon != null && heldWeapon.activeSelf));
        }
    }

    void OnAimingChanged(bool previousValue, bool currentValue)
    {
        SetAiming(currentValue);
    }

    void OnGroundedChanged(bool previousValue, bool currentValue)
    {
        SetGrounded(currentValue);
    }

    void OnHasGunChanged(bool previousValue, bool currentValue)
    {
        SetHeldWeapon(currentValue);

        if (!currentValue)
            gunShotSpent = false;
    }

    [ClientRpc]
    void PlayShootingTriggerClientRpc()
    {
        if (IsServer)
            return; // server already triggered it

        SetShootingTrigger();
    }

    [ClientRpc]
    void PlayPickingUpTriggerClientRpc()
    {
        if (IsServer)
            return; // server already triggered it

        SetPickingUpTrigger();
    }

    [ClientRpc]
    void HideWeaponPickupClientRpc(Vector3 pickupPosition)
    {
        WeaponPickup.HideClosestAvailableScenePickup(pickupPosition);
    }

    [ClientRpc]
    void DropHeldGunClientRpc(Vector3 position, Quaternion rotation, Vector3 velocity)
    {
        DropHeldGunVisual(position, rotation, velocity);
        SetHeldWeapon(false);
        SetAiming(false);
        aimUntilTime = 0f;
        shotQueued = false;
    }

    [ClientRpc]
    void SpawnBulletClientRpc(Vector3 origin, Vector3 direction)
    {
        SpawnLocalBullet(origin, direction);
    }

    [ClientRpc]
    void PlayJumpTriggerClientRpc()
    {
        if (IsServer)
            return; // server zaten trigger attı

        SetGrounded(false);

        if (animator != null && hasJumpTrigger)
            animator.SetTrigger(JumpingParam);
    }

    static bool AnimatorHasTrigger(Animator anim, string triggerName)
    {
        foreach (var p in anim.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == triggerName)
                return true;
        }
        return false;
    }

    static bool AnimatorHasBool(Animator anim, string boolName)
    {
        foreach (var p in anim.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Bool && p.name == boolName)
                return true;
        }
        return false;
    }

    static GameObject FindChildByName(Transform parent, string childName)
    {
        if (string.IsNullOrEmpty(childName))
            return null;

        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
                return child.gameObject;
        }

        return null;
    }

    void DropHeldGunVisual(Vector3 position, Quaternion rotation, Vector3 velocity)
    {
        if (heldWeapon == null)
            return;

        GameObject dropped = Instantiate(heldWeapon, position, rotation);
        dropped.name = "Spent Gun";
        dropped.SetActive(true);

        foreach (WeaponPickup pickup in dropped.GetComponentsInChildren<WeaponPickup>())
            Destroy(pickup);

        Collider[] colliders = dropped.GetComponentsInChildren<Collider>();
        if (colliders.Length == 0)
            colliders = new Collider[] { dropped.AddComponent<BoxCollider>() };

        foreach (Collider collider in colliders)
            collider.isTrigger = false;

        Rigidbody body = dropped.GetComponent<Rigidbody>();
        if (body == null)
            body = dropped.AddComponent<Rigidbody>();

        body.useGravity = true;
        body.isKinematic = false;
        body.linearVelocity = velocity;

        Destroy(dropped, Mathf.Max(0.1f, droppedGunLifetime));
    }

    void OnGUI()
    {
        if (!showCrosshair || !IsOwner || Cursor.lockState != CursorLockMode.Locked)
            return;

        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;
        float g = crosshairGap * 0.5f;
        float L = crosshairArmLength;
        float t = crosshairThickness;

        Color prev = GUI.color;
        GUI.color = crosshairColor;

        GUI.DrawTexture(new Rect(cx - g - L, cy - t * 0.5f, L, t), WhitePixel);
        GUI.DrawTexture(new Rect(cx + g, cy - t * 0.5f, L, t), WhitePixel);
        GUI.DrawTexture(new Rect(cx - t * 0.5f, cy - g - L, t, L), WhitePixel);
        GUI.DrawTexture(new Rect(cx - t * 0.5f, cy + g, t, L), WhitePixel);

        GUI.color = prev;
    }

    static Texture2D WhitePixel
    {
        get
        {
            if (s_whitePixel == null)
            {
                s_whitePixel = new Texture2D(1, 1);
                s_whitePixel.SetPixel(0, 0, Color.white);
                s_whitePixel.Apply();
                s_whitePixel.hideFlags = HideFlags.HideAndDontSave;
            }
            return s_whitePixel;
        }
    }

    void UpdateCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
                return;
        }

        Vector3 pivot = transform.position + Vector3.up * pivotHeight;
        Quaternion orbit =
            Quaternion.AngleAxis(yaw, Vector3.up) *
            Quaternion.AngleAxis(pitch, Vector3.right);

        Vector3 camPos = pivot + orbit * (Vector3.back * cameraDistance);
        mainCamera.transform.position = camPos;
        mainCamera.transform.LookAt(transform.position + Vector3.up * lookAtHeight);
    }
    public void KillAndRespawn()
    {
        if (!IsServer || isDead)
            return;

        StartCoroutine(RespawnRoutine());
    }

    System.Collections.IEnumerator RespawnRoutine()
    {
        isDead = true;

        SetPlayerVisibleClientRpc(false);

        controller.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        Vector3 respawnPos = new Vector3(
            Random.Range(randomRespawnMin.x, randomRespawnMax.x),
            Random.Range(randomRespawnMin.y, randomRespawnMax.y),
            Random.Range(randomRespawnMin.z, randomRespawnMax.z)
        );

        transform.position = respawnPos;

        netVerticalVelocity.Value = 0f;

        controller.enabled = true;

        SetPlayerVisibleClientRpc(true);

        isDead = false;
    }

    [ClientRpc]
    void SetPlayerVisibleClientRpc(bool visible)
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            r.enabled = visible;

        foreach (Collider c in GetComponentsInChildren<Collider>(true))
            c.enabled = visible;

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = visible;
    }
}
