using UnityModManagerNet;

namespace QuickChart {
    public class Settings : UnityModManager.ModSettings {
        public bool IsKorean = true;
        
        public bool AutoInsertPositionTrack = true;
            public float PositionTrackUnit = 1f;

        public bool AutoInsertMoveTrack = true;
            public int EaseModeIndex = 0;
            public int EaseFunctionIndex = 9;
        
        public bool SwapShortcuts = false;
        
        public bool SpeedShortcutEnabled = true;
            public float BpmDelta = 1f;

        public bool PauseShortcutEnabled = true;
            public bool AdjustPositionWithPause = false;
            public bool AutoSetCountdownTicks = true;

        public bool AllowBackwardPaste = true;
        public bool DisableMovePageShortcuts = false;
        
        public override void Save(UnityModManager.ModEntry modEntry) {
            Save(this, modEntry);
        }
    }
}