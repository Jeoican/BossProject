﻿using TrProtocol.Models;

namespace TrProtocol.Packets
{
    public class PlayLegacySound : Packet
    {
        public override MessageID Type => MessageID.PlayLegacySound;
        public Vector2 Point { get; set; }
        public ushort Sound { get; set; }
        public ProtocolBitsByte Bits1 { get; set; }
        [Condition("Bits1", 0)]
        public int Style { get; set; }
        [Condition("Bits1", 1)]
        public float Volume { get; set; }
        [Condition("Bits1", 2)]
        public float Pitch { get; set; }
    }
}