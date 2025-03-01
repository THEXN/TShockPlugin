﻿using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rests;
using SixLabors.ImageSharp.Formats.Png;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace CaiBot;

internal static class CaiBotApi
{
    internal static bool IsWebsocketConnected =>
        Plugin.WebSocket.State == WebSocketState.Open;
    
    internal static readonly Dictionary<string,(DateTime,int)> WhiteListCaches = new();
    
    internal static async Task SendDateAsync(string message)
    {
        if (Plugin.DebugMode)
        {
            TShock.Log.ConsoleInfo($"[CaiAPI]发送BOT数据包：{message}");
        }

        var messageBytes = Encoding.UTF8.GetBytes(message);
        await Plugin.WebSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true,
            CancellationToken.None);
    }

    internal static async Task HandleMessageAsync(string receivedData)
    {
        var jsonObject = JObject.Parse(receivedData);
        var type = (string) jsonObject["type"]!;
        RestObject result;
        switch (type)
        {
            case "delserver":
                TShock.Log.ConsoleInfo("[CaiAPI]BOT发送解绑命令...");
                Config.Settings.Token = string.Empty;
                Config.Settings.Write();
                Plugin.GenBindCode(EventArgs.Empty);
                Plugin.WebSocket.Dispose();
                break;
            case "hello":
                TShock.Log.ConsoleInfo("[CaiAPI]CaiBOT连接成功...");
                //发送服务器信息
                result = new RestObject
                {
                    { "type", "hello" },
                    { "tshock_version", TShock.VersionNum.ToString() },
                    { "plugin_version", Plugin.VersionNum },
                    { "terraria_version", Main.versionNumber },
                    { "cai_whitelist", Config.Settings.WhiteList },
                    { "os", RuntimeInformation.RuntimeIdentifier },
                    { "world", TShock.Config.Settings.UseServerName ? TShock.Config.Settings.ServerName : Main.worldName },
                    { "sync_group_chat", Config.Settings.SyncChatFromGroup },
                    { "sync_server_chat", Config.Settings.SyncChatFromServer },
                    { "group", (long) jsonObject["group"]! }
                };
                await SendDateAsync(JsonConvert.SerializeObject(result));
                break;
            case "groupid":
                var groupId = (long) jsonObject["groupid"]!;
                TShock.Log.ConsoleInfo($"[CaiAPI]群号获取成功: {groupId}");
                if (Config.Settings.GroupNumber != 0L)
                {
                    TShock.Log.ConsoleWarn($"[CaiAPI]检测到你在配置文件中已设置群号[{Config.Settings.GroupNumber}],BOT自动获取的群号将被忽略！");
                }
                else
                {
                    Config.Settings.GroupNumber = groupId;
                }

                break;
            case "cmd":
                var cmd = (string) jsonObject["cmd"]!;
                CaiBotPlayer tr = new ();
                Commands.HandleCommand(tr, cmd);
                TShock.Utils.SendLogs($"[CaiBot] `{(string) jsonObject["at"]!}`来自群`{(long) jsonObject["group"]!}`执行了: {(string) jsonObject["cmd"]!}", Color.PaleVioletRed);
                result = new RestObject { { "type", "cmd" }, { "result", string.Join('\n', tr.GetCommandOutput()) }, { "at", (string) jsonObject["at"]! }, { "group", (long) jsonObject["group"]! } };
                await SendDateAsync(JsonConvert.SerializeObject(result));
                break;
            case "online":
                var onlineResult = new StringBuilder();
                if (TShock.Utils.GetActivePlayerCount() == 0)
                {
                    onlineResult.Append("没有玩家在线捏...");
                }
                else
                {
                    onlineResult.AppendLine($"在线玩家({TShock.Utils.GetActivePlayerCount()}/{TShock.Config.Settings.MaxSlots})");
                    onlineResult.Append(string.Join(',', TShock.Players.Where(x => x is { Active: true }).Select(x => x.Name)));
                }

                var onlineProcessList = Utils.GetOnlineProcessList();
                var onlineProcess = !onlineProcessList.Any() ? "已毕业" : onlineProcessList.ElementAt(0) + "前";
                result = new RestObject
                {
                    { "type", "online" },
                    { "result", onlineResult.ToString() }, // “怎么有种我是男的的感觉” -- 张芷睿大人 (24.12.22)
                    { "worldname", string.IsNullOrEmpty(Main.worldName) ? "地图还没加载捏~" : Main.worldName },
                    { "process", onlineProcess },
                    { "group", (long) jsonObject["group"]! }
                };
                await SendDateAsync(JsonConvert.SerializeObject(result));
                break;
            case "process":
                result = new RestObject
                {
                    { "type", "process" }, 
                    { "process", Utils.GetProcessList() },
                    { "kill_counts", Utils.GetKillCountList()},
                    { "worldname", Main.worldName },
                    { "drunk_world", Main.drunkWorld },
                    { "zenith_world" , Main.zenithWorld },
                    { "world_icon", Utils.GetWorldIconName()},
                    { "group", (long) jsonObject["group"]! } 
                    
                };
                await SendDateAsync(JsonConvert.SerializeObject(result));
                break;
            case "whitelist":
                var name = (string) jsonObject["name"]!;
                var code = (int) jsonObject["code"]!;
                
                WhiteListCaches[name] = (DateTime.Now, code);
                
                if (Login.CheckWhite(name, code))
                {
                    var plr = TShock.Players.FirstOrDefault(x => x.Name == name);
                    if (plr != null)
                    {
                        Login.HandleLogin(plr);
                    }
                }
                break;
            case "selfkick":
                name = (string) jsonObject["name"]!;
                var playerList2 = TSPlayer.FindByNameOrID("tsn:" + name);
                if (playerList2.Count == 0)
                {
                    return;
                }

                playerList2[0].Kick("在群中使用自踢命令.", true, saveSSI: true);
                break;
            case "lookbag":
                name = (string) jsonObject["name"]!;
                var playerList3 = TSPlayer.FindByNameOrID("tsn:" + name);
                if (playerList3.Count != 0)
                {
                    var plr = playerList3[0].TPlayer;
                    var lookOnlineResult = LookBag.LookOnline(plr);
                    result = new RestObject
                    {
                        { "type", "lookbag" },
                        { "name", lookOnlineResult.Name },
                        { "exist", 1 },
                        { "life", $"{lookOnlineResult.Health}/{lookOnlineResult.MaxHealth}" },
                        { "mana", $"{lookOnlineResult.Mana}/{lookOnlineResult.MaxMana}" },
                        { "quests_completed", lookOnlineResult.QuestsCompleted },
                        { "inventory", lookOnlineResult.ItemList },
                        { "buffs", lookOnlineResult.Buffs },
                        { "enhances", lookOnlineResult.Enhances },
                        { "economic", EconomicData.GetEconomicData(lookOnlineResult.Name) },
                        { "group", (long) jsonObject["group"]! }
                    };
                }
                else
                {
                    var acc = TShock.UserAccounts.GetUserAccountByName(name);
                    if (acc == null)
                    {
                        result = new RestObject { { "type", "lookbag" }, { "exist", 0 }, { "group", (long) jsonObject["group"]! } };
                        await SendDateAsync(JsonConvert.SerializeObject(result));
                        return;
                    }

                    var data = TShock.CharacterDB.GetPlayerData(new TSPlayer(-1), acc.ID);
                    if (data == null)
                    {
                        result = new RestObject { { "type", "lookbag" }, { "exist", 0 }, { "group", (long) jsonObject["group"]! } };
                        await SendDateAsync(JsonConvert.SerializeObject(result));
                        return;
                    }

                    var lookOnlineResult = LookBag.LookOffline(acc, data);
                    result = new RestObject
                    {
                        { "type", "lookbag" },
                        { "name", lookOnlineResult.Name },
                        { "exist", 1 },
                        { "life", $"{lookOnlineResult.Health}/{lookOnlineResult.MaxHealth}" },
                        { "mana", $"{lookOnlineResult.Mana}/{lookOnlineResult.MaxMana}" },
                        { "quests_completed", lookOnlineResult.QuestsCompleted },
                        { "inventory", lookOnlineResult.ItemList },
                        { "buffs", lookOnlineResult.Buffs },
                        { "enhances", lookOnlineResult.Enhances },
                        { "economic", EconomicData.GetEconomicData(lookOnlineResult.Name) },
                        { "group", (long) jsonObject["group"]! }
                    };
                }

                await SendDateAsync(JsonConvert.SerializeObject(result));

                break;
            case "mappng":
                var bitmap = MapGenerator.CreateMapImg();
                using (MemoryStream ms = new ())
                {
                    await bitmap.SaveAsync(ms, new PngEncoder());
                    var imageBytes = ms.ToArray();
                    var base64 = Convert.ToBase64String(imageBytes);
                    result = new RestObject { { "type", "mappngV2" }, { "result", Utils.CompressBase64(base64) }, { "group", (long) jsonObject["group"]! } };
                }

                await SendDateAsync(JsonConvert.SerializeObject(result));
                break;
            case "mapfile":
                var mapfile = MapGenerator.CreateMapFile();
                result = new RestObject { { "type", "mapfileV2" }, { "base64", Utils.CompressBase64(mapfile.Item1) }, { "name", mapfile.Item2 }, { "group", (long) jsonObject["group"]! } };
                await SendDateAsync(JsonConvert.SerializeObject(result));
                break;
            case "worldfile":
                result = new RestObject { { "type", "worldfileV2" }, { "base64", Utils.CompressBase64(Utils.FileToBase64String(Main.worldPathName)) }, { "name", Path.GetFileName(Main.worldPathName)  }, { "group", (long) jsonObject["group"]! } };
                await SendDateAsync(JsonConvert.SerializeObject(result));
                break;
            case "pluginlist":
                var pluginList = ServerApi.Plugins.Select(p => new PluginInfo(p.Plugin.Name, p.Plugin.Description, p.Plugin.Author, p.Plugin.Version)).ToList();
                result = new RestObject { { "type", "pluginlist" }, { "plugins", pluginList }, { "group", (long) jsonObject["group"]! } };
                await SendDateAsync(JsonConvert.SerializeObject(result));
                break;
            case "chat": //"[群名{0}]玩家昵称{1}:内容{2}" 额外 {3}:群QQ号 {4}:发送者QQ
                var groupName = (string) jsonObject["group_name"]!;
                var nickname = (string) jsonObject["nickname"]!;
                var chatText = (string) jsonObject["chat_text"]!;
                var groupNumber = (long) jsonObject["group_id"]!;
                var senderId = (long) jsonObject["sender_id"]!;
                
                if (Config.Settings.GroupChatIgnoreUsers.Contains(senderId))
                {
                    break;
                }
                
                if (Config.Settings.CustomGroupName.TryGetValue(groupNumber, out var value))
                {
                    groupName = value;
                }

                TShock.Utils.Broadcast(string.Format(Config.Settings.GroupChatFormat, groupName, nickname, chatText, groupNumber, senderId), Color.White);
                break;
        }
    }
}