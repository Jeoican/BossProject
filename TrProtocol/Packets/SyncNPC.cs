﻿using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public struct SyncNPC : IPacket
    {
        public MessageID Type => MessageID.SyncNPC;
        public short NPCSlot { get; set; }
        public Vector2 Offset { get; set; }
        public Vector2 Velocity { get; set; }
        public ushort Target { get; set; }
        public ProtocolBitsByte Bit1 { get; set; }
        public ProtocolBitsByte Bit2 { get; set; }
        [Condition("Bit1", 2)]
        public float AI1 { get; set; }
        [Condition("Bit1", 3)]
        public float AI2 { get; set; }
        [Condition("Bit1", 4)]
        public float AI3 { get; set; }
        [Condition("Bit1", 5)]
        public float AI4 { get; set; }
        [BoundWith("MaxNPCID ")]
        public short NPCType { get; set; }
        [Condition("Bit2", 0)]
        public byte PlayerCount { get; set; }
        [Condition("Bit2", 2)]
        public float StrengthMultiplier { get; set; }
        [Condition("Bit1", 7, false)]
        public ProtocolBitsByte Bit3 { get; set; }
        [Condition("Bit3", 0)]
        public sbyte PrettyShortHP { get; set; }
        [Condition("Bit3", 1)]
        public short ShortHP { get; set; }
        [Condition("Bit3", 2)]
        public int HP { get; set; }
        public byte[] Extra { get; set; }
    }
}