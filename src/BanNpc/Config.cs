﻿using Newtonsoft.Json;
namespace BanNpc;


public class Config
{
    [JsonProperty("阻止怪物生成表")]
    public HashSet<int> Npcs { get; set; } = new();

    public static Config Read(string PATH)
    {
        return !File.Exists(PATH) ? new Config() : JsonConvert.DeserializeObject<Config>(File.ReadAllText(PATH)) ?? new();
    }
    public void Write(string Path)//给定路径进行写
    {
        File.WriteAllText(Path, JsonConvert.SerializeObject(this));
    }
}