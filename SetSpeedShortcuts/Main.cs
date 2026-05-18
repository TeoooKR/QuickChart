using System.Collections.Generic;
using System.Reflection;
using ADOFAI;
using HarmonyLib;
using MobileMenu;
using UnityModManagerNet;
using UnityEngine;

public static class Main {
    public static UnityModManager.ModEntry.ModLogger Logger;
    private static Harmony _harmony;
    private static Settings _settings;
    private static bool _isEnabled;

    static float _keyHoldTimer;
    static float _repeatTimer;

    private static string _bpmDeltaStr = "1";
    private static float _bpmDelta = 1f;
    

    public static void Setup(UnityModManager.ModEntry modEntry) {
        Logger = modEntry.Logger;

        _settings = UnityModManager.ModSettings.Load<Settings>(modEntry);

        _bpmDelta = _settings.BpmDelta;
        _bpmDeltaStr = _bpmDelta.ToString();

        modEntry.OnToggle = OnToggle;
        modEntry.OnGUI = OnGUI;
        modEntry.OnSaveGUI = OnSaveGUI;
        modEntry.OnUpdate = OnUpdate;
    }

    private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value) {
        _isEnabled = value;
        if (value) {
            _harmony = new Harmony(modEntry.Info.Id);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        } else {
            _harmony.UnpatchAll(modEntry.Info.Id);
        }
        return true;
    }

    private static void OnGUI(UnityModManager.ModEntry modEntry) {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Alt+Shift+↑/↓ BPM 변화량");
        GUILayout.Space(8);

        string input = GUILayout.TextField(_bpmDeltaStr, GUILayout.Width(32));

        if (input != _bpmDeltaStr) {
            _bpmDeltaStr = input;
            if (float.TryParse(input, out float result)) {
                if (result < 0) result = 0;
                _bpmDelta = result;
                _settings.BpmDelta = _bpmDelta;
            } else if (string.IsNullOrEmpty(input)) {
                _bpmDelta = 0;
                _settings.BpmDelta = 0;
            }
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private static void OnSaveGUI(UnityModManager.ModEntry modEntry) {
        _settings.Save(modEntry);
    }

    readonly private static MethodInfo AddEventMethod = typeof(scnEditor).GetMethod("AddEvent",
        BindingFlags.NonPublic | BindingFlags.Instance);


    private static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime) {
        if (!_isEnabled) return;

        if (CheckShortcut(KeyCode.F4)) {
        }

        if (CheckShortcut(KeyCode.UpArrow, ctrl: true)) HandlePause(1);
        if (CheckShortcut(KeyCode.DownArrow, ctrl: true)) HandlePause(-1);

        if (CheckShortcut(KeyCode.UpArrow, alt: true)) HandleSetSpeed(2.0f, true);
        if (CheckShortcut(KeyCode.DownArrow, alt: true)) HandleSetSpeed(0.5f, true);

        bool up = CheckShortcut(KeyCode.UpArrow, alt: true, shift: true, useKeyDown: false);
        bool down = CheckShortcut(KeyCode.DownArrow, alt: true, shift: true, useKeyDown: false);

        if (up || down) {
            float delta = up ? _bpmDelta : -_bpmDelta;

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
        if(!keyCheck) return false;

        bool isCtrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool isAltPressed = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        bool isShiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        return isCtrlPressed == ctrl && isAltPressed == alt && isShiftPressed == shift;
    }

    private static void RemoveTrashUndos() {
        var editor = scnEditor.instance;

        if(editor.undoStates.Count >= 2) {
            editor.undoStates.RemoveRange(editor.undoStates.Count - 2, 2);
        } else if(editor.undoStates.Count > 0) {
            editor.undoStates.RemoveAt(editor.undoStates.Count - 1);
        }
    }

    private static void HandlePause(int delta) {
        var editor = scnEditor.instance;
        if(!editor.SelectionIsSingle()) return;

        editor.SaveState();
        int id = editor.selectedFloors[0].seqID;
        var selectedEvent = editor.GetSelectedFloorEvents(LevelEventType.Pause)?.Find(e => true);
        bool shouldShowPanel;

        if(selectedEvent == null) {
            if(delta < 0) return;

            AddEventMethod.Invoke(editor, new object[] {
                id, LevelEventType.Pause
            });
            shouldShowPanel = true;
        } else {
            var data = selectedEvent.GetData();
            float result = (float) data["duration"] + delta;

            if(result > 0) {
                data["duration"] = result;
                shouldShowPanel = true;

                if(result >= 3 && result < 4 && delta > 0) data["countdownTicks"] = 4;
                else if(result >= 2 && result < 3 && delta < 0) data["countdownTicks"] = 0;
            } else {
                editor.RemoveEvents(new List<LevelEvent> {
                    selectedEvent
                });

                var nextTrack = editor.GetFloorEvents(id + 1, LevelEventType.PositionTrack);
                if(nextTrack.Count > 0)
                    editor.RemoveEvents(new List<LevelEvent> {
                        nextTrack[0]
                    }); // remove position
                shouldShowPanel = false;
            }
        }

        editor.ApplyEventsToFloors();
        editor.levelEventsPanel.ShowTabsForFloor(id);
        if(shouldShowPanel) editor.levelEventsPanel.ShowPanel(LevelEventType.Pause);

        RemoveTrashUndos();
    }
    public static void InsertPositionTrack(int id) {
        var editor = scnEditor.instance;
        if (!editor.SelectionIsSingle()) return;

        Logger.Log(id.ToString());
        if (id - 1 >= editor.levelData.angleData.Count) return; // return if id is more than last tile

        var relativeAngle = GetFloorRelativeAngle(id - 1);
        if (Mathf.Approximately((float) relativeAngle, 360f)) return;

        if (editor.GetFloorEvents(id, LevelEventType.PositionTrack).Count > 0) return; // if there is position track already

        editor.SaveState();
        float absoluteAngle = editor.levelData.angleData[id - 1];
        float radian = absoluteAngle * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));

        AddEventMethod.Invoke(editor, new object[] {
            id, LevelEventType.PositionTrack
        });

        var lastEvent = editor.events[editor.events.Count - 1];
        var data = lastEvent.GetData();
        data["positionOffset"] = offset;
        lastEvent.disabled["positionOffset"] = false;

        editor.ApplyEventsToFloors();
    }

    private static double GetFloorRelativeAngle(int floorIndex) {
        var editor = ADOBase.editor;

        if (editor == null || floorIndex < 0 || floorIndex >= editor.floors.Count - 1) {
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
        bool shouldShowPanel;

        if (selectedEvent == null) {
            AddEventMethod.Invoke(editor, new object[] {
                id, LevelEventType.SetSpeed
            });

            var lastEvent = editor.events[editor.events.Count - 1];
            var data = lastEvent.GetData();

            if (calculateByMultiplier) {
                data["speedType"] = SpeedType.Multiplier;
                data["bpmMultiplier"] = value;
            } else {
                data["beatsPerMinute"] = Mathf.Max(0.1f, prevBpm + value);
            }
            shouldShowPanel = true;
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

                decimal preciseBpm = (decimal) currentBpm + (decimal) value;

                if (preciseBpm > 0m) {
                    data["speedType"] = SpeedType.Bpm;
                    data["beatsPerMinute"] = (float) preciseBpm;
                }
            }

            bool nowBpmMode = data["speedType"].ToString() == "Bpm" || data["speedType"].ToString() == "0";
            float finalSpeed = nowBpmMode ? System.Convert.ToSingle(data["beatsPerMinute"]) : prevBpm * System.Convert.ToSingle(data["bpmMultiplier"]);

            if (Mathf.Approximately(finalSpeed, prevBpm)) {
                editor.RemoveEvents(new List<LevelEvent> {
                    selectedEvent
                });
                shouldShowPanel = false;
            } else {
                shouldShowPanel = true;
            }

        }

        editor.ApplyEventsToFloors();
        editor.levelEventsPanel.ShowTabsForFloor(id);
        if (shouldShowPanel) {
            var targetEvent = selectedEvent ?? editor.events[editor.events.Count - 1];
            editor.levelEventsPanel.ShowPanel(LevelEventType.SetSpeed);
            editor.levelEventsPanel.UpdatePropertyText(targetEvent, "beatsPerMinute");
            editor.levelEventsPanel.UpdatePropertyText(targetEvent, "bpmMultiplier");
            editor.levelEventsPanel.UpdatePropertyText(targetEvent, "speedType");
        }

        RemoveTrashUndos();
    }
}