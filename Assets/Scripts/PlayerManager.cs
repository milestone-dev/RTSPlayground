using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    RaycastHit hit;
    List<UnitController> selectedUnits = new List<UnitController>();
    bool isDragging;
    Vector3 mousePosition;
    Texture2D dragTexture;
    public int humanPlayerID;
    public float playerResources;
    private ParticleSystem targetEmitter;

    public static PlayerManager instance;

    private void Start()
    {
        instance = this;
        dragTexture = new Texture2D(1, 1);
        dragTexture.SetPixel(0, 0, Color.white);
        dragTexture.Apply();
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

        if (selectedUnits.Count > 0)
        {
            UnitController firstSelectedUnit = selectedUnits[0];
            var unitInfoData = new Dictionary<object, object>();
            unitInfoData.Add(firstSelectedUnit.stats.unitName, null);
            unitInfoData.Add("Player", firstSelectedUnit.playerID);
            unitInfoData.Add("HP", firstSelectedUnit.hp);
            unitInfoData.Add("Order", firstSelectedUnit.currentOrder);
            if (firstSelectedUnit.stats.canHarvest)
                unitInfoData.Add("Carrying Resources", firstSelectedUnit.harvestResourceCarryAmount);
            if (firstSelectedUnit.stats.isResourceNode)
                unitInfoData.Add("Resources", firstSelectedUnit.resourcesLeft);
            if (firstSelectedUnit.currentTargetUnit)
                unitInfoData.Add("Target Unit", $"{firstSelectedUnit.currentTargetUnit.stats.unitName} ({firstSelectedUnit.currentTargetUnit})");

            DrawInfoBox(unitInfoData);

            float buttonX = 0;
            float buttonY = 0;
            float buttonPadding = 8;
            float buttonWidth = 48;
            for (var i = 0; i < firstSelectedUnit.stats.trainableUnits.Count; i++)
            {
                UnitStats trainableUnitStats = firstSelectedUnit.stats.trainableUnits[i];
                if (GUI.Button(
                    new Rect(Screen.width - 200 + buttonX, Screen.height - 100 + buttonY, buttonWidth, buttonWidth),
                    new GUIContent(trainableUnitStats.name, trainableUnitStats.GetTooltipText()))
                ) {
                    if (playerResources >= trainableUnitStats.productionCost)
                    {
                        firstSelectedUnit.TrainUnit(trainableUnitStats);
                        playerResources -= trainableUnitStats.productionCost;
                    }
                }
                buttonX += buttonWidth + buttonPadding;
            }
            if (!string.IsNullOrEmpty(GUI.tooltip))
            {
                float offset = 40;
                GUIStyle style = GUI.skin.box;
                style.alignment = TextAnchor.MiddleLeft;
                style.wordWrap = true;
                Vector2 size = Vector2.zero;
                size.x = 100;
                size.y = style.CalcHeight(new GUIContent(GUI.tooltip), size.x);
                GUI.Box(new Rect(Input.mousePosition.x - size.x - offset, Screen.height - Input.mousePosition.y - size.y - offset, size.x, size.y), GUI.tooltip);
            }
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
                    if (unit.playerID == humanPlayerID && unit.stats.type == UnitType.Unit && IsUnitWithinSelectionBounds(unit.transform))
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
                            unit.SetTargetDestination(hit.point);
                            targetEmitter.transform.position = hit.point + new Vector3(0, 0.5f, 0);
                            targetEmitter.Play();
                        }
                    }
                } else if (hit.transform.CompareTag("Unit")) {
                    UnitController targetUnit = hit.transform.gameObject.GetComponent<UnitController>();
                    foreach (UnitController unit in selectedUnits)
                    {
                        if (unit.playerID == humanPlayerID)
                        {
                            unit.SetTargetUnit(targetUnit);
                            targetUnit.FlashSelectionRing();
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
        GUI.DrawTexture(rect, dragTexture);

        GUI.color = borderColor;
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, 1), dragTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, 1, rect.height), dragTexture);
        GUI.DrawTexture(new Rect(rect.xMax - 1, rect.yMin, 1, rect.height), dragTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - 1, rect.width, 1), dragTexture);

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