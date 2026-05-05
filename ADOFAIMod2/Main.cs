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
        if(!IsEnabled) return;
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.UpArrow)) {
            MULTIPLIER = 2;
            IS_ARROW = true;
            // if(scnEditor.instance.events != null) {
            //     LevelEvent lastEvent = scnEditor.instance.events[scnEditor.instance.events.Count - 1] ;
            //     // lastEvent.data["bpmMultiplier"] *= 2;
            //
            // }
            ADOBase.editor.AddEventAtSelected(LevelEventType.SetSpeed);
        }
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.DownArrow)) {
            MULTIPLIER = 0.5F;
            IS_ARROW = true;
            ADOBase.editor.AddEventAtSelected(LevelEventType.SetSpeed);                
        }
    }
    private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
    {
        IsEnabled = value;
    
        if (value)
        {
            harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
        else
        { 
            harmony.UnpatchAll(modEntry.Info.Id);
        }
        return true;
    }
}