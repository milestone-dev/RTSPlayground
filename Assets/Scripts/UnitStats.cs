using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UnitType
{
    Unit,
    Building
}

[CreateAssetMenu(fileName = "Unit Stats", menuName = "Unit Stats")]
public class UnitStats : ScriptableObject
{
    [Header("Classification")]
    public UnitType type;
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

    [Header("Base stats")]
    public float maxHP = 1;
    public float productionCost = 0;

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
    public List<UnitStats> trainableUnits;

    public string GetTooltipText()
    {
        return $"{unitDescription}\n\n{productionCost} resources";
    }
}
