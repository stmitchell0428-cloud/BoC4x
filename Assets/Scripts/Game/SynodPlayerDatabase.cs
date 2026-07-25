using UnityEngine;

public static class SynodPlayerDatabase
{
    public const int MaxPlayers = 4;

    public static string DisplayName(SynodPlayerId id) => id switch
    {
        SynodPlayerId.Player1 => "Your Synod",
        SynodPlayerId.Player2 => "Strasbourg Synod",
        SynodPlayerId.Player3 => "Magdeburg Synod",
        SynodPlayerId.Player4 => "Nuremberg Synod",
        _ => "Synod"
    };

    public static string DefaultCapitalName(SynodPlayerId id) => id switch
    {
        SynodPlayerId.Player1 => "Wittenberg",
        SynodPlayerId.Player2 => "Strasbourg",
        SynodPlayerId.Player3 => "Magdeburg",
        SynodPlayerId.Player4 => "Nuremberg",
        _ => "Synod City"
    };

    public static Color ColorFor(SynodPlayerId id) => id switch
    {
        SynodPlayerId.Player1 => new Color(0.25f, 0.45f, 0.85f),
        SynodPlayerId.Player2 => new Color(0.18f, 0.58f, 0.78f),
        SynodPlayerId.Player3 => new Color(0.32f, 0.36f, 0.82f),
        SynodPlayerId.Player4 => new Color(0.22f, 0.62f, 0.68f),
        _ => new Color(0.25f, 0.45f, 0.85f)
    };
}
