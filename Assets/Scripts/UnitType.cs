using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UnitID
{
    StartLocation,
    Minerals,
    FactionATownHall,
    FactionAFort,
    FactionADepot,
    FactionAWorker,
    FactionAFighter,
}

public enum UnitClass
{
    Unit,
    Building,
    PlacementGhost
}

[CreateAssetMenu(fileName = "Unit Stats", menuName = "Unit Stats")]
public class UnitType : ScriptableObject
{
    [Header("Classification")]
    public UnitID id;
    public UnitClass unitClass;
    public bool canAttack = false;
    public bool canHarvest = false;
    public bool isResourceNode = false;
    public bool isResourceDepot = false;

    [Header("Display & Rendering")]
    public string unitName;
    public string unitDescription;
    public Texture2D iconTexture;
    public GameObject prefabModel;
    public GameObject portraitModel;
    public KeyCode keyCode;

    [Header("Base stats")]
    public float maxHP = 1;
    public float productionCost = 0;
    public float productionTime = 0;

    [Header("Movement")]
    public float movementSpeed = 5;
    public float movementAcceleration = 5;
    public float movementAngularSpeed = 180;
    public float movementStoppingDistance = 3;

    [Header("Attacking")]
    public float attackAggroRange;
    public float attackRange;
    public float attackSpeed;
    public float attackDamage;

    [Header("Harvesting")]
    public float harvestSeekRange;
    public float harvestRange;
    public float harvestSpeed;
    public float harvestAmount;
    public float harvestResourceCarryMax;
    

    [Header("Resources")]
    public float resourcesProvided;

    [Header("Training")]
    public List<UnitType> trainableUnits;

    [Header("Construction")]
    public List<UnitType> constructableUnits;

    public string GetTooltipText()
    {
        return $"{unitDescription}\n\n{productionCost} resources";
    }
}
