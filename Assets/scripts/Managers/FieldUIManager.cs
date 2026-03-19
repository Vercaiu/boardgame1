using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class FieldUIManager : MonoBehaviour
{
    public static FieldUIManager Instance;
    public GameObject fieldUIPrefab;
    public Sprite[] cardSprites;
    public Sprite cardBackSprite;

    private Dictionary<ulong, GameObject> fields = new();
    private ulong localPlayerClientId;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Debug.Log("FieldUIManager: Start");

        // Don't create immediately, wait for network spawn
        StartCoroutine(InitializeAfterSpawn());

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    System.Collections.IEnumerator InitializeAfterSpawn()
    {
        // Wait a frame for network objects to spawn
        yield return new WaitForSeconds(0.5f);

        foreach (var field in FindObjectsOfType<PlayerStateNet>())
        {
            if (!fields.ContainsKey(field.ClientId.Value))
            {
                CreateUI(field);
            }
        }
    }

    void OnClientConnected(ulong clientId)
    {
        Debug.Log($"FieldUIManager: Client {clientId} connected");

        // Wait a bit for PlayerStateNet to spawn
        StartCoroutine(CreateUIAfterDelay());
    }

    System.Collections.IEnumerator CreateUIAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);

        foreach (var field in FindObjectsOfType<PlayerStateNet>())
        {
            if (!fields.ContainsKey(field.ClientId.Value))
            {
                Debug.Log($"FieldUIManager: Creating UI for newly discovered player {field.ClientId.Value}");
                CreateUI(field);
            }
        }
    }

    void CreateUI(PlayerStateNet state)
    {

        Debug.Log("is the UI ever actually created");
        var ui = Instantiate(fieldUIPrefab);
        fields[state.ClientId.Value] = ui;

        // Track local player's client ID
        if (state.IsOwner)
        {
            localPlayerClientId = state.ClientId.Value;
        }

        // Initialize LocalHandUI
        LocalHandUI handUI = ui.GetComponent<LocalHandUI>();
        if (handUI != null)
        {
            Debug.Log("initializing localhand displayyyyyy");
            handUI.cardSprites = cardSprites;
            handUI.cardBackSprite = cardBackSprite;
            handUI.Initialize(state);
        }
        else
        {
            Debug.LogError($"FieldUIManager: No LocalHandUI found on field prefab!");
        }

        // Initialize localfieldUI
        localfieldUI fieldUI = ui.GetComponent<localfieldUI>();
        if (fieldUI != null)
        {
            Debug.Log("initializing localfield displayyyyyy");
            fieldUI.cardSprites = cardSprites;
            fieldUI.Initialize(state);
        }
        else
        {
            Debug.LogError($"FieldUIManager: No localfieldUI found on field prefab!");
        }

        // Initialize PlayerBreakdownDisplay
        PlayerBreakdownDisplay breakdownDisplay = ui.GetComponent<PlayerBreakdownDisplay>();
        if (breakdownDisplay != null)
        {
            Debug.Log("initializing breakdown displayyyyyy");
            breakdownDisplay.SetClientId(state.ClientId.Value);
        }
        else
        {
            Debug.LogWarning($"FieldUIManager: No PlayerBreakdownDisplay found on field prefab for client {state.ClientId.Value}");
        }

        // Hide all fields except the local player's field initially
        CanvasGroup canvasGroup = ui.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = ui.AddComponent<CanvasGroup>();
        }

        if (state.IsOwner)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        else
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public GameObject GetFieldForClient(ulong clientId)
    {
        if (fields.TryGetValue(clientId, out GameObject field))
        {
            return field;
        }
        return null;
    }

    public List<ulong> GetAllClientIds()
    {
        return new List<ulong>(fields.Keys);
    }

    public ulong GetLocalPlayerClientId()
    {
        return localPlayerClientId;
    }

    public void ShowField(ulong clientId)
    {
        // Hide all fields using CanvasGroup
        foreach (var kvp in fields)
        {
            CanvasGroup cg = kvp.Value.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
        }

        // Show the requested field
        if (fields.TryGetValue(clientId, out GameObject targetField))
        {
            CanvasGroup cg = targetField.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

        }
        else
        {
            Debug.LogWarning($"FieldUIManager: Field not found for client {clientId}");
        }
    }
}