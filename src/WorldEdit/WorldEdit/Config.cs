using System.IO;
using Newtonsoft.Json;

namespace WorldEdit;

/// <summary>
/// ����༭�����࣬�������ͳ־û�ϵͳ����
/// </summary>
public class Config
{
    /// <summary>
    /// ħ�������߿ɲ�������󷽿���������
    /// </summary>
    public int MagicWandTileLimit { get; set; } = 10000;

    /// <summary>
    /// �����������ʷ��¼����
    /// </summary>
    public int MaxUndoCount { get; set; } = 50;

    /// <summary>
    /// �Ƿ��δ��֤��ҽ��ó���ϵͳ
    /// </summary>
    public bool DisableUndoSystemForUnrealPlayers { get; set; } = false;

    /// <summary>
    /// �Ƿ���ԭ��ͼ�ļ���ǰ��Ӵ������û�ID
    /// </summary>
    public bool StartSchematicNamesWithCreatorUserID { get; set; } = false;

    /// <summary>
    /// ԭ��ͼ�ļ��洢�ļ���·��
    /// </summary>
    public string SchematicFolderPath { get; set; } = "schematics";

    /// <summary>
    /// ��ָ���ļ�·����ȡ����
    /// </summary>
    /// <param name="configFilePath">�����ļ�·��</param>
    /// <returns>����ʵ��</returns>
    public static Config Read(string configFilePath)
    {
        try
        {
            if (!File.Exists(configFilePath))
            {
                return new Config().Write(configFilePath);
            }

            var jsonContent = File.ReadAllText(configFilePath);
            return JsonConvert.DeserializeObject<Config>(jsonContent) ?? new Config();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"��ȡ�����ļ�ʧ��: {ex.Message}");
            return new Config();
        }
    }

    /// <summary>
    /// ����ǰ����д��ָ���ļ�
    /// </summary>
    /// <param name="configFilePath">�����ļ�·��</param>
    /// <returns>��ǰ����ʵ��</returns>
    public Config Write(string configFilePath)
    {
        try
        {
            // ȷ��Ŀ¼����
            var directory = Path.GetDirectoryName(configFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var jsonContent = JsonConvert.SerializeObject(
                this,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Include,
                    ObjectCreationHandling = ObjectCreationHandling.Replace
                });

            File.WriteAllText(configFilePath, jsonContent);
            return this;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"д�������ļ�ʧ��: {ex.Message}");
            return this;
        }
    }
}