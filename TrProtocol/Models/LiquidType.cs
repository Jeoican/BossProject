﻿using TrProtocol.Serializers;

namespace TrProtocol.Models
{
    [Serializer(typeof(ByteEnumSerializer<LiquidType>))]
    public enum LiquidType : byte
    {
        Water = 1,
        Lava = 2,
        Honey = 3
    }
}
