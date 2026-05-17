using ADOFAI;
using HarmonyLib;
using UnityModManagerNet;

[HarmonyPatch(typeof(scnEditor),"InsertFloatFloor")]
public static class InsertFloatFloorPatch {
    public static void Postfix() {
        var editor = scnEditor.editor;
        var selectedFloors = editor.selectedFloors;
        var id = selectedFloors[0].seqID;
        if (editor.GetFloorEvents(id, LevelEventType.Pause).Count > 0) {
            Main.InsertPositionTrack(id + 1);
        }
    }
}

[HarmonyPatch(typeof(scnEditor),"AddEvent")]
public static class AddEventPatch {
    public static void Postfix(int floorID, LevelEventType eventType) {
        if (eventType == LevelEventType.Pause) {
            Main.InsertPositionTrack(floorID + 1);
        }
    }
}
