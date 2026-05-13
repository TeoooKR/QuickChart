// using ADOFAI;
// using HarmonyLib;
//
// [HarmonyPatch(typeof(scnEditor), "AddEvent")]
// public static class AddEventPatch {
//     static void Postfix(scnEditor __instance, LevelEventType eventType) {
//         if (eventType == LevelEventType.SetSpeed && Main.IsArrow) {
//             LevelEvent lastEvent = __instance.events[__instance.events.Count - 1];
//             if (Main.IsTypeMultiplier) {
//                 
//                 lastEvent.data["bpmMultiplier"] = Main.Multiplier;
//                 lastEvent.data["speedType"] = 1; // idk why this works lol
//             } else if(!Main.IsTypeMultiplier) {
//                 lastEvent.data["beatsPerMinute"] = Main.BeatsPerMinute;
//             }
//             Main.IsArrow = false;
//         }
//     }
// }