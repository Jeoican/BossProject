﻿using BossFramework.BInterfaces;
using BossFramework.BModels;
using TrProtocol.Packets;

namespace BossFramework.BNet.PacketHandlers
{
    public class DestroyProjectileHandler : PacketHandlerBase<KillProjectile>
    {
        public override bool OnGetPacket(BPlayer plr, KillProjectile packet)
        {
            BCore.ProjRedirector.OnProjDestory(plr, packet);
            return true;
        }

        public override bool OnSendPacket(BPlayer plr, KillProjectile packet)
        {
            return false;
        }
    }
}
