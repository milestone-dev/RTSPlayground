using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AIController : MonoBehaviour
{
    public static AIController[] controllers = new AIController[9];
    public int playerID;
    public List<UnitController> workerUnits;
    public List<UnitController> militaryUnits;
    public List<UnitController> productionStructureUnits;

    public bool IsPlayer(int id) { return id == playerID; }
    public static AIController GetController(int playerID) { return AIController.controllers[playerID]; }
    public static AIController AddController(int playerID) {
        AIController.controllers[playerID] = Instantiate(Resources.Load<AIController>("Prefabs/AIController"), Vector3.zero, Quaternion.identity);
        AIController.controllers[playerID].playerID = playerID;
        return AIController.controllers[playerID];
    }

    // Use this for initialization
    void Start()
    {
        AIController.controllers[playerID] = this;
    }

    // Update is called once per frame
    void Update()
    {

    }

    void AddProductionStructureUnit(UnitController unit) { productionStructureUnits.Add(unit); }
    void RemoveProductionStructureUnit(UnitController unit) { productionStructureUnits.Remove(unit); }
    void AddWorkerUnit(UnitController unit) { workerUnits.Add(unit); }
    void RemoveWorkerUnit(UnitController unit) { workerUnits.Remove(unit); }
    void AddMilitaryUnit(UnitController unit) { militaryUnits.Add(unit); }
    void RemoveMilitaryUnit(UnitController unit) { militaryUnits.Remove(unit); }
}
