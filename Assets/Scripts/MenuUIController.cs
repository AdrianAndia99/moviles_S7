using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Controla la UI del Menú principal mostrando datos y permitiendo acciones.
/// Debe existir en la escena de menú y referenciar al PlayerDataManager (que persiste).
/// </summary>
public class MenuUIController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerDataManager dataManager; // Asignar (el mismo que persiste)

    [Header("Texts")] 
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private TMP_Text skillPointsText;
    [SerializeField] private TMP_Text statsText;

    [Header("Edit Name")] 
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button applyNameBtn;

    [Header("Stats Buttons")] 
    [SerializeField] private Button addStrBtn;
    [SerializeField] private Button addDefBtn;
    [SerializeField] private Button addAgiBtn;

    [Header("Gameplay Actions")] 
    [SerializeField] private int expPerClick = 25;
    [SerializeField] private bool clickAnywhereGivesExp = true;
    [Tooltip("Si está activo, no se otorga EXP cuando el click ocurre sobre un elemento UI (botones, inputfields, etc.)")] 
    [SerializeField] private bool ignoreClicksOverUI = true;

    private void OnEnable()
    {
        if (dataManager != null)
        {
            dataManager.OnPlayerDataReady += RefreshAll;
            dataManager.OnPlayerDataChanged += RefreshAll;
        }

        if (applyNameBtn) applyNameBtn.onClick.AddListener(ApplyName);
        if (addStrBtn) addStrBtn.onClick.AddListener(() => dataManager.SpendSkillPoint(PlayerDataManager.StatType.Strength));
        if (addDefBtn) addDefBtn.onClick.AddListener(() => dataManager.SpendSkillPoint(PlayerDataManager.StatType.Defense));
        if (addAgiBtn) addAgiBtn.onClick.AddListener(() => dataManager.SpendSkillPoint(PlayerDataManager.StatType.Agility));
    // Eliminado botón de EXP; ahora solo clicks de pantalla (si está activo)

        // Si ya estaba listo antes de cargar la escena
        if (dataManager != null && dataManager.Data != null)
        {
            RefreshAll(dataManager.Data);
        }
    }

    private void OnDisable()
    {
        if (dataManager != null)
        {
            dataManager.OnPlayerDataReady -= RefreshAll;
            dataManager.OnPlayerDataChanged -= RefreshAll;
        }
        if (applyNameBtn) applyNameBtn.onClick.RemoveListener(ApplyName);
        if (addStrBtn) addStrBtn.onClick.RemoveAllListeners();
        if (addDefBtn) addDefBtn.onClick.RemoveAllListeners();
        if (addAgiBtn) addAgiBtn.onClick.RemoveAllListeners();
    // (Sin botón de EXP)
    }

    private void ApplyName()
    {
        if (string.IsNullOrWhiteSpace(nameInput.text)) return;
        dataManager.UpdateLocalPlayerName(nameInput.text);
    }

    private void RefreshAll(PlayerData data)
    {
        if (data == null) return;
        if (playerNameText) playerNameText.text = data.PlayerName;
        if (levelText) levelText.text = $"Nivel: {data.Level}";
        if (expText) expText.text = $"Exp: {data.CurrentExp}/{data.ExpToNextLevel}";
        if (skillPointsText) skillPointsText.text = $"Puntos: {data.SkillPoints}";
        if (statsText) statsText.text = $"STR {data.Strength} | DEF {data.Defense} | AGI {data.Agility}";
        if (nameInput && string.IsNullOrWhiteSpace(nameInput.text)) nameInput.text = data.PlayerName;

        // Habilita/Deshabilita botones de stats según puntos
        bool canSpend = data.SkillPoints > 0;
        if (addStrBtn) addStrBtn.interactable = canSpend;
        if (addDefBtn) addDefBtn.interactable = canSpend;
        if (addAgiBtn) addAgiBtn.interactable = canSpend;
    }

    private void Update()
    {
        if (!clickAnywhereGivesExp) return;
        if (dataManager == null || dataManager.Data == null) return;
        if (Input.GetMouseButtonDown(0))
        {
            if (ignoreClicksOverUI && IsPointerOverUI()) return;
            dataManager.AddExperience(expPerClick);
        }
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        // Método rápido: si cualquier UI bloquea el pointer
        if (EventSystem.current.IsPointerOverGameObject()) return true;

        // (Opcional) Raycast más explícito para soportar pantalla táctil o múltiples pointers
        var eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0; // Si hay algo UI debajo
    }
}
