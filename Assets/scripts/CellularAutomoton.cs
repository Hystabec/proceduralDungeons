using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class CellularAutomoton : MonoBehaviour
{
    [SerializeField]
    GameObject dungeonRoomPrefab;
    [SerializeField]
    float widthOfPrefab = 1, heightOfPrefab = 1;

    [SerializeField]
    bool useCustomStartLocation = false;
    [SerializeField]
    Vector3 customStartLocation = Vector3.zero;

    [SerializeField]
    int maxDepth = 10, minDepth = 1;
    int LargestDepth = 0;
    

    [SerializeField]
    float ChanceToMergeRooms = 0.5f, DefaultBranchVitality = 1.0f, BranchVitalityDecrease = 0.1f;

    Dictionary<GridVector, GameObject> gridDictionary = new Dictionary<GridVector, GameObject>();

    List<CellularAutomotonSubScript> listOfRoomsToRun = new();
    List<CellularAutomotonSubScript> asyncListToAdd = new();

    int numberOfSpawnedRooms = 1;

    public int GetMaxDepth() { return maxDepth; }
    public int GetMinDepth() { return minDepth; }
    public float GetChanceToMergeRooms() { return ChanceToMergeRooms; }
    public float GetDefaultBranchVitality() { return DefaultBranchVitality; }
    public float GetBranchVitalityDecrease() { return BranchVitalityDecrease; }

    GameObject lastRoomRun;

    public void AddToDictionary(GridVector gridVector, GameObject Room)
    {
        gridDictionary.Add(gridVector, Room);
    }

    public bool ContainsRoomWithVector(GridVector gridVector)
    {
        bool found = gridDictionary.ContainsKey(gridVector);
        return found;
    }

    public GameObject FindRoomWithVector(GridVector gridVector)
    {
        if (gridDictionary.ContainsKey(gridVector))
        {
            return gridDictionary[gridVector];
        }
        else
            return null;
    }

    public GridVector FindVectorWithGameObject(GameObject go)
    {
        if(gridDictionary.ContainsValue(go))
        {
            return gridDictionary.FirstOrDefault(x => x.Value == go).Key;
        }
        else
            return null;
    }
    
    public bool SetLargestDepth(int depth)
    {
        if (LargestDepth > depth)
        {
            return false;
        }
        else
        {
            LargestDepth = depth;
            return true;
        }
    }

    public int GetLargestDepth() { return LargestDepth; }

    public void AddToListToRun(CellularAutomotonSubScript ToAdd, bool MainBranch = false)
    {
        if (!MainBranch)
            listOfRoomsToRun.Add(ToAdd);
        else
        {
            AsyncAddToListToRun(ToAdd);
        }
    }

    public void AsyncAddToListToRun(CellularAutomotonSubScript ToAdd)
    {
        asyncListToAdd.Add(ToAdd);
    }

    public void AsyncAddToListToRunSub()
    {
        asyncListToAdd.Reverse();

        foreach(var scrpt in asyncListToAdd)
        {
            listOfRoomsToRun.Insert(0, scrpt);
        }

        asyncListToAdd.Clear();
    }

    public GameObject SpawnNewRoom(GridVector location)
    {
        Vector3 loc;

        if (useCustomStartLocation)
            loc = customStartLocation + new Vector3(location.x * widthOfPrefab, location.y * heightOfPrefab, 0);
        else
            loc = new Vector3(location.x * widthOfPrefab, location.y * heightOfPrefab, 0);

        GameObject newRoom = Instantiate(dungeonRoomPrefab, loc, Quaternion.identity);
        newRoom.AddComponent<CellularAutomotonSubScript>();

        numberOfSpawnedRooms++;

        return newRoom;
    }

    void Run()
    {
        GameObject newRoom;

        if (useCustomStartLocation)
            newRoom = Instantiate(dungeonRoomPrefab, customStartLocation, Quaternion.identity);
        else
            newRoom = Instantiate(dungeonRoomPrefab, new Vector3(), Quaternion.identity);

        var cellScript = newRoom.AddComponent<CellularAutomotonSubScript>();

        cellScript.SetDepth(0);
        cellScript.GiveBaseScript(this);
        cellScript.SetGridLocation(new(0, 0));
        cellScript.SetBranchVitality(DefaultBranchVitality);

        AddToDictionary(new(0, 0), newRoom);

        cellScript.Run();

        while (listOfRoomsToRun.Count > 0)
        {
            listOfRoomsToRun[0].Run();
            lastRoomRun = listOfRoomsToRun[0].gameObject;
            listOfRoomsToRun.RemoveAt(0);
            AsyncAddToListToRunSub();
        }
    }
    void Reset()
    {
        LargestDepth = 0;
        numberOfSpawnedRooms = 1;

        listOfRoomsToRun.Clear();
        asyncListToAdd.Clear();

        foreach(var entry in gridDictionary)
        {
            var go = entry.Value;
            Destroy(go);
        }

        gridDictionary.Clear();
    }

    void Start()
    {
        Run();   
    }

    void Update()
    {
        if(Input.GetKeyUp(KeyCode.Space))
        {
            Reset();
            Run();
        }

        if(Input.GetKeyUp(KeyCode.F))
        {
            var allRooms = FindObjectsOfType<CellularAutomotonSubScript>();

            foreach(var cass in allRooms) 
            {
                if(cass.GetDepth() == LargestDepth)
                {
                    cass.gameObject.GetComponent<SpriteRenderer>().color = Color.red;
                    lastRoomRun.GetComponent<SpriteRenderer>().color = Color.green;
                }
            }
        }
    }
}
