using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using static UnityEngine.UIElements.VisualElement;
using Unity.VisualScripting;
using Unity.Netcode;

public class FieldCycleController : MonoBehaviour
{
    [Header("Navigation")]
    private List<ulong> clientIds = new List<ulong>();
    private int currentIndex = 0;

    [Header("Player Selection UI")]
    public Transform playerButtonContainer;
    public GameObject playerButtonPrefab;

    private List<GameObject> playerButtons = new List<GameObject>();
    private bool buttonsCreated = false;

    void Start()
    {
        UpdateClientList();
    }

    void UpdateClientList()
    {
        if (FieldUIManager.Instance != null)
        {
            clientIds = FieldUIManager.Instance.GetAllClientIds();

            // Find the index of the local player
            ulong localClientId = FieldUIManager.Instance.GetLocalPlayerClientId();
            currentIndex = clientIds.IndexOf(localClientId);

            if (currentIndex == -1)
            {
                currentIndex = 0;
            }

            Debug.Log($"FieldCycleController: Found {clientIds.Count} fields, starting at index {currentIndex}");
        }
    }

    public void ViewNextField()
    {
        if (clientIds.Count == 0)
        {
            UpdateClientList();
            return;
        }

        currentIndex = (currentIndex + 1) % clientIds.Count;
        ShowCurrentField();
    }

    public void ViewPreviousField()
    {
        if (clientIds.Count == 0)
        {
            UpdateClientList();
            return;
        }

        currentIndex--;
        if (currentIndex < 0)
        {
            currentIndex = clientIds.Count - 1;
        }
        ShowCurrentField();
    }

    public void ViewMyField()
    {
        if (FieldUIManager.Instance != null)
        {
            ulong localClientId = FieldUIManager.Instance.GetLocalPlayerClientId();
            currentIndex = clientIds.IndexOf(localClientId);

            if (currentIndex == -1)
            {
                Debug.LogWarning("FieldCycleController: Local player field not found!");
                return;
            }

            ShowCurrentField();
        }
    }

    void ShowCurrentField()
    {
        if (clientIds.Count == 0 || currentIndex < 0 || currentIndex >= clientIds.Count)
        {
            Debug.LogWarning("FieldCycleController: Invalid index or empty client list");
            return;
        }

        ulong clientIdToShow = clientIds[currentIndex];
        FieldUIManager.Instance?.ShowField(clientIdToShow);

        Debug.Log($"FieldCycleController: Showing field {currentIndex + 1}/{clientIds.Count} (Client ID: {clientIdToShow})");
    }

    // Call this method to create the player buttons (they stay permanently)
    public void CreatePlayerButtons()
    {
        if (buttonsCreated)
        {
            Debug.Log("FieldCycleController: Buttons already created!");
            return;
        }

        if (playerButtonContainer == null || playerButtonPrefab == null)
        {
            Debug.LogError("FieldCycleController: Player button UI references not set!");
            return;
        }

        // Update the client list
        UpdateClientList();

        // Clear any existing buttons (just in case)
        foreach (GameObject button in playerButtons)
        {
            Destroy(button);
        }
        playerButtons.Clear();

        // Create a button for each player
        ulong localClientId = FieldUIManager.Instance.GetLocalPlayerClientId();

        foreach (ulong clientId in clientIds)
        {
            GameObject buttonObj = Instantiate(playerButtonPrefab, playerButtonContainer);
            playerButtons.Add(buttonObj);

            // Set button text
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                if (clientId == localClientId)
                {
                    buttonText.text = $"Client {clientId} (You)";
                }
                else
                {
                    buttonText.text = $"Client {clientId}";
                }
            }

            // Add button click listener
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                ulong capturedClientId = clientId; // Capture for lambda
                button.onClick.AddListener(() => ViewPlayerField(capturedClientId));
            }

            Debug.Log($"FieldCycleController: Created button for Client {clientId}");
        }

        buttonsCreated = true;
        Debug.Log($"FieldCycleController: Created {playerButtons.Count} player buttons");
    }

    // Called when a player button is clicked
    void ViewPlayerField(ulong clientId)
    {
        // Find the index of this client
        currentIndex = clientIds.IndexOf(clientId);

        if (currentIndex != -1)
        {
            ShowCurrentField();
        }
        else
        {
            Debug.LogWarning($"FieldCycleController: Client {clientId} not found in list!");
        }
    }

    // Add this method to your existing FieldCycleController class

    public void CreatePlayerButtonsInOrder(List<ulong> orderedClientIds)
    {
        if (playerButtonContainer == null || playerButtonPrefab == null)
        {
            Debug.LogError("FieldCycleController: Player button UI references not set!");
            return;
        }

        // Clear existing buttons
        foreach (GameObject button in playerButtons)
        {
            Destroy(button);
        }
        playerButtons.Clear();

        clientIds = new List<ulong>(orderedClientIds);

        ulong localClientId = NetworkManager.Singleton.LocalClientId;

        for (int i = 0; i < clientIds.Count; i++)
        {
            ulong clientId = clientIds[i];

            GameObject buttonObj = Instantiate(playerButtonPrefab, playerButtonContainer);
            playerButtons.Add(buttonObj);

            PlayerStateNet player = PlayerStateNet.GetPlayer(clientId);

            string playerName = player != null
                ? player.GetPlayerName()
                : $"Client {clientId}";

            // Set button text
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                string playerLabel = $"Player {i + 1}: {playerName}";

                if (clientId == localClientId)
                {
                    playerLabel += " (You)";
                }

                buttonText.text = playerLabel;
            }

            // Button click
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                ulong capturedClientId = clientId;
                button.onClick.AddListener(() => ViewPlayerField(capturedClientId));
            }
        }

        buttonsCreated = true;
    }

    // Optional: Call this to refresh buttons if players join/leave
    public void RefreshPlayerButtons()
    {
        buttonsCreated = false;
        CreatePlayerButtons();
    }
}