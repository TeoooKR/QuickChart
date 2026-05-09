using System.Reflection;
using ADOFAI;
using HarmonyLib;
using UnityModManagerNet;
using UnityEngine;

public static class Main
{
    public static UnityModManager.ModEntry.ModLogger Logger;
    public static Harmony harmony;
    public static bool IsEnabled = false;
    public static void Setup(UnityModManager.ModEntry modEntry)
    {
        modEntry.Logger.Log("Setup!");
        Logger = modEntry.Logger;
    
        modEntry.OnUpdate = OnUpdate; 
        modEntry.OnToggle = OnToggle;
    }
    static public float MULTIPLIER = 1;
    static public float BEATS_PER_MINUTE = 0;
    static public bool IS_ARROW = false;
    static public bool IS_TYPE_MULTIPLIER = false;
    private static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime) {
        if (!IsEnabled) return;

        if (Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.UpArrow)) {
            HandleSpeedMultiply(2.0f);
        } else if (Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.DownArrow)) {
            HandleSpeedMultiply(0.5f);
        } else if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.UpArrow)) {
            HandleSpeedOne(1);
        } else if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.DownArrow)) {
            HandleSpeedOne(-1);
        }
    }
    private static void HandleSpeedMultiply(float ratio) {
        var selectedEvents = scnEditor.instance.GetSelectedFloorEvents(LevelEventType.SetSpeed);
        
        if (selectedEvents != null && selectedEvents.Count > 0) {
            LevelEvent ev = selectedEvents[0];
            string type = ev.data["speedType"].ToString();
            
            if (type == "Multiplier" || type == "1") {
                float currentMult = System.Convert.ToSingle(ev.data["bpmMultiplier"]);
                ev.data["bpmMultiplier"] = currentMult * ratio;
                scnEditor.instance.levelEventsPanel.UpdatePropertyText(ev, "bpmMultiplier"); //update input
            } 
            else if (type == "Bpm") {
                float currentBpm = System.Convert.ToSingle(ev.data["beatsPerMinute"]);
                ev.data["beatsPerMinute"] = currentBpm * ratio;
                scnEditor.instance.levelEventsPanel.UpdatePropertyText(ev, "beatsPerMinute"); //update input
            }
            
            scnEditor.instance.ApplyEventsToFloors(); //update tiles
        } else {
            IS_TYPE_MULTIPLIER = true;
            MULTIPLIER = ratio;
            IS_ARROW = true;
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
                if (currentBpm + delta > 0) {
                    ev.data["beatsPerMinute"] = currentBpm + delta;
                } else {
                    ev.data["beatsPerMinute"] = currentBpm;
                }
                scnEditor.instance.levelEventsPanel.UpdatePropertyText(ev, "beatsPerMinute"); //update input
            }
            scnEditor.instance.ApplyEventsToFloors(); //update tiles
        } else {
            IS_TYPE_MULTIPLIER = false;
            float currentBpm = ADOBase.editor.levelData.bpm * ADOBase.editor.selectedFloors[0].speed;
            BEATS_PER_MINUTE = currentBpm;
            if (currentBpm + delta > 0) {
                BEATS_PER_MINUTE += delta;
            }
            IS_ARROW = true;
            scnEditor.instance.AddEventAtSelected(LevelEventType.SetSpeed);
        }
    }
    
    private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value) {
        IsEnabled = value;
        if (value) {
            harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
        else { 
            harmony.UnpatchAll(modEntry.Info.Id);
        }
        return true;
    }
}