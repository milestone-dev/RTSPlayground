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
    public UnitType type;
    public bool canAttack = false;
    public bool canHarvest = false;
    public bool isResourceNode = false;
    public bool isResourceDepot = false;

    public GameObject prefabModel;

    public float maxHP = 1;

    public float movementSpeed = 5;
    public float movementAngularSpeed = 180;
    public float movementAcceleration = 5;
    public float movementStoppingDistance = 3;

    [Header("Attacking Unit")]
    public float attackAggroRange;
    public float attackRange;
    public float attackSpeed;
    public float attackDamage;

    [Header("Harvester Unit")]
    public float harvestRange;
    public float harvestSpeed;
    public float harvestAmount;
    public float harvestResourceCarryMax;

    [Header("Resource Node")]
    public float resourcesProvided;

    [Header("Training Units")]
    public List<UnitStats> trainableUnits;
}
