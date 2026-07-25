/// <summary>Walther-track crisis categories that can trigger cards and schism.</summary>
public enum CrisisType
{
    None = 0,
    Legalism,
    Antinomian,
    DoctrinalDrift
}

/// <summary>Escalation stage for an active crisis (especially doctrinal drift).</summary>
public enum CrisisStage
{
    None = 0,
    Rumblings,
    Tension,
    Breaking
}
