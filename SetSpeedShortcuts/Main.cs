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
    static public bool IS_ARROW = false;
    private static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime) {
        if (!IsEnabled) return;

        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.UpArrow)) {
            HandleSpeedChange(2.0f);
        }
        else if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.DownArrow)) {
            HandleSpeedChange(0.5f);
        }
    }
    private static void HandleSpeedChange(float ratio) {
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
            MULTIPLIER = ratio; 
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