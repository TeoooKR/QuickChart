using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ADOFAI;
using HarmonyLib;
using UnityModManagerNet;

namespace QuickChart {
    public static class Patch {
        readonly private static MethodInfo FloorPointsBackwardsMethod = typeof(scnEditor).GetMethod(
            "FloorPointsBackwards",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new Type[] { typeof(float) },
            null
        );
        
        readonly private static MethodInfo OffsetFloorIDsInEventsMethod = typeof(scnEditor).GetMethod(
            "OffsetFloorIDsInEvents", 
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        readonly private static MethodInfo FlashTileMethod = typeof(scnEditor).GetMethod(
            "FlashTile", 
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        
        readonly private static MethodInfo MoveCameraToFloorMethod = typeof(scnEditor).GetMethod(
            "MoveCameraToFloor", 
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        
        [HarmonyPatch(typeof(scnEditor), "InsertFloatFloor")]
        public static class InsertFloatFloorPatch {
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

        [HarmonyPatch(typeof(scnEditor), "PasteFloors")]
        public static class PasteFloorPatch {
            public static bool Prefix(scnEditor __instance) {
                if ((bool) FloorPointsBackwardsMethod.Invoke(__instance, new object[] {
                        ((scnEditor.FloorData) __instance.clipboard[0]).floatDirection
                    })) {
                    Main.Logger.Log("반대야!");
                    List<int> intList = new List<int>();

                    int seqId = __instance.selectedFloors[0].seqID;

                    using (new SaveStateScope(__instance)) {
                        OffsetFloorIDsInEventsMethod.Invoke(__instance, new object[] {
                            seqId, __instance.clipboard.Count
                        });
                        for (int index = 0; index < __instance.clipboard.Count<object>(); ++index) {
                            scnEditor.FloorData floorData = (scnEditor.FloorData) __instance.clipboard[index];
                            List<LevelEvent> levelEventData = floorData.levelEventData;


                            float floatDirection = floorData.floatDirection;
                            __instance.levelData.angleData.Insert(seqId, floatDirection);

                            ++seqId;
                            intList.Add(seqId);

                        }
                    }

                    __instance.RemakePath();
                    __instance.SelectFloor(__instance.floors[seqId]);
                    MoveCameraToFloorMethod.Invoke(__instance, new object[] {
                        __instance.floors[seqId]
                    });

                    foreach (int index in intList) {
                        FlashTileMethod.Invoke(__instance, new object[] {
                            __instance.floors[index]
                        });
                    }

                    FlashTileMethod.Invoke(__instance, new object[] {
                        __instance.floors[__instance.selectedFloors[0].seqID]
                    });

                    Main.RemoveTrashUndos(1);
                    return false;
                }
                return true;
            }
        }
    }
}