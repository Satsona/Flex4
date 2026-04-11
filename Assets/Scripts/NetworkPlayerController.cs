using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerController : NetworkBehaviour
{
    public NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>();

    public override void OnNetworkSpawn()
    {
        Position.OnValueChanged += OnPositionChanged;

        if (IsOwner)
        {
            RequestMove();
        }
    }

    public override void OnNetworkDespawn()
    {
        Position.OnValueChanged -= OnPositionChanged;
    }

    private void OnPositionChanged(Vector3 oldPos, Vector3 newPos)
    {
        transform.position = newPos;
    }

    public void RequestMove()
    {
        RequestMoveServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void RequestMoveServerRpc(RpcParams rpcParams = default)
    {
        Vector3 randomPosition = new Vector3(
            Random.Range(-3f, 3f),
            1f,
            Random.Range(-3f, 3f)
        );

        Position.Value = randomPosition;
    }
}