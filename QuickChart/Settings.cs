using UnityModManagerNet;

namespace QuickChart {
    public class Settings : UnityModManager.ModSettings {
        public string Language = "ko";
        
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
            public bool InsertColorTrack = false;

        public bool AllowBackwardPaste = true;
        public bool DisableMovePageShortcuts = false;
        
        public bool AutoInsertTwirl = false;
        
        public string ChangeAngleStartTile = "";
        public string ChangeAngleEndTile = "";
        public string ChangeAngleFind = "30";
        public string ChangeAngleReplace = "22.5";
        public bool MaintainTimingWithSpeed = false;
        
        public string PseudoMidspinCount = "2";
        public string PseudoMidspinStep = "1";
        public string PseudoMidspinOffset = "1";
        
        public override void Save(UnityModManager.ModEntry modEntry) {
            Save(this, modEntry);
        }
    }
}