using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spade phase: the dealer shields itself with a ring of cards. Each card is a wall with one
/// hit of health. After a few seconds the ring pushes outward and every card slides until it
/// hits something that is not the ball. If the deal shows a joker, the cards are flung much
/// harder and ricochet around the whole playfield for a while. The joker odds rise as the
/// boss timer runs low.
/// </summary>
public class SpadePhase : BossPhase
{
    [Header("Shield")]
    [SerializeField] private ShieldCard cardPrefab;
    [SerializeField, Min(3)] private int cardCount = 8;
    [SerializeField, Min(1f)] private float ringRadius = 3.6f;
    [SerializeField, Min(0f)] private float minHoldTime = 3f;
    [SerializeField, Min(0f)] private float maxHoldTime = 5f;

    [Header("Push")]
    [SerializeField, Min(0.1f)] private float launchSpeed = 6f;

    [Header("Joker (calm -> urgent)")]
    [SerializeField, Range(0f, 1f)] private float jokerChanceCalm = 0.1f;
    [SerializeField, Range(0f, 1f)] private float jokerChanceUrgent = 0.6f;
    [SerializeField, Min(0.1f)] private float jokerSpeed = 14f;
    [Tooltip("How long joker cards keep ricocheting around the table before vanishing.")]
    [SerializeField, Min(0.1f)] private float jokerBounceTime = 5f;

    private readonly List<ShieldCard> cards = new List<ShieldCard>();
    private Coroutine routine;
    private bool launched;

    protected override void OnBegin()
    {
        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(Run());
    }

    protected override void OnEnd()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        // Don't leave a standing shield behind when the next phase starts.
        if (!launched)
            LaunchCards(false);
    }

    public override void Cleanup()
    {
        OnEnd();
        foreach (ShieldCard card in cards)
        {
            if (card != null)
                Destroy(card.gameObject);
        }
        cards.Clear();
    }

    private IEnumerator Run()
    {
        DealRing();
        yield return new WaitForSeconds(Random.Range(minHoldTime, maxHoldTime));

        bool joker = Random.value < Mathf.Lerp(jokerChanceCalm, jokerChanceUrgent, Urgency);
        LaunchCards(joker);
        routine = null;
    }

    private void DealRing()
    {
        cards.RemoveAll(c => c == null);
        launched = false;

        if (cardPrefab == null || Boss == null)
        {
            Debug.LogWarning("Spade phase needs a shield card prefab assigned.", this);
            return;
        }

        Vector3 center = Boss.transform.position;
        for (int i = 0; i < cardCount; i++)
        {
            float angle = i * Mathf.PI * 2f / cardCount;
            Vector3 dir = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            Vector3 pos = center + dir * ringRadius;
            ShieldCard card = Instantiate(cardPrefab, pos, Quaternion.LookRotation(dir, Vector3.up));
            card.SetOutwardDirection(dir);
            cards.Add(card);
        }
    }

    private void LaunchCards(bool joker)
    {
        launched = true;
        cards.RemoveAll(c => c == null);
        if (cards.Count == 0)
            return;

        int jokerIndex = joker ? Random.Range(0, cards.Count) : -1;
        for (int i = 0; i < cards.Count; i++)
        {
            ShieldCard card = cards[i];
            if (i == jokerIndex)
                card.ShowJoker();

            card.Launch(joker ? jokerSpeed : launchSpeed, joker ? jokerBounceTime : 0f);
        }
    }
}
