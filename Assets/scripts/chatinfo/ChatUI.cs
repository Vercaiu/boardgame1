using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.VisualScripting;

public class ChatUI : MonoBehaviour
{
    [Header("UI References")]
    public ScrollRect scrollRect;
    public Transform messageContainer;
    public GameObject messagePrefab;
    public TMP_InputField inputField;
    public Button sendButton;

    [Header("Settings")]
    public int maxMessages = 100;

    private List<GameObject> messageObjects = new List<GameObject>();

    void Start()
    {
        // Subscribe to chat events
        if (ChatManager.Instance != null)
        {
            ChatManager.Instance.OnMessageReceived += OnMessageReceived;
        }

        // Setup UI listeners
        sendButton.onClick.AddListener(SendMessage);
        inputField.onSubmit.AddListener(OnInputSubmit);
    }

    void OnDestroy()
    {
        if (ChatManager.Instance != null)
        {
            ChatManager.Instance.OnMessageReceived -= OnMessageReceived;
        }
    }

    void OnInputSubmit(string text)
    {
        SendMessage();
    }

    void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(inputField.text)) return;
        ChatManager.Instance?.SendMessage(inputField.text);
        inputField.text = "";
        inputField.ActivateInputField();
    }

    void OnMessageReceived(ChatMessage message)
    {
        GameObject msgObj = Instantiate(messagePrefab, messageContainer);
        TextMeshProUGUI textComponent = msgObj.GetComponent<TextMeshProUGUI>();

        if (message.isSystemMessage)
        {
            textComponent.text =
                $"<color=#FFFF00>[SYSTEM]</color> {message.message}";
        }
        else
        {
            textComponent.text =
                $"<color=#00FFFF>[{message.senderName}]</color> {message.message}";
        }

        messageObjects.Add(msgObj);

        if (messageObjects.Count > maxMessages)
        {
            GameObject oldestMessage = messageObjects[0];
            messageObjects.RemoveAt(0);
            Destroy(oldestMessage);
        }

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    // Helper method to add system messages from anywhere
    public void LogSystem(string message)
    {
        ChatManager.Instance?.SendSystemMessage(message);
    }
}