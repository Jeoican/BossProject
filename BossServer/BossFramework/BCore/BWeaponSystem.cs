﻿using BossFramework.BAttributes;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Terraria.ID;
using TerrariaApi.Server;
using TrProtocol;
using TrProtocol.Packets;
using TShockAPI;

namespace BossFramework.BCore
{
    public static class BWeaponSystem
    {
        public const short FillItem = 3853;
        public static string WeaponScriptPath => Path.Combine(ScriptManager.ScriptRootPath, "Weapons");
        [AutoInit]
        private static void InitWeapon()
        {
            BLog.DEBUG("初始化自定义武器");

            if (!Directory.Exists(WeaponScriptPath))
                Directory.CreateDirectory(WeaponScriptPath);

            LoadWeapon();

            ServerApi.Hooks.GameUpdate.Register(BPlugin.Instance, OnGameUpdate);

            ProjRedirector.ProjCreate += OnProjCreate;
            ProjRedirector.ProjDestroy += OnProjDestroy;

            BNet.PacketHandlers.PlayerDamageHandler.PlayerDamage += OnPlayerHurt;
        }
        [Reloadable]
        private static void LoadWeapon()
        {
            BWeapons = ScriptManager.LoadScripts<BaseBWeapon>(WeaponScriptPath);
            BLog.Success($"成功加载 {BWeapons.Length} 个自定义武器");

            BInfo.OnlinePlayers.Where(p => p.IsCustomWeaponMode).BForEach(p =>
            {
                p.Weapons = (from w in BWeapons select (BaseBWeapon)Activator.CreateInstance(w.GetType(), null)).ToArray(); //给玩家生成武器对象
                p.ChangeItemsToBWeapon();
            });
        }

        public static BaseBWeapon[] BWeapons { get; private set; }

