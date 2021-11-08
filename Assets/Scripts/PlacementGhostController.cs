using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlacementGhostController : MonoBehaviour
{
    public UnitType unitType;
    private Renderer placementGhostObjectRenderer;
    private GameObject model;
    public Material ghostMaterialValid;
    public Material ghostMaterialInvalid;
    public bool isPlacementValid;
    public float gridSize = 2f;

    public void setUnitType(UnitType type)
    {
        unitType = type;
        model = Instantiate(unitType.prefabModel, Vector3.zero, Quaternion.identity);
        model.transform.parent = transform;
        model.transform.localPosition = new Vector3(0, -0.5f, 0);
        placementGhostObjectRenderer = model.GetComponent<Renderer>();
    }

    private void SetPlacementGhostValid(bool valid)
    {
        var ghostMaterials = new Material[placementGhostObjectRenderer.materials.Length];
        for (var i = 0; i < ghostMaterials.Length; i++)
        {
            ghostMaterials[i] = valid ? ghostMaterialValid : ghostMaterialInvalid;
        }
        placementGhostObjectRenderer.materials = ghostMaterials;
    }

    public void UpdatePlacement()
    {
        Collider[] hitColliders = Physics.OverlapBox(gameObject.transform.position, placementGhostObjectRenderer.bounds.size / 2, Quaternion.identity);
        bool valid = true;
        for (var i = 0; i < hitColliders.Length; i++)
        {
            if (hitColliders[i].CompareTag("Unit"))
            {
                valid = false;
                break;
            }
        }
        isPlacementValid = valid;
        SetPlacementGhostValid(valid);
    }

    public void Destroy()
    {
        Destroy(this);
    }

    public void SetPositionRoundedToGrid(Vector3 targetPosition)
    {
        transform.position = new Vector3(
            RoundToNearestGrid(targetPosition.x),
            targetPosition.y,
            RoundToNearestGrid(targetPosition.z));
    }

    float RoundToNearestGrid(float pos)
    {
        float xDiff = pos % gridSize;
        pos -= xDiff;
        if (xDiff > (gridSize / 2))
        {
            pos += gridSize;
        }
        return pos;
    }
}
