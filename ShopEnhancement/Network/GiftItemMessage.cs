using System;
using System.Buffers.Binary;
using System.Text;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace ShopEnhancement.Network;

public class GiftItemMessage : INetMessage
{
    public bool ShouldBroadcast => false;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Info;

    public string ItemId { get; set; } = "";
    public string ItemType { get; set; } = ""; // "Card", "Relic", "Potion"
    public ulong SenderId { get; set; }
    public ulong TargetId { get; set; }
    public int UpgradeCount { get; set; }
    public int Misc { get; set; }

    public GiftItemMessage() { }

    public GiftItemMessage(string itemId, string itemType, ulong senderId, ulong targetId, int upgradeCount = 0, int misc = 0)
    {
        ItemId = itemId;
        ItemType = itemType;
        SenderId = senderId;
        TargetId = targetId;
        UpgradeCount = upgradeCount;
        Misc = misc;
    }

    public void Serialize(PacketWriter writer)
    {
        WriteString(writer, ItemId);
        WriteString(writer, ItemType);
        writer.WriteULong(SenderId);
        writer.WriteULong(TargetId);
        writer.WriteInt(UpgradeCount);
        writer.WriteInt(Misc);
    }

    public void Deserialize(PacketReader reader)
    {
        ItemId = ReadString(reader);
        ItemType = ReadString(reader);
        SenderId = reader.ReadULong();
        TargetId = reader.ReadULong();
        UpgradeCount = reader.ReadInt();
        Misc = reader.ReadInt();
    }

    private void WriteString(PacketWriter writer, string str)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(str);
        writer.WriteShort((short)bytes.Length);
        writer.WriteBytes(bytes, bytes.Length);
    }

    private string ReadString(PacketReader reader)
    {
        short len = reader.ReadShort();
        byte[] bytes = new byte[len];
        reader.ReadBytes(bytes, len);
        return Encoding.UTF8.GetString(bytes);
    }
}
