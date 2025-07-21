using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Tile_Entities;
using TShockAPI;
using TShockAPI.DB;
using WorldEdit.Expressions;

namespace WorldEdit;

public static class Tools
{
    internal const int BUFFER_SIZE = 1048576;

    internal static int MAX_UNDOS;

    private static int TranslateTempCounter = 0;
    private static readonly Random rnd = new Random();
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    public static bool Translate(string path, bool logError, string? tempCopyPath = null)
    {
        // �Ƴ���������� `text`��ֱ��ʹ����ʱ·��
        var tempPath = tempCopyPath ?? Path.Combine("worldedit", $"temp-{rnd.Next()}-{Interlocked.Increment(ref TranslateTempCounter)}.dat");
        File.Copy(path, tempPath, true); // �򻯲�����

        var flag = true;
        try
        {
            LoadWorldDataOld(path).Write(path);
        }
        catch (Exception ex) // �޸������� `value` Ϊ��ͨ�õ� `ex`
        {
            if (logError)
            {
                TShock.Log.ConsoleError($"[WorldEdit] File '{path}' could not be converted to Terraria v1.4:\n{ex}");
            }
            flag = false;
        }

        if (!flag)
        {
            File.Copy(tempPath, path, true); // �򻯲�����
        }

        File.Delete(tempPath); // ֱ��ʹ����ʱ·������
        return flag;
    }

    public static bool InMapBoundaries(int X, int Y)
    {
        return X >= 0 && Y >= 0 && X < Main.maxTilesX && Y < Main.maxTilesY;
    }

    public static string GetClipboardPath(int accountID)
    {
        return Path.Combine("worldedit", $"clipboard-{Main.worldID}-{accountID}.dat");
    }

    public static bool IsCorrectName(string name)
    {
        // �Ƴ������ `(char c)` �������������������ƶϣ�
        return name.All(c => !InvalidFileNameChars.Contains(c));
    }

    // ���·������Ż�����������߼�����
    public static List<int> GetColorID(string color)
    {
        if (int.TryParse(color, out var result) && result >= 0 && result < 32) // �ϲ���������
        {
            return new List<int> { result };
        }

        var list = new List<int>();
        foreach (var color2 in WorldEdit.Colors)
        {
            if (color2.Key == color)
            {
                return new List<int> { color2.Value };
            }
            if (color2.Key.StartsWith(color))
            {
                list.Add(color2.Value);
            }
        }
        return list;
    }

    public static List<int> GetTileID(string tile)
    {
        if (int.TryParse(tile, out var result) && result >= 0 && result < 693) // �ϲ���������
        {
            return new List<int> { result };
        }

        var list = new List<int>();
        foreach (var tile2 in WorldEdit.Tiles)
        {
            if (tile2.Key == tile)
            {
                return new List<int> { tile2.Value };
            }
            if (tile2.Key.StartsWith(tile))
            {
                list.Add(tile2.Value);
            }
        }
        return list;
    }

    public static List<int> GetWallID(string wall)
    {
        if (int.TryParse(wall, out var result) && result >= 0 && result < 347) // �ϲ���������
        {
            return new List<int> { result };
        }

        var list = new List<int>();
        foreach (var wall2 in WorldEdit.Walls)
        {
            if (wall2.Key == wall)
            {
                return new List<int> { wall2.Value };
            }
            if (wall2.Key.StartsWith(wall))
            {
                list.Add(wall2.Value);
            }
        }
        return list;
    }

    public static int GetSlopeID(string slope)
    {
        if (int.TryParse(slope, out var result) && result >= 0 && result < 6) // �ϲ���������
        {
            return result;
        }
        return WorldEdit.Slopes.TryGetValue(slope, out var value) ? value : -1; // �Ƴ����� `if` ��
    }

    public static bool HasClipboard(int accountID)
    {
        return File.Exists(GetClipboardPath(accountID));
    }

    public static Rectangle ReadSize(Stream stream)
    {
        // �Ƴ���������������ִ������
        using var binaryReader = new BinaryReader(stream);
        return new Rectangle(binaryReader.ReadInt32(), binaryReader.ReadInt32(), binaryReader.ReadInt32(), binaryReader.ReadInt32());
    }

    public static Rectangle ReadSize(string path)
    {
        return ReadSize(File.Open(path, FileMode.Open));
    }

