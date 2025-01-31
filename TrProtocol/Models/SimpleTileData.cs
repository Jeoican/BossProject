﻿namespace TrProtocol.Models
{
    [LegacySerializer]
    public struct SimpleTileData
    {
        public ProtocolBitsByte Flags1 { get; set; }
        public ProtocolBitsByte Flags2 { get; set; }
        [Condition("Flags2", 2)]
        public byte TileColor { get; set; }
        [Condition("Flags2", 3)]
        public byte WallColor { get; set; }
        [BoundWith("MaxTileType")]
        [Condition("Flags1", 0)]
        public ushort TileType { get; set; }

        private bool TileFrameImportant => Flags1[0] && Terraria.Main.tileFrameImportant[TileType];
        [Condition("TileFrameImportant")]
        public short FrameX { get; set; }
        [Condition("TileFrameImportant")]
        public short FrameY { get; set; }
        [Condition("Flags1", 2)]
        public ushort WallType { get; set; }
        [Condition("Flags1", 3)]
        public byte Liquid { get; set; }
        [Condition("Flags1", 3)]
        public byte LiquidType { get; set; }
    }
}
