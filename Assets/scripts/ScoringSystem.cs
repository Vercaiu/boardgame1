using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

public class ScoringSystem : MonoBehaviour
{
    public static ScoringSystem Instance;

    [Header("Snowgoule Scoring")]
    public int snowgouleType0Penalty = -2;
    public int snowgouleType1Penalty = -3;
    public int snowgouleType2Penalty = -4;
    public int snowgouleAllThreeBonus = 10;

    [Header("Tree Scoring")]
    public int pointsPerTreeInChain = 1;

    [Header("Fire Scoring")]
    public int firePointsIncrement = 2;
    public int firePointsDecrement = -3;

    [Header("Moose Scoring")]
    public int mooseFirstPlace = 4, mooseSecondPlace = 2, mooseThirdPlace = 1;

    [Header("Bats Scoring")]
    public int batsFirstPlace = 5, batsSecondPlace = 3, batsThirdPlace = 1;

    [Header("Geese Scoring")]
    public int geeseFirstPlace = 7, geeseSecondPlace = 4, geeseThirdPlace = 2;

    [Header("Card Type Scoring")]
    public int signId0IsolationPoints = 1;
    public int signId1IsolationPoints = 2;
    public int signId2SameGroupPoints = 2;
    public int signId3SameGroupPoints = 4;

    public event System.Action<Dictionary<ulong, int>> OnScoresUpdated;

    private Dictionary<ulong, int> playerScores = new Dictionary<ulong, int>();
    private Dictionary<ulong, ScoreBreakdown> detailedBreakdowns = new Dictionary<ulong, ScoreBreakdown>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RecalculateAllScores()
    {
        playerScores.Clear();
        detailedBreakdowns.Clear();

        var allClientIds = FieldUIManager.Instance.GetAllClientIds();
        var fieldScores = allClientIds.ToDictionary(id => id, id => CalculateFieldScore(id));

        CalculateComparativeScores(fieldScores);

        foreach (var kvp in fieldScores)
        {
            playerScores[kvp.Key] = kvp.Value.GetTotalScore();
            detailedBreakdowns[kvp.Key] = CreateBreakdown(kvp.Key, kvp.Value);
        }

        Debug.Log("=== SCORES RECALCULATED ===");
        foreach (var kvp in playerScores)
            Debug.Log($"Client {kvp.Key}: {kvp.Value} points");

        OnScoresUpdated?.Invoke(playerScores);
    }

    FieldScore CalculateFieldScore(ulong clientId)
    {
        var score = new FieldScore();
        var cards = GetPlayerFieldCards(clientId);
        Debug.Log(cards.Count);
        if (cards.Count == 0) return score;

        score.snowgouleScore = CalculateSnowgouleScore(cards, out _);
        score.treeScore = CalculateTreeScore(cards, out _);
        score.fireScore = CalculateFireScore(cards, out _);
        score.cardIdScore = CalculateSignIdScore(cards, out _, out _, out _);

        foreach (var card in cards)
        {
            score.totalMoose += card.moose;
            score.totalBats += card.bats;
            score.totalGeese += card.geese;
        }

        return score;
    }

    int CalculateSnowgouleScore(List<CardData> cards, out int[] counts)
    {
        counts = new int[3];
        foreach (var card in cards)
        {
            if (card.snowgouleid >= 0 && card.snowgouleid <= 2) counts[card.snowgouleid]++;
            else if (card.snowgouleid == 3) { counts[1]++; counts[2]++; }
        }

        int total = 0;
        bool hasAllThree = counts[0] > 0 && counts[1] > 0 && counts[2] > 0;
        if (hasAllThree)
        {
            total += snowgouleAllThreeBonus;
            counts[0]--; counts[1]--; counts[2]--;
        }

        total += counts[0] * snowgouleType0Penalty + counts[1] * snowgouleType1Penalty + counts[2] * snowgouleType2Penalty;
        return total;
    }

