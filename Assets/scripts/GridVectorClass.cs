using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GridVector
{
    public readonly int x, y;

    public GridVector()
    {
        x = 0; y = 0;
    }

    public GridVector(int x)
    {
        this.x = x;
        this.y = x;
    }

    public GridVector(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(x, y);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as GridVector);
    }

    public bool Equals(GridVector gv)
    {
        return gv != null && (this.x == gv.x && this.y == gv.y);
    }

    public static GridVector operator +(GridVector v1, GridVector v2) => new GridVector(v1.x + v2.x, v1.y + v2.y);
    public static GridVector operator -(GridVector v1, GridVector v2) => new GridVector(v1.x - v2.x, v1.y - v2.y);
}