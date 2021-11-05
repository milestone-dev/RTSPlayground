using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public enum Order
{
    Stop,
    Move,
    Follow,
    Attack,
    Harvest,
    ReturnResources,
    Die,
}

public class UnitController : MonoBehaviour
{
    private NavMeshAgent navAgent;
    private NavMeshObstacle navObstacle;
    private ParticleSystem fireParticleSystem;
    private ParticleSystem mineParticleSystem;
    private GameObject highlightCircle;
    public float collisionSize;
    public UnitStats stats;
    public int playerID = 0;
    public Order currentOrder;
    public float hp;
    public float attackCooldown;
    public float harvestCooldown;
    public float harvestResourceCarryAmount;
    public UnitController currentTargetUnit;
    public UnitController lastTargetResourceUnit;
    public float resourcesLeft;

    public bool isNeutral  { get { return playerID == 0; } }
    public bool isResourceBusy  { get { return currentTargetUnit != null; } }
    public bool IsEnemy(UnitController unit) { return unit.playerID != playerID; }
    public bool IsOwn(UnitController unit) { return unit.playerID == playerID; }

    private float DistanceToUnit(UnitController unit)
    {
        return Vector3.Distance(transform.position, unit.transform.position);
    }

    private void Start()
    {
        if (!stats) {
            Debug.LogFormat("{0} is missing UnitStats data", this);
            Die();
        }

        fireParticleSystem = transform.Find("FireParticleSystem").GetComponent<ParticleSystem>();
        mineParticleSystem = transform.Find("MineParticleSystem").GetComponent<ParticleSystem>();
        highlightCircle = transform.Find("Highlight").gameObject;

        if (stats.type == UnitType.Unit)
        {
            navAgent = gameObject.AddComponent<NavMeshAgent>();
            navAgent.speed = stats.movementSpeed;
            navAgent.angularSpeed = stats.movementAngularSpeed;
            navAgent.acceleration = stats.movementAcceleration;
            navAgent.speed = stats.movementSpeed;
        }
        else if (stats.type == UnitType.Building)
        {
            navObstacle = gameObject.AddComponent<NavMeshObstacle>();
        }

        if (stats.prefabModel)
        {
            GameObject model =  Instantiate(stats.prefabModel, Vector3.zero, Quaternion.identity);
            model.transform.parent = gameObject.transform;
            Vector3 modelSize = model.GetComponent<Renderer>().bounds.size;
            Vector3 modelPosition = gameObject.transform.position + new Vector3(0, modelSize.y/2, 0);
            model.transform.position = modelPosition;

            BoxCollider collider = GetComponent<BoxCollider>();
            collider.size = modelSize;

            if (navObstacle)
                navObstacle.size = modelSize;

            collisionSize = Mathf.Max(modelSize.x, modelSize.z);
            float circleScaleMultiplier = 75;
            Vector3 circleScale = new Vector3(circleScaleMultiplier * collisionSize, circleScaleMultiplier  * collisionSize, circleScaleMultiplier  * collisionSize);
            highlightCircle.transform.localScale = circleScale;
        }

        // Set up variables
        hp = stats.maxHP;
        resourcesLeft = stats.resourcesProvided;


    }

    private void setOrder(Order order)
    {
        currentOrder = order;
    }