    public static WorldSectionData LoadWorldData(Stream stream)
    {
        int x;
        int y;
        int num;
        int num2;
        using (var binaryReader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
        {
            x = binaryReader.ReadInt32();
            y = binaryReader.ReadInt32();
            num = binaryReader.ReadInt32();
            num2 = binaryReader.ReadInt32();
        }
        using var binaryReader2 = new BinaryReader(new BufferedStream(new GZipStream(stream, CompressionMode.Decompress), 1048576));
        var worldSectionData = new WorldSectionData(num, num2)
        {
            X = x,
            Y = y
        };
        for (var i = 0; i < num; i++)
        {
            for (var j = 0; j < num2; j++)
            {
                worldSectionData.Tiles[i, j] = (ITile) (object) binaryReader2.ReadTile();
            }
        }
        try
        {
            var num3 = binaryReader2.ReadInt32();
            worldSectionData.Signs = new WorldSectionData.SignData[num3];
            for (var k = 0; k < num3; k++)
            {
                worldSectionData.Signs[k] = WorldSectionData.SignData.Read(binaryReader2);
            }
            var num4 = binaryReader2.ReadInt32();
            worldSectionData.Chests = new WorldSectionData.ChestData[num4];
            for (var l = 0; l < num4; l++)
            {
                worldSectionData.Chests[l] = WorldSectionData.ChestData.Read(binaryReader2);
            }
            var num5 = binaryReader2.ReadInt32();
            worldSectionData.ItemFrames = new WorldSectionData.DisplayItemData[num5];
            for (var m = 0; m < num5; m++)
            {
                worldSectionData.ItemFrames[m] = WorldSectionData.DisplayItemData.Read(binaryReader2);
            }
        }
        catch (EndOfStreamException)
        {
        }
        try
        {
            var num6 = binaryReader2.ReadInt32();
            worldSectionData.LogicSensors = new WorldSectionData.LogicSensorData[num6];
            for (var n = 0; n < num6; n++)
            {
                worldSectionData.LogicSensors[n] = WorldSectionData.LogicSensorData.Read(binaryReader2);
            }
            var num7 = binaryReader2.ReadInt32();
            worldSectionData.TrainingDummies = new WorldSectionData.PositionData[num7];
            for (var num8 = 0; num8 < num7; num8++)
            {
                worldSectionData.TrainingDummies[num8] = WorldSectionData.PositionData.Read(binaryReader2);
            }
        }
        catch (EndOfStreamException)
        {
        }
        try
        {
            var num9 = binaryReader2.ReadInt32();
            worldSectionData.WeaponsRacks = new WorldSectionData.DisplayItemData[num9];
            for (var num10 = 0; num10 < num9; num10++)
            {
                worldSectionData.WeaponsRacks[num10] = WorldSectionData.DisplayItemData.Read(binaryReader2);
            }
            var num11 = binaryReader2.ReadInt32();
            worldSectionData.TeleportationPylons = new WorldSectionData.PositionData[num11];
            for (var num12 = 0; num12 < num11; num12++)
            {
                worldSectionData.TeleportationPylons[num12] = WorldSectionData.PositionData.Read(binaryReader2);
            }
            var num13 = binaryReader2.ReadInt32();
            worldSectionData.DisplayDolls = new WorldSectionData.DisplayItemsData[num13];
            for (var num14 = 0; num14 < num13; num14++)
            {
                worldSectionData.DisplayDolls[num14] = WorldSectionData.DisplayItemsData.Read(binaryReader2);
            }
            var num15 = binaryReader2.ReadInt32();
            worldSectionData.HatRacks = new WorldSectionData.DisplayItemsData[num15];
            for (var num16 = 0; num16 < num15; num16++)
            {
                worldSectionData.HatRacks[num16] = WorldSectionData.DisplayItemsData.Read(binaryReader2);
            }
            var num17 = binaryReader2.ReadInt32();
            worldSectionData.FoodPlatters = new WorldSectionData.DisplayItemData[num17];
            for (var num18 = 0; num18 < num17; num18++)
            {
                worldSectionData.FoodPlatters[num18] = WorldSectionData.DisplayItemData.Read(binaryReader2);
            }
        }
        catch (EndOfStreamException)
        {
        }
        return worldSectionData;
    }

    public static WorldSectionData LoadWorldData(string path)
    {
        return LoadWorldData(File.Open(path, FileMode.Open));
    }

    internal static WorldSectionData LoadWorldDataOld(Stream stream)
    {
        using var binaryReader = new BinaryReader(new BufferedStream(new GZipStream(stream, CompressionMode.Decompress), 1048576));
        var x = binaryReader.ReadInt32();
        var y = binaryReader.ReadInt32();
        var num = binaryReader.ReadInt32();
        var num2 = binaryReader.ReadInt32();
        var worldSectionData = new WorldSectionData(num, num2)
        {
            X = x,
            Y = y
        };
        for (var i = 0; i < num; i++)
        {
            for (var j = 0; j < num2; j++)
            {
                worldSectionData.Tiles[i, j] = (ITile) (object) binaryReader.ReadTileOld();
            }
        }
        try
        {
            var num3 = binaryReader.ReadInt32();
            worldSectionData.Signs = new WorldSectionData.SignData[num3];
            for (var k = 0; k < num3; k++)
            {
                worldSectionData.Signs[k] = WorldSectionData.SignData.Read(binaryReader);
            }
            var num4 = binaryReader.ReadInt32();
            worldSectionData.Chests = new WorldSectionData.ChestData[num4];
            for (var l = 0; l < num4; l++)
            {
                worldSectionData.Chests[l] = WorldSectionData.ChestData.Read(binaryReader);
            }
            var num5 = binaryReader.ReadInt32();
            worldSectionData.ItemFrames = new WorldSectionData.DisplayItemData[num5];
            for (var m = 0; m < num5; m++)
            {
                worldSectionData.ItemFrames[m] = WorldSectionData.DisplayItemData.Read(binaryReader);
            }
        }
        catch (EndOfStreamException)
        {
        }
        try
        {
            var num6 = binaryReader.ReadInt32();
            worldSectionData.LogicSensors = new WorldSectionData.LogicSensorData[num6];
            for (var n = 0; n < num6; n++)
            {
                worldSectionData.LogicSensors[n] = WorldSectionData.LogicSensorData.Read(binaryReader);
            }
            var num7 = binaryReader.ReadInt32();
            worldSectionData.TrainingDummies = new WorldSectionData.PositionData[num7];
            for (var num8 = 0; num8 < num7; num8++)
            {
                worldSectionData.TrainingDummies[num8] = WorldSectionData.PositionData.Read(binaryReader);
            }
        }
        catch (EndOfStreamException)
        {
        }
        return worldSectionData;
    }

    internal static WorldSectionData LoadWorldDataOld(string path)
    {
        return LoadWorldDataOld(File.Open(path, FileMode.Open));
    }

    public static Tile ReadTile(this BinaryReader reader)
    {
        var val = new Tile
        {
            sTileHeader = (ushort) reader.ReadInt16(),
            bTileHeader = reader.ReadByte(),
            bTileHeader2 = reader.ReadByte()
        };
        if (val.active())
        {
            val.type = reader.ReadUInt16();
            if (Main.tileFrameImportant[val.type])
            {
                val.frameX = reader.ReadInt16();
                val.frameY = reader.ReadInt16();
            }
        }
        val.wall = reader.ReadUInt16();
        val.liquid = reader.ReadByte();
        return val;
    }

    private static Tile ReadTileOld(this BinaryReader reader)
    {
        var val = new Tile
        {
            sTileHeader = (ushort) reader.ReadInt16(),
            bTileHeader = reader.ReadByte(),
            bTileHeader2 = reader.ReadByte()
        };
        if (val.active())
        {
            val.type = reader.ReadUInt16();
            if (val.type != 49 && Main.tileFrameImportant[val.type])
            {
                val.frameX = reader.ReadInt16();
                val.frameY = reader.ReadInt16();
            }
        }
        val.wall = reader.ReadByte();
        val.liquid = reader.ReadByte();
        return val;
    }

    public static NetItem ReadNetItem(this BinaryReader reader)
    {
        return new NetItem(reader.ReadInt32(), reader.ReadInt32(), reader.ReadByte());
    }

    internal static NetItem[] ReadNetItems(this BinaryReader reader)
    {
        var num = reader.ReadInt32();
        var array = new NetItem[num];
        for (var i = 0; i < num; i++)
        {
            array[i] = reader.ReadNetItem();
        }
        return array;
    }

    public static int ClearSigns(int x, int y, int x2, int y2, bool emptyOnly)
    {
        var num = 0;
        var area = new Rectangle(x, y, x2 - x, y2 - y);

        foreach (var sign in Main.sign)
        {
            if (sign != null && area.Contains(sign.x, sign.y) && (!emptyOnly || string.IsNullOrWhiteSpace(sign.text)))
            {
                num++;
                Sign.KillSign(sign.x, sign.y);
            }
        }

        return num;
    }

    public static int ClearChests(int x, int y, int x2, int y2, bool emptyOnly)
    {
        var num = 0;
        var area = new Rectangle(x, y, x2 - x, y2 - y);

        foreach (var chest in Main.chest)
        {
            if (chest != null && area.Contains(chest.x, chest.y) &&
               (!emptyOnly || chest.item.All(i => i == null || i.netID == 0)))
            {
                num++;
                Chest.DestroyChest(chest.x, chest.y);
            }
        }

        return num;
    }

    public static void ClearObjects(int x, int y, int x2, int y2)
    {
        ClearSigns(x, y, x2, y2, emptyOnly: false);
        ClearChests(x, y, x2, y2, emptyOnly: false);
        for (var i = x; i <= x2; i++)
        {
            for (var j = y; j <= y2; j++)
            {
                if (TEItemFrame.Find(i, j) != -1)
                {
                    TEItemFrame.Kill(i, j);
                }
                if (TELogicSensor.Find(i, j) != -1)
                {
                    TELogicSensor.Kill(i, j);
                }
                if (TETrainingDummy.Find(i, j) != -1)
                {
                    TETrainingDummy.Kill(i, j);
                }
                if (TEWeaponsRack.Find(i, j) != -1)
                {
                    TEWeaponsRack.Kill(i, j);
                }
                if (TETeleportationPylon.Find(i, j) != -1)
                {
                    TETeleportationPylon.Kill(i, j);
                }
                if (TEDisplayDoll.Find(i, j) != -1)
                {
                    TEDisplayDoll.Kill(i, j);
                }
                if (TEHatRack.Find(i, j) != -1)
                {
                    TEHatRack.Kill(i, j);
                }
                if (TEFoodPlatter.Find(i, j) != -1)
                {
                    TEFoodPlatter.Kill(i, j);
                }
            }
        }
    }

    public static void LoadWorldSection(string path, int? X = null, int? Y = null, bool Tiles = true)
    {
        LoadWorldSection(LoadWorldData(path), X, Y, Tiles);
    }

    public static void LoadWorldSection(WorldSectionData Data, int? X = null, int? Y = null, bool Tiles = true)
    {
        var num = X ?? Data.X;
        var num2 = Y ?? Data.Y;
        if (Tiles)
        {
            for (var i = 0; i < Data.Width; i++)
            {
                for (var j = 0; j < Data.Height; j++)
                {
                    var num3 = i + num;
                    var num4 = j + num2;
                    if (InMapBoundaries(num3, num4))
                    {
                        Main.tile[num3, num4] = Data.Tiles[i, j];
                        Main.tile[num3, num4].skipLiquid(true);
                    }
                }
            }
        }
        ClearObjects(num, num2, num + Data.Width, num2 + Data.Height);
        foreach (var sign in Data.Signs)
        {
            var num5 = Sign.ReadSign(sign.X + num, sign.Y + num2, true);
            if (num5 != -1 && InMapBoundaries(sign.X, sign.Y))
            {
                Sign.TextSign(num5, sign.Text);
            }
        }
        foreach (var itemFrame in Data.ItemFrames)
        {
            var num6 = TEItemFrame.Place(itemFrame.X + num, itemFrame.Y + num2);
            if (num6 != -1)
            {
                var val = (TEItemFrame) TileEntity.ByID[num6];
                if (InMapBoundaries(((TileEntity) val).Position.X, ((TileEntity) val).Position.Y))
                {
                    val.item = new Item();
                    var item = val.item;
                    var item2 = itemFrame.Item;
                    item.netDefaults(item2.NetId);
                    var item3 = val.item;
                    item2 = itemFrame.Item;
                    item3.stack = item2.Stack;
                    var item4 = val.item;
                    item2 = itemFrame.Item;
                    item4.prefix = item2.PrefixId;
                }
            }
        }
        foreach (var chest in Data.Chests)
        {
            var num7 = chest.X + num;
            var num8 = chest.Y + num2;
            int num9;
            if ((num9 = Chest.FindChest(num7, num8)) == -1 && (num9 = Chest.CreateChest(num7, num8, -1)) == -1)
            {
                continue;
            }
            var val2 = Main.chest[num9];
            if (InMapBoundaries(chest.X, chest.Y))
            {
                for (var k = 0; k < chest.Items.Length; k++)
                {
                    var netItem = chest.Items[k];
                    var val3 = new Item();
                    val3.netDefaults(netItem.NetId);
                    val3.stack = netItem.Stack;
                    val3.prefix = netItem.PrefixId;
                    Main.chest[num9].item[k] = val3;
                }
            }
        }
        foreach (var logicSensor in Data.LogicSensors)
        {
            var num10 = TELogicSensor.Place(logicSensor.X + num, logicSensor.Y + num2);
            if (num10 != -1)
            {
                var val4 = (TELogicSensor) TileEntity.ByID[num10];
                if (InMapBoundaries(((TileEntity) val4).Position.X, ((TileEntity) val4).Position.Y))
                {
                    val4.logicCheck = logicSensor.Type;
                }
            }
        }
        foreach (var trainingDummy in Data.TrainingDummies)
        {
            var num11 = TETrainingDummy.Place(trainingDummy.X + num, trainingDummy.Y + num2);
            if (num11 != -1)
            {
                var val5 = (TETrainingDummy) TileEntity.ByID[num11];
                if (InMapBoundaries(((TileEntity) val5).Position.X, ((TileEntity) val5).Position.Y))
                {
                    val5.npc = -1;
                }
            }
        }
        foreach (var weaponsRack in Data.WeaponsRacks)
        {
            var num12 = TEWeaponsRack.Place(weaponsRack.X + num, weaponsRack.Y + num2);
            if (num12 != -1)
            {
                var val6 = (TEWeaponsRack) TileEntity.ByID[num12];
                if (InMapBoundaries(((TileEntity) val6).Position.X, ((TileEntity) val6).Position.Y))
                {
                    val6.item = new Item();
                    var item5 = val6.item;
                    var item2 = weaponsRack.Item;
                    item5.netDefaults(item2.NetId);
                    var item6 = val6.item;
                    item2 = weaponsRack.Item;
                    item6.stack = item2.Stack;
                    var item7 = val6.item;
                    item2 = weaponsRack.Item;
                    item7.prefix = item2.PrefixId;
                }
            }
        }
        foreach (var teleportationPylon in Data.TeleportationPylons)
        {
            TETeleportationPylon.Place(teleportationPylon.X + num, teleportationPylon.Y + num2);
        }
        foreach (var displayDoll in Data.DisplayDolls)
        {
            var num13 = TEDisplayDoll.Place(displayDoll.X + num, displayDoll.Y + num2);
            if (num13 == -1)
            {
                continue;
            }
            var val7 = (TEDisplayDoll) TileEntity.ByID[num13];
            if (InMapBoundaries(((TileEntity) val7).Position.X, ((TileEntity) val7).Position.Y))
            {
                val7._items = (Item[]) (object) new Item[displayDoll.Items.Length];
                for (var l = 0; l < displayDoll.Items.Length; l++)
                {
                    var netItem2 = displayDoll.Items[l];
                    var val8 = new Item();
                    val8.netDefaults(netItem2.NetId);
                    val8.stack = netItem2.Stack;
                    val8.prefix = netItem2.PrefixId;
                    val7._items[l] = val8;
                }
                val7._dyes = (Item[]) (object) new Item[displayDoll.Dyes.Length];
                for (var m = 0; m < displayDoll.Dyes.Length; m++)
                {
                    var netItem3 = displayDoll.Dyes[m];
                    var val9 = new Item();
                    val9.netDefaults(netItem3.NetId);
                    val9.stack = netItem3.Stack;
                    val9.prefix = netItem3.PrefixId;
                    val7._dyes[m] = val9;
                }
            }
        }
        foreach (var hatRack in Data.HatRacks)
        {
            var num14 = TEHatRack.Place(hatRack.X + num, hatRack.Y + num2);
            if (num14 == -1)
            {
                continue;
            }
            var val10 = (TEHatRack) TileEntity.ByID[num14];
            if (InMapBoundaries(((TileEntity) val10).Position.X, ((TileEntity) val10).Position.Y))
            {
                val10._items = (Item[]) (object) new Item[hatRack.Items.Length];
                for (var n = 0; n < hatRack.Items.Length; n++)
                {
                    var netItem4 = hatRack.Items[n];
                    var val11 = new Item();
                    val11.netDefaults(netItem4.NetId);
                    val11.stack = netItem4.Stack;
                    val11.prefix = netItem4.PrefixId;
                    val10._items[n] = val11;
                }
                val10._dyes = (Item[]) (object) new Item[hatRack.Dyes.Length];
                for (var num15 = 0; num15 < hatRack.Dyes.Length; num15++)
                {
                    var netItem5 = hatRack.Dyes[num15];
                    var val12 = new Item();
                    val12.netDefaults(netItem5.NetId);
                    val12.stack = netItem5.Stack;
                    val12.prefix = netItem5.PrefixId;
                    val10._dyes[num15] = val12;
                }
            }
        }
        foreach (var foodPlatter in Data.FoodPlatters)
        {
            var num16 = TEFoodPlatter.Place(foodPlatter.X + num, foodPlatter.Y + num2);
            if (num16 != -1)
            {
                var val13 = (TEFoodPlatter) TileEntity.ByID[num16];
                if (InMapBoundaries(((TileEntity) val13).Position.X, ((TileEntity) val13).Position.Y))
                {
                    val13.item = new Item();
                    var item8 = val13.item;
                    var item2 = foodPlatter.Item;
                    item8.netDefaults(item2.NetId);
                    var item9 = val13.item;
                    item2 = foodPlatter.Item;
                    item9.stack = item2.Stack;
                    var item10 = val13.item;
                    item2 = foodPlatter.Item;
                    item10.prefix = item2.PrefixId;
                }
            }
        }
        ResetSection(num, num2, num + Data.Width, num2 + Data.Height);
    }

    public static void PrepareUndo(int x, int y, int x2, int y2, TSPlayer plr)
    {
        if (WorldEdit.Config.DisableUndoSystemForUnrealPlayers && !plr.RealPlayer)
        {
            return;
        }
        if (WorldEdit.Database.GetSqlType() == SqlType.Mysql)
        {
            WorldEdit.Database.Query("INSERT IGNORE INTO WorldEdit VALUES (@0, -1, -1)", plr.Account.ID);
        }
        else
        {
            WorldEdit.Database.Query("INSERT OR IGNORE INTO WorldEdit VALUES (@0, 0, 0)", plr.Account.ID);
        }
        WorldEdit.Database.Query("UPDATE WorldEdit SET RedoLevel = -1 WHERE Account = @0", plr.Account.ID);
        WorldEdit.Database.Query("UPDATE WorldEdit SET UndoLevel = UndoLevel + 1 WHERE Account = @0", plr.Account.ID);
        var num = 0;
        using (var queryResult = WorldEdit.Database.QueryReader("SELECT UndoLevel FROM WorldEdit WHERE Account = @0", plr.Account.ID))
        {
            if (queryResult.Read())
            {
                num = queryResult.Get<int>("UndoLevel");
            }
        }
        var path = Path.Combine("worldedit", $"undo-{Main.worldID}-{plr.Account.ID}-{num}.dat");
        SaveWorldSection(x, y, x2, y2, path);
        foreach (var item in Directory.EnumerateFiles("worldedit", $"redo-{Main.worldID}-{plr.Account.ID}-*.dat"))
        {
            File.Delete(item);
        }
        File.Delete(Path.Combine("worldedit", $"undo-{Main.worldID}-{plr.Account.ID}-{num - MAX_UNDOS}.dat"));
    }

    public static bool Redo(int accountID)
    {
        if (WorldEdit.Config.DisableUndoSystemForUnrealPlayers && accountID == 0)
        {
            return false;
        }
        var num = 0;
        var num2 = 0;
        using (var queryResult = WorldEdit.Database.QueryReader("SELECT RedoLevel, UndoLevel FROM WorldEdit WHERE Account = @0", accountID))
        {
            if (!queryResult.Read())
            {
                return false;
            }
            num = queryResult.Get<int>("RedoLevel") - 1;
            num2 = queryResult.Get<int>("UndoLevel") + 1;
        }
        if (num < -1)
        {
            return false;
        }
        var path = Path.Combine("worldedit", $"redo-{Main.worldID}-{accountID}-{num + 1}.dat");
        WorldEdit.Database.Query("UPDATE WorldEdit SET RedoLevel = @0 WHERE Account = @1", num, accountID);
        if (!File.Exists(path))
        {
            return false;
        }
        var path2 = Path.Combine("worldedit", $"undo-{Main.worldID}-{accountID}-{num2}.dat");
        WorldEdit.Database.Query("UPDATE WorldEdit SET UndoLevel = @0 WHERE Account = @1", num2, accountID);
        var val = ReadSize(path);
        SaveWorldSection(Math.Max(0, val.X), Math.Max(0, val.Y), Math.Min(val.X + val.Width - 1, Main.maxTilesX - 1), Math.Min(val.Y + val.Height - 1, Main.maxTilesY - 1), path2);
        LoadWorldSection(path);
        File.Delete(path);
        return true;
    }

    public static void ResetSection(int x, int y, int x2, int y2)
    {
        var sectionX = Netplay.GetSectionX(x);
        var sectionX2 = Netplay.GetSectionX(x2);
        var sectionY = Netplay.GetSectionY(y);
        var sectionY2 = Netplay.GetSectionY(y2);
        foreach (var item in Netplay.Clients.Where((RemoteClient s) => s.IsActive))
        {
            var length = item.TileSections.GetLength(0);
            var length2 = item.TileSections.GetLength(1);
            for (var i = sectionX; i <= sectionX2; i++)
            {
                for (var j = sectionY; j <= sectionY2; j++)
                {
                    if (i >= 0 && j >= 0 && i < length && j < length2)
                    {
                        item.TileSections[i, j] = false;
                    }
                }
            }
        }
    }

    public static void SaveWorldSection(int x, int y, int x2, int y2, string path)
    {
        SaveWorldSection(x, y, x2, y2).Write(path);
    }

    public static void Write(this BinaryWriter writer, ITile tile)
    {
        writer.Write(tile.sTileHeader);
        writer.Write(tile.bTileHeader);
        writer.Write(tile.bTileHeader2);
        if (tile.active())
        {
            writer.Write(tile.type);
            if (Main.tileFrameImportant[tile.type])
            {
                writer.Write(tile.frameX);
                writer.Write(tile.frameY);
            }
        }
        writer.Write(tile.wall);
        writer.Write(tile.liquid);
    }

    public static void Write(this BinaryWriter writer, NetItem item)
    {
        writer.Write(item.NetId);
        writer.Write(item.Stack);
        writer.Write(item.PrefixId);
    }

    internal static void Write(this BinaryWriter writer, NetItem[] items)
    {
        writer.Write(items.Length);
        foreach (var item in items)
        {
            writer.Write(item);
        }
    }

    public static WorldSectionData SaveWorldSection(int x, int y, int x2, int y2)
    {
        var width = x2 - x + 1;
        var height = y2 - y + 1;
        var worldSectionData = new WorldSectionData(width, height)
        {
            X = x,
            Y = y
        };
        for (var i = x; i <= x2; i++)
        {
            for (var j = y; j <= y2; j++)
            {
                worldSectionData.ProcessTile(Main.tile[i, j], i - x, j - y);
            }
        }
        return worldSectionData;
    }

    public static bool Undo(int accountID)
    {
        if (WorldEdit.Config.DisableUndoSystemForUnrealPlayers && accountID == 0)
        {
            return false;
        }
        int num;
        int num2;
        using (var queryResult = WorldEdit.Database.QueryReader("SELECT RedoLevel, UndoLevel FROM WorldEdit WHERE Account = @0", accountID))
        {
            if (!queryResult.Read())
            {
                return false;
            }
            num = queryResult.Get<int>("RedoLevel") + 1;
            num2 = queryResult.Get<int>("UndoLevel") - 1;
        }
        if (num2 < -1)
        {
            return false;
        }
        var path = Path.Combine("worldedit", $"undo-{Main.worldID}-{accountID}-{num2 + 1}.dat");
        WorldEdit.Database.Query("UPDATE WorldEdit SET UndoLevel = @0 WHERE Account = @1", num2, accountID);
        if (!File.Exists(path))
        {
            return false;
        }
        var path2 = Path.Combine("worldedit", $"redo-{Main.worldID}-{accountID}-{num}.dat");
        WorldEdit.Database.Query("UPDATE WorldEdit SET RedoLevel = @0 WHERE Account = @1", num, accountID);
        var val = ReadSize(path);
        SaveWorldSection(Math.Max(0, val.X), Math.Max(0, val.Y), Math.Min(val.X + val.Width - 1, Main.maxTilesX - 1), Math.Min(val.Y + val.Height - 1, Main.maxTilesY - 1), path2);
        LoadWorldSection(path);
        File.Delete(path);
        return true;
    }

    public static bool CanSet(bool Tile, ITile tile, int type, Selection selection, Expression expression, MagicWand magicWand, int x, int y, TSPlayer player)
    {
        return (!Tile) ? (tile.wall != type && selection(x, y, player) && expression.Evaluate(tile) && magicWand.InSelection(x, y)) : (((type >= 0 && (!tile.active() || tile.type != type)) || (type == -1 && tile.active()) || (type == -2 && (tile.liquid == 0 || tile.liquidType() != 1)) || (type == -3 && (tile.liquid == 0 || tile.liquidType() != 2)) || (type == -4 && (tile.liquid == 0 || tile.liquidType() != 0))) && selection(x, y, player) && expression.Evaluate(tile) && magicWand.InSelection(x, y));
    }

    public static WEPoint[] CreateLine(int x1, int y1, int x2, int y2)
    {
        var list = new List<WEPoint> { new WEPoint((short) x1, (short) y1) };
        var dx = x2 - x1;
        var dy = y2 - y1;
        var stepX = dx > 0 ? 1 : dx < 0 ? -1 : 0;
        var stepY = dy > 0 ? 1 : dy < 0 ? -1 : 0;
        var absDx = Math.Abs(dx);
        var absDy = Math.Abs(dy);

        var xStep = stepX;
        var yStep = stepY;
        var error = absDx > absDy ? absDx : absDy;
        var delta = absDx > absDy ? absDy : absDx;
        var remainder = error / 2;

        var x = x1;
        var y = y1;

        for (var i = 0; i < error; i++)
        {
            remainder -= delta;
            if (remainder < 0)
            {
                remainder += error;
                x += stepX;
                y += stepY;
            }
            else
            {
                x += absDx > absDy ? stepX : 0;
                y += absDy > absDx ? stepY : 0;
            }
            list.Add(new WEPoint((short) x, (short) y));
        }

        return list.ToArray();
    }
    public static bool InEllipse(int x1, int y1, int x2, int y2, int x, int y)
    {
        var center = new Vector2((x1 + x2) / 2f, (y1 + y2) / 2f);
        var radiusX = Math.Abs((x2 - x1) / 2f);
        var radiusY = Math.Abs((y2 - y1) / 2f);

        var a = radiusX;
        var b = radiusY;

        if (radiusY > radiusX)
        {
            a = radiusY;
            b = radiusX;
        }

        return InEllipse(x1, y1, center.X, center.Y, a, b, x, y);
    }

    private static bool InEllipse(int x1, int y1, float cX, float cY, float rMax, float rMin, int x, int y)
    {
        return (Math.Pow((float) x - cX - (float) x1, 2.0) / Math.Pow(rMax, 2.0)) + (Math.Pow((float) y - cY - (float) y1, 2.0) / Math.Pow(rMin, 2.0)) <= 1.0;
    }

    public static WEPoint[] CreateEllipseOutline(int x1, int y1, int x2, int y2)
    {
        var radiusX = Math.Abs((x2 - x1) / 2f);
        var radiusY = Math.Abs((y2 - y1) / 2f);

        var a = radiusX;
        var b = radiusY;

        if (radiusY > radiusX)
        {
            a = radiusY;
            b = radiusX;
        }

        var list = new List<WEPoint>();
        for (var i = x1; i <= x1 + (int) radiusX; i++)
        {
            for (var j = y1; j <= y1 + (int) radiusY; j++)
            {
                if (!InEllipse(x1, y1, radiusX, radiusY, a, b, i, j))
                {
                    continue;
                }

                if (list.Count > 0)
                {
                    var wEPoint = list.Last();
                    var num4 = j;
                    while (wEPoint.Y - num4 >= 1)
                    {
                        addPoint(list, x1, y1, x2, y2, i, num4++);
                    }
                }
                else
                {
                    var num5 = y1 + (int) radiusY - j;
                    if (num5 > 0)
                    {
                        var num6 = j;
                        while (num5-- >= 0)
                        {
                            addPoint(list, x1, y1, x2, y2, i, num6++);
                        }
                    }
                }

                addPoint(list, x1, y1, x2, y2, i, j);
                break;
            }
        }

        return list.ToArray();
    }

    private static void addPoint(List<WEPoint> points, int x1, int y1, int x2, int y2, int i, int j)
    {
        points.Add(new WEPoint((short) (x2 - i + x1), (short) j));
        points.Add(new WEPoint((short) i, (short) (y2 - j + y1)));
        points.Add(new WEPoint((short) (x2 - i + x1), (short) (y2 - j + y1)));
        points.Add(new WEPoint((short) i, (short) j));
    }

    public static WEPoint[,] CreateStatueText(string Text, int Width, int Height)
    {
        var array = new WEPoint[Width, Height];
        if (string.IsNullOrWhiteSpace(Text))
        {
            return array;
        }
        var list = new List<Tuple<WEPoint[,], int>>();
        var array2 = Text.ToLower().Replace("\\n", "\n").Split('\n');
        var num = 0;
        for (var i = 0; i < array2.Length; i++)
        {
            var tuple = CreateStatueRow(array2[i], Width, i == 0);
            if ((num += tuple.Item1.GetLength(1) + tuple.Item2) > Height)
            {
                break;
            }
            list.Add(tuple);
        }
        var num2 = 0;
        foreach (var item in list)
        {
            num2 += item.Item2;
            var length = item.Item1.GetLength(0);
            var length2 = item.Item1.GetLength(1);
            for (var j = 0; j < length; j++)
            {
                for (var k = 0; k < length2 && k + num2 <= Height; k++)
                {
                    array[j, k + num2] = item.Item1[j, k];
                }
            }
            num2 += length2;
        }
        return array;
    }

    private static Tuple<WEPoint[,], int> CreateStatueRow(string Row, int Width, bool FirstRow)
    {
        var tuple = RowSettings(Row, FirstRow);
        var array = new WEPoint[Width, tuple.Item4];
        var list = tuple.Item1.ToCharArray().ToList();
        var num = (int) Math.Ceiling((double) ((list.Count * 2) - Width) / 2.0);
        var num2 = 0;
        if (num > 0)
        {
            list.RemoveRange(list.Count - num, num);
        }
        if (tuple.Item2 == 1 && list.Count * 2 <= Width)
        {
            num2 = (Width - (list.Count * 2)) / 2;
        }
        else if (tuple.Item2 == 2 && list.Count * 2 <= Width)
        {
            num2 = Width - (list.Count * 2);
        }
        for (var i = 0; i < list.Count; i++)
        {
            var array2 = CreateStatueLetter(list[i]);
            for (var j = 0; j < 2 && j + num2 <= Width; j++)
            {
                for (var k = 0; k < tuple.Item4; k++)
                {
                    array[num2, k] = array2[j, k];
                }
                num2++;
            }
        }
        return new Tuple<WEPoint[,], int>(array, tuple.Item3);
    }

    private static Tuple<string, int, int, int> RowSettings(string Row, bool FirstRow)
    {
        var item = 0;
        var result = (!FirstRow) ? 1 : 0;
        var item2 = 3;
        while (Row.StartsWith("\\") && Row.Length > 1)
        {
            switch (char.ToLower(Row[1]))
            {
                case 'l':
                    item = 0;
                    Row = Row[2..];
                    break;
                case 'm':
                    item = 1;
                    Row = Row[2..];
                    break;
                case 'r':
                    item = 2;
                    Row = Row[2..];
                    break;
                case 'c':
                    item2 = 2;
                    Row = Row[2..];
                    break;
                case 's':
                {
                    Row = Row[2..];
                    var text = "";
                    var num = 0;
                    while (Row.Length > num + 1 && char.IsDigit(Row[num]))
                    {
                        text += Row[num++];
                    }
                    Row = Row[num..];
                    if (!int.TryParse(text, out result) || result < 0)
                    {
                        result = !FirstRow ? 1 : 0;
                    }
                    break;
                }
            }
        }
        return new Tuple<string, int, int, int>(Row, item, result, item2);
    }

    private static WEPoint[,] CreateStatueLetter(char Letter)
    {
        var array = new WEPoint[2, 3];
        short num = 0;
        short num2;
        if (Letter > '/' && Letter < ':')
        {
            num2 = (short) ((Letter - 48) * 36);
        }
        else
        {
            if (Letter <= '`' || Letter >= '{')
            {
                return array;
            }
            num2 = (short) ((Letter - 87) * 36);
        }
        for (var num3 = num2; num3 <= num2 + 18; num3 += 18)
        {
            var num4 = 0;
            for (short num5 = 0; num5 <= 36; num5 += 18)
            {
                array[num, num4++] = new WEPoint(num3, num5);
            }
            num++;
        }
        return array;
    }
}
