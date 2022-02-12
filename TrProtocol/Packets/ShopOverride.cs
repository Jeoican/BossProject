﻿using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public class ShopOverride : Packet
    {
        public override MessageID Type => MessageID.ShopOverride;
        public byte ItemSlot { get; set; }
        public short ItemType { get; set; }
        public short Stack { get; set; }
        public byte Prefix { get; set; }
        public int Value { get; set; }
        public ProtocolBitsByte BuyOnce { get; set; } // only first bit counts
    }
}