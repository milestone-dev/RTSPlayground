using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public enum Order
{
    Stop,
    Move,
    Follow,
    Guard,
    Attack,
    AttackMove,
    Patrol,
    Harvest,
    ReturnResources,
    Construct,
    Die,
}

public class UnitController : MonoBehaviour
{
    public NavMeshAgent navAgent;
    public NavMeshObstacle navObstacle;
    private ParticleSystem fireParticleSystem;
    private ParticleSystem mineParticleSystem;
    private GameObject highlightCircle;
    private GameObject mineralsObject;
    private BoxCollider boxCollider;
    public float collisionSize;
    public UnitType type;
    public int playerID = 0;
    public Order currentOrder;
    public float hp;
    public float attackCooldown;
    public float harvestCooldown;
    public float harvestResourceCarryAmount;
    public Vector3 rallyPointPosition = Vector3.zero;
    public UnitController rallyPointUnit = null;
    public Vector3 currentTargetPosition = Vector3.zero;
    public UnitController currentTargetUnit;
    public UnitController lastTargetResourceUnit;
    public UnitController aiDataLastConstructedUnit;
    public float resourcesLeft;

    public AIController ai;

    public UnitType constructionUnitType;

    public List<UnitType> productionQueue = new List<UnitType>();
    public float remainingProductionTime;

    public UnitID id { get { return type.id; } }
    public bool isOwnedByHumanPlayer  { get { return playerID == PlayerManager.instance.humanPlayerID; } }
    public bool isNeutral  { get { return playerID == 0; } }
    public bool isResourceBusy  { get { return currentTargetUnit != null; } }
    public bool isUnitProducer { get { return type.trainableUnits.Count != 0; } }
    public bool isUnitConstructor { get { return type.constructableUnits.Count != 0; } }
    public bool isTrainingUnit { get { return isUnitProducer && remainingProductionTime > 0 && productionQueue.Count > 0; } }
    public bool isUnit {  get { return type.unitClass == UnitClass.Unit; } }
    public bool isBuilding {  get { return type.unitClass == UnitClass.Building; } }
    public bool isHarvester {  get { return type.canHarvest; } }
    public bool isMilitary {  get { return type.canAttack; } }
    public bool IsEnemy(UnitController unit) { return !unit.isNeutral && unit.playerID != playerID; }
    public bool IsOwn(UnitController unit) { return unit.playerID == playerID; }

    private float DistanceToUnit(UnitController unit)
    {
        return Vector3.Distance(transform.position, unit.transform.position);
    }

    private float DistanceToUnitBounds(UnitController unit)
    {
        return Vector3.Distance(transform.position, unit.gameObject.GetComponent<Collider>().ClosestPoint(transform.position));
    }

    private void Start()
    {
        fireParticleSystem = transform.Find("FireParticleSystem").GetComponent<ParticleSystem>();
        mineParticleSystem = transform.Find("MineParticleSystem").GetComponent<ParticleSystem>();
        highlightCircle = transform.Find("Highlight").gameObject;
        mineralsObject = transform.Find("Minerals").gameObject;

        if (!type)
        {
            Debug.Log($"{this} is missing UnitType data");
            Die();
            return;
        }

        name = type.name;

        if (isUnit)
        {
            navAgent = gameObject.AddComponent<NavMeshAgent>();
            navAgent.speed = type.movementSpeed;
            navAgent.angularSpeed = type.movementAngularSpeed;
            navAgent.acceleration = type.movementAcceleration;
            navAgent.speed = type.movementSpeed;
        }
        else if (isBuilding)
        {
            navObstacle = gameObject.AddComponent<NavMeshObstacle>();
            navObstacle.carving = true;
            navObstacle.carveOnlyStationary = true;
        }

        if (type.prefabModel)
        {
            GameObject model = Instantiate(type.prefabModel, Vector3.zero, Quaternion.identity);
            model.transform.parent = gameObject.transform;
            Renderer renderer = model.GetComponent<Renderer>();
            Vector3 modelSize = renderer.bounds.size;
            model.transform.localPosition = new Vector3(0, -0.5f, 0);

            boxCollider = GetComponent<BoxCollider>();
            boxCollider.size = modelSize;

            if (navObstacle)
                navObstacle.size = modelSize;

            collisionSize = Mathf.Max(modelSize.x, modelSize.z);
            float circleScaleMultiplier = 0.65f;
            Vector3 circleScale = new Vector3(circleScaleMultiplier * collisionSize, circleScaleMultiplier * collisionSize, circleScaleMultiplier * collisionSize);
            highlightCircle.transform.localScale = circleScale;

            // Player color
            var modelMaterials = new Material[renderer.materials.Length];
            for (var i = 0; i < modelMaterials.Length; i++)
            {
                if (renderer.materials[i].color == Color.cyan)
                {
                    modelMaterials[i] = PlayerManager.instance.getPlayerMaterial(playerID);
                } else
                {
                    modelMaterials[i] = renderer.materials[i];
                }
            }
            renderer.materials = modelMaterials;
        }

        // Set up variables
        hp = type.maxHP;
        resourcesLeft = type.resourcesProvided;
        harvestCooldown = type.harvestSpeed;

        ai = AIController.GetController(playerID);
        if (ai)
        {
            ai.AddUnit(this);
            if (type.canHarvest) ai.AddHarvesterUnit(this);
            if (type.canAttack) ai.AddMilitaryUnit(this);
            if (isUnitConstructor) ai.AddConstructionUnit(this);
            if (isUnitProducer) ai.AddProductionStructureUnit(this);
        }
    }