        #region 事件
        public static void OnGameUpdate(EventArgs args)
        {
            BInfo.OnlinePlayers.Where(p => p.IsCustomWeaponMode)
                .BForEach(p =>
                {
                    if (p.TrPlayer.controlUseItem)
                        p.Weapons.FirstOrDefault(w => p.ItemInHand.NetId == 0 ? w.Equals(p.TsPlayer.SelectedItem) : w.Equals(p.ItemInHand))
                        ?.OnUseItem(p, BInfo.GameTick);
                });
        }
        [SimpleTimer(Time = 1)]
        public static void OnSecendUpdate()
        {
            BInfo.OnlinePlayers.Where(p => p?.IsCustomWeaponMode ?? false)
                .BForEach(p =>
                {
                    if (p.RelesedProjs.Where(p => DateTimeOffset.Now.ToUnixTimeSeconds() - p.CreateTime > BInfo.ProjMaxLiveTick).ToArray() is { Length: > 0 } inactiveProjs)
                        inactiveProjs.BForEach(projInfo =>
                        {
                            projInfo.KillProj();
                            p.RelesedProjs.Remove(projInfo); //存活太久则删除
                        });
                });
        }
        public static bool CheckIncomeItem(BPlayer plr, SyncEquipment item)
        {
            if (plr.IsChangingWeapon)
                return true;
            if (item.ItemSlot > 50 || item.ItemType == 0) //空格子或者拿手上则忽略
                return false;
            if (plr.Weapons?.Where(w => w.Equals(item)).FirstOrDefault() is { } bweapon)
            {
                var targetItem = plr.TsPlayer.TPlayer.inventory[item.ItemSlot];
                if ((targetItem is null || targetItem?.type == 0)
                    && (plr.ItemInHand.NetId == 0 || !bweapon.Equals(plr.ItemInHand))) //如果目标为空物品并且手上没拿东西或者拿的东西不一样
                {
                    plr.TsPlayer.TPlayer.inventory[item.ItemSlot] ??= new();
                    plr.TsPlayer.TPlayer.inventory[item.ItemSlot].SetDefaults(item.ItemType);
                    plr.TsPlayer.TPlayer.inventory[item.ItemSlot].stack = item.Stack;
                    plr.TsPlayer.TPlayer.inventory[item.ItemSlot].prefix = item.Prefix;
                    Task.Run(() => plr.ChangeSingleItemToBWeapon(bweapon, item.ItemSlot));
                    return true;
                }
            }
            return false;
        }
        public static void OnPlayerHurt(BEventArgs.PlayerDamageEventArgs args)
        {
            var plr = args.Player;
            if (plr.IsCustomWeaponMode)
            {
                var hurt = args.Hurt;
                var targetPlayer = TShock.Players[hurt.OtherPlayerSlot]?.GetBPlayer();
                var deathReason = hurt.Reason;
                if (plr.RelesedProjs.Where(p => p.Proj.ProjSlot == deathReason._sourceProjectileIndex).FirstOrDefault() is { } projInfo)
                {
                    args.Handled = projInfo.FromWeapon.OnProjHit(plr, targetPlayer, projInfo.Proj, hurt.Damage, hurt.HitDirection, (byte)hurt.CoolDown);
                }
                else if (plr.Weapons.Where(w => w.ItemID == deathReason._sourceItemType && w.Prefix == deathReason._sourceItemPrefix).FirstOrDefault() is { } weapon)
                {
                    args.Handled = weapon.OnHit(plr, targetPlayer, hurt.Damage, hurt.HitDirection, (byte)hurt.CoolDown);
                }
                if (args.Handled) //伤害handle后向造成伤害的玩家同步真实血量
                    plr.SendPacket(new PlayerHealth()
                    {
                        PlayerSlot = targetPlayer.Index,
                        StatLife = (short)targetPlayer.TrPlayer.statLife,
                        StatLifeMax = (short)targetPlayer.TrPlayer.statLifeMax2
                    });
            }
        }
        public static void OnProjCreate(BEventArgs.ProjCreateEventArgs args)
        {
            if (!args.Player.IsCustomWeaponMode)
                return;
            if (args.Player.Weapons.Where(w => w.Equals(args.Player.TsPlayer.SelectedItem)).FirstOrDefault() is { } weapon)
            {
                var selectItem = args.Player.TsPlayer.SelectedItem;
                if (weapon.OnShootProj(args.Player, args.Proj, args.Proj.Velocity, (weapon.ShootProj ?? selectItem.shoot) == args.Proj.ProjType)) //如果返回true则关闭客户端对应弹幕
                {
                    args.Handled = true;
                    args.Proj.ProjType = 0;
                    args.Player.SendPacket(args.Proj);
                }
            }
        }
        public static void OnProjDestroy(BEventArgs.ProjDestroyEventArgs args)
        {
            if (args.Player.RelesedProjs.Where(p => p.Proj.ProjSlot == args.KillProj.ProjSlot).FirstOrDefault() is { } projInfo)
            {
                projInfo.FromWeapon.OnProjDestroy(args.Player, args.KillProj);
                args.Player.RelesedProjs.Remove(projInfo);
            }
        }
        #endregion

