using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Manages the 4×4 localisation grid on the LocalisationDesk.
/// Each cell corresponds to one of the 16 epicenter zones.
/// The player looks at a cell and presses E to select it (highlights it).
/// Attach to the LocalisationDesk GameObject.
/// </summary>
public class LocalisationController : MonoBehaviour
{
    [Header("Grid Settings")]
    [Tooltip("Parent transform containing the 16 cell GameObjects (named Cell_0 to Cell_15).")]
    [SerializeField] private Transform gridParent;

    [Header("Materials")]
    [SerializeField] private Material cellDefaultMaterial;
    [SerializeField] private Material cellHighlightMaterial;
    [SerializeField] private Material cellSelectedMaterial;

    [Header("Interaction")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private float aimTolerance = 0.12f;
    [SerializeField] private Camera playerCamera;

    private List<Renderer> cellRenderers = new List<Renderer>();
    private int selectedCellIndex = -1;
    private int hoveredCellIndex  = -1;
    private int targetZoneIndex   = -1;
    private InputAction interactAction;

    private void OnEnable()
    {
        WaveGameManager.OnWaveDataGenerated += OnWaveDataReceived;
    }

    private void OnDisable()
    {
        WaveGameManager.OnWaveDataGenerated -= OnWaveDataReceived;
    }

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        PlayerInput playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput != null)
            interactAction = playerInput.actions["Interact"];

        BuildCellRendererList();
    }

    private void BuildCellRendererList()
    {
        cellRenderers.Clear();
        if (gridParent == null) return;

        for (int i = 0; i < gridParent.childCount; i++)
        {
            Renderer r = gridParent.GetChild(i).GetComponent<Renderer>();
            if (r != null) cellRenderers.Add(r);
        }
    }

    private void Update()
    {
        CheckHover();

        if (hoveredCellIndex >= 0 && interactAction != null && interactAction.WasPressedThisFrame())
        {
            SelectCell(hoveredCellIndex);
        }
    }

    private void CheckHover()
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        int previousHover = hoveredCellIndex;
        hoveredCellIndex = -1;

        if (Physics.SphereCast(ray, aimTolerance, out RaycastHit hit, interactDistance))
        {
            for (int i = 0; i < cellRenderers.Count; i++)
            {
                if (cellRenderers[i] != null && hit.collider.gameObject == cellRenderers[i].gameObject)
                {
                    hoveredCellIndex = i;
                    break;
                }
            }
        }

        if (hoveredCellIndex != previousHover)
        {
            RefreshCellVisuals();
        }
    }

    /// <summary>Selects the given cell index and updates highlight.</summary>
    private void SelectCell(int index)
    {
        selectedCellIndex = index;
        RefreshCellVisuals();
    }

    private void RefreshCellVisuals()
    {
        for (int i = 0; i < cellRenderers.Count; i++)
        {
            if (cellRenderers[i] == null) continue;

            Material mat;
            if (i == selectedCellIndex)
                mat = cellSelectedMaterial;
            else if (i == hoveredCellIndex)
                mat = cellHighlightMaterial;
            else
                mat = cellDefaultMaterial;

            cellRenderers[i].material = mat;
        }
    }

    private void OnWaveDataReceived(WaveDataSO data)
    {
        targetZoneIndex   = data.epicenterZoneIndex;
        selectedCellIndex = -1;
        hoveredCellIndex  = -1;
        RefreshCellVisuals();
    }

    /// <summary>Returns whether the player selected the correct zone.</summary>
    public bool IsZoneCorrect => selectedCellIndex == targetZoneIndex;

    /// <summary>Returns whether the player has made a selection.</summary>
    public bool HasSelection => selectedCellIndex >= 0;
}