    int CalculateTreeScore(List<CardData> cards, out int longestChain)
    {
        longestChain = 0;
        int current = 0;
        foreach (var card in cards)
        {
            if (card.trees > 0) current += card.trees;
            else { longestChain = Mathf.Max(longestChain, current); current = 0; }
        }
        longestChain = Mathf.Max(longestChain, current);
        return longestChain * pointsPerTreeInChain;
    }

    int CalculateFireScore(List<CardData> cards, out int totalFires)
    {
        totalFires = cards.Sum(c => c.fire);
        if (totalFires <= 4) return totalFires * firePointsIncrement;
        return 4 * firePointsIncrement + (totalFires - 4) * firePointsDecrement;
    }

    int CalculateSignIdScore(List<CardData> cards, out int isolationPts, out int groupPts, out int slopePts)
    {
        isolationPts = 0;
        groupPts = 0;
        slopePts = CalculateSlopeScore(cards);

        for (int i = 0; i < cards.Count; i++)
        {
            int id = cards[i].cardId;
            if (id == 0 || id == 8)
            {
                bool leftMatch = i > 0 && (cards[i - 1].cardId == 0 || cards[i - 1].cardId == 8);
                bool rightMatch = i < cards.Count - 1 && (cards[i + 1].cardId == 0 || cards[i + 1].cardId == 8);
                if (!leftMatch && !rightMatch)
                {
              //      Debug.Log(isolationPts + " are the isolation points");
                    isolationPts += id == 0 ? signId0IsolationPoints : signId1IsolationPoints;
                }

            }

            if (id == 1 || id == 2)
            {
                if (i > 0 && i < cards.Count - 1)
                {
                    int lg = GetSignIdGroup(cards[i - 1].cardId), rg = GetSignIdGroup(cards[i + 1].cardId);
                    if (lg == rg && lg != -1)
                    {
                        groupPts += id == 1 ? signId2SameGroupPoints : signId3SameGroupPoints;
                 //       Debug.Log(groupPts + " are the same group points");
                    }

                }
            }
        }

        return isolationPts + groupPts + slopePts;
    }

    int GetSignIdGroup(int cardId)
    {
        if (cardId == 0 || cardId == 8) return 0;
        if (cardId >= 1 && cardId <= 2) return 1;
        if (cardId >= 3 && cardId <= 6) return 2;
        return -1;
    }

    int CalculateSlopeScore(List<CardData> cards)
    {
        int total = 0, downs = 0, ups = 0;
        bool started = false;

        foreach (var card in cards)
        {
            int id = card.cardId;
            if (id < 3 || id > 6) continue;

            bool isDown = id == 5 || id == 6;
            int magnitude = (id == 4 || id == 6) ? 2 : 1;

            if (isDown)
            {
                if (ups > 0 && started) total += downs * ups;
                downs = magnitude; ups = 0; started = true;
            }
            else if (started)
            {
                ups = Mathf.Min(ups + magnitude, 3);
            }
        }

        if (ups > 0 && started) total += downs * ups;
        return total;
    }

    void CalculateComparativeScores(Dictionary<ulong, FieldScore> fieldScores)
    {
        CalculateMostScore(fieldScores, s => s.totalMoose, (s, p) => s.mooseScore = p, mooseFirstPlace, mooseSecondPlace, mooseThirdPlace);
        CalculateMostScore(fieldScores, s => s.totalBats, (s, p) => s.batsScore = p, batsFirstPlace, batsSecondPlace, batsThirdPlace);
        CalculateMostScore(fieldScores, s => s.totalGeese, (s, p) => s.geeseScore = p, geeseFirstPlace, geeseSecondPlace, geeseThirdPlace);
    }

    void CalculateMostScore(Dictionary<ulong, FieldScore> fieldScores,
        System.Func<FieldScore, int> getTotal, System.Action<FieldScore, int> setScore,
        int first, int second, int third)
    {
        int[] places = { first, second, third };
        int placeIdx = 0;

        foreach (var group in fieldScores.GroupBy(kvp => getTotal(kvp.Value)).OrderByDescending(g => g.Key))
        {
            if (placeIdx >= places.Length) break;
            int count = group.Count();
            int pts = Enumerable.Range(placeIdx, Mathf.Min(count, places.Length - placeIdx)).Sum(i => places[i]) / count;
            foreach (var p in group) setScore(p.Value, pts);
            placeIdx += count;
        }
    }