    private void Update() 
    {
        if (attackCooldown > 0)
            attackCooldown = Mathf.Max(0, attackCooldown - Time.deltaTime);

        if (harvestCooldown > 0)
            harvestCooldown = Mathf.Max(0, harvestCooldown - Time.deltaTime);

        if (currentTargetUnit == null && currentOrder == Order.Attack)
            setOrder(Order.Stop);

        if (currentOrder != Order.Harvest && mineParticleSystem.isPlaying)
            mineParticleSystem.Stop();

        if (stats.isResourceNode && currentTargetUnit != null && currentTargetUnit.currentTargetUnit != this)
            ClearTargetUnit();

        if (currentOrder == Order.Harvest && harvestResourceCarryAmount >= stats.harvestResourceCarryMax)
        {
            UnitController closestResourceDepotUnit = FindClosestResourceDepot();
            if (closestResourceDepotUnit)
            {
                SetTargetUnit(closestResourceDepotUnit, Order.ReturnResources);
            } else
            {
                ClearTargetUnit();
            }
        }

        // Acting on target unit orders
        if (currentTargetUnit != null)
        {
            if (currentOrder == Order.Follow)
            {
                MoveTorwardsTargetUnit();
                //RotateTowardsUnit(currentTargetUnit);
            }

            if (currentOrder == Order.Attack)
            {
                //RotateTowardsUnit(currentTargetUnit);
                AttackTargetUnit();
            }

            if (currentOrder == Order.Harvest)
            {
                lastTargetResourceUnit = currentTargetUnit;
                RotateTowardsUnit(currentTargetUnit);
                HarvestTargetUnit();
            }

            if (currentOrder == Order.ReturnResources)
            {
                ReturnResourcesToDepotUnit();
            }
        }
    }

    private void MoveTorwardsTargetUnit()
    {
        if (navAgent)
            navAgent.destination = currentTargetUnit.transform.position;
    }

    private void StopMovingTorwardsTargetUnit()
    {
        if (!navAgent)
            return;

        navAgent.isStopped = true;
        navAgent.ResetPath();
    }

    private void RotateTowardsUnit(UnitController unit)
    {
        Vector3 direction = (unit.transform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 0.05f);
    }

    public void SetTargetDestination(Vector3 destination)
    {
        setOrder(Order.Move);
        currentTargetUnit = null;
        if (navAgent)
            navAgent.destination = destination;
    }

    public void SetTargetUnit(UnitController targetUnit)
    {
        currentTargetUnit = targetUnit;

        currentTargetUnit.FlashSelectionRing();

        if (stats.canAttack && IsEnemy(currentTargetUnit) && !currentTargetUnit.isNeutral)
            setOrder(Order.Attack);
        else if (stats.canHarvest && currentTargetUnit.isNeutral && currentTargetUnit.stats.isResourceNode)
            setOrder(Order.Harvest);
        else if (stats.canHarvest && harvestResourceCarryAmount > 0 && IsOwn(currentTargetUnit) && currentTargetUnit.stats.isResourceDepot)
            setOrder(Order.ReturnResources);
        else if (IsOwn(currentTargetUnit))
            setOrder(Order.Follow);
    }

    public void SetTargetUnit(UnitController targetUnit, Order order)
    {
        currentTargetUnit = targetUnit;
        setOrder(order);
    }

    public void ClearTargetUnit()
    {
        currentTargetUnit = null;
        lastTargetResourceUnit = null;
        setOrder(Order.Stop);
    }

    public void SetSelected(bool isSelected)
    {
        highlightCircle.gameObject.SetActive(isSelected);
    }

    private void SetSelectedFalse()
    {
        SetSelected(false);
    }

    public void FlashSelectionRing()
    {
        highlightCircle.gameObject.SetActive(true);
        Invoke("SetSelectedFalse", 0.3f);
    }

    public void Die()
    {
        Destroy(gameObject);
    }

    public void Damage(float attackDamage, UnitController damagingUnit)
    {
        hp -= attackDamage;
        if (hp <= 0)
            Die();

        if (damagingUnit != null && stats.canAttack) 
            SetTargetUnit(damagingUnit);
    }

    public void ExtractResources(float amount, UnitController harvesterUnit)
    {
        float extractedResources = Mathf.Min(amount, resourcesLeft);
        resourcesLeft -= extractedResources;
        harvesterUnit.harvestResourceCarryAmount += extractedResources;
        if (resourcesLeft <= 0)
            Die();
    }

