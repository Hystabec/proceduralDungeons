using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class prioQue
{
    List<GridCell> cellQueue = new();

    public void Push(GridCell newCell)
    {
        for(int i  = 0; i < cellQueue.Count; i++)
        {
            if(newCell.GetCost() < cellQueue[i].GetCost())
            {
                cellQueue.Insert(i, newCell);
                return;
            }
        }

        //if it gets here add on the end of the queue
        cellQueue.Add(newCell);
    }

    public void Pop()
    {
        cellQueue.RemoveAt(0);
    }

    public GridCell Top()
    {
        return cellQueue[0];
    }

    public bool Empty()
    {
        if(cellQueue.Count == 0)
            return true;
        
        return false;
    }
}

class yList
{
    public List<DungeonRoom> roomList = new();

    public int YindexForZero = 0, YMaxIndex = 0, YMinIndex = 0;
}

public class prioSortList
{
    List<yList> cellList = new();
    
    int XindexForZero = 0;

    int XMaxIndex = 0;
    int XMinIndex = 0;

    public void Add(DungeonRoom cell)
    {
        if(cellList.Count == 0)
        {
            cellList.Add(new yList());
            cellList[0].roomList.Add(null);
        }

        while(cell.asCell.x < XMinIndex)
        {
            var temp = new yList();
            temp.roomList.Add(null);
            cellList.Insert(0, temp);
            XMinIndex--;
            XindexForZero++;
        }

        while(cell.asCell.x > XMaxIndex)
        {
            var temp = new yList();
            temp.roomList.Add(null);
            cellList.Add(temp);
            XMaxIndex++;
        }

        while(cell.asCell.y < cellList[XindexForZero + cell.asCell.x].YMinIndex)
        {
            cellList[XindexForZero+cell.asCell.x].roomList.Insert(0, null);
            cellList[XindexForZero + cell.asCell.x].YMinIndex--;
            cellList[XindexForZero + cell.asCell.x].YindexForZero++;
        }

        while(cell.asCell.y > cellList[XindexForZero + cell.asCell.x].YMaxIndex)
        {
            cellList[XindexForZero + cell.asCell.x].roomList.Add(null);
            cellList[XindexForZero + cell.asCell.x].YMaxIndex++;
        }

        var tempIndexFromZero = cellList[XindexForZero + cell.asCell.x].YindexForZero;
        cellList[XindexForZero + cell.asCell.x].roomList[tempIndexFromZero + cell.asCell.y] = cell;
    }

    public void Delete(DungeonRoom cell) 
    {
        var tempIndexFromZero = cellList[XindexForZero + cell.asCell.x].YindexForZero;
        cellList[XindexForZero + cell.asCell.x].roomList[tempIndexFromZero + cell.asCell.y] = null;
    }

    public DungeonRoom Find(Vector2Int cellLoc)
    {
        if(cellLoc.x > XMaxIndex)
            return null;

        if(cellLoc.x < 0-XindexForZero)
            return null;

        if(cellLoc.y > cellList[XindexForZero + cellLoc.x].YMaxIndex)
            return null;

        if(cellLoc.y < 0 - cellList[XindexForZero+cellLoc.x].YindexForZero) 
            return null;

        var tempIndexFromZero = cellList[XindexForZero + cellLoc.x].YindexForZero;
        return cellList[XindexForZero + cellLoc.x].roomList[tempIndexFromZero + cellLoc.y];
    }

    public void Clear()
    {
        foreach (yList List in cellList)
        {
            List.roomList.Clear();
            List.YindexForZero = 0;
            List.YMaxIndex = 0;
            List.YMinIndex = 0;
        }

        cellList.Clear();
    }
}
