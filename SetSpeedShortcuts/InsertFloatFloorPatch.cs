using ADOFAI;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(scnEditor), "InsertFloatFloor")]
public static class InsertFloatFloorPatch {
    public static void Postfix() {
        var editor = scnEditor.editor;
        if (editor.selectedFloors.Count == 0) return;

        var id = editor.selectedFloors[0].seqID;

        var shiftedPT = editor.GetFloorEvents(id + 2, LevelEventType.PositionTrack);
        if (shiftedPT.Count > 0) {
            editor.RemoveEvent(shiftedPT[0]);
        }

        if (Main._autoInsertPositionTrack) {
            var pauseEvents = editor.GetFloorEvents(id, LevelEventType.Pause);
            if (pauseEvents.Count > 0) {
                
                Main.InsertPositionTrack(id + 1);

                var moveTracks = editor.GetFloorEvents(id, LevelEventType.MoveTrack);
                if (moveTracks.Count == 0) {
                    Main.InsertMoveTrack(id);
                } else {
                    var mtData = moveTracks[0].GetData();
                    var ptList = editor.GetFloorEvents(id + 1, LevelEventType.PositionTrack);
                    if (ptList.Count > 0) {
                        mtData["positionOffset"] = ptList[0].GetData()["positionOffset"];
                    }
                    
                    decimal tileBeats = (decimal)Main.GetFloorRelativeAngle(id) / 180m;
                    float pauseDuration = System.Convert.ToSingle(pauseEvents[0].GetData()["duration"]);
                    mtData["duration"] = (float)(tileBeats + (decimal)pauseDuration);
                }
            }
        }
        
        editor.ApplyEventsToFloors();
    }
}

[HarmonyPatch(typeof(scnEditor),"AddEvent")]
public static class AddEventPatch { 
    public static void Postfix(int floorID, LevelEventType eventType) {
        if (Main._autoInsertPositionTrack && eventType == LevelEventType.Pause) {
            Main.InsertPositionTrack(floorID + 1);
            if (floorID >= scnEditor.editor.floors.Count - 1) {
            } else {
                Main.InsertMoveTrack(floorID);
            }
            
        }
    }
}