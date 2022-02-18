﻿using BossFramework.BAttributes;
using BossFramework.BModels;
using BossFramework.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BossFramework.BCore
{
    public static class CommandPlaceholder
    {
        public static List<PlaceholderInfo> Placeholders { get; private set; } = new();

        [AutoInit]
        private static void InitPlaceholder()
        {
            Placeholders = DBTools.GetAll<PlaceholderInfo>().ToList();
        }
    }
}
