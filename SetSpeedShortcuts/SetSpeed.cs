using ADOFAI;
using HarmonyLib;

[HarmonyPatch(typeof(scnEditor), "AddEvent")]
public static class AddEventPatch {
    static void Postfix(scnEditor __instance, LevelEventType eventType) {
        if (eventType == LevelEventType.SetSpeed && Main.IS_ARROW) {
            LevelEvent lastEvent = __instance.events[__instance.events.Count - 1];
            if (Main.IS_TYPE_MULTIPLIER) {
                lastEvent.data["bpmMultiplier"] = Main.MULTIPLIER;
                lastEvent.data["speedType"] = 1; // idk why this works lol
            } else if(!Main.IS_TYPE_MULTIPLIER) {
                lastEvent.data["beatsPerMinute"] = Main.BEATS_PER_MINUTE;
            }
            Main.IS_ARROW = false;
        }
    }
}