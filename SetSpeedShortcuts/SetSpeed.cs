using ADOFAI;
using HarmonyLib;

[HarmonyPatch(typeof(scnEditor), "AddEvent")]
public static class AddEventPatch {
    static void Postfix(scnEditor __instance, LevelEventType eventType) {
        if (eventType == LevelEventType.SetSpeed && Main.IS_ARROW) {
            LevelEvent lastEvent = __instance.events[__instance.events.Count - 1];
            lastEvent.data["bpmMultiplier"] = Main.MULTIPLIER;
            lastEvent.data["speedType"] = 1; // idk why this works lol
            Main.IS_ARROW = false;
        }
    }
}