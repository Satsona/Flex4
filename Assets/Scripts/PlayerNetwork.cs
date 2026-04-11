using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class PlayerNetwork : NetworkBehaviour
{
    public float speed = 6f;

    private CharacterController controller;
    private Camera cam;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // SADECE kendi playerında kamera aktif olsun
        if (IsOwner)
        {
            cam = Camera.main;
            cam.transform.SetParent(transform);
            cam.transform.localPosition = new Vector3(0, 10, -8);
            cam.transform.localRotation = Quaternion.Euler(40, 0, 0);
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(h, 0, v);

        controller.Move(move * speed * Time.deltaTime);
    }
}