using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Authentication;

/// <summary>
/// Se encarga de cargar / crear / guardar los datos de jugador y exponer eventos para la UI.
/// Debe vivir junto a (o después de) UnityPlayerAuth. Puedes marcar este GameObject como DontDestroyOnLoad.
/// </summary>
public class PlayerDataManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private UnityPlayerAuth auth; // Asignar en el inspector

    [Header("Opciones")]
    [SerializeField] private bool loadMenuSceneAfterAuth = true;
    [SerializeField] private string menuSceneName = "MenuScene"; // Crea esta escena y agrégala a Build Settings
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Header("Progresión")]
    [SerializeField] private int baseExp = 100; // EXP necesaria para pasar de nivel 1 a 2
    [SerializeField] private int expIncrementPerLevel = 50; // Incremento lineal por nivel
    [SerializeField] private int skillPointsPerLevel = 5;

    public const string ProfileKey = "player_profile"; // Key en Cloud Save

    public PlayerData Data { get; private set; }

    public event Action<PlayerData> OnPlayerDataReady;    // Llamado una vez tras cargar/crear
    public event Action<PlayerData> OnPlayerDataChanged;  // Llamado en cada cambio + guardado

    private bool _isSaving;

    private void Awake()
    {
        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnEnable()
    {
        if (auth != null)
        {
            auth.OnSingedIn += HandleSignedIn; // Se engancha al evento existente (typo incluido)
        }
    }

    private void OnDisable()
    {
        if (auth != null)
        {
            auth.OnSingedIn -= HandleSignedIn;
        }
    }

    private async void HandleSignedIn(PlayerInfo info, string playerNameFromAuth)
    {
        await LoadOrCreateAsync(playerNameFromAuth);
        OnPlayerDataReady?.Invoke(Data);

        if (loadMenuSceneAfterAuth && !string.IsNullOrEmpty(menuSceneName))
        {
            // Cargamos escena de menú
            if (SceneManager.GetActiveScene().name != menuSceneName)
            {
                SceneManager.LoadScene(menuSceneName);
            }
        }
    }

    public async Task LoadOrCreateAsync(string defaultName)
    {
        try
        {
            var loaded = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { ProfileKey });
            if (loaded.TryGetValue(ProfileKey, out var item))
            {
                string json = item.Value.GetAsString();
                Data = JsonUtility.FromJson<PlayerData>(json);
                // Actualiza el nombre si estuviera vacío
                if (string.IsNullOrWhiteSpace(Data.PlayerName))
                {
                    Data.PlayerName = SanitizeName(defaultName);
                }
            }
            else
            {
                CreateDefault(defaultName);
                await SaveAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PlayerDataManager] No se pudo cargar perfil existente: {ex.Message}. Creando uno nuevo.");
            CreateDefault(defaultName);
            await SaveAsync();
        }
    }

    private void CreateDefault(string baseName)
    {
        Data = new PlayerData
        {
            PlayerName = SanitizeName(baseName),
            Level = 1,
            CurrentExp = 0,
            ExpToNextLevel = CalculateExpNeeded(1),
            SkillPoints = 0,
            Strength = 1,
            Defense = 1,
            Agility = 1
        };
    }

    private string SanitizeName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Player";
        // El nombre que devuelve Authentication a veces incluye #hash; lo removemos para mostrarlo editable
        int idx = raw.IndexOf('#');
        if (idx >= 0) raw = raw.Substring(0, idx);
        return raw.Trim();
    }

    private int CalculateExpNeeded(int level)
    {
        // Fórmula simple lineal: base + (level-1)*incremento
        return baseExp + (level - 1) * expIncrementPerLevel;
    }

    public async void AddExperience(int amount)
    {
        if (Data == null || amount <= 0) return;
        Data.CurrentExp += amount;
        bool leveled = false;
        while (Data.CurrentExp >= Data.ExpToNextLevel)
        {
            Data.CurrentExp -= Data.ExpToNextLevel;
            Data.Level++;
            Data.SkillPoints += skillPointsPerLevel;
            Data.ExpToNextLevel = CalculateExpNeeded(Data.Level);
            leveled = true;
        }
        await SaveAsync();
        OnPlayerDataChanged?.Invoke(Data);
        if (leveled)
        {
            Debug.Log($"[PlayerDataManager] ¡Subiste a nivel {Data.Level}!");
        }
    }

    public enum StatType { Strength, Defense, Agility }

    public async void SpendSkillPoint(StatType stat)
    {
        if (Data == null || Data.SkillPoints <= 0) return;
        Data.SkillPoints--;
        switch (stat)
        {
            case StatType.Strength: Data.Strength++; break;
            case StatType.Defense: Data.Defense++; break;
            case StatType.Agility: Data.Agility++; break;
        }
        await SaveAsync();
        OnPlayerDataChanged?.Invoke(Data);
    }

    public async void UpdateLocalPlayerName(string newName)
    {
        if (Data == null) return;
        string sanitized = SanitizeName(newName);
        if (string.IsNullOrWhiteSpace(sanitized)) return;
        Data.PlayerName = sanitized;
        await SaveAsync();
        OnPlayerDataChanged?.Invoke(Data);
    }

    private async Task SaveAsync()
    {
        if (Data == null) return;
        if (_isSaving) return; // Evita overlapping saves simples
        _isSaving = true;
        try
        {
            string json = JsonUtility.ToJson(Data);
            var dict = new Dictionary<string, object> { { ProfileKey, json } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(dict);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PlayerDataManager] Error al guardar: {ex.Message}");
        }
        finally
        {
            _isSaving = false;
        }
    }
}