    private void setOrder(Order order)
    {
        currentOrder = order;
    }

    private void Update() 
    {
        if (!type)
            return;

        if (attackCooldown > 0)
            attackCooldown = Mathf.Max(0, attackCooldown - Time.deltaTime);

        if (currentTargetUnit == null && currentOrder == Order.Attack)
            setOrder(Order.Stop);

        if (currentOrder != Order.Harvest && mineParticleSystem.isPlaying)
            mineParticleSystem.Stop();

        if (type.canHarvest && harvestResourceCarryAmount > 0 && !mineralsObject.activeSelf)
            mineralsObject.SetActive(true);
        else if (type.canHarvest && harvestResourceCarryAmount == 0 && mineralsObject.activeSelf)
            mineralsObject.SetActive(false);

        // Clear resource "busy" miner targeting
        if (type.isResourceNode && currentTargetUnit != null && currentTargetUnit.currentTargetUnit != this)
            ClearTargetUnit();

        if (navAgent && currentTargetPosition != Vector3.zero && navAgent.destination != currentTargetPosition)
            navAgent.destination = currentTargetPosition;

        if ((currentOrder == Order.Move || currentOrder == Order.AttackMove) && navAgent && navAgent.remainingDistance < 0.5)
            Stop();

        if (currentOrder == Order.Patrol && navAgent && (navAgent.remainingDistance < 0.5 || navAgent.pathStatus == NavMeshPathStatus.PathPartial))
            PatrolNearbyArea();

        if (currentOrder == Order.Stop && type.canAttack)
            setOrder(Order.Guard);

        if (currentOrder == Order.Guard || currentOrder == Order.AttackMove || currentOrder == Order.Patrol)
        {
            UnitController enemyUnit = FindEnemyUnitInRange();
            if (enemyUnit)
                SetTargetUnit(enemyUnit, Order.Attack);
        }

        if (currentOrder == Order.Harvest && harvestResourceCarryAmount >= type.harvestResourceCarryMax)
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

        if (currentOrder == Order.Construct)
            ConstructIntendedUnit();

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
                RotateTowardsUnit(currentTargetUnit);
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

        if (isUnitProducer)
        {
            HandleUnitTraining();
        }

        if (type.canHarvest)
        {
            if ((currentOrder == Order.Harvest || currentOrder == Order.ReturnResources) && navAgent.obstacleAvoidanceType != ObstacleAvoidanceType.NoObstacleAvoidance)
                navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            else if ((currentOrder != Order.Harvest && currentOrder != Order.ReturnResources) && navAgent.obstacleAvoidanceType != ObstacleAvoidanceType.HighQualityObstacleAvoidance)
                navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }
    }

    private void MoveTorwardsTargetUnit()
    {
        Vector3 currentTargetUnitClosestPosition = currentTargetUnit.gameObject.GetComponent<Collider>().ClosestPoint(transform.position);

        if (navAgent)
            navAgent.destination = currentTargetUnitClosestPosition;
    }

    private void Stop()
    {
        StopMovingTorwardsTarget();
        setOrder(Order.Stop);
    }

    private void StopMovingTorwardsTarget()
    {
        if (!navAgent)
            return;

        currentTargetPosition = Vector3.zero;
        navAgent.isStopped = true;
        navAgent.ResetPath();
    }

    private void RotateTowardsUnit(UnitController unit)
    {
        Vector3 direction = (unit.transform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 0.05f);
    }

    public void SetTargetPosition(Vector3 position)
    {
        SetTargetPosition(position, Order.Move);
    }

    public void SetTargetPosition(Vector3 position, Order order)
    {
        currentTargetPosition = position;
        currentTargetUnit = null;
        setOrder(order);
    }

