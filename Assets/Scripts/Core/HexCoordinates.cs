using System.Collections.Generic;
using UnityEngine;

/// <summary>Odd-r offset coordinates for flat-top hex sprites (Q = column, R = row).</summary>
public readonly struct HexCoordinates
{
    public readonly int Q;
    public readonly int R;

    public HexCoordinates(int q, int r)
    {
        Q = q;
        R = r;
    }

    public IEnumerable<HexCoordinates> GetNeighbors()
    {
        if ((Q & 1) == 0)
        {
            yield return new HexCoordinates(Q + 1, R);
            yield return new HexCoordinates(Q + 1, R - 1);
            yield return new HexCoordinates(Q, R - 1);
            yield return new HexCoordinates(Q - 1, R);
            yield return new HexCoordinates(Q - 1, R - 1);
            yield return new HexCoordinates(Q, R + 1);
        }
        else
        {
            yield return new HexCoordinates(Q + 1, R + 1);
            yield return new HexCoordinates(Q + 1, R);
            yield return new HexCoordinates(Q, R - 1);
            yield return new HexCoordinates(Q - 1, R + 1);
            yield return new HexCoordinates(Q - 1, R);
            yield return new HexCoordinates(Q, R + 1);
        }
    }

    public int DistanceTo(HexCoordinates other)
    {
        var a = ToAxial();
        var b = other.ToAxial();
        return (Mathf.Abs(a.q - b.q) + Mathf.Abs(a.q + a.r - b.q - b.r) + Mathf.Abs(a.r - b.r)) / 2;
    }

    /// <summary>Flat-top odd-r layout. hexSize = outer radius of one hex.</summary>
    public Vector3 ToWorldPosition(float hexSize)
    {
        float x = hexSize * 1.5f * Q;
        float y = hexSize * Mathf.Sqrt(3f) * (R + (Q & 1) * 0.5f);
        return new Vector3(x, y, 0f);
    }

    public static HexCoordinates FromWorldPosition(Vector3 world, float hexSize)
    {
        float q = (2f / 3f * world.x) / hexSize;
        float r = (-1f / 3f * world.x + Mathf.Sqrt(3f) / 3f * world.y) / hexSize;
        var axial = RoundAxial(q, r);
        return FromAxial(axial.q, axial.r);
    }

    (int q, int r) ToAxial()
    {
        int q = Q;
        int r = R - (Q - (Q & 1)) / 2;
        return (q, r);
    }

    static HexCoordinates FromAxial(int q, int r)
    {
        int col = q;
        int row = r + (q - (q & 1)) / 2;
        return new HexCoordinates(col, row);
    }

    static (int q, int r) RoundAxial(float q, float r)
    {
        float s = -q - r;
        int rq = Mathf.RoundToInt(q);
        int rr = Mathf.RoundToInt(r);
        int rs = Mathf.RoundToInt(s);

        float qDiff = Mathf.Abs(rq - q);
        float rDiff = Mathf.Abs(rr - r);
        float sDiff = Mathf.Abs(rs - s);

        if (qDiff > rDiff && qDiff > sDiff)
            rq = -rr - rs;
        else if (rDiff > sDiff)
            rr = -rq - rs;

        return (rq, rr);
    }

    public override string ToString() => $"({Q},{R})";

    public override bool Equals(object obj) =>
        obj is HexCoordinates other && Q == other.Q && R == other.R;

    public override int GetHashCode() => Q * 997 + R;

    public static bool operator ==(HexCoordinates a, HexCoordinates b) => a.Q == b.Q && a.R == b.R;
    public static bool operator !=(HexCoordinates a, HexCoordinates b) => !(a == b);
}
