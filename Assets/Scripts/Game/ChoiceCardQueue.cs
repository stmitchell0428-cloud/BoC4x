using System;
using System.Collections.Generic;

/// <summary>One turn-start choice card per player turn (FIFO by registered order).</summary>
public static class ChoiceCardQueue
{
    struct Entry
    {
        public int Order;
        public Func<bool> TryPresent;
    }

    static readonly List<Entry> pending = new();
    static int registeredTurn = -1;
    static bool served;

    public const int OrderNarrative = 0;
    public const int OrderLiturgical = 1;
    public const int OrderPastoral = 2;
    public const int OrderTestimony = 3;

    public static void Register(int order, Func<bool> tryPresent)
    {
        if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn)
            return;

        int turn = TurnManager.Instance.TurnNumber;
        if (turn != registeredTurn)
        {
            pending.Clear();
            registeredTurn = turn;
            served = false;
        }

        pending.Add(new Entry { Order = order, TryPresent = tryPresent });
    }

    public static void ProcessTurnStart()
    {
        if (served || ChoiceCardBlocking.BlocksOtherEvents())
            return;

        pending.Sort((a, b) => a.Order.CompareTo(b.Order));
        foreach (var entry in pending)
        {
            if (entry.TryPresent())
            {
                served = true;
                break;
            }
        }

        pending.Clear();
    }
}
