using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class NetworkUIManager : MonoBehaviour
{
    private VisualElement root;
    private Button hostButton;
    private Button clientButton;
    private Button serverButton;
    private Button moveButton;
    private Label statusLabel;

    private void OnEnable()
    {
        UIDocument uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("NetworkUIManager requires a UIDocument on the same GameObject.");
            enabled = false;
            return;
        }

        root = uiDocument.rootVisualElement;

        hostButton = CreateButton("Host");
        clientButton = CreateButton("Client");
        serverButton = CreateButton("Server");
        moveButton = CreateButton("Move");
        statusLabel = new Label("Not Connected");

        root.Clear();
        root.Add(hostButton);
        root.Add(clientButton);
        root.Add(serverButton);
        root.Add(moveButton);
        root.Add(statusLabel);

        hostButton.clicked += OnHostClicked;
        clientButton.clicked += OnClientClicked;
        serverButton.clicked += OnServerClicked;
        moveButton.clicked += OnMoveClicked;
    }

    private void OnDisable()
    {
        if (hostButton != null) hostButton.clicked -= OnHostClicked;
        if (clientButton != null) clientButton.clicked -= OnClientClicked;
        if (serverButton != null) serverButton.clicked -= OnServerClicked;
        if (moveButton != null) moveButton.clicked -= OnMoveClicked;
    }

    private Button CreateButton(string text)
    {
        Button btn = new Button();
        btn.text = text;
        btn.style.width = 200;
        btn.style.height = 36;
        btn.style.marginBottom = 6;
        return btn;
    }

    private void Update()
    {
        if (NetworkManager.Singleton == null ||
            hostButton == null ||
            clientButton == null ||
            serverButton == null ||
            moveButton == null ||
            statusLabel == null)
        {
            return;
        }

        bool connected = NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer;

        hostButton.style.display = connected ? DisplayStyle.None : DisplayStyle.Flex;
        clientButton.style.display = connected ? DisplayStyle.None : DisplayStyle.Flex;
        serverButton.style.display = connected ? DisplayStyle.None : DisplayStyle.Flex;
        moveButton.style.display = connected ? DisplayStyle.Flex : DisplayStyle.None;

        if (!connected) statusLabel.text = "Not Connected";
        else if (NetworkManager.Singleton.IsHost) statusLabel.text = "Host";
        else if (NetworkManager.Singleton.IsServer) statusLabel.text = "Server";
        else statusLabel.text = "Client";
    }

    private void OnHostClicked()
    {
        NetworkManager.Singleton.StartHost();
    }

    private void OnClientClicked()
    {
        NetworkManager.Singleton.StartClient();
    }

    private void OnServerClicked()
    {
        NetworkManager.Singleton.StartServer();
    }

    private void OnMoveClicked()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
            return;

        NetworkObject playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (playerObject == null)
            return;

        NetworkPlayerController player = playerObject.GetComponent<NetworkPlayerController>();
        if (player == null)
            return;

        player.RequestMove();
    }
}