using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class AIRequest
{
    public bool completed;
    public int priority;
    public virtual bool complete(AIController ai)
    {
        return false;
    }
}

public class AITrainUnitAmountRequest : AIRequest
{
    UnitID unitID;
    int amount;

    public AITrainUnitAmountRequest(UnitID ui, int amt)
    {
        unitID = ui;
        amount = amt;
    }

    public override bool complete(AIController ai)
    {
        if (ai.UnitCount(unitID, true) >= amount)
            return true;

        if (ai.UnitCount(unitID, true) < amount)
            if (ai.UnitDependenciesSatisfied(unitID))
                if (ai.CanAffordUnit(unitID))
                    ai.TrainUnit(unitID);
        return false;
    }
}

public class AIConstructUnitAmountRequest : AIRequest
{
    UnitID unitID;
    UnitController constructorUnit;

    public AIConstructUnitAmountRequest(UnitID ui)
    {
        unitID = ui;
    }

    public override bool complete(AIController ai)
    {
        if (!constructorUnit)
            constructorUnit = ai.FindUnit(UnitID.FactionAWorker);

        if (constructorUnit && constructorUnit.currentOrder != Order.Construct)
        {
            if (ai.UnitDependenciesSatisfied(unitID) && ai.CanAffordUnit(unitID))
            {
                UnitController townHall = ai.FindUnit(UnitID.FactionATownHall);
                Vector3 originPosition = townHall.transform.position;
                Vector3 placementPosition = originPosition  + new Vector3(Random.Range(-30, 30), 0, Random.Range(-30, 30));
                if (constructorUnit.CanMoveToPoint(placementPosition))
                { 
                    if (ai.CanPlaceStructure(unitID, placementPosition))
                    {
                        constructorUnit.constructionUnitType = UnitType.Get(unitID);
                        constructorUnit.SetTargetPosition(townHall.transform.position + new Vector3(Random.Range(-30, 30), 0, Random.Range(-30, 30)), Order.Construct);
                    }
                }
            }
        }

        if  (constructorUnit && constructorUnit.aiDataLastConstructedUnit && constructorUnit.aiDataLastConstructedUnit.type.id == unitID)
        {
            constructorUnit.SetTargetUnit(ai.FindUnit(UnitID.FactionATownHall), Order.ReturnResources);
            return true;
        }

        return false;
    }
}

public class AIController : MonoBehaviour
{
    public static AIController[] controllers = new AIController[9];
    public static AIController GetController(int playerID) { return AIController.controllers[playerID]; }
    public static AIController AddController(int playerID) {
        AIController.controllers[playerID] = Instantiate(Resources.Load<AIController>("Prefabs/AIController"), Vector3.zero, Quaternion.identity);
        AIController.controllers[playerID].playerID = playerID;
        return AIController.controllers[playerID];
    }

    public int playerID;
    public bool IsPlayer(int id) { return id == playerID; }
    public List<AIRequest> requestList = new List<AIRequest>();
    
    public float resources = 50;
    public void AddResources(float res) { resources += res; }
    public void SubtractResources(float res) { resources -= res; }

    public List<UnitController> units;
    public List<UnitController> harvesterUnits;
    public List<UnitController> militaryUnits;
    public List<UnitController> constructionUnits;
    public List<UnitController> productionStructureUnits;
    public void AddUnit(UnitController unit) { units.Add(unit); }
    public void RemoveUnit(UnitController unit) {
        harvesterUnits.Remove(unit);
        militaryUnits.Remove(unit);
        constructionUnits.Remove(unit);
        productionStructureUnits.Remove(unit);
    }
    public void AddHarvesterUnit(UnitController unit) { harvesterUnits.Add(unit); }
    public void RemoveHarvesterUnit(UnitController unit) { harvesterUnits.Remove(unit); }
    public void AddConstructionUnit(UnitController unit) { constructionUnits.Add(unit); }
    public void RemoveConstructionUnit(UnitController unit) { constructionUnits.Remove(unit); }
    public void AddMilitaryUnit(UnitController unit) { militaryUnits.Add(unit); }
    public void RemoveMilitaryUnit(UnitController unit) { militaryUnits.Remove(unit); }
    public void AddProductionStructureUnit(UnitController unit) { productionStructureUnits.Add(unit); }
    public void RemoveProductionStructureUnit(UnitController unit) { productionStructureUnits.Remove(unit); }


