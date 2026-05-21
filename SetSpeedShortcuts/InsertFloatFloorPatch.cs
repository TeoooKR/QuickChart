using ADOFAI;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(scnEditor),"InsertFloatFloor")]
public static class InsertFloatFloorPatch {
    public static void Postfix() {
        var editor = scnEditor.editor;
        var selectedFloors = editor.selectedFloors;
        var id = selectedFloors[0].seqID;
        if (editor.GetFloorEvents(id + 2, LevelEventType.PositionTrack).Count > 0) { // remove position
            var nextFloor = editor.GetFloorEvents(id + 2, LevelEventType.PositionTrack);
            editor.RemoveEvent(nextFloor[0]);
        }
        editor.ApplyEventsToFloors();
        if (Main._autoInsertPositionTrack && editor.GetFloorEvents(id, LevelEventType.Pause).Count > 0) {
            Main.InsertPositionTrack(id + 1);
        }
    }
}

[HarmonyPatch(typeof(scnEditor),"AddEvent")]
public static class AddEventPatch { 
    public static void Postfix(int floorID, LevelEventType eventType) {
        if (Main._autoInsertPositionTrack && eventType == LevelEventType.Pause) {
            Main.InsertPositionTrack(floorID + 1);
        }
    }
}