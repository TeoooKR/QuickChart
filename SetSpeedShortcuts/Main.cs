using System.Collections.Generic;
using System.Reflection;
using ADOFAI;
using HarmonyLib;
using UnityModManagerNet;
using UnityEngine;

public static class Main
{
    public static UnityModManager.ModEntry.ModLogger Logger;
    public static Harmony Harmony;
    public static bool IsEnabled;

    static float _keyHoldTimer;
    static float _repeatTimer;

    public static void Setup(UnityModManager.ModEntry modEntry)
    {
        modEntry.Logger.Log("Setup!");
        Logger = modEntry.Logger;
    
        modEntry.OnUpdate = OnUpdate; 
        modEntry.OnToggle = OnToggle;
    }
    static public float Multiplier = 1;
    static public float BeatsPerMinute;
    static public bool IsArrow;
    static public bool IsTypeMultiplier;

    private static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime) {
        if (!IsEnabled) return;

        if (Input.GetKeyDown(KeyCode.F4)) {
        }
        
        if (Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.UpArrow)) {
            HandleSpeedMultiply(2.0f);
        } else if (Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.DownArrow)) {
            HandleSpeedMultiply(0.5f);
        } 
        else if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl)) {
            bool up = Input.GetKey(KeyCode.UpArrow);
            bool down = Input.GetKey(KeyCode.DownArrow);
            if (up || down) {
                float delta = up ? 1 : -1;
                if (_keyHoldTimer == 0f) {
                    HandleSpeedOne(delta);
                    _keyHoldTimer += deltaTime;
                } else {
                    _keyHoldTimer += deltaTime;
                    if (_keyHoldTimer > 0.4f) {
                        _repeatTimer += deltaTime;
                        if (_repeatTimer > 0.05f) {
                            HandleSpeedOne(delta);
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

    private static void HandleSpeedMultiply(float ratio) {
        var selectedEvents = scnEditor.instance.GetSelectedFloorEvents(LevelEventType.SetSpeed);
        if (!scnEditor.instance.SelectionIsSingle()) {
            return;
        }
        if (selectedEvents != null && selectedEvents.Count > 0) {
            LevelEvent ev = selectedEvents[0];
            string type = ev.data["speedType"].ToString();
            
            if (type == "Multiplier" || type == "1") {
                float currentMult = System.Convert.ToSingle(ev.data["bpmMultiplier"]);
                if (Mathf.Approximately(currentMult * ratio, 1)) {
                    scnEditor.instance.RemoveEvents(new List<LevelEvent> { ev });
                } else {
                    ev.data["bpmMultiplier"] = currentMult * ratio;
                    scnEditor.instance.levelEventsPanel.UpdatePropertyText(ev, "bpmMultiplier"); //update input    
                }
            } 
            else if (type == "Bpm") {
                float currentBpm = System.Convert.ToSingle(ev.data["beatsPerMinute"]);
                int previousTile = ADOBase.editor.selectedFloors[0].seqID - 1;
                float previousBpm = ADOBase.editor.levelData.bpm;

                if (previousTile > 0) {
                    previousBpm *= ADOBase.editor.floors[previousTile].speed;
                }
                if (Mathf.Approximately(currentBpm * ratio, previousBpm)) {
                    scnEditor.instance.RemoveEvents(new List<LevelEvent> { ev });
                } else {
                    ev.data["beatsPerMinute"] = currentBpm * ratio;
                    scnEditor.instance.levelEventsPanel.UpdatePropertyText(ev, "beatsPerMinute"); //update input
                }
            }
            scnEditor.instance.ApplyEventsToFloors(); //update tiles
            scnEditor.instance.levelEventsPanel.ShowPanel(LevelEventType.SetSpeed);
        } else {
            IsTypeMultiplier = true;
            Multiplier = ratio;
            IsArrow = true;
            scnEditor.instance.AddEventAtSelected(LevelEventType.SetSpeed);
        }
    }
    
    private static void HandleSpeedOne(float delta) {
        var selectedEvents = scnEditor.instance.GetSelectedFloorEvents(LevelEventType.SetSpeed);
        
        if (selectedEvents != null && selectedEvents.Count > 0) {
            LevelEvent ev = selectedEvents[0];
            string type = ev.data["speedType"].ToString();
            
            if (type == "Multiplier" || type == "1") {
                return;
            } 
            else if (type == "Bpm") {
                float currentBpm = System.Convert.ToSingle(ev.data["beatsPerMinute"]);
                int previousTile = ADOBase.editor.selectedFloors[0].seqID - 1;
                float previousBpm = ADOBase.editor.levelData.bpm;

                if (previousTile > 0) {
                    previousBpm *= ADOBase.editor.floors[previousTile].speed;
                }
                if (Mathf.Approximately(currentBpm + delta, previousBpm)) {
                    scnEditor.instance.RemoveEvents(new List<LevelEvent> { ev });
                } else {
                    if (currentBpm + delta > 0) {
                        ev.data["beatsPerMinute"] = currentBpm + delta;
                    } else {
                        ev.data["beatsPerMinute"] = currentBpm;
                    }
                    scnEditor.instance.levelEventsPanel.UpdatePropertyText(ev, "beatsPerMinute"); //update input
                }

            }
            scnEditor.instance.ApplyEventsToFloors(); //update tiles
        } else {
            IsTypeMultiplier = false;
            float currentBpm = ADOBase.editor.levelData.bpm * ADOBase.editor.selectedFloors[0].speed;
            BeatsPerMinute = currentBpm;
            if (currentBpm + delta > 0) {
                BeatsPerMinute += delta;
            }
            IsArrow = true;
            scnEditor.instance.AddEventAtSelected(LevelEventType.SetSpeed);
        }
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
}