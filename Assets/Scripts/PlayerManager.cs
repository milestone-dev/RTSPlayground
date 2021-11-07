using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    RaycastHit hit;
    List<UnitController> selectedUnits = new List<UnitController>();
    bool isDragging;
    Vector3 mousePosition;
    public int humanPlayerID;
    public float playerResources;
    private ParticleSystem targetEmitter;

    public static PlayerManager instance;

    private void Start()
    {
        instance = this;
        targetEmitter = transform.Find("TargetEmitter").GetComponent<ParticleSystem>();
        playerResources = 100;
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(Screen.width - 80, 10, 80, 20), string.Format("{0} Resouces", playerResources));

        if (isDragging)
        {
            var rect = GetScreenRect(mousePosition, Input.mousePosition);
            DrawSelectionBox(rect, new Color(.8f, .8f, .8f, .1f), Color.white);
        }

        const float inset = 12;
        const float padding = 24;
        const float sectionHeight = 180;

        GUI.BeginGroup(new Rect(inset, Screen.height - sectionHeight, Screen.width - inset * 2, sectionHeight - inset));
        GUI.Box(new Rect(0, 0, Screen.width - inset * 2, sectionHeight - inset), "");
        if (selectedUnits.Count > 0)
        {
            // Unit info
            UnitController firstSelectedUnit = selectedUnits[0];
            var unitInfoData = new Dictionary<object, object>();
            unitInfoData.Add(firstSelectedUnit.type.unitName, null);
            unitInfoData.Add("Player", firstSelectedUnit.playerID);
            unitInfoData.Add("HP", firstSelectedUnit.hp);
            unitInfoData.Add("Order", firstSelectedUnit.currentOrder);
            if (firstSelectedUnit.type.canHarvest)
                unitInfoData.Add("Carrying Resources", firstSelectedUnit.harvestResourceCarryAmount);
            if (firstSelectedUnit.type.isResourceNode)
                unitInfoData.Add("Resources", firstSelectedUnit.resourcesLeft);
            if (firstSelectedUnit.currentTargetUnit)
                unitInfoData.Add("Target Unit", $"{firstSelectedUnit.currentTargetUnit.type.unitName} ({firstSelectedUnit.currentTargetUnit})");

            if (firstSelectedUnit.isBuilding)
            {
                if (firstSelectedUnit.rallyPointPosition != Vector3.zero)
                    unitInfoData.Add("Rally Point Position", firstSelectedUnit.rallyPointPosition);

                if (firstSelectedUnit.rallyPointUnit != null)
                    unitInfoData.Add("Rally Point Unit", firstSelectedUnit.rallyPointUnit);
            }

            if (firstSelectedUnit.isUnitTrainer)
            {
                unitInfoData.Add("Training Queue", firstSelectedUnit.productionQueue.Count);
            }
            string infoText = "";
            const float rowHeight = 18;
            float labelHeight = rowHeight * unitInfoData.Count;

            foreach (string key in unitInfoData.Keys)
            {
                infoText += string.Format("{0}: {1}\n", key, unitInfoData[key]);
            }

            GUI.Label(new Rect(padding, padding, 600, labelHeight), infoText);

            // Progress bar
            if (firstSelectedUnit.isTrainingUnit) {
                Color defaultColor = GUI.backgroundColor;
                const float progressBoxWidth = 120;
                const float progressBoxHeight = 12;
                const float progressBoxY = sectionHeight / 2 - progressBoxHeight;
                const float progressBoxX = 300;
                float progress = Mathf.Max(0, (firstSelectedUnit.productionQueue[0].productionTime - firstSelectedUnit.remainingProductionTime) / firstSelectedUnit.productionQueue[0].productionTime);
                GUI.color = new Color(1, 1, 1, 0.1f);
                GUI.DrawTexture(new Rect(progressBoxX, progressBoxY, progressBoxWidth, progressBoxHeight), Texture2D.whiteTexture);
                GUI.color = new Color(1, 1, 1);
                GUI.DrawTexture(new Rect(progressBoxX, progressBoxY, progressBoxWidth * progress, progressBoxHeight), Texture2D.whiteTexture);
            }

            // Command card
            const float buttonPadding = 4;
            const float buttonWidth = 48;
            const float gridWidth = 200;
            const float gridY = padding;
            float gridX = Screen.width - padding - gridWidth;
            float buttonX = 0;
            float buttonY = 0;
            GUI.skin.button.fontSize = 10;
            for (var i = 0; i < firstSelectedUnit.type.trainableUnits.Count; i++)
            {
                UnitType trainableUnitType = firstSelectedUnit.type.trainableUnits[i];
                if (GUI.Button(
                    new Rect(gridX + buttonX, gridY + buttonY, buttonWidth, buttonWidth),
                    new GUIContent(trainableUnitType.name, trainableUnitType.GetTooltipText()))
                ) {
                    if (playerResources >= trainableUnitType.productionCost)
                    {
                        firstSelectedUnit.TrainUnit(trainableUnitType);
                        playerResources -= trainableUnitType.productionCost;
                    }
                }

                if ((i + 1) % 3 == 0)
                {
                    buttonX = 0;
                    buttonY += buttonWidth + buttonPadding;
                }
                else
                {
                    buttonX += buttonWidth + buttonPadding;
                }
            }

        }
        GUI.EndGroup();

        if (!string.IsNullOrEmpty(GUI.tooltip))
        {
            float offset = 20;
            GUIStyle style = GUI.skin.box;
            style.alignment = TextAnchor.MiddleLeft;
            style.wordWrap = true;
            Vector2 size = Vector2.zero;
            size.x = 100;
            size.y = style.CalcHeight(new GUIContent(GUI.tooltip), size.x);
            GUI.Box(new Rect(Input.mousePosition.x - size.x - offset, Screen.height - Input.mousePosition.y - size.y - offset, size.x, size.y), GUI.tooltip);
        }
        
    }

    private void DrawInfoBox(Dictionary<object, object> infoData)
    {
        string infoText = "";
        float rowHeight = 18;
        float labelHeight = rowHeight * infoData.Count;

        foreach (string key in infoData.Keys)
        {
            infoText += string.Format("{0}: {1}\n", key, infoData[key]);
        }

        GUI.Label(new Rect(rowHeight, Screen.height - labelHeight - rowHeight, 600, labelHeight), infoText);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            mousePosition = Input.mousePosition;
            isDragging = true;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit) && hit.transform.CompareTag("Unit"))
            {
                UnitController unit = hit.transform.gameObject.GetComponent<UnitController>();
                if (!selectedUnits.Contains(unit))
                {
                    SelectUnit(unit, Input.GetKey(KeyCode.LeftShift));
                }
                else
                {
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        DeselectUnit(unit);
                    }
                }
            }
        }

        if (isDragging && Input.GetMouseButtonUp(0))
        {
            if (Vector3.Distance(mousePosition, Input.mousePosition) > 32)
            {
                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    DeselectAllUnits();
                }
                foreach (UnitController unit in FindObjectsOfType<UnitController>())
                {
                    if (unit.playerID == humanPlayerID && unit.type.unitClass == UnitClass.Unit && IsUnitWithinSelectionBounds(unit.transform))
                    {
                        SelectUnit(unit, true);
                    }
                }
            }
            isDragging = false;
        }

        if (Input.GetMouseButtonDown(1) && selectedUnits.Count > 0)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit)) {
                if (hit.transform.CompareTag("Ground"))
                {
                    foreach (UnitController unit in selectedUnits)
                    {
                        if (unit.playerID == humanPlayerID)
                        {
                            if (unit.isUnit)
                            {
                                unit.SetTargetPosition(hit.point);
                                targetEmitter.transform.position = hit.point + new Vector3(0, 0.5f, 0);
                                targetEmitter.Play();
                            } else if (unit.isBuilding)
                            {
                                unit.SetRallyPoint(hit.point);
                                targetEmitter.transform.position = hit.point + new Vector3(0, 0.5f, 0);
                                targetEmitter.Play();
                            }
                        }
                    }
                } else if (hit.transform.CompareTag("Unit")) {
                    UnitController targetUnit = hit.transform.gameObject.GetComponent<UnitController>();
                    foreach (UnitController unit in selectedUnits)
                    {
                        if (unit.playerID == humanPlayerID)
                        {
                            if (unit.isUnit)
                            {
                                unit.SetTargetUnit(targetUnit);
                                targetUnit.FlashSelectionRing();
                            } else if (unit.isBuilding)
                            {
                                unit.SetRallyPoint(targetUnit);
                                targetUnit.FlashSelectionRing();
                            }
                        }
                    }
                }
            }
        }
    }

    private bool IsUnitWithinSelectionBounds(Transform unit)
    {
        if (!isDragging)
        {
            return false;   
        }
        Camera camera = Camera.main;
        Bounds selectionBounds = GetViewportBounds(camera, mousePosition, Input.mousePosition);
        var unitPos = camera.WorldToViewportPoint(unit.position);
        var within = selectionBounds.Contains(unitPos);
        return within;

    }

    private void SelectUnit(UnitController unit, bool includeInSelection = false)
    {
        if (!includeInSelection)
        {
            DeselectAllUnits();
        }
        selectedUnits.Add(unit);
        unit.SetSelected(true);
    }

    private void DeselectAllUnits()
    {
        for (var i = 0; i < selectedUnits.Count; i++)
        {
            if (selectedUnits[i])
                selectedUnits[i].SetSelected(false);
        }
        selectedUnits.Clear();
    }

    private void DeselectUnit(UnitController unit)
    {
        unit.SetSelected(false);
        selectedUnits.Remove(unit);
    }

    // DrawScreen Stuff

    public void DrawSelectionBox(Rect rect, Color color, Color borderColor)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        GUI.color = borderColor;
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, 1), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, 1, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - 1, rect.yMin, 1, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - 1, rect.width, 1), Texture2D.whiteTexture);

    }

    public Rect GetScreenRect(Vector3 screenPosition1, Vector3 screenPosition2)
    {
        // Move origin from bottom left to top left
        screenPosition1.y = Screen.height - screenPosition1.y;
        screenPosition2.y = Screen.height - screenPosition2.y;
        // Calculate corners
        var topLeft = Vector3.Min(screenPosition1, screenPosition2);
        var bottomRight = Vector3.Max(screenPosition1, screenPosition2);
        // Create Rect
        return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
    }

    public Bounds GetViewportBounds(Camera camera, Vector3 screenPosition1, Vector3 screenPosition2)
    {
        var v1 = camera.ScreenToViewportPoint(screenPosition1);
        var v2 = camera.ScreenToViewportPoint(screenPosition2);
        var min = Vector3.Min(v1, v2);
        var max = Vector3.Max(v1, v2);
        min.z = camera.nearClipPlane;
        max.z = camera.farClipPlane;

        var bounds = new Bounds();

        bounds.SetMinMax(min, max);
        return bounds;
    }
}