    public void HarvestTargetUnit()
    {
        if (!currentTargetUnit || !stats.canHarvest || !currentTargetUnit.stats.isResourceNode)
            return;

        if (harvestCooldown > 0)
            return;

        if (currentTargetUnit.isResourceBusy && currentTargetUnit.currentTargetUnit != this)
        {
            UnitController nextResource = FindClosestFreeResource();
            if (nextResource)
            {
                SetTargetUnit(nextResource, Order.Harvest);
            } else
            {
                return;
            }
        }
        if (DistanceToUnit(currentTargetUnit) > stats.harvestRange + currentTargetUnit.collisionSize * .6f)
        {
            MoveTorwardsTargetUnit();
            return;
        }

        StopMovingTorwardsTargetUnit();
        mineParticleSystem.Play();
        currentTargetUnit.currentTargetUnit = this;
        harvestCooldown += stats.harvestSpeed;
        lastTargetResourceUnit = currentTargetUnit;
        currentTargetUnit.ExtractResources(stats.harvestAmount, this);
    }

    public void ReturnResourcesToDepotUnit()
    {
        if (!currentTargetUnit)
            return;

        if (DistanceToUnit(currentTargetUnit) > stats.harvestRange + currentTargetUnit.collisionSize * .6f)
        {
            MoveTorwardsTargetUnit();
            return;
        }

        StopMovingTorwardsTargetUnit();
        PlayerManager.instance.playerResources += harvestResourceCarryAmount;
        harvestResourceCarryAmount = 0;
        if (lastTargetResourceUnit)
            SetTargetUnit(lastTargetResourceUnit, Order.Harvest);
        else
            ClearTargetUnit();
    }

    public void AttackTargetUnit()
    {
        if (!currentTargetUnit || !stats.canAttack)
            return;


        if (attackCooldown > 0)
            return;

        if (DistanceToUnit(currentTargetUnit) > stats.attackRange)
        {
            MoveTorwardsTargetUnit();
            return;
        }

        StopMovingTorwardsTargetUnit();
        RotateTowardsUnit(currentTargetUnit);
        attackCooldown += stats.attackSpeed;
        fireParticleSystem.Play();
        currentTargetUnit.Damage(stats.attackDamage, this);
    }

    public void TrainUnit(UnitStats stats)
    {
        Vector3 position = transform.position;
        position.x += collisionSize;
        GameObject unitObject = Instantiate(Resources.Load<GameObject>("UnitPrefab"), position, Quaternion.identity);
        UnitController unit = unitObject.GetComponent<UnitController>();
        unit.stats = stats;
        unit.playerID = playerID;
        if (unit.stats.canHarvest)
        {
            unit.SetTargetUnit(unit.FindClosestFreeResource(), Order.Harvest);
        }
    }

    // TODO Break out to separate global helper class?
    private UnitController FindClosestResourceDepot()
    {
        float shortestDistance = float.PositiveInfinity;
        UnitController resourceDepotTargetUnit = null;
        foreach (UnitController resourceDepotUnit in FindObjectsOfType<UnitController>())
        {
            if (resourceDepotUnit.playerID == playerID && resourceDepotUnit.stats.isResourceDepot)
            {
                float distance = DistanceToUnit(resourceDepotUnit);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    resourceDepotTargetUnit = resourceDepotUnit;
                }
            }
        }

        if (resourceDepotTargetUnit && shortestDistance < float.PositiveInfinity)
        {
            return resourceDepotTargetUnit;
        }
        else
        {
            Debug.Log("Unable to find a nearby resource depot");
            return null;

        }
    }

    private UnitController FindClosestFreeResource()
    {
        float shortestDistance = float.PositiveInfinity;
        UnitController resourceTargetUnit = null;
        foreach (UnitController resourceUnit in FindObjectsOfType<UnitController>())
        {
            if (resourceUnit.isNeutral && resourceUnit.stats.isResourceNode && !resourceUnit.isResourceBusy)
            {
                float distance = DistanceToUnit(resourceUnit);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    resourceTargetUnit = resourceUnit;
                }
            }
        }
        return resourceTargetUnit;
    }

}
