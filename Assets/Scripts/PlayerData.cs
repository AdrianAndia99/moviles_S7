using System;

// Datos persistentes del jugador que se guardan en Cloud Save en un único JSON
[Serializable]
public class PlayerData
{
    public string PlayerName; // Nombre visible (sin el tag #1234 si usas el de Authentication)
    public int Level;
    public int CurrentExp;
    public int ExpToNextLevel;
    public int SkillPoints;

    // Estadísticas básicas que el jugador puede mejorar
    public int Strength;
    public int Defense;
    public int Agility;

    // Para control de versiones del modelo de datos (por si cambias el schema luego)
    public int Version = 1;
}
