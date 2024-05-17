using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum Direction
{
    Up, 
    Down, 
    Left, 
    Right
}

public class CellularAutomotonSubScript : MonoBehaviour
{
    CellularAutomoton baseScript;

    int currentDepth = -1;

    GridVector gridLocation = new GridVector();

    float currentBranchVitality = -1.0f;

    public void GiveBaseScript(CellularAutomoton baseScript) { this.baseScript = baseScript; }
    public void SetDepth(int depth) { currentDepth = depth; }
    public int GetDepth() { return currentDepth; }
    public void SetGridLocation(GridVector gridVector) { this.gridLocation = gridVector; }
    public void SetBranchVitality(float vitality) {  currentBranchVitality = vitality; }


    List<Direction> directionList = new() { Direction.Up, Direction.Down, Direction.Left, Direction.Right };

    void SuffleDirectionList()
    {
        for(int i = directionList.Count - 1; i > 0; i--) 
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var temp = directionList[i];
            directionList[i] = directionList[j];
            directionList[j] = temp;
        }
    }

    GridVector DirectionToVector(Direction dir)
    {
        GridVector reuturnVec = new();

        switch (dir)
        {
            case Direction.Up:
                reuturnVec = gridLocation + new GridVector(0, 1);
                break;
            case Direction.Down:
                reuturnVec = gridLocation + new GridVector(0, -1);
                break;
            case Direction.Left:
                reuturnVec = gridLocation + new GridVector(-1, 0);
                break;
            case Direction.Right:
                reuturnVec = gridLocation + new GridVector(1, 0);
                break;
        }

        return reuturnVec;
    }

    void OpenDoorInDirection(Direction dir, GameObject mainRoom, GameObject connectingRoom)
    {
        switch(dir)
        {
            case Direction.Up:
                mainRoom.GetComponent<dungeonRooms>().OpenTop();
                connectingRoom.GetComponent<dungeonRooms>().OpenBottom();
                break;
            case Direction.Down:
                mainRoom.GetComponent<dungeonRooms>().OpenBottom();
                connectingRoom.GetComponent<dungeonRooms>().OpenTop();
                break;
            case Direction.Left:
                mainRoom.GetComponent<dungeonRooms>().OpenLeft();
                connectingRoom.GetComponent<dungeonRooms>().OpenRight();
                break;
            case Direction.Right:
                mainRoom.GetComponent<dungeonRooms>().OpenRight();
                connectingRoom.GetComponent<dungeonRooms>().OpenLeft();
                break;
        }
    }

    public void Run()
    {
        //this will be used to run its logic and spawn the next rooms
        if (currentDepth >= baseScript.GetMaxDepth())
        {
            Debug.Log("Max achived");
            return;
        }

        //this is here so that the order in which the rooms are made/scripts are run is random
        SuffleDirectionList();

        if(currentDepth < baseScript.GetMinDepth() && currentDepth >= baseScript.GetLargestDepth())
        {
            //has to spawn a room
            for(int i = 0; i < directionList.Count; i++)
            {
                //this will find a space that is free
                var nextVec = DirectionToVector(directionList[i]);

                if(!baseScript.ContainsRoomWithVector(nextVec))
                {
                    //no room in that direction - place new room
                    var NewRoom = baseScript.SpawnNewRoom(nextVec);
                    var NewScript = NewRoom.GetComponent<CellularAutomotonSubScript>();
                    NewScript.SetDepth(currentDepth + 1);
                    NewScript.GiveBaseScript(baseScript);
                    NewScript.SetGridLocation(nextVec);
                    NewScript.SetBranchVitality(baseScript.GetDefaultBranchVitality());
                    baseScript.AddToDictionary(nextVec, NewRoom);
                    baseScript.AddToListToRun(NewScript, true);
                    baseScript.SetLargestDepth(currentDepth + 1);

                    //open doors
                    OpenDoorInDirection(directionList[i], this.gameObject, NewRoom);

                    directionList.RemoveAt(i);
                    break;
                }
            }
        }

        for (int i = 0; i < directionList.Count; i++)
        {
            var nextVec = DirectionToVector(directionList[i]);

            if (baseScript.ContainsRoomWithVector(nextVec))
            {
                //try merge room
                var ranNum = UnityEngine.Random.value;

                if (ranNum > baseScript.GetChanceToMergeRooms())
                    continue;

                OpenDoorInDirection(directionList[i], this.gameObject, baseScript.FindRoomWithVector(nextVec));
            }
            else
            {
                //try spawn room
                var ranNum = UnityEngine.Random.value;

                if (ranNum > currentBranchVitality)
                    continue;

                var NewRoom = baseScript.SpawnNewRoom(nextVec);
                var NewScript = NewRoom.GetComponent<CellularAutomotonSubScript>();
                NewScript.SetDepth(currentDepth + 1);
                NewScript.GiveBaseScript(baseScript);
                NewScript.SetGridLocation(nextVec);
                NewScript.SetBranchVitality(currentBranchVitality - baseScript.GetBranchVitalityDecrease());
                baseScript.AddToDictionary(nextVec, NewRoom);
                baseScript.AddToListToRun(NewScript);
                baseScript.SetLargestDepth(currentDepth + 1);

                OpenDoorInDirection(directionList[i], this.gameObject, NewRoom);
            }
        }
    }
}
