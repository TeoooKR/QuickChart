using System.IO;
using System.Xml.Serialization;
using UnityModManagerNet;

public class Settings : UnityModManager.ModSettings
{
    public float bpmDelta = 1;

    public override void Save(UnityModManager.ModEntry modEntry) {
        var filepath = GetPath(modEntry);
        try {
            using (var writer = new StreamWriter(filepath)) {
                var serializer = new XmlSerializer(GetType());
                serializer.Serialize(writer, this);
            }
        } catch {
        }
    }

    public override string GetPath(UnityModManager.ModEntry modEntry) {
        return Path.Combine(modEntry.Path, GetType().Name + ".xml");
    }

}

