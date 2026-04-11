using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Third-person movement (WASD relative to camera yaw) and orbit camera (mouse).
/// Works on a plain scene object, or on a <see cref="NetworkObject"/> when spawned as owner.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class ThirdPersonPlayer : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float gravity = -20f;
    [Tooltip("Jump height in meters (uses CharacterController grounded check).")]
    public float jumpHeight = 1.25f;

    [Header("Camera orbit")]
    public float mouseSensitivity = 2f;
    public float cameraDistance = 2.75f;
    [Tooltip("Height of the orbit pivot above the player's feet.")]
    public float pivotHeight = 1.35f;
    [Tooltip("Vertical angle limits (degrees).")]
    public float minPitch = -8f;
    public float maxPitch = 48f;

    [Header("Look target")]
    [Tooltip("Aim point height above feet for the camera to look at.")]
    public float lookAtHeight = 1.35f;

    [Header("Crosshair (temporary — disable or remove later)")]
    public bool showCrosshair = true;
    public Color crosshairColor = new Color(1f, 1f, 1f, 0.9f);
    [Tooltip("Half-length of each arm from the center gap, in pixels.")]
    public float crosshairArmLength = 10f;
    public float crosshairThickness = 2f;
    [Tooltip("Empty space at screen center between the four arms.")]
    public float crosshairGap = 4f;

    private CharacterController controller;
    private Camera mainCamera;
    private NetworkObject networkObject;

    private float yaw;
    private float pitch;
    private float verticalVelocity;

    private static Texture2D s_whitePixel;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        networkObject = GetComponent<NetworkObject>();
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
    }

    void Update()
    {
        if (!ShouldControl())
            return;

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

        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;
            if (Input.GetButtonDown("Jump"))
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * moveSpeed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
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
        if (networkObject == null)
            return true;
        return networkObject.IsSpawned && networkObject.IsOwner;
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
        Quaternion orbit = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(pitch, Vector3.right);
        Vector3 camPos = pivot + orbit * (Vector3.back * cameraDistance);
        mainCamera.transform.position = camPos;
        mainCamera.transform.LookAt(transform.position + Vector3.up * lookAtHeight);
    }
}
