using System;
using System.Collections.Generic;
using StackExchange.Redis;

namespace MyServer // 修改命名空间
{
    public class DBManager
    {
        private static ConnectionMultiplexer? redis;
        private static IDatabase? db;

        // 连接状态
        public static bool IsConnected => redis != null && redis.IsConnected;

        // 连接到Redis服务器
        public static bool Connect(string connectionString = "localhost:6379")
        {
            try
            {
                redis = ConnectionMultiplexer.Connect(connectionString);
                db = redis.GetDatabase();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[数据库] 成功连接到Redis服务器");
                Console.ResetColor();
                return true;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[数据库] 连接失败: {ex.Message}");
                Console.ResetColor();
                return false;
            }
        }

        // 断开服务器连接
        public static void Disconnect()
        {
            try
            {
                redis?.Close();
                redis?.Dispose();
                redis = null;
                db = null;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[数据库] 已断开Redis服务器连接");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[数据库] 断开连接时出错: {ex.Message}");
                Console.ResetColor();
            }
        }

        // 存储道具信息
        public static bool StoreItem(string itemId, string itemName, int quantity, string description = "")
        {
            if (db == null)
            {
                Console.WriteLine("[数据库] 未连接到数据库");
                return false;
            }

            try
            {
                HashEntry[] itemFields = {
                    new HashEntry("id", itemId),
                    new HashEntry("name", itemName),
                    new HashEntry("quantity", quantity),
                    new HashEntry("description", description),
                    new HashEntry("createTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                };

                string hashKey = $"item:{itemId}";
                db.HashSet(hashKey, itemFields);

                db.SetAdd("player:inventory", itemId);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[数据库] 道具存储成功: {itemName} (ID: {itemId})");
                Console.ResetColor();
                return true;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[数据库] 存储道具失败: {ex.Message}");
                Console.ResetColor();
                return false;
            }
        }

        // 获取单个道具信息
        public static Dictionary<string, string>? GetItem(string itemId)
        {
            if (db == null)
            {
                Console.WriteLine("[数据库] 未连接到数据库");
                return null;
            }

            try
            {
                string hashKey = $"item:{itemId}";
                if (!db.KeyExists(hashKey))
                {
                    Console.WriteLine($"[数据库] 道具不存在: {itemId}");
                    return null;
                }

                HashEntry[] entries = db.HashGetAll(hashKey);
                var itemInfo = new Dictionary<string, string>();

                foreach (var entry in entries)
                {
                    itemInfo[entry.Name] = entry.Value;
                }

                Console.WriteLine($"[数据库] 获取道具成功: {itemId}");
                return itemInfo;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[数据库] 获取道具失败: {ex.Message}");
                Console.ResetColor();
                return null;
            }
        }

        // 获取背包中所有道具信息
        public static List<Dictionary<string, string>> GetAllItems()
        {
            var items = new List<Dictionary<string, string>>();

            if (db == null)
            {
                Console.WriteLine("[数据库] 未连接到数据库");
                return items;
            }

            try
            {
                var itemIds = db.SetMembers("player:inventory");

                foreach (var itemId in itemIds)
                {
                    var itemInfo = GetItem(itemId!);
                    if (itemInfo != null)
                    {
                        items.Add(itemInfo);
                    }
                }

                Console.WriteLine($"[数据库] 获取背包道具成功，共 {items.Count} 个道具");
                return items;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[数据库] 获取背包道具失败: {ex.Message}");
                Console.ResetColor();
                return items;
            }
        }

        // 更新道具数量
        public static bool UpdateItemQuantity(string itemId, int newQuantity)
        {
            if (db == null)
            {
                Console.WriteLine("[数据库] 未连接到数据库");
                return false;
            }

            try
            {
                string hashKey = $"item:{itemId}";
                if (!db.KeyExists(hashKey))
                {
                    Console.WriteLine($"[数据库] 道具不存在: {itemId}");
                    return false;
                }

                db.HashSet(hashKey, "quantity", newQuantity);
                db.HashSet(hashKey, "updateTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"[数据库] 道具数量更新成功: {itemId} -> {newQuantity}");
                Console.ResetColor();
                return true;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[数据库] 更新道具失败: {ex.Message}");
                Console.ResetColor();
                return false;
            }
        }

        // 删除道具
        public static bool DeleteItem(string itemId)
        {
            if (db == null)
            {
                Console.WriteLine("[数据库] 未连接到数据库");
                return false;
            }

            try
            {
                string hashKey = $"item:{itemId}";
                if (!db.KeyExists(hashKey))
                {
                    Console.WriteLine($"[数据库] 道具不存在: {itemId}");
                    return false;
                }

                db.KeyDelete(hashKey);
                db.SetRemove("player:inventory", itemId);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[数据库] 道具删除成功: {itemId}");
                Console.ResetColor();
                return true;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[数据库] 删除道具失败: {ex.Message}");
                Console.ResetColor();
                return false;
            }
        }
        // 清空所有道具数据（危险操作，需要确认）
        public static bool ClearAllItems()
        {
            if (db == null)
            {
                Console.WriteLine("[数据库] 未连接到数据库");
                return false;
            }

            try
            {
                var inventory = db.SetMembers("player:inventory");
                int deletedCount = 0;

                foreach (var itemId in inventory)
                {
                    db.KeyDelete($"item:{itemId}");
                    deletedCount++;
                }
                db.KeyDelete("player:inventory");

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[数据库] 已清空 {deletedCount} 个道具");
                Console.ResetColor();
                return true;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[数据库] 清空道具失败: {ex.Message}");
                Console.ResetColor();
                return false;
            }
        }
        // 初始化一些示例道具

        public static void InitializeSampleItems()
        {
            if (db == null)
            {
                Console.WriteLine("[数据库] 未连接到数据库");
                return;
            }

            try
            {
                // 只添加示例道具，不删除任何现有数据
                // 检查每个示例道具是否已存在，避免重复添加
                if (!db.KeyExists("item:item001"))
                {
                    StoreItem("item001", "治疗药水", 5, "恢复100点生命值");
                }
                if (!db.KeyExists("item:item002"))
                {
                    StoreItem("item002", "魔法药水", 3, "恢复50点魔法值");
                }
                if (!db.KeyExists("item:item003"))
                {
                    StoreItem("item003", "力量卷轴", 1, "暂时提升攻击力");
                }
                if (!db.KeyExists("item:item004"))
                {
                    StoreItem("item004", "传送卷轴", 2, "传送到最近的城市");
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("[数据库] 示例道具检查完成");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[数据库] 初始化示例道具失败: {ex.Message}");
                Console.ResetColor();
            }
        }
    }

}