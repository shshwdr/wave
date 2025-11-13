using UnityEngine;
using UnityEditor;
using System.IO;

public class SpriteSplitter
{
    [MenuItem("Tools/Split Sprites")]
    public static void SplitSprites()
    {
        string enemyPath = "Assets/Resources/enemy";
        
        if (!AssetDatabase.IsValidFolder(enemyPath))
        {
            Debug.LogError($"路径不存在: {enemyPath}");
            return;
        }

        int processedCount = 0;
        int errorCount = 0;

        // 遍历enemy文件夹下的所有文件夹
        string[] folders = AssetDatabase.GetSubFolders(enemyPath);
        
        foreach (string folder in folders)
        {
            string folderName = Path.GetFileName(folder);
            Debug.Log($"处理文件夹: {folderName}");
            
            // 获取文件夹中的所有png文件
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                {
                    ProcessTexture(assetPath, ref processedCount, ref errorCount);
                }
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"处理完成! 成功: {processedCount}, 失败: {errorCount}");
    }

    private static void ProcessTexture(string assetPath, ref int processedCount, ref int errorCount)
    {
        TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        
        if (textureImporter == null)
        {
            Debug.LogWarning($"无法获取TextureImporter: {assetPath}");
            errorCount++;
            return;
        }

        try
        {
            // 1. 设置sprite模式为multiple
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Multiple;

            // 2. 设置slice为128x128
            int spriteSize = 128;
            TextureImporterSettings textureSettings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(textureSettings);
            
            textureSettings.spriteMeshType = SpriteMeshType.Tight;
            textureSettings.spriteExtrude = 1;
            textureSettings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            
            textureImporter.SetTextureSettings(textureSettings);

            // 计算sprite数量
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture != null)
            {
                int width = texture.width;
                int height = texture.height;
                int cols = width / spriteSize;
                int rows = height / spriteSize;

                // 创建sprite meta data
                SpriteMetaData[] spriteMetaData = new SpriteMetaData[cols * rows];
                
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < cols; col++)
                    {
                        int index = row * cols + col;
                        spriteMetaData[index] = new SpriteMetaData
                        {
                            name = $"{Path.GetFileNameWithoutExtension(assetPath)}_{row}_{col}",
                            rect = new Rect(col * spriteSize, height - (row + 1) * spriteSize, spriteSize, spriteSize),
                            alignment = (int)SpriteAlignment.BottomCenter,
                            pivot = new Vector2(0.5f, 0f) // bottom pivot
                        };
                    }
                }

                textureImporter.spritesheet = spriteMetaData;
            }

            // 3. 应用更改
            EditorUtility.SetDirty(textureImporter);
            textureImporter.SaveAndReimport();
            
            processedCount++;
            Debug.Log($"已处理: {assetPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"处理失败 {assetPath}: {e.Message}");
            errorCount++;
        }
    }
}

