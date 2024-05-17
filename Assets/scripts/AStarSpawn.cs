using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;



public class AStarSpawn : MonoBehaviour
{
    [SerializeField]
    Vector3 StartWorldLocation = Vector3.zero;

    [SerializeField]
    bool useStartCellAsWorldStartLocation = true;

    [SerializeField]
    Vector2Int startGridCell, endGridCell;

    [SerializeField]
    bool RandomEndCell = false;

    [SerializeField]
    GameObject roomPrefab;

    [SerializeField]
    float widthOfPrefab, heightOfPrefab;

    [SerializeField]
    int dungeonWidth, dungeonHeight;

    [SerializeField]
    float DM_MaxChanceOfPlacingARoom = 0.5f, DM_ChanceDecrease = 0.1f;

    List<GridCell> dungeonCells;
    List<DungeonRoom> placedRooms = new();
    prioSortList sortedGrid = new();

    List<GameObject> thisRooms = new();

    Vector3 spawnOffset = Vector3.zero;

    //List<List<GridCell>> dungeonGrid;

    private void Reset()
    {
        dungeonCells.Clear();
        
        foreach(GameObject go in thisRooms)
        {
            Destroy(go);
        }

        placedRooms = new();
        sortedGrid = new();

        dungeonCells = new List<GridCell>();

        //loops over all elements in the grid and sets them to false
        for (int y = 0; y < dungeonHeight; y++)
        {
            for (int x = 0; x < dungeonWidth; x++)
            {
                dungeonCells.Add(new GridCell());
            }
        }

        if (useStartCellAsWorldStartLocation)
            spawnOffset = new Vector3(startGridCell.x * widthOfPrefab, startGridCell.y * heightOfPrefab);

        //looping over grid and assiging neighbours
        for (int y = 0; y < dungeonHeight; y++)
        {
            for (int x = 0; x < dungeonWidth; x++)
            {
                var cell = dungeonCells[x + (y * dungeonWidth)];
                cell.worldLocation = (new Vector3((x * widthOfPrefab) + StartWorldLocation.x, (y * heightOfPrefab) + StartWorldLocation.y, (cell.worldLocation.z) + StartWorldLocation.z));

                cell.Vec = new(x, y);

                //left
                if (x > 0)
                {
                    cell.neighbours.Add(dungeonCells[(x - 1) + (y * dungeonWidth)]);
                }

                //right
                if (x < dungeonWidth - 1)
                {
                    cell.neighbours.Add(dungeonCells[(x + 1) + (y * dungeonWidth)]);
                }

                //up
                if (y > 0)
                {
                    cell.neighbours.Add(dungeonCells[x + ((y - 1) * dungeonWidth)]);
                }

                //down
                if (y < dungeonHeight - 1)
                {
                    cell.neighbours.Add(dungeonCells[x + ((y + 1) * dungeonWidth)]);
                }
            }
        }

        GenerateLayout();
    }

    void Start()
    {
        dungeonCells = new List<GridCell>();

        //loops over all elements in the grid and sets them to false
        for(int y = 0; y < dungeonHeight; y++) 
        {
            for(int x = 0; x < dungeonWidth; x++)
            {
                dungeonCells.Add(new GridCell());
            }
        }

        if (useStartCellAsWorldStartLocation)
            spawnOffset = new Vector3(startGridCell.x * widthOfPrefab, startGridCell.y * heightOfPrefab);

        //looping over grid and assiging neighbours
        for(int y =0; y < dungeonHeight; y++)
        {
            for(int x = 0 ; x < dungeonWidth; x++)
            {
                var cell = dungeonCells[x + (y * dungeonWidth)];
                cell.worldLocation = (new Vector3((x * widthOfPrefab)+StartWorldLocation.x, (y * heightOfPrefab)+StartWorldLocation.y, (cell.worldLocation.z)+StartWorldLocation.z));

                cell.Vec = new(x, y);

                //left
                if(x > 0)
                {
                    cell.neighbours.Add(dungeonCells[(x - 1) + (y * dungeonWidth)]);
                }

                //right
                if(x < dungeonWidth - 1)
                {
                    cell.neighbours.Add(dungeonCells[(x + 1) + (y * dungeonWidth)]);
                }

                //up
                if(y > 0)
                {
                    cell.neighbours.Add(dungeonCells[x + ((y - 1) * dungeonWidth)]);
                }

                //down
                if(y < dungeonHeight - 1)
                {
                    cell.neighbours.Add(dungeonCells[x + ((y + 1) * dungeonWidth)]);
                }
            }
        }

        GenerateLayout();
    }

