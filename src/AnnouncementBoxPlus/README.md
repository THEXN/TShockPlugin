# AnnouncementBoxPlus

- 作者: Cai
- 出处: 本仓库
- 广播盒加强以及管理控制

## 介绍

1. 广播盒内插占位符和格式化   
2. 可以添加编辑广播盒的权限   
3. 可以开关广播盒   
4. 可以设置广播盒有效范围(单位:像素)   


## 配置
> 配置文件位置：tshock/AnnouncementBoxPlus.json
```json5
{
  "禁用广播盒": false,
  "广播内容仅触发者可见": true,
  "广播范围(像素)(0为无限制)": 50,
  "启用广播盒权限(AnnouncementBoxPlus.Edit)": true,
  "启用插件广播盒发送格式": false,
  "广播盒发送格式": "%当前时间% %玩家组名% %玩家名%:%内容% #详细可查阅文档",
  "启用广播盒占位符(详细查看文档)": true
}
```
## 命令

| 命令名     |   说明   |
|---------|:------:|
| /reload | 重载配置文件 |

## 广播盒格式占位符

| 占位符    |   说明   |
|--------|:------:|
| %玩家组名% | 玩家组的名字 |
| %玩家名%  |  玩家名字  |
| %当前时间% | 当前现实时间 |
| %内容%   | 广播盒原内容 |

## 广播盒内容占位符

| 占位符         |      说明       |
|-------------|:-------------:|
| %玩家组名%      |    玩家组的名字     |
| %玩家名%       |     玩家名字      |
| %当前时间%      |    当前现实时间     |
| %当前服务器在线人数% |  获取当前服务器在线人数  |
| %渔夫任务鱼名称%   |    渔夫任务鱼名称    |
| %渔夫任务鱼ID%   |    渔夫任务鱼ID    |
| %渔夫任务鱼地点%   |    渔夫任务鱼地点    |
| %地图名称%      |    当前地图名称     |
| %玩家血量%      |     玩家血量      |
| %玩家魔力%      |     玩家魔力      |
| %玩家血量最大值%   |    玩家血量最大值    |
| %玩家魔力最大值%   |    玩家魔力最大值    |
| %玩家幸运值%     |     玩家幸运值     |
| %玩家X坐标%     |    玩家图格X坐标    |
| %玩家Y坐标%     |    玩家图格Y坐标    |
| %玩家所处区域%    | 玩家所处区域Region  |
| %玩家死亡状态%    |    玩家死亡状态     |
| %当前环境%      |    玩家当前环境     |
| %服务器在线列表%   | 服务器在线列表(/who) |
| %渔夫任务鱼完成%   |  玩家是否完成渔夫任务鱼  |

## 更新日志

### v1.0.5
- 重构渔夫任务点提取逻辑
- 添加 GetString

### v1.0.1 
- 完善卸载函数

## 反馈
- 优先发issued -> 共同维护的插件库：https://github.com/UnrealMultiple/TShockPlugin
- 次优先：TShock官方群：816771079
- 大概率看不到但是也可以：国内社区trhub.cn ，bbstr.net , tr.monika.love
