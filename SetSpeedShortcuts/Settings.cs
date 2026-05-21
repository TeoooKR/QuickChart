using UnityModManagerNet;

public class Settings : UnityModManager.ModSettings {
    public bool AutoInsertPositionTrack = true;
    public float PositionTrackUnit = 1f; 
    public bool SpeedShortcutEnabled = true;
    public bool PauseShortcutEnabled = true;
    public bool AdjustPositionWithPause = true;
    public float BpmDelta = 1f;
    
    public override void Save(UnityModManager.ModEntry modEntry)
    {
        Save(this, modEntry);
    }
}