    void ResetGrid()
    {
        foreach(GridCell cell in dungeonCells) 
        {
            cell.parent = null;
            cell.g = int.MaxValue / 2;
            cell.h = int.MaxValue / 2;
        }
    }

    GridCell FindCell(GridVector cellLoc)
    {
        if(cellLoc.x > dungeonWidth || cellLoc.x < 0 || cellLoc.y > dungeonHeight || cellLoc.y < 0)
            return null;

        return dungeonCells[cellLoc.y * dungeonWidth + cellLoc.x];
    }

    int EuclideanDistance(GridCell start, GridCell end)
    {
        return (int)Vector3.Distance(start.worldLocation, end.worldLocation);
    }

    void openRoomDoor(GameObject room, Vector3 Dir)
    {
        if (Dir == Vector3.up)
        {
            room.GetComponent<dungeonRooms>().OpenTop();
        }

        if (Dir == Vector3.down)
        {
            room.GetComponent<dungeonRooms>().OpenBottom();
        }

        if (Dir == Vector3.left)
        {
            room.GetComponent<dungeonRooms>().OpenLeft();
        }

        if (Dir == Vector3.right)
        {
            room.GetComponent<dungeonRooms>().OpenRight();
        }
    }

    void DrunkMans(List<DungeonRoom> truePath, int pathIndex)
    {
        DungeonRoom currentRoom = truePath[pathIndex];
        bool loop = true;
        float PlaceChance = DM_MaxChanceOfPlacingARoom;

        List<DungeonRoom> thisPath = new()
        {
            truePath[pathIndex]
        };

        while (loop)
        {
            var ranNum = UnityEngine.Random.value;

            //check if can place a room
            if (ranNum > PlaceChance)
                return; 

            PlaceChance -= DM_ChanceDecrease;
            //find place direction

            int dir = UnityEngine.Random.Range(0, 4);

            Vector2Int placedDir = new();
            switch (dir)
            {
            case 0:
                placedDir = Vector2Int.up;
                break;
            case 1:
                placedDir = Vector2Int.down;
                break;
            case 2:
                placedDir = Vector2Int.left;
                break;
            case 3:
                placedDir = Vector2Int.right;
                break;
            }

            //find if there is a room there
            var reRoom = sortedGrid.Find(new(currentRoom.asCell.Vec.x + placedDir.x, currentRoom.asCell.Vec.y + placedDir.y));

            if(reRoom == null)
            {
                GameObject newRoom = Instantiate(roomPrefab, new Vector3((currentRoom.asCell.Vec.x + placedDir.x) * widthOfPrefab, (currentRoom.asCell.Vec.y + placedDir.y) * heightOfPrefab, currentRoom.asCell.worldLocation.z) - spawnOffset, quaternion.identity);
                thisRooms.Add(newRoom);

                newRoom.name = "DrunkMan's room";

                var newCell = new GridCell
                {
                    worldLocation = newRoom.transform.position + spawnOffset,
                    Vec  =  new(currentRoom.asCell.Vec.x + placedDir.x, currentRoom.asCell.Vec.y + placedDir.y)
                };
                newCell.neighbours.Add(currentRoom.asCell);

                var newDungeonRoom = new DungeonRoom(newCell, newRoom);

                sortedGrid.Add(newDungeonRoom);

                Vector3 Dir = (newDungeonRoom.asCell.worldLocation) - (currentRoom.asCell.worldLocation);
                openRoomDoor(currentRoom.asGameObject, Dir);

                Dir = (currentRoom.asCell.worldLocation) - newDungeonRoom.asCell.worldLocation;
                openRoomDoor(newDungeonRoom.asGameObject, Dir);

                currentRoom = newDungeonRoom;
            }
            else
            {
                Vector3 Dir = reRoom.asCell.worldLocation - currentRoom.asCell.worldLocation;
                openRoomDoor(currentRoom.asGameObject, Dir);

                Dir = currentRoom.asCell.worldLocation - reRoom.asCell.worldLocation;
                openRoomDoor(reRoom.asGameObject, Dir);

                return;
            }
        }
    }

