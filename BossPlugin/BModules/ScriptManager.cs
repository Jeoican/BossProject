﻿using BossPlugin.BAttributes;
using CSScriptLibrary;
using System;
using System.Collections.Generic;
using System.IO;

namespace BossPlugin.BModels
{
    public static class ScriptManager
    {
        public static string ScriptRootPath => Path.Combine(BInfo.FilePath, "Scripts");
        public static string MiniGameScriptPath => Path.Combine(ScriptRootPath, "MiniGames");
        /// <summary>
        /// 加载指定脚本文件
        /// </summary>
        /// <typeparam name="T">尝试加载为指定类型</typeparam>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public static T LoadSingleScript<T>(string filePath) where T : class
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"脚本文件 {filePath} 不存在");
            return CSScript.Evaluator.LoadCode<T>(File.ReadAllText(filePath));
        }
        /// <summary>
        /// 加载指定路径下的所有脚本文件
        /// </summary>
        /// <typeparam name="T">指定类型</typeparam>
        /// <param name="path">路径</param>
        /// <returns>脚本</returns>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public static T[] LoadScripts<T>(string path) where T : class
        {
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"路径 {path} 不存在");
            var scripts = new List<T>();
            Directory.GetFiles(MiniGameScriptPath).ForEach(f =>
            {
                try
                {
                    scripts.Add(LoadSingleScript<T>(f));
                }
                catch (Exception ex)
                {
                    BLog.Error(ex);
                }
            });
            return scripts.ToArray();
        }
        [AutoInit(order: 10)]
        private static void CheckPath()
        {
            if (!Directory.Exists(ScriptRootPath))
                Directory.CreateDirectory(ScriptRootPath);
            if (!Directory.Exists(MiniGameScriptPath))
                Directory.CreateDirectory(MiniGameScriptPath);
        }
    }
}