        #region 物品操作
        private static void FillInventory(this BPlayer plr)
        {
            var slotPacket = new SyncEquipment()
            {
                ItemType = FillItem,
                PlayerSlot = plr.Index,
                Prefix = 80,
                Stack = 1,
            };
            List<Packet> packetData = new();
            plr.TrPlayer.inventory.ForEach((item, i) =>
            {
                if (i < 50 && (item is null || item?.type == 0))
                {
                    plr.TsPlayer.TPlayer.inventory[i] ??= new();
                    plr.TsPlayer.TPlayer.inventory[i].SetDefaults(FillItem);
                    plr.TsPlayer.TPlayer.inventory[i].prefix = 80;
                    slotPacket.ItemSlot = (short)i;
                    packetData.Add(slotPacket);
                }
            });
            plr.SendPackets(packetData);
        }
        private static void RemoveFillItems(this BPlayer plr)
        {
            var slotPacket = new SyncEquipment()
            {
                ItemType = 0,
                PlayerSlot = plr.Index,
                Prefix = 0,
                Stack = 0,
            };
            List<Packet> packetData = new();
            plr.TsPlayer?.TPlayer.inventory.ForEach((item, i) =>
            {
                slotPacket.ItemSlot = (short)i;
                packetData.Add(slotPacket);
            });
            plr.SendPackets(packetData);
        }
        private static void SpawnBWeapon(this BPlayer plr, BaseBWeapon weapon, int slot)
        {
            plr.TrPlayer.inventory[slot] ??= TShock.Utils.GetItemById(weapon.ItemID);
            plr.TrPlayer.inventory[slot].SetDefaults(weapon.ItemID);
            plr.TrPlayer.inventory[slot].prefix = (byte)weapon.Prefix;
            plr.TrPlayer.inventory[slot].stack = weapon.Stack; //将玩家背标目标位置更改为指定物品

            var itemID = 400 - slot;
            plr.SendPacket(new InstancedItem()
            {
                ItemSlot = (short)itemID,
                Owner = 0,
                Prefix = (byte)weapon.Prefix,
                ItemType = (short)weapon.ItemID,
                Position = plr.TrPlayer.position,
                Stack = (short)weapon.Stack,
                Velocity = default
            }); //生成普通物品

            var packet = weapon.TweakePacket;
            packet.ItemSlot = (short)itemID;
            plr.SendPacket(packet); //转换为自定义物品

            plr.RemoveItem(slot, false); //移除旧的物品
        }
        private static void ChangeSingleItemToBWeapon(this BPlayer plr, BaseBWeapon weapon, int slot)
        {
            if (!plr.IsCustomWeaponMode || plr.IsChangingWeapon)
                return;
            plr.IsChangingWeapon = true;

            plr.FillInventory(); //先填满没东西的格子
            plr.SpawnBWeapon(weapon, slot);
            plr.RemoveFillItems(); //清理占位物品

            Task.Delay(200).Wait();
            plr.IsChangingWeapon = false;
        }
        private static void ChangeItemsToBWeapon(this BPlayer plr)
        {
            if (!plr.IsCustomWeaponMode || plr.IsChangingWeapon)
                return;
            plr.IsChangingWeapon = true;

            plr.FillInventory(); //先填满没东西的格子
            for (int i = 49; i >= 0; i--)
            {
                var item = plr.TsPlayer?.TPlayer?.inventory[i];
                if (BWeapons.Where(w => w.Equals(item)).FirstOrDefault() is { } bweapon)
                    plr.SpawnBWeapon(bweapon, i);
                Task.Delay(10).Wait();
            }
            plr.RemoveFillItems(); //清理占位物品

            Task.Delay(200).Wait();
            plr.IsChangingWeapon = false;
        }
        private static void BackToNormalItem(this BPlayer plr)
        {
            plr.TrPlayer.inventory.ForEach((item, i) =>
            {
                if (BWeapons.Where(w => w.Equals(item)).Any())
                    plr.SendPacket(new SyncEquipment()
                    {
                        ItemSlot = (short)i,
                        ItemType = (short)item.type,
                        PlayerSlot = plr.Index,
                        Prefix = item.prefix,
                        Stack = (short)item.stack
                    });
            });
        }
        public static void ChangeCustomWeaponMode(this BPlayer plr, bool? enable = null)
        {
            var oldMode = plr.IsCustomWeaponMode;
            plr.IsCustomWeaponMode = enable ?? !plr.IsCustomWeaponMode;
            plr.TsPlayer.SetBuff(BuffID.Webbed, 60, true); //冻结
            plr.TsPlayer.SetBuff(BuffID.Stoned, 60, true); //石化
            if (oldMode != plr.IsCustomWeaponMode && !plr.IsChangingWeapon)
            {
                plr.SendPacket(BUtils.GetCurrentWorldData(true));
                if (plr.IsCustomWeaponMode)
                {
                    plr.Weapons = (from w in BWeapons select (BaseBWeapon)Activator.CreateInstance(w.GetType(), null)).ToArray(); //给玩家生成武器对象
                    Task.Run(plr.ChangeItemsToBWeapon);
                }
                else
                {
                    plr.Weapons = null;
                    plr.RelesedProjs.ForEach(r => r.Proj.Inactive());
                    plr.RelesedProjs.Clear();
                    plr.BackToNormalItem();
                }
            }
        }
        #endregion
    }
}
