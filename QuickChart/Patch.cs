using ADOFAI;
using HarmonyLib;

namespace QuickChart {
    [HarmonyPatch(typeof(scnEditor), "InsertFloatFloor")]
    public static class Patch {
        public static void Postfix() {
            var editor = scnEditor.instance;
            if (editor.selectedFloors.Count == 0) return;

            var floorID = editor.selectedFloors[0].seqID;

            editor.RemakePath();

            var shiftedPT = editor.GetFloorEvents(floorID + 2, LevelEventType.PositionTrack);
            if (shiftedPT.Count > 0) {
                editor.RemoveEvent(shiftedPT[0]);
            }

            var pauseEventsOnCurrent = editor.GetFloorEvents(floorID, LevelEventType.Pause);
            if (pauseEventsOnCurrent.Count > 0) {
                Main.UpdateCountdownTicks(pauseEventsOnCurrent[0], floorID);
            }

            if (Main._autoInsertPositionTrack) {
                if (pauseEventsOnCurrent.Count > 0) {
                    Main.InsertPositionTrack(floorID + 1);

                    var moveTracks = editor.GetFloorEvents(floorID, LevelEventType.MoveTrack);
                    if (moveTracks.Count == 0) {
                        Main.InsertMoveTrack(floorID);
                    } else {
                        var mtData = moveTracks[0].GetData();
                        var ptList = editor.GetFloorEvents(floorID + 1, LevelEventType.PositionTrack);
                        if (ptList.Count > 0) {
                            mtData["positionOffset"] = ptList[0].GetData()["positionOffset"];
                        }
                    
                        decimal tileBeats = (decimal)Main.GetFloorRelativeAngle(floorID) / 180m;
                        float pauseDuration = System.Convert.ToSingle(pauseEventsOnCurrent[0].GetData()["duration"]);
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
            if (eventType == LevelEventType.Pause) {
                if (Main._autoInsertPositionTrack) {
                    Main.InsertPositionTrack(floorID + 1);
                }
                if (Main._autoInsertMoveTrack) {
                    Main.InsertMoveTrack(floorID);
                }    
            }
        }
    }
}