    private void Start()
    {
        if (GameManager.instance.showMeTheMoney)
            resources = 10000;

        requestList.Add(new AITrainUnitAmountRequest(UnitID.FactionAWorker, 8));
        requestList.Add(new AIConstructUnitAmountRequest(UnitID.FactionADepot));
        requestList.Add(new AIConstructUnitAmountRequest(UnitID.FactionAFort));
        requestList.Add(new AITrainUnitAmountRequest(UnitID.FactionAFighter, 2));
        requestList.Add(new AITrainUnitAmountRequest(UnitID.FactionAWorker, 2));

        // Send workers to mine

        // Send military units to defend

        // Send military units to scout

        // Send military units to attack
    }

    // Update is called once per frame
    void Update()
    {
        if (requestList.Count > 0)
        {
            if (!requestList[0].completed)
            {
                Debug.Log(requestList[0]);
                requestList[0].completed = requestList[0].complete(this);
            } else
            {
                requestList.RemoveAt(0);
            }
        }
    }

    // Dependency checks
    public bool UnitDependenciesSatisfied(UnitID unitID)
    {
        UnitType unitType = UnitType.Get(unitID);
        List<UnitID> unsatisfiedDependencies = new List<UnitID>(UnitType.Get(unitID).dependencies);
        foreach (UnitID dependency in unitType.dependencies)
        {
            foreach (UnitController unit in units)
            {
                if (unit.id == dependency)
                {
                    unsatisfiedDependencies.Remove(dependency);
                }
            }
        }
        return unsatisfiedDependencies.Count == 0;
    }

    public bool CanAffordUnit(UnitID unitID)
    {
        return UnitType.Get(unitID).productionCost <= resources;
    }

    public UnitController FindUnit(UnitID unitID)
    {
        foreach (UnitController unit in units)
        {
            if (unit.id == unitID)
                return unit;
        }
        return null;
    }

    public UnitController FindUnitThatProducesUnit(UnitID unitID)
    {
        foreach (UnitController unit in units)
        {
            if (unit.type.trainableUnits.Contains(unitID))
                return unit;
        }
        return null;
    }

    public int UnitCount(UnitID unitID)
    {
        return UnitCount(unitID, false);
    }

    public int UnitCount(UnitID unitID, bool includePartial)
    {
        int count = 0;
        foreach (UnitController unit in units)
        {
            if (unit.id == unitID) count++;
            if (includePartial)
            {
                foreach(UnitType unitType in unit.productionQueue)
                {
                    if (unitType.id == unitID) count++;
                }
            }
        }
        return count;
    }


    // TODO Extend and optimize for queue time and distance to location
    public bool TrainUnit(UnitID unitID)
    {
        UnitController unitProducer = FindUnitThatProducesUnit(unitID);
        if (!unitProducer) return false;
        return unitProducer.TrainUnit(UnitType.Get(unitID));
    }

    public void UnitProduced(UnitController unit)
    {
        if (unit.isHarvester) unit.HarvestNearbyResources();
        if (unit.isMilitary) unit.PatrolNearbyArea();
    }

    public bool CanPlaceStructure(UnitID unitID, Vector3 point)
    {
        PlacementGhostController placementGhost = Instantiate(Resources.Load<GameObject>("Prefabs/PlacementGhost"), point, Quaternion.identity).GetComponent<PlacementGhostController>();
        placementGhost.SetUnitType(UnitType.Get(unitID));
        placementGhost.UpdatePlacement();
        bool isPlacementvalid = placementGhost.isPlacementValid;
        placementGhost.Destroy();
        return isPlacementvalid;
    }
}
