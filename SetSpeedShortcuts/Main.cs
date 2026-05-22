using System.Collections.Generic;
using System.Reflection;
using ADOFAI;
using HarmonyLib;
using UnityModManagerNet;
using UnityEngine;

public static class Main {
    public static UnityModManager.ModEntry.ModLogger Logger;
    private static Harmony _harmony;
    private static Settings _settings;
    private static bool _isEnabled;

    static float _keyHoldTimer;
    static float _repeatTimer;

    public static bool _autoInsertPositionTrack = true;
    private static string _positionTrackUnitStr = "1";
    private static float _positionTrackUnit = 1f;

    private static bool _speedShortcutEnabled = true;
    private static string _bpmDeltaStr = "1";
    private static float _bpmDelta = 1f;

    private static bool _pauseShortcutEnabled = true;
    private static bool _adjustPositionTrackWithPause = true;

    public static void Setup(UnityModManager.ModEntry modEntry) {
        Logger = modEntry.Logger;

        _settings = UnityModManager.ModSettings.Load<Settings>(modEntry);

        _autoInsertPositionTrack = _settings.AutoInsertPositionTrack;
        _positionTrackUnit = _settings.PositionTrackUnit;
        _positionTrackUnitStr = _positionTrackUnit.ToString();

        _speedShortcutEnabled = _settings.SpeedShortcutEnabled;
        _bpmDelta = _settings.BpmDelta;
        _bpmDeltaStr = _bpmDelta.ToString();
        
        _pauseShortcutEnabled = _settings.PauseShortcutEnabled;
        _adjustPositionTrackWithPause = _settings.AdjustPositionWithPause;

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
        GUILayout.BeginVertical();

        bool prevAuto = _autoInsertPositionTrack;
        _autoInsertPositionTrack = GUILayout.Toggle(_autoInsertPositionTrack, "일시정지 설치 시 길 위치 자동 설정");
        if (prevAuto != _autoInsertPositionTrack) {
            _settings.AutoInsertPositionTrack = _autoInsertPositionTrack;
        }
        
        GUI.enabled = _autoInsertPositionTrack; 
        GUILayout.BeginHorizontal();
        GUILayout.Space(32);
        GUILayout.Label("길 위치 이동 단위");
        string unitInput = GUILayout.TextField(_positionTrackUnitStr, GUILayout.Width(45));
        if (unitInput != _positionTrackUnitStr) {
            _positionTrackUnitStr = unitInput;
            if (float.TryParse(unitInput, out float unitResult)) {
                _positionTrackUnit = unitResult;
                _settings.Save(modEntry);
                _settings.PositionTrackUnit = _positionTrackUnit;
            }
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUI.enabled = true;
        
        bool prevSpeed = _speedShortcutEnabled;
        _speedShortcutEnabled = GUILayout.Toggle(_speedShortcutEnabled, "속도 설정 단축키 활성화 (Alt+↑/↓, Alt+Shift+↑/↓)");
        if (prevSpeed != _speedShortcutEnabled) {
            _settings.SpeedShortcutEnabled = _speedShortcutEnabled;
        }

        GUI.enabled = _speedShortcutEnabled; 
        GUILayout.BeginHorizontal();
        GUILayout.Space(32);
        GUILayout.Label("Alt+Shift+↑/↓ BPM 변화량");
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
        GUI.enabled = true;
        
        bool prevPause = _pauseShortcutEnabled;
        _pauseShortcutEnabled = GUILayout.Toggle(_pauseShortcutEnabled, "비트 일시정지 단축키 활성화 (Ctrl+↑/↓)");
        if (prevPause != _pauseShortcutEnabled) {
            _settings.PauseShortcutEnabled = _pauseShortcutEnabled;
        }

        GUI.enabled = _pauseShortcutEnabled;
        GUILayout.BeginHorizontal();
        GUILayout.Space(32);
        bool prevAdjust = _adjustPositionTrackWithPause;
        _adjustPositionTrackWithPause = GUILayout.Toggle(_adjustPositionTrackWithPause, "비트 수에 따라 길 위치 배수 적용");
        if (prevAdjust != _adjustPositionTrackWithPause) {
            _settings.AdjustPositionWithPause = _adjustPositionTrackWithPause;
        }
        GUILayout.EndHorizontal();
        GUI.enabled = true;

        GUILayout.EndVertical();
    }
    private static void OnSaveGUI(UnityModManager.ModEntry modEntry) {
        _settings.Save(modEntry);
    }

    readonly private static MethodInfo AddEventMethod = typeof(scnEditor).GetMethod("AddEvent",
        BindingFlags.NonPublic | BindingFlags.Instance);


    private static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime) {
        if (!_isEnabled) return;

        if (_pauseShortcutEnabled) {
            if (CheckShortcut(KeyCode.UpArrow, ctrl: true)) HandlePause(1);
            if (CheckShortcut(KeyCode.DownArrow, ctrl: true)) HandlePause(-1);
        }

        if (_speedShortcutEnabled) {
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

            AddEventMethod.Invoke(editor, new object[] { id, LevelEventType.Pause });
            shouldShowPanel = true;
            
        } else {
            var data = selectedEvent.GetData();
            float currentDuration = (float) data["duration"];
            float result = currentDuration + delta;

            if (result > 0) {
                data["duration"] = result;
                shouldShowPanel = true;

                if(result >= 3 && result < 4 && delta > 0) data["countdownTicks"] = 4;
                else if(result >= 2 && result < 3 && delta < 0) data["countdownTicks"] = 0;

                if (_autoInsertPositionTrack) {
                    var nextTrackList = editor.GetFloorEvents(id + 1, LevelEventType.PositionTrack);
                    if (nextTrackList.Count > 0) {
                        editor.RemoveEvents(new List<LevelEvent> { nextTrackList[0] });
                    }
                    InsertPositionTrack(id + 1);
                }
            } else {
                editor.RemoveEvents(new List<LevelEvent> { selectedEvent });

                if (_autoInsertPositionTrack) {
                    var nextTrack = editor.GetFloorEvents(id + 1, LevelEventType.PositionTrack);
                    if(nextTrack.Count > 0)
                        editor.RemoveEvents(new List<LevelEvent> { nextTrack[0] });
                }
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
        if (id - 1 >= editor.levelData.angleData.Count) return;

        var relativeAngle = GetFloorRelativeAngle(id - 1);
        if (Mathf.Approximately((float) relativeAngle, 360f)) return;
        if (editor.GetFloorEvents(id, LevelEventType.PositionTrack).Count > 0) return;

        editor.SaveState();
        float absoluteAngle = editor.levelData.angleData[id - 1];
        float radian = absoluteAngle * Mathf.Deg2Rad;
        Vector2 baseOffset = new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));

        float finalMultiplier = 1f;
        if (_adjustPositionTrackWithPause) {
            var pauseEvents = editor.GetFloorEvents(id - 1, LevelEventType.Pause);
            if (pauseEvents != null && pauseEvents.Count > 0) {
                var pauseData = pauseEvents[0].GetData();
                if (pauseData.ContainsKey("duration")) {
                    finalMultiplier = System.Convert.ToSingle(pauseData["duration"]);
                }
            }
        }

        AddEventMethod.Invoke(editor, new object[] { id, LevelEventType.PositionTrack });

        var lastEvent = editor.events[editor.events.Count - 1];
        var data = lastEvent.GetData();
        data["positionOffset"] = baseOffset * (finalMultiplier * _positionTrackUnit);
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