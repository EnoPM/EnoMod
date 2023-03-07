using System.Text.Json;
using EnoMod.Customs;
using Reactor.Networking.Attributes;

namespace EnoMod.Kernel;

public static class Rpc
{
    [MethodRpc((uint) CustomRpc.ShareCustomOptions)]
    public static void ShareCustomOptions(PlayerControl _, string text)
    {
        if (AmongUsClient.Instance.AmHost) return;
        var customOptionInfos = Deserialize<CustomOptionInfo[]>(text);
        foreach (var customOptionInfo in customOptionInfos)
        {
            var option = CustomOption.Tab.Options().Find(co => co.Key == customOptionInfo.Key);
            if (option == null) return;
            option.SelectionIndex = customOptionInfo.Selection;
        }
    }

    public class CustomOptionInfo
    {
        public string Key { get; set; }
        public int Selection { get; set; }
    }

    public static string Serialize<T>(T data)
    {
        return JsonSerializer.Serialize(data);
    }

    public static T Deserialize<T>(string data)
    {
        return JsonSerializer.Deserialize<T>(data) ?? throw new EnoModException("Deserialization error");
    }
}
