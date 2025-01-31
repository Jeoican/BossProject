﻿namespace TrProtocol.Packets
{
    public struct SyncChestItem : IPacket
    {
        public MessageID Type => MessageID.SyncChestItem;
        public short ChestSlot { get; set; }
        public byte ChestItemSlot { get; set; }
        public short Stack { get; set; }
        public byte Prefix { get; set; }
        public short ItemType { get; set; }
    }
}