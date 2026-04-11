using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonPlayer : NetworkBehaviour
{
    static readonly int RunningParam = Animator.StringToHash("Running");
    static readonly int JumpingParam = Animator.StringToHash("Jumping");

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

    [Header("Crosshair (temporary — disable or remove later)")]
    public bool showCrosshair = true;
    public Color crosshairColor = new Color(1f, 1f, 1f, 0.9f);
    public float crosshairArmLength = 10f;
    public float crosshairThickness = 2f;
    public float crosshairGap = 4f;

    CharacterController controller;
    Camera mainCamera;

    float yaw;
    float pitch;
    float verticalVelocity;

    bool hasJumpTrigger;
    Vector3 lastPosition;

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
        }

        lastPosition = transform.position;
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

        if (ShouldControl())
            Cursor.lockState = CursorLockMode.Locked;

        lastPosition = transform.position;
    }

    void Update()
    {
        bool isOwner = ShouldControl();

        if (isOwner)
            UpdateOwnerInputAndMovement();
        else
            UpdateRemoteAnimation();

        lastPosition = transform.position;
    }

    void UpdateOwnerInputAndMovement()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Cursor.lockState = CursorLockMode.None;

        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            Cursor.lockState = CursorLockMode.Locked;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        yaw += mouseX;
        pitch = Mathf.Clamp(pitch - mouseY, minPitch, maxPitch);

        UpdateCamera();

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);
        Vector3 forward = yawRot * Vector3.forward;
        Vector3 right = yawRot * Vector3.right;
        Vector3 move = forward * v + right * h;

        if (move.sqrMagnitude > 1f)
            move.Normalize();

        bool grounded = controller.isGrounded;
        bool wantsRunAnim = grounded && (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f);

        if (animator != null)
            animator.SetBool(RunningParam, wantsRunAnim);

        Vector3 face = new Vector3(move.x, 0f, move.z);
        if (face.sqrMagnitude > 1e-6f)
        {
            Quaternion target = Quaternion.LookRotation(face.normalized);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                target,
                rotationSpeed * Time.deltaTime
            );
        }

        bool jumped = false;

        if (grounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (Input.GetButtonDown("Jump"))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumped = true;
            }
        }

        if (jumped && animator != null && hasJumpTrigger)
        {
            animator.SetTrigger(JumpingParam); // owner kendi ekranında hemen görsün
            SendJumpTriggerServerRpc();        // diğerlerine de gitsin
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * moveSpeed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    void UpdateRemoteAnimation()
    {
        if (animator == null)
            return;

        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;

        bool isRunning = delta.sqrMagnitude > remoteMoveThreshold;
        animator.SetBool(RunningParam, isRunning);
    }

    [ServerRpc]
    void SendJumpTriggerServerRpc()
    {
        PlayJumpTriggerClientRpc();
    }

    [ClientRpc]
    void PlayJumpTriggerClientRpc()
    {
        if (IsOwner)
            return; // owner zaten kendi tarafında oynattı

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

    void OnGUI()
    {
        if (!showCrosshair || !ShouldControl() || Cursor.lockState != CursorLockMode.Locked)
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

    bool ShouldControl()
    {
        return IsSpawned && IsOwner;
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
}