﻿using Newtonsoft.Json;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TShockAPI;
using static AIChatPlugin.Configuration;

namespace AIChatPlugin;
internal class Utils
{
    #region 问题审核
    public static readonly Dictionary<int, List<string>> playerContexts = new();
    public static readonly Dictionary<int, bool> isProcessing = new();
    public static DateTime lastCmdTime = DateTime.MinValue;
    public const int cooldownDuration = 5;
    public static void ChatWithAI(TSPlayer player, string question)
    {
        var playerIndex = player.Index;
        if (isProcessing.ContainsKey(playerIndex) && isProcessing[playerIndex])
        {
            player.SendErrorMessage("[i:1344]有其他玩家在询问问题，请排队[i:1344]");
            return;
        }
        if ((DateTime.Now - lastCmdTime).TotalSeconds < cooldownDuration)
        {
            var remainingTime = cooldownDuration - (int)(DateTime.Now - lastCmdTime).TotalSeconds;
            player.SendErrorMessage($"[i:1344]请耐心等待{remainingTime}秒后再输入![i:1344]");
            return;
        }
        if (string.IsNullOrWhiteSpace(question))
        {
            player.SendErrorMessage("[i:1344]您的问题不能为空，请输入您想询问的内容！[i:1344]");
            return;
        }
        lastCmdTime = DateTime.Now;
        player.SendSuccessMessage("[i:1344]正在处理您的请求，请稍候...[i:1344]");
        isProcessing[playerIndex] = true;
        Task.Run(async () =>
        {
            try
            {
                await ProcessAIChat(player, question);
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[AIChatPlugin] 处理`{player.Name}`的请求时发生错误！详细信息：{ex.Message}");
                if (player.RealPlayer)
                {
                    player.SendErrorMessage("[AIChatPlugin] 处理请求时发生错误！详细信息请查看日志");
                }
            }
            finally
            {
                isProcessing[playerIndex] = false;
            }
        });
    }
    #endregion
    #region 请求处理
    public static async Task ProcessAIChat(TSPlayer player, string question)
    {
        try
        {
            var cleanedQuestion = CleanMessage(question);
            var context = GetContext(player.Index);
            var formattedContext = context.Count > 0 ? "上下文信息:\n" + string.Join("\n", context) + "\n\n" : "";
            using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(Config.AITimeoutPeriod) };
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer 742701d3fea4bed898578856989cb03c.5mKVzv5shSIqkkS7");
            var tools = new List<object>()
            {
                new
                {
                    type = "web_search",
                    web_search = new
                    {
                        enable = true,
                        search_query = question
                    }
                }
            };
            var requestBody = new
            {
                model = "glm-4-flash",
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = formattedContext + $"（设定：{Config.AISettings}）请您引用以上的上下文信息回答现在的问题（必须不允许复读,如复读请岔开话题,不允许继续下去）：\n那，" + question
                    }
                },
                tools
            };
            var response = await client.PostAsync("https://open.bigmodel.cn/api/paas/v4/chat/completions",
            new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json"));
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<AIResponse>(jsonResponse);
                var taskId = result?.Id ?? "Not provided";
                TShock.Log.Debug($"[AIChatPlugin] Dialogue ID：{taskId}");
                if (result != null && result.Choices != null && result.Choices.Length > 0)
                {
                    var firstChoice = result.Choices[0];
                    var responseMessage = firstChoice.Message.Content;
                    responseMessage = CleanMessage(responseMessage);
                    if (responseMessage.Length > Config.AIAnswerWordsLimit)
                    {
                        responseMessage = TruncateMessage(responseMessage);
                    }
                    var formattedQuestion = FormatMessage(question);
                    var formattedResponse = FormatMessage(responseMessage);
                    StringBuilder broadcastMessageBuilder = new();
                    broadcastMessageBuilder.AppendFormat("[i:267][c/FFD700:{0}]\n", player.Name);
                    broadcastMessageBuilder.AppendFormat("[i:149][c/00FF00:提问: {0}]\n", formattedQuestion);
                    broadcastMessageBuilder.AppendLine("[c/A9A9A9:============================]");
                    broadcastMessageBuilder.AppendFormat("[i:4805][c/FF00FF:{0}]\n", Config.AIName);
                    broadcastMessageBuilder.AppendFormat("[i:149][c/FF4500:回答:] {0}\n", formattedResponse);
                    broadcastMessageBuilder.AppendLine("[c/A9A9A9:============================]");
                    var broadcastMessage = broadcastMessageBuilder.ToString();
                    TSPlayer.All.SendInfoMessage(broadcastMessage);
                    TShock.Log.ConsoleInfo(broadcastMessage);
                    AddToContext(player.Index, question, true);
                    AddToContext(player.Index, responseMessage, false);
                }
                else
                {
                    player.SendErrorMessage("[AIChatPlugin] 很抱歉，这次未获得有效的AI响应");
                }
            }
            else
            {
                TShock.Log.ConsoleError($"[AIChatPlugin] AI未能及时响应，状态码：{response.StatusCode}");
                if (player.RealPlayer)
                {
                    player.SendErrorMessage("[AIChatPlugin] AI未能及时响应！详细信息请查看日志");
                }
            }
        }
        catch (TaskCanceledException)
        {
            player.SendErrorMessage("[AIChatPlugin] 请求超时！");
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[AIChatPlugin] 出现错误！详细信息：{ex.Message}");
            if (player.RealPlayer)
            {
                player.SendErrorMessage("[AIChatPlugin] 出现错误！详细信息请查看日志");
            }
        }
    }
    public class AIResponse
    {
        public Choice[] Choices { get; set; } = Array.Empty<Choice>();
        public string? Id { get; set; }
    }
    public class Choice
    {
        public Message Message { get; set; } = new Message();
    }
    public class Message
    {
        public string Content { get; set; } = string.Empty;
    }
    #endregion
    #region 历史限制
    public static void AddToContext(int playerId, string message, bool isUserMessage)
    {
        if (!playerContexts.ContainsKey(playerId))
        {
            playerContexts[playerId] = new List<string>();
        }
        var taggedMessage = isUserMessage ? $"问题：{message}" : $"回答：{message}";
        if (playerContexts[playerId].Count >= Config.AIContextuallimitations)
        {
            playerContexts[playerId].RemoveAt(0);
        }
        playerContexts[playerId].Add(taggedMessage);
    }
    public static List<string> GetContext(int playerId)
    {
        return playerContexts.ContainsKey(playerId) ? playerContexts[playerId] : new List<string>();
    }
    public static void AIclear(CommandArgs args)
    {
        if (playerContexts.Count == 0)
        {
            args.Player.SendInfoMessage("[AIChatPlugin] 当前没有任何人的上下文记录");
        }
        else
        {
            playerContexts.Clear();
            args.Player.SendSuccessMessage("[AIChatPlugin] 所有人的上下文已清除");
        }
    }
    #endregion
    #region 回答优限
    public static string TruncateMessage(string message)
    {
        if (message.Length <= Config.AIAnswerWordsLimit)
        {
            return message;
        }
        var enumerator = StringInfo.GetTextElementEnumerator(message);
        StringBuilder truncated = new();
        var count = 0;
        while (enumerator.MoveNext())
        {
            var textElement = enumerator.GetTextElement();
            if (truncated.Length + textElement.Length > Config.AIAnswerWordsLimit)
            {
                break;
            }
            truncated.Append(textElement);
            count++;
        }
        if (count == 0 || truncated.Length >= Config.AIAnswerWordsLimit)
        {
            truncated.Append($"\n\n[i:1344]超出字数限制{Config.AIAnswerWordsLimit}已截断！[i:1344]");
        }
        return truncated.ToString();
    }
    public static string FormatMessage(string message)
    {
        StringBuilder formattedMessage = new();
        var enumerator = StringInfo.GetTextElementEnumerator(message);
        var currentLength = 0;
        while (enumerator.MoveNext())
        {
            var textElement = enumerator.GetTextElement();
            if (currentLength + textElement.Length > Config.AIAnswerWithLinebreaks)
            {
                if (formattedMessage.Length > 0)
                {
                    formattedMessage.AppendLine();
                }
                currentLength = 0;
            }
            formattedMessage.Append(textElement);
            currentLength += textElement.Length;
        }
        return formattedMessage.ToString();
    }
    public static string CleanMessage(string message)
    {
        return Regex.IsMatch(message, @"[\uD800-\uDBFF][\uDC00-\uDFFF]") ? string.Empty : message;
    }
    #endregion
}