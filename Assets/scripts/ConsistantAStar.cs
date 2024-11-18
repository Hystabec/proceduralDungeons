using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor.ShaderKeywordFilter;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Experimental.AI;

public class ConsistantAStar : MonoBehaviour
{
    [SerializeField] Vector2Int endGridCell;
    [SerializeField] bool RandomEndCell = false;
    [SerializeField] bool EnsureMinDepth = false, EnsureMaxDepth = false;
    [SerializeField] int minDepth = 0, maxDepth = 0;
    [SerializeField] GameObject roomPrefab;
    [SerializeField] float widthOfPrefab, heightOfPrefab;


    [SerializeField] float wanderMaxChanceOfPlacingARoom = 0.8f, WanderChanceDecreaseEachStep = 0.05f;
    [SerializeField] bool wanderAstarBackToPath = true;


    List<DungeonRoom> placedRooms;
    List<GameObject> placedRoomsAsGameObjects;
    List<GridCell> takenCells;
    Dictionary<GridVector, GridCell> gridAsDictionary;

    #region utilMethods
    GridVector GetRandomCellInRadius(int minDistance, int  maxDistance)
    {
        //this works on the idea that cells going diaganal is one step not two

        int x = UnityEngine.Random.Range(0, maxDistance);

        minDistance = Math.Max(minDistance - x, 0);
        maxDistance = Math.Max(maxDistance - x, 0);

        int y = UnityEngine.Random.Range(minDistance, maxDistance);

        if (UnityEngine.Random.Range(0, 2) == 1)
            x = 0 - x;

        if (UnityEngine.Random.Range(0, 2) == 1)
            y = 0 - y;

        return new GridVector(x, y);
    }

    void SetAroundCells(GridCell cellToGetAround)
    {
        //this finds the cells around and also populates the grid as it goes

        GridVector vec = cellToGetAround.Vec;

        //up
        GridCell up;
        if(!gridAsDictionary.TryGetValue(new GridVector(vec.x, vec.y + 1), out up))
        {
            up = new GridCell();
            up.neighbours.Add(cellToGetAround);
            up.Vec = new GridVector(vec.x, vec.y + 1);
            up.worldLocation = new Vector3(vec.x * widthOfPrefab, (vec.y+1) * heightOfPrefab, 0);
            gridAsDictionary.Add(new GridVector(vec.x, vec.y + 1), up);
        }

        //down
        GridCell down;
        if(!gridAsDictionary.TryGetValue(new GridVector(vec.x, vec.y -1), out down))
        {
            down = new GridCell();
            down.neighbours.Add(cellToGetAround);
            down.Vec = new GridVector(vec.x, vec.y - 1);
            down.worldLocation = new Vector3(vec.x * widthOfPrefab, (vec.y-1) * heightOfPrefab, 0);
            gridAsDictionary.Add(new GridVector(vec.x, vec.y - 1), down);
        }

        //left
        GridCell left;
        if (!gridAsDictionary.TryGetValue(new GridVector(vec.x-1, vec.y), out left))
        {
            left = new GridCell();
            left.neighbours.Add(cellToGetAround);
            left.Vec = new GridVector(vec.x-1, vec.y);
            left.worldLocation = new Vector3((vec.x-1) * widthOfPrefab, vec.y * heightOfPrefab, 0);
            gridAsDictionary.Add(new GridVector(vec.x-1, vec.y), left);
        }

        //right
        GridCell right;
        if (!gridAsDictionary.TryGetValue(new GridVector(vec.x + 1, vec.y), out right))
        {
            right = new GridCell();
            right.neighbours.Add(cellToGetAround);
            right.Vec = new GridVector(vec.x + 1, vec.y);
            right.worldLocation = new Vector3((vec.x + 1) * widthOfPrefab, vec.y * heightOfPrefab, 0);
            gridAsDictionary.Add(new GridVector(vec.x+1, vec.y), right);
        }

        if(!cellToGetAround.neighbours.Contains(up))
            cellToGetAround.neighbours.Add(up);

        if (!cellToGetAround.neighbours.Contains(down))
            cellToGetAround.neighbours.Add(down);

        if (!cellToGetAround.neighbours.Contains(left))
            cellToGetAround.neighbours.Add(left);

        if (!cellToGetAround.neighbours.Contains(right))
            cellToGetAround.neighbours.Add(right);
    }

    int EuclideanDistance(GridCell start, GridCell end)
    {
        return (int)Vector3.Distance(start.worldLocation, end.worldLocation);
    }

    void resetGHParent()
    {
        foreach(var cell in gridAsDictionary)
        {
            cell.Value.g = int.MaxValue / 2;
            cell.Value.h = int.MaxValue / 2;
            cell.Value.parent = null;
        }
    }

    void restDungeon()
    {
        foreach (var room in placedRoomsAsGameObjects)
        {
            Destroy(room);
        }

        placedRooms = new List<DungeonRoom>();
        placedRoomsAsGameObjects = new();
        gridAsDictionary = new();
    }
    #endregion