    public void SetTargetUnit(UnitController targetUnit)
    {
        currentTargetUnit = targetUnit;

        if (type.canAttack && IsEnemy(currentTargetUnit) && !currentTargetUnit.isNeutral)
            setOrder(Order.Attack);
        else if (type.canHarvest && currentTargetUnit.isNeutral && currentTargetUnit.type.isResourceNode)
            setOrder(Order.Harvest);
        else if (type.canHarvest && harvestResourceCarryAmount > 0 && IsOwn(currentTargetUnit) && currentTargetUnit.type.isResourceDepot)
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

    public void SetRallyPoint(UnitController unit)
    {
        rallyPointPosition = Vector3.zero;
        rallyPointUnit = unit;
    }

    public void SetRallyPoint(Vector3 point)
    {
        rallyPointUnit = null;
        rallyPointPosition = point;
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

    public void HarvestNearbyResources()
    {
        SetTargetUnit(FindClosestFreeResource(), Order.Harvest);
    }

    public void PatrolNearbyArea()
    {
        SetTargetPosition(transform.position + new Vector3(Random.Range(-15, 15), 0, Random.Range(-15, 15)), Order.Patrol);
    }

    public void Die()
    {
        Remove();
    }

    public void Remove()
    {
        if (ai) ai.RemoveUnit(this);
        Destroy(gameObject);
    }

    public void Damage(float attackDamage, UnitController damagingUnit)
    {
        hp -= attackDamage;
        if (hp <= 0)
            Die();

        if (damagingUnit != null && type.canAttack) 
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
        if (!currentTargetUnit || !type.canHarvest || !currentTargetUnit.type.isResourceNode)
            return;


        if (currentTargetUnit.isResourceBusy && currentTargetUnit.currentTargetUnit != this)
        {
            UnitController nextResource = FindClosestFreeResource();
            mineParticleSystem.Stop();
            if (nextResource)
            {
                SetTargetUnit(nextResource, Order.Harvest);
            } else
            {
                MoveTorwardsTargetUnit();
                StopMovingTorwardsTarget();
            }
            return;
        }

        if (DistanceToUnitBounds(currentTargetUnit) > type.harvestRange)
        {
            mineParticleSystem.Stop();
            MoveTorwardsTargetUnit();
            return;
        }

        StopMovingTorwardsTarget();

        if (navAgent.velocity != Vector3.zero)
            return;


        mineParticleSystem.Play();
        currentTargetUnit.currentTargetUnit = this;
        lastTargetResourceUnit = currentTargetUnit;

        if (harvestCooldown > 0)
        {
            harvestCooldown = Mathf.Max(0, harvestCooldown - Time.deltaTime);
            return;
        }

        harvestCooldown += type.harvestSpeed;
        currentTargetUnit.ExtractResources(type.harvestAmount, this);
    }

    public void ReturnResourcesToDepotUnit()
    {
        if (!currentTargetUnit)
            return;

        if (DistanceToUnitBounds(currentTargetUnit) > type.harvestRange)
        {
            MoveTorwardsTargetUnit();
            return;
        }

        StopMovingTorwardsTarget();

        if (ai)
        {
            ai.AddResources(harvestResourceCarryAmount);
        } else
        {
            PlayerManager.instance.playerResources += harvestResourceCarryAmount;
        }
        harvestResourceCarryAmount = 0;
        if (lastTargetResourceUnit)
            SetTargetUnit(lastTargetResourceUnit, Order.Harvest);
        else
            HarvestNearbyResources();
    }

    public void AttackTargetUnit()
    {
        if (!currentTargetUnit || !type.canAttack)
            return;

        if (attackCooldown > 0)
            return;

        if (DistanceToUnitBounds(currentTargetUnit) > type.attackRange)
        {
            MoveTorwardsTargetUnit();
            return;
        }

        StopMovingTorwardsTarget();
        attackCooldown += type.attackSpeed;
        fireParticleSystem.Play();
        currentTargetUnit.Damage(type.attackDamage, this);
    }

    public bool TrainUnit(UnitType unitType)
    {
        if (ai)
        {
            if (!ai.CanAffordUnit(unitType.id)) return false;
            ai.SubtractResources(unitType.productionCost);
        } else
        {
            if (!PlayerManager.instance.CanAffordUnit(unitType.id)) return false;
            PlayerManager.instance.playerResources -= unitType.productionCost;
        }

        if (this.productionQueue.Count == 0)
            this.remainingProductionTime = unitType.productionTime;

        this.productionQueue.Add(unitType);
        return true;
    }

    public bool ConstructIntendedUnit()
    {
        if (Vector3.Distance(transform.position, currentTargetPosition) > 1)
            return false;

        aiDataLastConstructedUnit = UnitController.CreateUnit(constructionUnitType, currentTargetPosition, this.playerID);
        constructionUnitType = null;
        Stop();
        return true;
    }

    public void HandleUnitTraining()
    {
        if (this.productionQueue.Count == 0)
            return;

        if (GameManager.instance.operationCWAL && this.remainingProductionTime > 1)
            this.remainingProductionTime = 1;

        UnitType firstUnitType = this.productionQueue[0];
        if (this.remainingProductionTime <= 0)
        {
            // Create the unit and remove it from the queue
            Vector3 position = transform.position;
            position.x += collisionSize;
            UnitController producedUnit = UnitController.CreateUnit(firstUnitType, position, this.playerID, this);
            this.productionQueue.RemoveAt(0);

            if (ai) ai.UnitProduced(producedUnit);

            // Queue up the next unit
            if (this.productionQueue.Count > 0)
                this.remainingProductionTime = this.productionQueue[0].productionTime;
        } else
        {
            this.remainingProductionTime -= Time.deltaTime;
        }
    }

    public static UnitController CreateUnit(UnitType unitType, Vector3 position, int playerID)
    {
        return UnitController.CreateUnit(unitType, position, playerID, null);
    }

    public static UnitController CreateUnit(UnitType unitType, Vector3 position, int playerID, UnitController creatingUnit)
    {
        GameObject unitObject = Instantiate(Resources.Load<GameObject>("Prefabs/UnitPrefab"), position, Quaternion.identity);
        if (!unitObject) return null;
        UnitController unit = unitObject.GetComponent<UnitController>();
        unit.type = unitType;
        unit.playerID = playerID;
        if (unit.type.unitClass == UnitClass.Unit)
        {
            if (creatingUnit && creatingUnit.rallyPointUnit != null)
            {
                unit.SetTargetUnit(creatingUnit.rallyPointUnit);
            } else if (creatingUnit && creatingUnit.rallyPointPosition != Vector3.zero)
            {
                unit.SetTargetPosition(creatingUnit.rallyPointPosition);
            } else
            {
                //unit.SetTargetPosition(position + new Vector3(0.1f, 0, 0.1f));
                //unit.Invoke("Stop", .5f);
            }
        }
        return unit;
    }

    public bool CanMoveToPoint(Vector3 point)
    {
        return NavMesh.CalculatePath(transform.position, point, NavMesh.AllAreas, new NavMeshPath());
    }

    // TODO Break out to separate global helper class?
    private UnitController FindClosestResourceDepot()
    {
        float shortestDistance = float.PositiveInfinity;
        UnitController resourceDepotTargetUnit = null;
        foreach (UnitController resourceDepotUnit in FindObjectsOfType<UnitController>())
        {
            if (resourceDepotUnit.playerID == playerID && resourceDepotUnit.type.isResourceDepot)
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
            Debug.Log("Unable to find a nearby resource depot!");
            return null;

        }
    }

    private UnitController FindClosestFreeResource()
    {
        float shortestDistance = float.PositiveInfinity;
        UnitController resourceTargetUnit = null;
        foreach (UnitController resourceUnit in FindObjectsOfType<UnitController>())
        {
            if (resourceUnit.isNeutral && resourceUnit.type.isResourceNode && !resourceUnit.isResourceBusy)
            {
                float distance = DistanceToUnit(resourceUnit);
                if (distance <= type.harvestSeekRange && distance < shortestDistance)
                {
                    shortestDistance = distance;
                    resourceTargetUnit = resourceUnit;
                }
            }
        }
        return resourceTargetUnit;
    }

    private UnitController FindEnemyUnitInRange()
    {
        float shortestDistance = float.PositiveInfinity;
        UnitController enemyTargetUnit = null;
        foreach (UnitController enemyUnit in FindObjectsOfType<UnitController>())
        {
            if (IsEnemy(enemyUnit))
            {
                float distance = DistanceToUnit(enemyUnit);
                if (distance < type.attackAggroRange && distance < shortestDistance)
                {
                    shortestDistance = distance;
                    enemyTargetUnit = enemyUnit;
                }
            }
        }
        return enemyTargetUnit;
    }

    void OnDrawGizmos()
    {
        // Draw a yellow sphere at the transform's position
        if (playerID == 0)
            Gizmos.color = Color.cyan;
        else if (playerID == 1)
            Gizmos.color = Color.green;
        else if (playerID == 2)
            Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 0.4f);
    }
}
