﻿namespace TrProtocol.Packets
{
    public struct PlayerHealth : IPacket, IPlayerSlot
    {
        public MessageID Type => MessageID.PlayerHealth;
        public byte PlayerSlot { get; set; }
        public short StatLife { get; set; }
        public short StatLifeMax { get; set; }
    }
}