    ScoreBreakdown CreateBreakdown(ulong clientId, FieldScore fs)
    {
        var cards = GetPlayerFieldCards(clientId);

        CalculateSnowgouleScore(cards, out var sgCounts);
        bool hasAllThree = sgCounts[0] > 0 && sgCounts[1] > 0 && sgCounts[2] > 0;

        CalculateTreeScore(cards, out int longestChain);
        CalculateFireScore(cards, out int totalFires);
        CalculateSignIdScore(cards, out int isoPts, out int grpPts, out int slpPts);

        string fireDetail = totalFires <= 4 ? $"({totalFires}/4 fires)" : $"({totalFires} fires - {totalFires - 4} over limit!)";

        return new ScoreBreakdown
        {
            totalScore = fs.GetTotalScore(),
            snowgouleScore = fs.snowgouleScore,
            snowgouleDetail = (hasAllThree ? "Bonus! " : "") + $"Type0:{sgCounts[0]}, Type1:{sgCounts[1]}, Type2:{sgCounts[2]}",
            treeScore = fs.treeScore,
            treeDetail = $"(longest chain: {longestChain})",
            fireScore = fs.fireScore,
            fireDetail = fireDetail,
            cardIdScore = fs.cardIdScore,
            cardIdDetail = $"(Isolation:{isoPts}, Groups:{grpPts}, Slopes:{slpPts})",
            mooseScore = fs.mooseScore,
            mooseDetail = $"({fs.totalMoose} total)",
            batsScore = fs.batsScore,
            batsDetail = $"({fs.totalBats} total)",
            geeseScore = fs.geeseScore,
            geeseDetail = $"({fs.totalGeese} total)",
            fieldContents = GetFieldContents(cards)
        };
    }

    string GetFieldContents(List<CardData> cards)
    {
        if (cards.Count == 0) return "No cards in field";
        return $"{cards.Count} cards:\n" + string.Join("\n", cards.Select(c =>
            $"• {c.cardName} (T:{c.trees} M:{c.moose} B:{c.bats} F:{c.fire} G:{c.geese} Sn:{c.snowgouleid} Si:{c.cardId})"));
    }

    List<CardData> GetPlayerFieldCards(ulong clientId)
    {
        var cards = new List<CardData>();
        var fieldObj = FieldUIManager.Instance?.GetFieldForClient(clientId);
        if (fieldObj == null) { Debug.Log($"No field obj for {clientId}"); return cards; }

        var fieldUI = fieldObj.GetComponent<localfieldUI>();
        if (fieldUI?.handPanel == null) { Debug.Log($"No handPanel for {clientId}"); return cards; }

        foreach (Transform child in fieldUI.handPanel)
        {
            if (child.name == "__pendingDestroy") continue;
            var cv = child.GetComponent<CardView>();
            if (cv != null) cards.Add(cv.GetData());
        }
        return cards;
    }

    public int GetScore(ulong clientId) => playerScores.TryGetValue(clientId, out int s) ? s : 0;
    public Dictionary<ulong, int> GetAllScores() => new Dictionary<ulong, int>(playerScores);
    public ScoreBreakdown GetScoreBreakdown(ulong clientId) =>
        detailedBreakdowns.TryGetValue(clientId, out var b) ? b : null;

    private class FieldScore
    {
        public int snowgouleScore, treeScore, fireScore, cardIdScore;
        public int mooseScore, batsScore, geeseScore;
        public int totalMoose, totalBats, totalGeese;
        public int GetTotalScore() => snowgouleScore + treeScore + fireScore + cardIdScore + mooseScore + batsScore + geeseScore;
    }
}