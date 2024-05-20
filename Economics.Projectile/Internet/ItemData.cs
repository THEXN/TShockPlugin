﻿using Newtonsoft.Json;

namespace Economics.Projectile;
public class ItemData
{
    [JsonProperty("弹幕数据")]
    public List<ProjectileReplace> ProjData { get; set; } = new();

    [JsonProperty("物品使用弹药")]
    public bool UseAmmo { get; set; } = false;

    [JsonProperty("备注")]
    public string Name { get; set; } = string.Empty;
}
