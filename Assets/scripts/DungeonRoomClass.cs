using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCell
{
    public GridCell parent;
    public List<GridCell> neighbours;
    public Vector3 worldLocation;

    public GridVector Vec;

    public float g, h;

    //this is here for A*
    public float GetCost()
    {
        return g + h;
    }

    public GridCell()
    {
        parent = null;
        neighbours = new List<GridCell>();
        g = int.MaxValue / 2;
        h = int.MaxValue / 2;
        worldLocation = new Vector3();
        Vec = new GridVector(-1);
    }
}

public class DungeonRoom
{
    public GridCell asCell = null;
    public GameObject asGameObject = null;

    public DungeonRoom(GridCell asCell, GameObject asGameObject)
    {
        this.asCell = asCell;
        this.asGameObject = asGameObject;
    }
}
