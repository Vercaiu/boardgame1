// SimpleMultiplayer.cs
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;
using TMPro;
using System.Threading.Tasks;
using Unity.Networking.Transport.Relay;

public class SimpleMultiplayer : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField joinCodeInput;
    public TextMeshProUGUI players;
    public GameObject setup;
    public GameObject startgame;
    private const int MaxPlayers = 4;
    public TMP_InputField playerNameInput;

    void SavePlayerName()
    {
        string name = playerNameInput.text.Trim();
        if (string.IsNullOrEmpty(name))
            name = "Player" + Random.Range(100, 999);
        PlayerPrefs.SetString("PlayerName", name);
    }

    async Task InitUnityServices()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    void OnClientConnected(ulong clientId)
    {
        UpdatePlayerCount();
    }

    void OnClientDisconnected(ulong clientId)
    {
        UpdatePlayerCount();
    }

    void UpdatePlayerCount()
    {
        if (NetworkManager.Singleton == null) return;
        if (!NetworkManager.Singleton.IsServer) return;
        int count = NetworkManager.Singleton.ConnectedClients.Count;
        players.text = $"{count}/{MaxPlayers}";
    }

    public async void StartHost()
    {
        SavePlayerName();
        await InitUnityServices();
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers - 1);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        Debug.Log("Join Code: " + joinCode);
        if (joinCodeInput != null)
            joinCodeInput.text = joinCode;
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));
        NetworkManager.Singleton.StartHost();
        UpdatePlayerCount();
    }

    public async void StartClient()
    {
        SavePlayerName();
        await InitUnityServices();
        if (joinCodeInput == null || string.IsNullOrEmpty(joinCodeInput.text))
        {
            Debug.LogError("No join code entered!");
            return;
        }
        string joinCode = joinCodeInput.text.Trim().ToUpper();
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));
        NetworkManager.Singleton.StartClient();
    }
}