using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using ADOFAI;
using ADOFAI.Editor;
using ADOFAI.Editor.Actions;
using HarmonyLib;
using UnityModManagerNet;
using UnityEngine;

namespace QuickChart {
    public static class Main {
        public static UnityModManager.ModEntry.ModLogger Logger;
        private static Harmony _harmony;
        private static Settings _settings;
        private static bool _isEnabled;

        static float _keyHoldTimer;
        static float _repeatTimer;
        static int _lastBpmDirection;
        
        private static bool _isKorean = true;
        private static bool _swapShortcuts;
        private static string _legacyPauseResultStr = "";

        readonly private static MethodInfo AddEventMethod = typeof(scnEditor).GetMethod("AddEvent",
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        readonly private static MethodInfo RegisterKeybindsMethod = typeof(scnEditor).GetMethod("RegisterKeybinds",
            BindingFlags.NonPublic | BindingFlags.Instance);

        
        public static bool _autoInsertPositionTrack = true;
            private static string _positionTrackUnitStr = "1";
            private static float _positionTrackUnit = 1f;
        
        public static bool _autoInsertMoveTrack = true;
            private static int _easeModeIdx;
            private static int _easeFuncIdx;
                readonly private static string[] _easingModes = { "In", "Out", "In-Out" };
                readonly private static string[] _easingModesFlash = { "-", "In", "Out", "In-Out" };
                readonly private static string[] _easingFunctions = { "Linear", "Sine", "Quad", "Cubic", "Quart", "Quint", "Expo", "Circ", "Elastic", "Back", "Bounce", "Flash" };
                
        private static bool _speedShortcutEnabled = true;
            private static string _bpmDeltaStr = "1";
            private static float _bpmDelta = 1f;
        
        private static bool _pauseShortcutEnabled = true;
            private static bool _adjustPositionTrackWithPause = true;
            private static bool _autoSetCountdownTicks = true;
            
        public static bool _allowBackwardPaste = true;
        public static bool _disableMovePageShortcuts = true;

        

        public static void Setup(UnityModManager.ModEntry modEntry) {
            Logger = modEntry.Logger;
            _settings = UnityModManager.ModSettings.Load<Settings>(modEntry);

            _isKorean = _settings.IsKorean;
            _swapShortcuts = _settings.SwapShortcuts;

            _autoInsertPositionTrack = _settings.AutoInsertPositionTrack;
                _positionTrackUnit = _settings.PositionTrackUnit;
                _positionTrackUnitStr = _positionTrackUnit.ToString(CultureInfo.InvariantCulture);

            _autoInsertMoveTrack = _settings.AutoInsertMoveTrack;
                _easeFuncIdx = _settings.EaseFunctionIndex;
                _easeModeIdx = _settings.EaseModeIndex;
                
            _speedShortcutEnabled = _settings.SpeedShortcutEnabled;
                _bpmDelta = _settings.BpmDelta;
                _bpmDeltaStr = _bpmDelta.ToString(CultureInfo.InvariantCulture);
            
            _pauseShortcutEnabled = _settings.PauseShortcutEnabled;
                _adjustPositionTrackWithPause = _settings.AdjustPositionWithPause;
                _autoSetCountdownTicks = _settings.AutoSetCountdownTicks;

            _allowBackwardPaste = _settings.AllowBackwardPaste;
            _disableMovePageShortcuts = _settings.DisableMovePageShortcuts;
            
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

        private static string GetTranslation(string kr, string en) {
            return _isKorean ? kr : en;
        }
        
        private static void OnGUI(UnityModManager.ModEntry modEntry) {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Language / 언어 설정:", GUILayout.ExpandWidth(false));
            GUILayout.Space(10f);
            int langIdx = _isKorean ? 0 : 1;
            int nextLangIdx = GUILayout.Toolbar(langIdx, new[] { "한국어", "English" }, GUILayout.Width(200f));
            if (langIdx != nextLangIdx) {
                _isKorean = (nextLangIdx == 0);
                _settings.IsKorean = _isKorean;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            bool prevSwap = _swapShortcuts;
            _swapShortcuts = GUILayout.Toggle(_swapShortcuts, GetTranslation("Ctrl, Alt 단축키 반전", "Swap Ctrl and Alt"));
            if (prevSwap != _swapShortcuts) _settings.SwapShortcuts = _swapShortcuts;

            GUILayout.Space(10f);

            bool prevAutoPos = _autoInsertPositionTrack;
            _autoInsertPositionTrack = GUILayout.Toggle(_autoInsertPositionTrack, GetTranslation("일시정지 설치 시 길 위치 자동 설치", "Auto-insert Position Track on Pause"));
            if (prevAutoPos != _autoInsertPositionTrack) _settings.AutoInsertPositionTrack = _autoInsertPositionTrack;
                    GUI.enabled = _autoInsertPositionTrack; 
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(32);
                    GUILayout.Label(GetTranslation("길 위치 이동 단위", "Position Track unit"));
                    string unitInput = GUILayout.TextField(_positionTrackUnitStr, GUILayout.Width(45));
                    if (unitInput != _positionTrackUnitStr) {
                        _positionTrackUnitStr = unitInput;
                        if (float.TryParse(unitInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float unitResult)) {
                            _positionTrackUnit = unitResult;
                            _settings.PositionTrackUnit = _positionTrackUnit;
                        }
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUI.enabled = true;
                    
                    
            bool prevAutoMove = _autoInsertMoveTrack;
            _autoInsertMoveTrack = GUILayout.Toggle(_autoInsertMoveTrack, GetTranslation("일시정지 설치 시 길 이동 자동 설치", "Auto-insert Move Track on Pause"));
            if (prevAutoMove != _autoInsertMoveTrack) _settings.AutoInsertMoveTrack = _autoInsertMoveTrack;
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(32);
                    GUI.enabled = _autoInsertMoveTrack;
                    GUILayout.Label(GetTranslation("가감속", "Easing"));
                    GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(32);
                            GUILayout.BeginVertical("box", GUILayout.Width(400));
                            string currentFunc = _easingFunctions[_easeFuncIdx];
                            bool isLinear = currentFunc == "Linear";
                            bool isFlash = currentFunc == "Flash";
                            GUI.enabled = _autoInsertMoveTrack && !isLinear;
                            string[] currentModes = isFlash ? _easingModesFlash : _easingModes;
                            if (_easeModeIdx >= currentModes.Length) _easeModeIdx = currentModes.Length - 1;
                            GUILayout.Label(GetTranslation("종류", "Mode"));
                            int nextModeIdx = GUILayout.Toolbar(_easeModeIdx, currentModes);
                            if (nextModeIdx != _easeModeIdx) {
                                _easeModeIdx = nextModeIdx;
                                _settings.EaseModeIndex = _easeModeIdx;
                            }
                            GUI.enabled = _autoInsertMoveTrack;
                            
                            GUILayout.Space(15);
                            GUILayout.Label(GetTranslation("함수", "Function"));
                            int nextFuncIdx = GUILayout.SelectionGrid(_easeFuncIdx, _easingFunctions, 4);
                            if (nextFuncIdx != _easeFuncIdx) {
                                _easeFuncIdx = nextFuncIdx;
                                _settings.EaseFunctionIndex = _easeFuncIdx;
                            }
                            GUILayout.EndVertical();
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                            GUI.enabled = true;
                            
                            
            bool prevSpeed = _speedShortcutEnabled;
            string speedShortcutStr = _swapShortcuts ? "(Ctrl+↑/↓, Ctrl+Shift+↑/↓)" : "(Alt+↑/↓, Alt+Shift+↑/↓)";
            _speedShortcutEnabled = GUILayout.Toggle(_speedShortcutEnabled, GetTranslation("속도 설정 단축키 활성화 ", "Enable Set Speed Shortcut ") + speedShortcutStr);
            if (prevSpeed != _speedShortcutEnabled) _settings.SpeedShortcutEnabled = _speedShortcutEnabled;
                    GUI.enabled = _speedShortcutEnabled; 
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(32);
                    string bpmShortcutStr = _swapShortcuts ? "Ctrl+Shift+↑/↓ " : "Alt+Shift+↑/↓ ";
                    GUILayout.Label(bpmShortcutStr + GetTranslation("BPM 변화량", "BPM Change Amount"));
                    string input = GUILayout.TextField(_bpmDeltaStr, GUILayout.Width(32));
                    if (input != _bpmDeltaStr) {
                        _bpmDeltaStr = input;
                        if (float.TryParse(input,NumberStyles.Float, CultureInfo.InvariantCulture, out float result)) {
                            if (result < 0) result = 0;
                            _bpmDelta = result;
                            _settings.BpmDelta = _bpmDelta;
                        }
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUI.enabled = true;
            
                    
            bool prevPause = _pauseShortcutEnabled;
            string pauseShortcutStr = _swapShortcuts ? "(Alt+↑/↓)" : "(Ctrl+↑/↓)";
            _pauseShortcutEnabled = GUILayout.Toggle(_pauseShortcutEnabled, GetTranslation("비트 일시정지 단축키 활성화", "Enable Pause Shortcut") + pauseShortcutStr);
            if (prevPause != _pauseShortcutEnabled) _settings.PauseShortcutEnabled = _pauseShortcutEnabled;
                    GUI.enabled = _pauseShortcutEnabled;
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(32);
                    bool prevAdjust = _adjustPositionTrackWithPause;
                    _adjustPositionTrackWithPause = GUILayout.Toggle(_adjustPositionTrackWithPause, GetTranslation("비트 수에 따라 길 위치 배수 적용", "Scale Position offset with Pause duration"));
                    if (prevAdjust != _adjustPositionTrackWithPause) _settings.AdjustPositionWithPause = _adjustPositionTrackWithPause;
                    GUILayout.EndHorizontal();
                    
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(32);
                    bool prevAutoTick = _autoSetCountdownTicks;
                    _autoSetCountdownTicks = GUILayout.Toggle(_autoSetCountdownTicks, GetTranslation("카운트다운 틱 자동 설정", "Auto-set Countdown Ticks"));
                    if (prevAutoTick != _autoSetCountdownTicks) _settings.AutoSetCountdownTicks = _autoSetCountdownTicks;
                    GUILayout.EndHorizontal();
                    GUI.enabled = true;
                    GUILayout.EndVertical();
                    
                    
            bool prevAllowBackward = _allowBackwardPaste;
            _allowBackwardPaste = GUILayout.Toggle(_allowBackwardPaste, GetTranslation("역방향 타일 붙여넣기 허용", "Allow Paste Backward Tiles"));
            if (prevAllowBackward != _allowBackwardPaste) _settings.AllowBackwardPaste = _allowBackwardPaste;
    
            bool prevDisableMovePage = _disableMovePageShortcuts;
            _disableMovePageShortcuts = GUILayout.Toggle(_disableMovePageShortcuts, GetTranslation("대괄호([, ]) 페이지 이동 단축키 비활성화", "Disable Move Page Shortcuts ([, ])"));
            if (prevDisableMovePage != _disableMovePageShortcuts) {
                _settings.DisableMovePageShortcuts = _disableMovePageShortcuts;

                var keybindManagerField = AccessTools.Field(typeof(scnEditor), "keybindManager");
                var keybindManager = keybindManagerField.GetValue(ADOBase.editor) as EditorKeybindManager;

                SetMovePageShortcuts(keybindManager, !_disableMovePageShortcuts);
            }
                    
                    
            GUILayout.BeginHorizontal();
            GUILayout.Space(16);
            GUILayout.Label(GetTranslation("<b>레거시 일시정지 최신화</b>", "<b>Convert Legacy Pause</b>"));
            GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(32);
                    GUILayout.Label("<color=#888888><size=12>" +
                                    GetTranslation(
                                        "v3.0.0 업데이트로 유턴 타일에서의 일시정지의 비트 수를 기존과 같은 박자를 유지하려면 +1 해야 합니다." +
                                        "\nlegacyPause 옵션이 추가되었는데, legacyPause가 켜져있다면 예전 방식대로, 꺼져 있다면 새 로직처럼 작동합니다. 아래에서 현재 레벨의 legacyPause 여부를 확인할 수 있습니다." +
                                        "\n" +
                                        "\n버튼 기능" +
                                        "\n  - ↑: 유턴 타일에 있는 일시정지 비트 수를 1 증가시킵니다." +
                                        "\n  - ↓: 유턴 타일에 있는 일시정지 비트 수를 1 감소시킵니다. (클릭 실수 시 복구용)" +
                                        "\n  - 화살표 버튼을 클릭하여 값을 수정하면 legacyPause 옵션은 자동으로 꺼집니다." +
                                        "\n" +
                                        "\nNote: legacyPause를 켜면 굳이 바꿀 필요가 없지만, 가끔 게임이 legacyPause를 끄는 현상이 있어서 만들었습니다.",
                                        
                                        "After the v3.0.0 update, you need to add +1 to the duration of pause which is on U-Turn tiles to keep the same timing as before." +
                                        "\nA legacyPause option has been added. if it's on, it works the old way, if it's off, it works the new way. You can check the status of legacyPause below." +
                                        "\n" +
                                        "\nButtons" +
                                        "\n  - ↑ (+1): Increases the pause duration on the U-Turn tile." +
                                        "\n  - ↓ (-1): Decreases the pause duration on the U-Turn tile. (Use this if you mistake)" +
                                        "\n" +
                                        "\nChanging the value with the arrow buttons will automatically turn off legacyPause." +
                                        "\n" +
                                        "\nNote: If you keep legacyPause on, you don't really need to change. However, this tool was made because the game sometimes turns off legacyPause."
                                        ) + "</size></color>");
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(32);
                    if (!ADOBase.isEditingLevel) {
                        GUILayout.Label(GetTranslation("현재 레벨 에디터에 있지 않습니다.", "Not currently in the level editor."));
                    } else {
                        GUILayout.BeginVertical();
                        if (ADOBase.editor.levelData.legacyPause) {
                            GUILayout.Label(GetTranslation(
                                "legacyPause: 켜짐\n버튼을 클릭하면 '꺼짐'으로 변경됩니다.", 
                                "legacyPause: on\nThis turns off when clicking the buttons."
                            ));
                        } else {
                            GUILayout.Label(GetTranslation("legacyPause: 꺼짐 (최신)", "legacyPause: off (Latest)"));
                        }
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("↑", GUILayout.Width(64), GUILayout.Height(32))) {
                            ConvertLegacyPause(false);
                        }
                        if (GUILayout.Button("↓", GUILayout.Width(64), GUILayout.Height(32))) {
                            ConvertLegacyPause(true);
                        }
                        GUILayout.EndHorizontal();
                        if (!string.IsNullOrEmpty(_legacyPauseResultStr)) {
                            GUILayout.Label(_legacyPauseResultStr);
                        }
                        GUILayout.EndVertical(); 
                    }
                    GUILayout.EndHorizontal(); 
        }
        
        public static void SetMovePageShortcuts(EditorKeybindManager manager, bool register) {
            if (register) {
                manager.RegisterKeybind(new EditorKeybind(KeyModifier.None, KeyCode.LeftBracket), (EditorAction) new ShowPreviousEventPageEditorAction());
                manager.RegisterKeybind(new EditorKeybind(KeyModifier.None, KeyCode.RightBracket), (EditorAction) new ShowNextEventPageEditorAction());
                manager.RegisterKeybind(new EditorKeybind(KeyModifier.Shift, KeyCode.LeftBracket), (EditorAction) new ShowFirstEventPageEditorAction());
                manager.RegisterKeybind(new EditorKeybind(KeyModifier.Shift, KeyCode.RightBracket), (EditorAction) new ShowLastEventPageEditorAction());
            } else {
                manager.UnregisterKeybind(new EditorKeybind(KeyModifier.None, KeyCode.LeftBracket));
                manager.UnregisterKeybind(new EditorKeybind(KeyModifier.None, KeyCode.RightBracket));
                manager.UnregisterKeybind(new EditorKeybind(KeyModifier.Shift, KeyCode.LeftBracket));
                manager.UnregisterKeybind(new EditorKeybind(KeyModifier.Shift, KeyCode.RightBracket));
            }
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry) {
            _settings.Save(modEntry);
        }
        
        private static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime) {
            if (!_isEnabled) return;

            bool pauseCtrl = !_swapShortcuts;
            bool pauseAlt = _swapShortcuts;
            bool speedCtrl = _swapShortcuts;
            bool speedAlt = !_swapShortcuts;

            if (_pauseShortcutEnabled) {
                if (CheckShortcut(KeyCode.UpArrow, ctrl: pauseCtrl, alt: pauseAlt)) HandlePause(1);
                if (CheckShortcut(KeyCode.DownArrow, ctrl: pauseCtrl, alt: pauseAlt)) HandlePause(-1);
            }
            if (_speedShortcutEnabled) {
                if (CheckShortcut(KeyCode.UpArrow, ctrl: speedCtrl, alt: speedAlt)) HandleSetSpeed(2.0f, true);
                if (CheckShortcut(KeyCode.DownArrow, ctrl: speedCtrl, alt: speedAlt)) HandleSetSpeed(0.5f, true);

                bool up = CheckShortcut(KeyCode.UpArrow, ctrl: speedCtrl, alt: speedAlt, shift: true, useKeyDown: false);
                bool down = CheckShortcut(KeyCode.DownArrow, ctrl: speedCtrl, alt: speedAlt, shift: true, useKeyDown: false);

                if (up || down) {
                    int currentDir = up ? 1 : -1;

                    if (currentDir != _lastBpmDirection) {
                        _keyHoldTimer = 0f;
                        _repeatTimer = 0f;
                        _lastBpmDirection = currentDir;
                    }

                    float delta = (currentDir == 1) ? _bpmDelta : -_bpmDelta;

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
                    _lastBpmDirection = 0;
                }
            }
        }
        
        public static void UpdateCountdownTicks(LevelEvent pauseEvent, int floorID, int delta = 0) {
            if (!_autoSetCountdownTicks || pauseEvent == null) return;

            var data = pauseEvent.GetData();
            float duration = Convert.ToSingle(data["duration"]);
            decimal tileBeats = (decimal)GetFloorRelativeAngle(floorID) / 180m;
            decimal totalBeats = tileBeats + (decimal)duration;

            if (IsFloorRelativeAngle360(floorID)) {
                totalBeats -= 1;
            }
            
            if (totalBeats >= 4m && delta >= 0) {
                data["countdownTicks"] = 4;
            } else if (totalBeats < 4m && delta < 0) {
                data["countdownTicks"] = 0;
            }
        }
        
        private static void HandlePause(int delta) {
            var editor = scnEditor.instance;
            if (!editor.SelectionIsSingle()) return; // 선택한 타일이 하나여야 통과
            editor.SaveState();
            int id = editor.selectedFloors[0].seqID;
            var selectedEvent = editor.GetSelectedFloorEvents(LevelEventType.Pause)?.Find(e => true);
            
            float finalDuration;
            bool shouldShowPanel;

            if (selectedEvent == null) {
                if (delta < 0) return;
                AddEventMethod.Invoke(editor, new object[] { id, LevelEventType.Pause });
                selectedEvent = editor.events[editor.events.Count - 1];
                
                finalDuration = delta;
                selectedEvent.GetData()["duration"] = finalDuration;
                shouldShowPanel = true;
            } else {
                var data = selectedEvent.GetData();
                float currentDuration = (float)data["duration"];
                finalDuration = currentDuration + delta;
                
                if (finalDuration > 0) {
                    data["duration"] = finalDuration;
                    shouldShowPanel = true;
                } else {
                    var nextTrackList = editor.GetFloorEvents(id + 1, LevelEventType.PositionTrack);
                    var moveTracks = editor.GetFloorEvents(id, LevelEventType.MoveTrack);
                    List<LevelEvent> eventsToRemove = new List<LevelEvent> { selectedEvent };
                    if (_autoInsertPositionTrack && nextTrackList.Count > 0) eventsToRemove.Add(nextTrackList[0]);
                    if (moveTracks.Count > 0) eventsToRemove.Add(moveTracks[0]);
                    editor.RemoveEvents(eventsToRemove);
                    shouldShowPanel = false;
                    finalDuration = 0;
                }
            }

            if (shouldShowPanel) {
                if (_autoInsertPositionTrack) {
                    var nextTrackList = editor.GetFloorEvents(id + 1, LevelEventType.PositionTrack);
                    if (nextTrackList.Count > 0) editor.RemoveEvents(new List<LevelEvent> { nextTrackList[0] });
                    InsertPositionTrack(id + 1);
                }

                var moveTracks = editor.GetFloorEvents(id, LevelEventType.MoveTrack);
                if (moveTracks.Count > 0) {
                    var mtData = moveTracks[0].GetData();
                    decimal tileBeats = (decimal)GetFloorRelativeAngle(id) / 180m;
                    mtData["duration"] = (float)(tileBeats + (decimal)finalDuration);

                    if (_adjustPositionTrackWithPause) {
                        float absoluteAngle = editor.levelData.angleData[id];
                        Vector2 baseOffset = new Vector2(Mathf.Cos(absoluteAngle * Mathf.Deg2Rad), Mathf.Sin(absoluteAngle * Mathf.Deg2Rad));
                        mtData["positionOffset"] = baseOffset * (finalDuration * _positionTrackUnit);
                    }
                }

                if (id < editor.floors.Count - 1) UpdateCountdownTicks(selectedEvent, id, delta);
                editor.levelEventsPanel.ShowPanel(LevelEventType.Pause);
            }

            editor.ApplyEventsToFloors();
            editor.levelEventsPanel.ShowTabsForFloor(id);
            RemoveTrashUndos(3); 
        }

        public static void InsertPositionTrack(int floorID) {
            var editor = scnEditor.instance;
            if (floorID - 1 >= editor.levelData.angleData.Count) return; // 없는 타일이면 리턴
            if (IsFloorRelativeAngle360(floorID - 1)) return; // 전 타일이 360도면 리턴
            if (editor.GetFloorEvents(floorID, LevelEventType.PositionTrack).Count > 0) return; // 길 위치가 있으면 리턴
            float absoluteAngle = editor.levelData.angleData[floorID - 1];
            float radian = absoluteAngle * Mathf.Deg2Rad;
            Vector2 baseOffset = new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
            float finalMultiplier = 1f;
            if (_adjustPositionTrackWithPause) {
                var pauseEvents = editor.GetFloorEvents(floorID - 1, LevelEventType.Pause);
                if (pauseEvents != null && pauseEvents.Count > 0) {
                    var pauseData = pauseEvents[0].GetData();
                    finalMultiplier = Convert.ToSingle(pauseData["duration"]);
                }
            }
            AddEventMethod.Invoke(editor, new object[] { floorID, LevelEventType.PositionTrack });
            var lastEvent = editor.events[editor.events.Count - 1];
            var data = lastEvent.GetData();
            data["positionOffset"] = baseOffset * (finalMultiplier * _positionTrackUnit);
            lastEvent.disabled["positionOffset"] = false;
            editor.ApplyEventsToFloors();
        }
        
        public static void InsertMoveTrack(int floorID) {
            var editor = ADOBase.editor;
            if (floorID >= editor.floors.Count - 1) return; // 없는 타일이면 리턴
            if (IsFloorRelativeAngle360(floorID)) return; // 360도 타일이면 리턴
            if (editor.GetFloorEvents(floorID, LevelEventType.MoveTrack).Count > 0) return; // 길 이동이 있으면 리턴
            
            decimal tileBeats = (decimal)GetFloorRelativeAngle(floorID) / 180m;
            decimal beats = tileBeats;
            var pause = editor.GetFloorEvents(floorID, LevelEventType.Pause);
            float pauseDuration = 0f;
            if (pause.Count > 0) {
                pauseDuration = Convert.ToSingle(pause[0].GetData()["duration"]);
                beats += (decimal)pauseDuration;
            }
            AddEventMethod.Invoke(editor, new object[] { floorID, LevelEventType.MoveTrack });
            
            var lastEvent = editor.events[editor.events.Count - 1];
            var data = lastEvent.GetData();
            data["duration"] = (float)beats;
            var nextTrackList = editor.GetFloorEvents(floorID + 1, LevelEventType.PositionTrack);
            if (nextTrackList.Count > 0) {
                data["positionOffset"] = nextTrackList[0].GetData()["positionOffset"];
            } else if (_adjustPositionTrackWithPause && pauseDuration > 0) {
                float absoluteAngle = editor.levelData.angleData[floorID];
                float radian = absoluteAngle * Mathf.Deg2Rad;
                Vector2 baseOffset = new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
                data["positionOffset"] = baseOffset * (pauseDuration * _positionTrackUnit);
            }

            string func = _easingFunctions[_easeFuncIdx];
            string resultEaseStr;

            if (func == "Linear") {
                resultEaseStr = "Linear";
            } else if (func == "Flash") {
                string mode = _easingModesFlash[_easeModeIdx];
                if (mode == "-") resultEaseStr = "Flash";
                else resultEaseStr = mode.Replace("-", "") + "Flash";
            } else {
                string mode = _easingModes[_easeModeIdx];
                resultEaseStr = mode.Replace("-", "") + func;
            }

            var easeType = lastEvent.info.propertiesInfo["ease"].enumType;
            try {
                data["ease"] = Enum.Parse(easeType, resultEaseStr);
            } catch {
                data["ease"] = Enum.Parse(easeType, "Linear");
            }
            editor.ApplyEventsToFloors();
            scnEditor.instance.RemakePath();
            editor.levelEventsPanel.ShowTabsForFloor(floorID);
            editor.levelEventsPanel.ShowPanel(LevelEventType.MoveTrack);
            
            RemoveTrashUndos();
        }
        
        private static void HandleSetSpeed(float value, bool calculateByMultiplier) {
            var editor = scnEditor.instance;
            if (!editor.SelectionIsSingle()) return; // 선택한 타일이 하나여야 통과
            editor.SaveState();
            int floorID = editor.selectedFloors[0].seqID;
            var selectedEvent = editor.GetSelectedFloorEvents(LevelEventType.SetSpeed)?.Find(e => true);
            float prevTileSpeed = (floorID > 0) ? editor.floors[floorID - 1].speed : 1f;
            float prevBpm = editor.levelData.bpm * prevTileSpeed;
            bool shouldShowPanel;
            if (selectedEvent == null) {
                AddEventMethod.Invoke(editor, new object[] { floorID, LevelEventType.SetSpeed });
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
                    float currentVal = Convert.ToSingle(data[targetKey]);
                    float nextVal = currentVal * value;
                    if (nextVal > 0f) data[targetKey] = nextVal;
                } else {
                    float currentBpm = isBpmMode ? Convert.ToSingle(data["beatsPerMinute"]) : prevBpm * Convert.ToSingle(data["bpmMultiplier"]);
                    decimal preciseBpm = (decimal) currentBpm + (decimal) value;
                    if (preciseBpm > 0m) {
                        data["speedType"] = SpeedType.Bpm;
                        data["beatsPerMinute"] = (float) preciseBpm;
                    }
                }
                bool nowBpmMode = data["speedType"].ToString() == "Bpm" || data["speedType"].ToString() == "0";
                float finalSpeed = nowBpmMode ? Convert.ToSingle(data["beatsPerMinute"]) : prevBpm * Convert.ToSingle(data["bpmMultiplier"]);
                if (Mathf.Approximately(finalSpeed, prevBpm)) {
                    editor.RemoveEvents(new List<LevelEvent> { selectedEvent });
                    shouldShowPanel = false;
                } else {
                    shouldShowPanel = true;
                }
            }
            editor.ApplyEventsToFloors();
            editor.levelEventsPanel.ShowTabsForFloor(floorID);
            if (shouldShowPanel) {
                var targetEvent = selectedEvent ?? editor.events[editor.events.Count - 1];
                editor.levelEventsPanel.ShowPanel(LevelEventType.SetSpeed);
                editor.levelEventsPanel.UpdatePropertyText(targetEvent, "beatsPerMinute");
                editor.levelEventsPanel.UpdatePropertyText(targetEvent, "bpmMultiplier");
                editor.levelEventsPanel.UpdatePropertyText(targetEvent, "speedType");
            }
            RemoveTrashUndos();
        }

        public static double GetFloorRelativeAngle(int floorID) {
            var editor = ADOBase.editor;
            if (editor == null || floorID < 0 || floorID >= editor.floors.Count - 1) return 0;
            ADOBase.lm.CalculateFloorAngleLengths();
            var floor = editor.floors[floorID];
            return floor.angleLength * Mathf.Rad2Deg;
        }

        private static bool IsFloorRelativeAngle360(int floorID) => 
            Mathf.Approximately((float)GetFloorRelativeAngle(floorID), 360f);
        
        private static bool CheckShortcut(KeyCode key, bool ctrl = false, bool alt = false, bool shift = false, bool useKeyDown = true) {
            bool keyCheck = useKeyDown ? Input.GetKeyDown(key) : Input.GetKey(key);
            if(!keyCheck) return false;
            bool isCtrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool isAltPressed = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            bool isShiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            return isCtrlPressed == ctrl && isAltPressed == alt && isShiftPressed == shift;
        }
        
        public static void RemoveTrashUndos(int amount = 2) {
            var editor = scnEditor.instance;
            int count = editor.undoStates.Count;
            if (count >= amount) {
                editor.undoStates.RemoveRange(count - amount, amount);
            } else if (count > 0) {
                editor.undoStates.RemoveAt(count - 1);
            }
        }
        
        private static void ConvertLegacyPause(bool isDown) {
            scnEditor editor = ADOBase.editor;
            var angleData = editor.levelData.angleData;
            int tiles = angleData.Count;
            List<int> changedTiles = new List<int>();
            
            editor.SaveState();

            for (int i = 0; i < tiles - 1; i++) {
                if (Mathf.Approximately(Mathf.Abs(angleData[i + 1] - angleData[i]), 180f)) {
                    var pauseEvents = editor.GetFloorEvents(i + 1, LevelEventType.Pause);
            
                    if (pauseEvents != null && pauseEvents.Count > 0) {
                        changedTiles.Add(i + 1);
                
                        float currentDuration = Convert.ToSingle(pauseEvents[0].GetData()["duration"]);
                        decimal preciseCalc = isDown ? (decimal)currentDuration - 1m : (decimal)currentDuration + 1m;
                        pauseEvents[0].GetData()["duration"] = (float)preciseCalc;

                        if (editor.selectedFloors.Count > 0 && editor.selectedFloors[0].seqID == i + 1) {
                            editor.levelEventsPanel.UpdatePropertyText(pauseEvents[0], "duration");
                        }
                    }
                }
            }

            editor.levelData.legacyPause = false;

            if (changedTiles.Count > 0) {
                _legacyPauseResultStr = GetTranslation($"{changedTiles.Count}개 변경!", $"{changedTiles.Count} tiles changed!") + $"({string.Join(", ", changedTiles)})";
            } else {
                RemoveTrashUndos(1);
                _legacyPauseResultStr = GetTranslation("0개 변경!", "0 tiles changed!");
            }
        }
    }
}