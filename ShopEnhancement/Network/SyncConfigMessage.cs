using System.Text.Json;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using ShopEnhancement.Config;

namespace ShopEnhancement.Network;

public struct SyncConfigMessage : INetMessage, IPacketSerializable
{
    public string ConfigJson;

    public bool ShouldBroadcast => true;

    public NetTransferMode Mode => NetTransferMode.Reliable;

    public LogLevel LogLevel => LogLevel.Info;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteString(ConfigJson ?? string.Empty);
    }

    public void Deserialize(PacketReader reader)
    {
        ConfigJson = reader.ReadString();
    }
}
