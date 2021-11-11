using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public bool operationCWAL;
    public bool showMeTheMoney;
    public static GameManager instance;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        Invoke("SetupMap", 0.1f);
    }

    void SetupMap()
    {
        foreach (UnitController unit in FindObjectsOfType<UnitController>())
        {
            if (unit.id == UnitID.StartLocation)
            {
                int playerID = unit.playerID;
                Vector3 position = unit.transform.position;
                if (!AIController.GetController(playerID) && playerID != PlayerManager.instance.humanPlayerID)
                    AIController.AddController(playerID);

                unit.Die();

                UnitController townHallUnit = UnitController.CreateUnit(UnitType.Get(UnitID.FactionATownHall), position, playerID);
                UnitType workerUnitType = UnitType.Get(UnitID.FactionAWorker);
                
                Vector3 workerCreatePosition = position + new Vector3(3, 0, 3);
                for (var i = 0; i < 4; i++)
                {
                    UnitController workerUnit = UnitController.CreateUnit(workerUnitType, workerCreatePosition, playerID);
                    workerUnit.HarvestNearbyResources();
                }
                

            }
        }
    }
}
