using UnityEngine;
using UnityEditor;
using System.IO;
using System;

/// <summary>
/// 编辑器脚本：用于在Unity编辑器中快速截图
/// 快捷键：A键（可在运行时和编辑时使用）
/// </summary>
public class ScreenshotEditor
{
    
    private const string SCREENSHOT_FOLDER = "Screenshots";
    
    /// <summary>
    /// 获取截图保存文件夹路径（在项目根目录下）
    /// </summary>
    private static string GetScreenshotFolderPath()
    {
        // 获取项目根目录（Assets的父目录）
        string projectPath = Application.dataPath;
        string projectRoot = Directory.GetParent(projectPath).FullName;
        return Path.Combine(projectRoot, SCREENSHOT_FOLDER);
    }
    
    /// <summary>
    /// 生成带时间戳的文件名
    /// </summary>
    private static string GenerateFileName()
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        return $"Screenshot_{timestamp}.png";
    }
    
    /// <summary>
    /// 截图功能（快捷键：A键）
    /// </summary>
    [MenuItem("Tools/Take Screenshot _a", false, 1)]
    public static void TakeScreenshot()
    {
        string folderPath = GetScreenshotFolderPath();
        
        // 确保文件夹存在
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            Debug.Log($"创建截图文件夹: {folderPath}");
        }
        
        // 生成文件名
        string fileName = GenerateFileName();
        string filePath = Path.Combine(folderPath, fileName);
        
        // 执行截图
        ScreenCapture.CaptureScreenshot(filePath);
        
        // 刷新资源数据库（让Unity识别新文件）
        AssetDatabase.Refresh();
        
        Debug.Log($"截图已保存: {filePath}");
        EditorUtility.DisplayDialog("截图成功", $"截图已保存到:\n{filePath}", "确定");
    }
    
    /// <summary>
    /// 打开截图文件夹
    /// </summary>
    [MenuItem("Tools/Open Screenshot Folder", false, 2)]
    public static void OpenScreenshotFolder()
    {
        string folderPath = GetScreenshotFolderPath();
        
        // 如果文件夹不存在，先创建
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        
        // 在文件管理器中打开文件夹
        EditorUtility.RevealInFinder(folderPath);
    }
}

