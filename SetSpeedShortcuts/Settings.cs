using UnityModManagerNet;

public class Settings : UnityModManager.ModSettings
{
    public float BpmDelta = 1f;
    public override void Save(UnityModManager.ModEntry modEntry)
    {
        Save(this, modEntry);
    }
}