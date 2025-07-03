﻿using Economics.Skill.JSInterpreter;
using Economics.Skill.Model.Options;
using Economics.Core.ConfigFiles;
using Newtonsoft.Json;
using TShockAPI;

namespace Economics.Skill.Model;

public class SkillContext
{
    [JsonProperty("名称")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("喊话")]
    public string Broadcast { get; set; } = string.Empty;

    [JsonProperty("技能唯一")]
    public bool SkillUnique { get; set; }

    [JsonProperty("全服唯一")]
    public bool SkillUniqueAll { get; set; }

    [JsonProperty("隐藏")]
    public bool Hidden { get; set; }

    [JsonProperty("技能价格")]
    public List<RedemptionRelationshipsOption> RedemptionRelationshipsOption { get; set; } = [];

    [JsonProperty("限制等级")]
    public List<string> LimitLevel { get; set; } = [];

    [JsonProperty("限制进度")]
    public List<string> LimitProgress { get; set; } = [];

    [JsonProperty("限制技能")]
    public List<int> LimitSkill { get; set; } = [];

    [JsonProperty("触发设置")]
    public SkillSparkOption SkillSpark { get; set; } = new();

    [JsonProperty("技能等级设置")]
    public Dictionary<int, List<RedemptionRelationshipsOption>> SkillLevelOptions { get; set; } = [];

    [JsonProperty("执行脚本")]
    public string? ExecuteScript
    {
        get => this.JsScript?.FilePathOrUri;
        set => this.JsScript = Set(value!);
    }

    [JsonIgnore]
    public JsScript? JsScript { get; set; }

    public static JsScript? Set(string path)
    {
        var jistScript = new JsScript(path);
        try
        {
            jistScript.Script = File.ReadAllText(Path.Combine(Interpreter.ScriptsDir, jistScript.FilePathOrUri));
        }
        catch (Exception ex)
        {
            TShock.Log.Error("无法加载{0}: {1}", path, ex.Message);
            return null;
        }
        ScriptContainer.PreprocessRequires(jistScript);
        return jistScript;
    }
}