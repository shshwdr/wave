using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Puzzle管理器 - 管理puzzle的加载和保存
/// </summary>
public class PuzzleManager : Singleton<PuzzleManager>
{
    private const string PUZZLE_FOLDER = "puzzle";
    
    /// <summary>
    /// 从Resources/puzzle/加载puzzle文件
    /// </summary>
    public int[,] LoadPuzzle(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return null;
            
        TextAsset puzzleFile = Resources.Load<TextAsset>($"{PUZZLE_FOLDER}/{identifier}");
        if (puzzleFile == null)
        {
            Debug.LogWarning($"Puzzle file not found: {PUZZLE_FOLDER}/{identifier}");
            return null;
        }
        
        return ParsePuzzleFile(puzzleFile.text);
    }
    
    /// <summary>
    /// 解析puzzle文件内容（6x8的int数组）
    /// 格式：每行8个数字，共6行，用空格或逗号分隔
    /// </summary>
    private int[,] ParsePuzzleFile(string content)
    {
        int[,] puzzle = new int[8, 6]; // x, y
        
        string[] lines = content.Split('\n');
        int y = 0;
        
        foreach (string line in lines)
        {
            if (y >= 6) break;
            
            string trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine)) continue;
            
            // 支持空格或逗号分隔
            string[] values = trimmedLine.Split(new char[] { ' ', ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            
            for (int x = 0; x < Mathf.Min(8, values.Length); x++)
            {
                if (int.TryParse(values[x], out int colorValue))
                {
                    puzzle[x, y] = colorValue;
                }
            }
            
            y++;
        }
        
        return puzzle;
    }
    
    /// <summary>
    /// 保存puzzle到Resources/puzzle/（使用时间戳作为文件名）
    /// </summary>
    public void SavePuzzle(int[,] puzzle, string folderPath = null)
    {
        if (puzzle == null || puzzle.GetLength(0) != 8 || puzzle.GetLength(1) != 6)
        {
            Debug.LogError("Invalid puzzle format!");
            return;
        }
        
        // 生成时间戳文件名
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"{timestamp}.txt";
        
        // 确定保存路径
        string savePath;
        if (string.IsNullOrEmpty(folderPath))
        {
            // 保存到Assets/Resources/puzzle/
            savePath = Path.Combine(Application.dataPath, "Resources", PUZZLE_FOLDER, fileName);
        }
        else
        {
            savePath = Path.Combine(folderPath, fileName);
        }
        
        // 确保目录存在
        string directory = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        // 生成文件内容
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int y = 0; y < 6; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                sb.Append(puzzle[x, y]);
                if (x < 7) sb.Append(" ");
            }
            if (y < 5) sb.AppendLine();
        }
        
        // 写入文件
        File.WriteAllText(savePath, sb.ToString());
        Debug.Log($"Puzzle saved to: {savePath}");
        
        #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
        #endif
    }
    
    /// <summary>
    /// 加载Resources/puzzle/中的第一个puzzle文件
    /// </summary>
    public int[,] LoadFirstPuzzle()
    {
        // 在编辑器中，从Resources文件夹查找
        #if UNITY_EDITOR
        string resourcesPath = Path.Combine(Application.dataPath, "Resources", PUZZLE_FOLDER);
        if (Directory.Exists(resourcesPath))
        {
            string[] files = Directory.GetFiles(resourcesPath, "*.txt");
            if (files.Length > 0)
            {
                string fileName = Path.GetFileNameWithoutExtension(files[0]);
                return LoadPuzzle(fileName);
            }
        }
        #endif
        
        // 运行时从Resources加载
        TextAsset[] puzzleFiles = Resources.LoadAll<TextAsset>(PUZZLE_FOLDER);
        if (puzzleFiles != null && puzzleFiles.Length > 0)
        {
            return ParsePuzzleFile(puzzleFiles[0].text);
        }
        
        Debug.LogWarning("No puzzle files found!");
        return null;
    }
}










