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
    public TMP_InputField joinCodeInput; // Assign in inspector

    private async Task InitUnityServices()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void StartHost()
    {
        await InitUnityServices();

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        Debug.Log("Join Code: " + joinCode);

        if (joinCodeInput != null)
            joinCodeInput.text = joinCode; // Display for players to share

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));

        NetworkManager.Singleton.StartHost();
    }

    public async void StartClient()
    {
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
