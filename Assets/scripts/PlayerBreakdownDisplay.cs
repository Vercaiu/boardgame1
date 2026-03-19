using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerBreakdownDisplay : MonoBehaviour
{
    [Header("Player Info")]
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI totalScoreText;

    [Header("Score Breakdown - Button+Text on same GameObject")]
    public Button snowgouleButton;
    public Button treeButton;
    public Button fireButton;
    public Button mooseButton;
    public Button batsButton;
    public Button geeseButton;
    public Button cardidbutton;

    public TextMeshProUGUI fieldContentsText;

    [Header("Popup")]
    public GameObject detailPopup;
    public TextMeshProUGUI popupText;

    private ScoreBreakdown currentBreakdown;
    private ulong assignedClientId;
    private bool isInitialized = false;

    void Start()
    {
        // Setup button listeners
        if (snowgouleButton != null)
            snowgouleButton.onClick.AddListener(() => ShowPopup(snowgouleButton.transform, currentBreakdown?.snowgouleDetail));

        if (treeButton != null)
            treeButton.onClick.AddListener(() => ShowPopup(treeButton.transform, currentBreakdown?.treeDetail));

        if (fireButton != null)
            fireButton.onClick.AddListener(() => ShowPopup(fireButton.transform, currentBreakdown?.fireDetail));

        if (mooseButton != null)
            mooseButton.onClick.AddListener(() => ShowPopup(mooseButton.transform, currentBreakdown?.mooseDetail));

        if (batsButton != null)
            batsButton.onClick.AddListener(() => ShowPopup(batsButton.transform, currentBreakdown?.batsDetail));

        if (geeseButton != null)
            geeseButton.onClick.AddListener(() => ShowPopup(geeseButton.transform, currentBreakdown?.geeseDetail));

        if (cardidbutton != null)
            cardidbutton.onClick.AddListener(() => ShowPopup(cardidbutton.transform, currentBreakdown?.cardIdDetail));

        // Hide popup initially
        if (detailPopup != null)
            detailPopup.SetActive(false);

        // Subscribe to score updates
        if (ScoringSystem.Instance != null)
        {
            ScoringSystem.Instance.OnScoresUpdated += OnScoresUpdated;
        }
    }

    void OnDestroy()
    {
        if (ScoringSystem.Instance != null)
        {
            ScoringSystem.Instance.OnScoresUpdated -= OnScoresUpdated;
        }
    }

    public void SetClientId(ulong clientId)
    {
        assignedClientId = clientId;
        isInitialized = true;

        Debug.Log($"PlayerBreakdownDisplay: Set client ID to {clientId}");

        RefreshDisplay();
    }

    void OnScoresUpdated(System.Collections.Generic.Dictionary<ulong, int> scores)
    {
        // Only update if we've been assigned a client ID
        if (isInitialized)
        {
            RefreshDisplay();
        }
    }

    void RefreshDisplay()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("PlayerBreakdownDisplay: Not initialized yet, skipping refresh");
            return;
        }

        if (ScoringSystem.Instance == null)
        {
            Debug.LogWarning("PlayerBreakdownDisplay: ScoringSystem.Instance is null");
            return;
        }

        ScoreBreakdown breakdown = ScoringSystem.Instance.GetScoreBreakdown(assignedClientId);

        if (breakdown == null)
        {
            Debug.LogWarning($"PlayerBreakdownDisplay: No breakdown found for client {assignedClientId}");
            return;
        }

        Debug.Log($"PlayerBreakdownDisplay: Updating display for client {assignedClientId} with score {breakdown.totalScore}");

        UpdateDisplay(assignedClientId, breakdown);
    }

    public void UpdateDisplay(ulong clientId, ScoreBreakdown breakdown)
    {
        Debug.Log($"UpdateDisplay called for {clientId} — total:{breakdown.totalScore} trees:{breakdown.treeScore} fire:{breakdown.fireScore} moose:{breakdown.mooseScore}");

        currentBreakdown = breakdown;

        PlayerStateNet player = PlayerStateNet.GetPlayer(clientId);

      //  bool isLocalPlayer = (Unity.Netcode.NetworkManager.Singleton.LocalClientId == clientId);
        string playerLabel = player.GetPlayerName();

        // Player info
        if (playerNameText != null)
            playerNameText.text = playerLabel;

        if (totalScoreText != null)
            totalScoreText.text = $"Total: {breakdown.totalScore}";

        // Score breakdown
        if (snowgouleButton != null)
        {
            TextMeshProUGUI text = snowgouleButton.GetComponent<TextMeshProUGUI>();
            if (text != null)
                text.text = $"Snowgoule: {FormatScore(breakdown.snowgouleScore)}";
        }

        if (treeButton != null)
        {
            TextMeshProUGUI text = treeButton.GetComponent<TextMeshProUGUI>();
            if (text != null)
                text.text = $"Trees: {FormatScore(breakdown.treeScore)}";
        }

        if (fireButton != null)
        {
            TextMeshProUGUI text = fireButton.GetComponent<TextMeshProUGUI>();
            if (text != null)
                text.text = $"Fire: {FormatScore(breakdown.fireScore)}";
        }

        if (mooseButton != null)
        {
            TextMeshProUGUI text = mooseButton.GetComponent<TextMeshProUGUI>();
            if (text != null)
                text.text = $"Moose: {FormatScore(breakdown.mooseScore)}";
        }

        if (batsButton != null)
        {
            TextMeshProUGUI text = batsButton.GetComponent<TextMeshProUGUI>();
            if (text != null)
                text.text = $"Birds: {FormatScore(breakdown.batsScore)}";
        }

        if (geeseButton != null)
        {
            TextMeshProUGUI text = geeseButton.GetComponent<TextMeshProUGUI>();
            if (text != null)
                text.text = $"Rabbits: {FormatScore(breakdown.geeseScore)}";
        }

        if (cardidbutton != null)
        {
            TextMeshProUGUI text = cardidbutton.GetComponent<TextMeshProUGUI>();
            if (text != null)
                text.text = $"fieled: {FormatScore(breakdown.cardIdScore)}";
        }

        // Field contents
      //  if (fieldContentsText != null)
         //   fieldContentsText.text = breakdown.fieldContents;
    }

    void ShowPopup(Transform buttonTransform, string detail)
    {
        if (detailPopup == null || popupText == null || string.IsNullOrEmpty(detail)) return;

        popupText.text = detail;
        detailPopup.SetActive(true);

        RectTransform popupRect = detailPopup.GetComponent<RectTransform>();
        RectTransform buttonRect = buttonTransform.GetComponent<RectTransform>();

        if (popupRect != null && buttonRect != null)
        {
            Vector3 buttonPos = buttonRect.position;
            popupRect.position = buttonPos + new Vector3(0, 10, 0);
        }

        CancelInvoke(nameof(HidePopup));
        Invoke(nameof(HidePopup), 3f);
    }

    void HidePopup()
    {
        if (detailPopup != null)
            detailPopup.SetActive(false);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && detailPopup != null && detailPopup.activeSelf)
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(
                detailPopup.GetComponent<RectTransform>(),
                Input.mousePosition))
            {
                HidePopup();
            }
        }
    }

    string FormatScore(int score)
    {
        if (score >= 0)
            return $"+{score}";
        else
            return score.ToString();
    }
}

public class ScoreBreakdown
{
    public int totalScore;

    public int snowgouleScore;
    public string snowgouleDetail;

    public int treeScore;
    public string treeDetail;

    public int fireScore;
    public string fireDetail;

    public int mooseScore;
    public string mooseDetail;

    public int batsScore;
    public string batsDetail;

    public int geeseScore;
    public string geeseDetail;

    public int cardIdScore;
    public string cardIdDetail;

    public string fieldContents;
}