    void GenerateLayout()
    {
        GridVector start = new GridVector(startGridCell.x, startGridCell.y);

        GridVector end;

        if (!RandomEndCell)
            end = new GridVector(endGridCell.x, endGridCell.y);
        else
            end = new GridVector(UnityEngine.Random.Range(0, dungeonWidth), UnityEngine.Random.Range(0, dungeonHeight));

        //check cells are in grid
        if(start.x < 0)
            Debug.LogError("startGridCell.x(" + start.x + ") needs to be greater than 0");
        
        if(start.x > dungeonWidth)
            Debug.LogError("startGridCell.x(" + start.x + ") needs to be less than dungeon width(" + dungeonWidth + ")");

        if(start.y < 0)
            Debug.LogError("startGridCell.y(" + start.y + ") needs to be greater than 0");

        if(start.y > dungeonHeight)
            Debug.LogError("startGridCell.y(" + start.y + ") needs to be less than dungeon height(" + dungeonHeight + ")");

        if (end.x < 0)
            Debug.LogError("endGridCell.x(" + end.x + ") needs to be greater than 0");

        if (end.x > dungeonWidth)
            Debug.LogError("endGridCell.x(" + end.x + ") needs to be less than dungeon width(" + dungeonWidth + ")");

        if (end.y < 0)
            Debug.LogError("endGridCell.y(" + end.y + ") needs to be greater than 0");

        if (end.y > dungeonHeight)
            Debug.LogError("endGridCell.y(" + end.y + ") needs to be less than dungeon height(" + dungeonHeight + ")");



        GridCell startCell = FindCell(start);
        GridCell endCell = FindCell(end);

        ResetGrid();

        prioQue openList = new();
        HashSet<GridCell> visited = new();

        GridCell pathEnd = null; 

        foreach(var neighbour in startCell.neighbours)
        {
            int g = EuclideanDistance(startCell, neighbour);
            int h = EuclideanDistance(neighbour, endCell);

            if(g+h<neighbour.GetCost())
            {
                neighbour.g = g; 
                neighbour.h = h;
                neighbour.parent = startCell;
            }

            //piority queue
            openList.Push(neighbour);
        }

        visited.Add(startCell);

        while(!openList.Empty())
        {
            var current = openList.Top();

            if(current.Vec.x == endCell.Vec.x && current.Vec.y == endCell.Vec.y)
            {
                pathEnd = current;
                break;
            }

            foreach(var neighbour in current.neighbours)
            {
                //if in the visited set ignore
                if (visited.Contains(neighbour))
                    continue;

                int g = (int)current.g + EuclideanDistance(current, neighbour);
                int h = EuclideanDistance(neighbour, endCell);

                if(g+h<neighbour.GetCost())
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

        if(pathEnd != null)
        {
            var tempCell = pathEnd;

            while(tempCell.parent != null)
            {
                path.Add(tempCell);
                tempCell = tempCell.parent;
            }

            path.Add(tempCell);
        }

        for(int index = 0; index < path.Count; index++)
        {
            var newRoom = Instantiate(roomPrefab, path[index].worldLocation - spawnOffset, quaternion.identity);
            thisRooms.Add(newRoom);

            var temp = new DungeonRoom(path[index], newRoom);
            placedRooms.Add(temp);
            sortedGrid.Add(temp);

            if(index+1 < path.Count)
            {
                Vector3 dir = path[index+1].worldLocation - path[index].worldLocation;

                if (dir == Vector3.up)
                {
                    newRoom.GetComponent<dungeonRooms>().OpenTop();
                }

                if (dir == Vector3.down)
                {
                    newRoom.GetComponent<dungeonRooms>().OpenBottom();
                }

                if (dir == Vector3.left)
                {
                    newRoom.GetComponent<dungeonRooms>().OpenLeft();
                }

                if (dir == Vector3.right)
                {
                    newRoom.GetComponent<dungeonRooms>().OpenRight();
                }
            }

            if (index - 1 >= 0)
            {
                Vector3 dir = path[index].worldLocation - path[index - 1].worldLocation;

                if (dir == Vector3.down)
                {
                    newRoom.GetComponent<dungeonRooms>().OpenTop();
                }

                if (dir == Vector3.up)
                {
                    newRoom.GetComponent<dungeonRooms>().OpenBottom();
                }

                if (dir == Vector3.right)
                {
                    newRoom.GetComponent<dungeonRooms>().OpenLeft();
                }

                if (dir == Vector3.left)
                {
                    newRoom.GetComponent<dungeonRooms>().OpenRight();
                }
            }
        }

        //loops other all path elements - dont to start or end cell
        for(int i = 1; i < path.Count-1; i++)
        {
            DrunkMans(placedRooms, i);
        }
    }


    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.Space))
        {
            Reset();
        }
    }
}