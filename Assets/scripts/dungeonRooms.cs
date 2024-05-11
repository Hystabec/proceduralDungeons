using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class dungeonRooms : MonoBehaviour
{
    [SerializeField]
    GameObject TopDoor, BottomDoor, LeftDoor, RightDoor;

    public void OpenTop()
    {
        TopDoor.SetActive(false);
    }

    public void OpenBottom()
    {
        BottomDoor.SetActive(false);
    }

    public void OpenLeft()
    {
        LeftDoor.SetActive(false);
    }

    public void OpenRight()
    {
        RightDoor.SetActive(false);
    }

    public void CloseTop()
    {
        TopDoor.SetActive(true);
    }

    public void CloseBottom()
    {
        BottomDoor.SetActive(true);
    }

    public void CloseLeft()
    {
        LeftDoor.SetActive(true);
    }

    public void CloseRight()
    {
        RightDoor.SetActive(true);
    }
}
