using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Network identity for the player. Movement and camera are handled by <see cref="ThirdPersonPlayer"/>.
/// </summary>
[RequireComponent(typeof(ThirdPersonPlayer))]
public class PlayerNetwork : NetworkBehaviour
{
}