    List<GridCell> pathFind(GridCell startCell, GridCell endCell)
    {
        resetGHParent();
        

        prioQue openList = new prioQue();
        HashSet<GridCell> visited = new();
        GridCell pathEnd = null;

        foreach (var neighbour in startCell.neighbours)
        {
            int g = EuclideanDistance(startCell, neighbour);
            int h = EuclideanDistance(neighbour, endCell);

            if (g + h < neighbour.GetCost())
            {
                neighbour.g = g;
                neighbour.h = h;
                neighbour.parent = startCell;
            }

            //piority queue
            openList.Push(neighbour);
        }

        visited.Add(startCell);

        while (!openList.Empty())
        {
            var current = openList.Top();

            if (current.Vec.x == endCell.Vec.x && current.Vec.y == endCell.Vec.y)
            {
                pathEnd = current;
                break;
            }

            SetAroundCells(current);

            foreach (var neighbour in current.neighbours)
            {
                //if in the visited set ignore
                if (visited.Contains(neighbour))
                    continue;

                int g = (int)current.g + EuclideanDistance(current, neighbour);
                int h = EuclideanDistance(neighbour, endCell);

                if (g + h < neighbour.GetCost())
                {
                    neighbour.g = g;
                    neighbour.h = h;
                    neighbour.parent = current;
                    openList.Push(neighbour);
                }
            }

            visited.Add(current);

            openList.Pop();
        }

        List<GridCell> path = new();

        if (pathEnd != null)
        {
            var tempCell = pathEnd;

            while (tempCell.parent != null)
            {
                path.Add(tempCell);
                tempCell = tempCell.parent;
            }

            path.Add(tempCell);
        }

        path.RemoveAt(0);
        path.Reverse();

        return path;
    }

    void Wander(List<GridCell> startOfWanderList, int startIndex)
    {
        GridCell currentCell = startOfWanderList[startIndex];
        List<GridCell> wanderPath = new();

        float currentChance = wanderMaxChanceOfPlacingARoom;
        while (currentChance > 0)
        {
            //pick direction
            int neighbourIndex = UnityEngine.Random.Range(0, currentCell.neighbours.Count);
            
            //if the next step goes into the main path - stop
            if (startOfWanderList.Contains(currentCell.neighbours[neighbourIndex]))
            {
                //hit main path, stop
                break;
            }

            //if its back into its self - try again but decrease chance
            if (wanderPath.Contains(currentCell.neighbours[neighbourIndex]))
            {
                //hit part of this wander path, decrease chance carry on
                currentChance -= WanderChanceDecreaseEachStep;
                continue;
            }

            //if space isnt taken spawn new room
            wanderPath.Add(currentCell.neighbours[neighbourIndex]);
            currentCell = currentCell.neighbours[neighbourIndex];

            currentChance -= WanderChanceDecreaseEachStep;
        }

        //once path ended - a star back to halfway between start and the end of the path
        for (int i = 0; i < wanderPath.Count; i++)
        {
            if (takenCells.Contains(wanderPath[i]))
            {
                //open correct doors
                continue;
            }

            takenCells.Add(wanderPath[i]);
            var newRoom = Instantiate(roomPrefab, wanderPath[i].worldLocation, Quaternion.identity);
            newRoom.name = "wanderRoom: " + startIndex + " : " + i;
            placedRoomsAsGameObjects.Add(newRoom);

            DungeonRoom newDungeonRoom = new DungeonRoom(wanderPath[i], newRoom);
            placedRooms.Add(newDungeonRoom);

            //opens doors in correct position for this and previous room (if there is a previous room)
        }

        if (wanderPath.Count == 0)
            return;

        if(wanderAstarBackToPath)
        {
            GridCell start = wanderPath[wanderPath.Count-1];
            GridCell end = startOfWanderList[startIndex == startOfWanderList.Count - 2 ? startOfWanderList.Count-1 : (startIndex + (startOfWanderList.Count - 1)) / 2];

            var pathBack = pathFind(start, end);

            for (int i = 0; i < pathBack.Count; i++)
            {
                if (takenCells.Contains(pathBack[i]))
                {
                    //open correct doors
                    continue;
                }

                takenCells.Add(pathBack[i]);
                var newRoom = Instantiate(roomPrefab, pathBack[i].worldLocation, Quaternion.identity);
                newRoom.name = "wanderRoomPathBack: " + startIndex + " : " + i;
                placedRoomsAsGameObjects.Add(newRoom);

                DungeonRoom newDungeonRoom = new DungeonRoom(pathBack[i], newRoom);
                placedRooms.Add(newDungeonRoom);

                //opens doors in correct position for this and previous room (if there is a previous room)
            }
        }
    }

    void generateDungeon()
    {
        placedRooms = new List<DungeonRoom>();
        placedRoomsAsGameObjects = new();
        gridAsDictionary = new();
        takenCells = new();

        GridVector startVec = new GridVector(0, 0);

        GridVector endVec;

        if (RandomEndCell)
            endVec = GetRandomCellInRadius(EnsureMinDepth ? minDepth : 0, EnsureMaxDepth ? maxDepth : 255);
        else
            endVec = new GridVector(endGridCell.x, endGridCell.y);

        GridCell startCell = new GridCell();
        startCell.Vec = startVec;
        gridAsDictionary.Add(startVec, startCell);
        SetAroundCells(startCell); //this is here to generate the cells around the start point

        GridCell endCell = new GridCell();
        endCell.Vec = endVec;
        gridAsDictionary.Add(endVec, endCell);
        SetAroundCells(endCell); //this is here to generate the cells around the end point

        List<GridCell> pathToEnd = pathFind(startCell, endCell);

        for (int i = 0; i < pathToEnd.Count; i++)
        {
            takenCells.Add(pathToEnd[i]);
            var newRoom = Instantiate(roomPrefab, pathToEnd[i].worldLocation, Quaternion.identity);
            newRoom.name = "mainPath: " + i;
            placedRoomsAsGameObjects.Add(newRoom);

            DungeonRoom newDungeonRoom = new DungeonRoom(pathToEnd[i], newRoom);
            placedRooms.Add(newDungeonRoom);

            //opens doors in correct position for this and previous room (if there is a previous room)
        }

        //wander
        for(int i = 1; i < pathToEnd.Count-1;i++)
        {
            Wander(pathToEnd, i);
        }
    }

    void Start()
    {
        generateDungeon();
    }

    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.Space))
        {
            restDungeon();
            generateDungeon();
        }
    }
}
