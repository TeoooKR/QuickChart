using System;
using System.Collections.Generic;
using System.Reflection;
using ADOFAI;
using HarmonyLib;
using UnityModManagerNet;
using UnityEngine;
public static class Main
{
    public static UnityModManager.ModEntry.ModLogger Logger;
    private static Harmony Harmony;
    public static Settings Settings;
    public static bool IsEnabled;

    static float _keyHoldTimer;
    static float _repeatTimer;

    public static string BpmDeltaStr = "1";
    public static float BpmDelta = 1f;

    public static void Setup(UnityModManager.ModEntry modEntry)
    {
        modEntry.Logger.Log("Setup!");
        Logger = modEntry.Logger;
    
        modEntry.OnToggle = OnToggle;
        modEntry.OnGUI = OnGUI;
        modEntry.OnSaveGUI = OnSaveGUI;
        Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
        modEntry.OnUpdate = OnUpdate; 
    }
    private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value) {
        IsEnabled = value;
        if (value) {
            Harmony = new Harmony(modEntry.Info.Id);
            Harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
        else { 
            Harmony.UnpatchAll(modEntry.Info.Id);
        }
        return true;
    }
    private static readonly MethodInfo AddEventMethod = typeof(scnEditor).GetMethod("AddEvent", 
        BindingFlags.NonPublic | BindingFlags.Instance);
    
    private static void OnGUI(UnityModManager.ModEntry modEntry) {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Alt+Shift+↑/↓ BPM 변화량");
        GUILayout.Space(8);
        string input = GUILayout.TextField(BpmDeltaStr, GUILayout.Width(32));
        GUILayout.FlexibleSpace();
        if (input != BpmDeltaStr) {
            if (float.TryParse(input, out float result)) {
                if (result < 0) result = 0;
                BpmDelta = result;
                BpmDeltaStr = input;
            }
                
            else if (string.IsNullOrEmpty(input)) {
                BpmDeltaStr = "";
                BpmDelta = 0;
            }
        }
        GUILayout.EndHorizontal();

    }
    private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
    {
        Settings.Save(modEntry);
    }
    private static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime) {
        if (!IsEnabled) return;

        if (CheckShortcut(KeyCode.UpArrow, ctrl: true)) HandlePause(1);
        if (CheckShortcut(KeyCode.DownArrow, ctrl: true)) HandlePause(-1);

        if (CheckShortcut(KeyCode.UpArrow, alt: true)) HandleSetSpeed(2.0f, true);
        if (CheckShortcut(KeyCode.DownArrow, alt: true)) HandleSetSpeed(0.5f, true);

        bool up = CheckShortcut(KeyCode.UpArrow, alt: true, shift: true, useKeyDown: false);
        bool down = CheckShortcut(KeyCode.DownArrow, alt: true, shift: true, useKeyDown: false);

        if (up || down) {
            float delta = up ? BpmDelta : -BpmDelta;
        
            if (_keyHoldTimer == 0f) {
                HandleSetSpeed(delta, false);
                _keyHoldTimer += deltaTime;
            } else {
                _keyHoldTimer += deltaTime;
                if (_keyHoldTimer > 0.4f) {
                    _repeatTimer += deltaTime;
                    if (_repeatTimer > 0.05f) {
                        HandleSetSpeed(delta, false);
                        _repeatTimer = 0f;
                    }
                }
            }
        } else {
            _keyHoldTimer = 0f;
            _repeatTimer = 0f;
        }
    }
    
    private static bool CheckShortcut(KeyCode key, bool ctrl = false, bool alt = false, bool shift = false, bool useKeyDown = true) {
        bool keyCheck = useKeyDown ? Input.GetKeyDown(key) : Input.GetKey(key);
        if (!keyCheck) return false;

        bool isCtrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool isAltPressed = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        bool isShiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        return isCtrlPressed == ctrl && isAltPressed == alt && isShiftPressed == shift;
    }

    private static void RemoveTrashUndos() {
        var editor = scnEditor.instance;

        if (editor.undoStates.Count >= 2) {
            editor.undoStates.RemoveRange(editor.undoStates.Count - 2, 2);
        }
        else if (editor.undoStates.Count > 0) {
            editor.undoStates.RemoveAt(editor.undoStates.Count - 1);
        }
    }
    
    private static void HandlePause(int delta) {
        var editor = scnEditor.instance;
        if (!editor.SelectionIsSingle()) return;
    
        editor.SaveState(); 
        int id = editor.selectedFloors[0].seqID;
        var selectedEvent = editor.GetSelectedFloorEvents(LevelEventType.Pause)?.Find(e => true);
        bool shouldShow;

        if (selectedEvent == null) {
            if (delta < 0) return;
        
            AddEventMethod.Invoke(editor, new object[] { id, LevelEventType.Pause });
            shouldShow = true;
        
            if (editor.GetFloorEvents(id + 1, LevelEventType.PositionTrack).Count == 0)
                AddMoveTrackToNextTile();
        } 
        else {
            var data = selectedEvent.GetData();
            float result = (float)data["duration"] + delta;

            if (result > 0) {
                data["duration"] = result;
                shouldShow = true;
            
                if (result >= 3 && result < 4 && delta > 0) data["countdownTicks"] = 4;
                else if (result >= 2 && result < 3 && delta < 0) data["countdownTicks"] = 0;
            } 
            else {
                editor.RemoveEvents(new List<LevelEvent> { selectedEvent });
                var nextTrack = editor.GetFloorEvents(id + 1, LevelEventType.PositionTrack);
                if (nextTrack.Count > 0) editor.RemoveEvents(new List<LevelEvent> { nextTrack[0] });
                shouldShow = false;
            }
        }

        editor.ApplyEventsToFloors();
        editor.levelEventsPanel.ShowTabsForFloor(id);
        if (shouldShow) editor.levelEventsPanel.ShowPanel(LevelEventType.Pause);

        RemoveTrashUndos();
    }
    private static void AddMoveTrackToNextTile() {
        var editor = scnEditor.instance;
        if (!editor.SelectionIsSingle()) return;
        
        var selectedFloor = editor.selectedFloors[0];
        int id = selectedFloor.seqID;
        if (id >= editor.levelData.angleData.Count) return;

        var relativeAngle = GetFloorRelativeAngle(id);
        if (Mathf.Approximately((float)relativeAngle, 360f)) return;
        
        editor.SaveState(); 
        float absoluteAngle = editor.levelData.angleData[id];
        float radian = absoluteAngle * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
        
        AddEventMethod.Invoke(editor, new object[] { id + 1, LevelEventType.PositionTrack });
        var lastEvent = editor.events[editor.events.Count - 1];
        
        var data = lastEvent.GetData();
        data["positionOffset"] = offset;
        lastEvent.disabled["positionOffset"] = false;

        editor.ApplyEventsToFloors();
        
    }  
    
    public static double GetFloorRelativeAngle(int floorIndex) {
        var editor = ADOBase.editor;
    
        if (editor == null || floorIndex < 0 || floorIndex >= editor.floors.Count - 1)
        {
            return 0;
        }

        ADOBase.lm.CalculateFloorAngleLengths();

        var floor = editor.floors[floorIndex];
    
        double angle = floor.angleLength * Mathf.Rad2Deg;

        return angle;
    }
    private static void HandleSetSpeed(float value, bool calculateByMultiplier) {
        var editor = scnEditor.instance;
        if (!editor.SelectionIsSingle()) return;

        editor.SaveState();
        int id = editor.selectedFloors[0].seqID;
        var selectedEvent = editor.GetSelectedFloorEvents(LevelEventType.SetSpeed)?.Find(e => true);
        
        float prevTileSpeed = (id > 0) ? editor.floors[id - 1].speed : 1f;
        float prevBpm = editor.levelData.bpm * prevTileSpeed;
        bool shouldShow;

        if (selectedEvent == null) {
            AddEventMethod.Invoke(editor, new object[] { id, LevelEventType.SetSpeed });

            var lastEvent = editor.events[editor.events.Count - 1];
            var data = lastEvent.GetData();

            if (calculateByMultiplier) {
                data["speedType"] = SpeedType.Multiplier;
                data["bpmMultiplier"] = value;
            } else {
                data["beatsPerMinute"] = Mathf.Max(0.1f, prevBpm + value);
            }
            shouldShow = true;
        } else {
            var data = selectedEvent.GetData();
            var currentType = data["speedType"]; 
            bool isBpmMode = currentType.ToString() == "Bpm" || currentType.ToString() == "0";

            if (calculateByMultiplier) {
                string targetKey = isBpmMode ? "beatsPerMinute" : "bpmMultiplier";
                float currentVal = System.Convert.ToSingle(data[targetKey]);
                float nextVal = currentVal * value;
    
                if (nextVal > 0f) {
                    data[targetKey] = nextVal;
                }
            } else {
                float currentBpm = isBpmMode ? System.Convert.ToSingle(data["beatsPerMinute"]) : prevBpm * System.Convert.ToSingle(data["bpmMultiplier"]);
                
                decimal preciseBpm = (decimal)currentBpm + (decimal)value;

                if (preciseBpm > 0m) {
                    data["speedType"] = SpeedType.Bpm; 
                    data["beatsPerMinute"] = (float)preciseBpm;
                }
            }

            bool nowBpmMode = data["speedType"].ToString() == "Bpm" || data["speedType"].ToString() == "0";
            float finalSpeed = nowBpmMode ? System.Convert.ToSingle(data["beatsPerMinute"]) : prevBpm * System.Convert.ToSingle(data["bpmMultiplier"]);

            if (Mathf.Approximately(finalSpeed, prevBpm)) {
                editor.RemoveEvents(new List<LevelEvent> { selectedEvent });
                shouldShow = false;
            } else {
                shouldShow = true;
            }
            
        }

        editor.ApplyEventsToFloors();
        editor.levelEventsPanel.ShowTabsForFloor(id);
        if (shouldShow) {
            var targetEvent = selectedEvent ?? editor.events[editor.events.Count - 1];
            editor.levelEventsPanel.ShowPanel(LevelEventType.SetSpeed);
            editor.levelEventsPanel.UpdatePropertyText(targetEvent, "beatsPerMinute");
            editor.levelEventsPanel.UpdatePropertyText(targetEvent, "bpmMultiplier");
            editor.levelEventsPanel.UpdatePropertyText(targetEvent, "speedType");
        }
        
        RemoveTrashUndos();
    }
}
