using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SeniorProjectRefactored.Objects;
using SeniorProjectRefactored.Scenes;
using System.Text.Json;

public class AssetHandler
{
    private GraphicsDevice _graphicsDevice;
    private string _rootFolder;

    private readonly string[] _subfolders = { "Objects", "Scenes", "Stages" };

    public Dictionary<string, List<LoadedAsset>> AssetsByFolder { get; private set; }

    public AssetHandler(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;

        _rootFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "StageCue"
        );

        EnsureDirectoriesExist();
        CopyStarterAssetsIfNeeded();

        AssetsByFolder = new Dictionary<string, List<LoadedAsset>>();

        LoadAllAssets();
    }

    private void EnsureDirectoriesExist()
    {
        if (!Directory.Exists(_rootFolder))
            Directory.CreateDirectory(_rootFolder);

        foreach (var subfolder in _subfolders)
        {
            string subfolderPath = Path.Combine(_rootFolder, subfolder);
            if (!Directory.Exists(subfolderPath))
                Directory.CreateDirectory(subfolderPath);
        }
    }

    private void CopyStarterAssetsIfNeeded()
    {
        string docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string targetPath = Path.Combine(docsPath, "StageCue");

        bool hasAnyAssets = _subfolders.Any(sub =>
        {
            string subPath = Path.Combine(targetPath, sub);
            return Directory.Exists(subPath) && Directory.EnumerateFiles(subPath, "*", SearchOption.AllDirectories).Any();
        });

        if (hasAnyAssets)
            return;

        string starterAssetsPath = Path.Combine(AppContext.BaseDirectory, "StarterAssets");
        if (!Directory.Exists(starterAssetsPath))
        {
            Debug.WriteLine("StarterAssets folder missing in app directory.");
            return;
        }

        CopyDirectory(starterAssetsPath, targetPath);
    }

    private void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(targetDir, fileName);
            File.Copy(file, destFile, true);
        }

        foreach (string dir in Directory.GetDirectories(sourceDir))
        {
            string dirName = Path.GetFileName(dir);
            string destDir = Path.Combine(targetDir, dirName);
            CopyDirectory(dir, destDir);
        }
    }

    private void LoadAllAssets()
    {
        foreach (string dir in Directory.GetDirectories(_rootFolder, "*", SearchOption.TopDirectoryOnly))
        {
            string folderName = Path.GetFileName(dir);

            if (!_subfolders.Contains(folderName, StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Skipping unrecognized root folder: {dir}");
                continue;
            }

            bool isScenes = folderName.Equals("Scenes", StringComparison.OrdinalIgnoreCase);

            foreach (var file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories))
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();
                bool isJson = ext == ".json";
                bool isPng = ext == ".png";

                if ((isScenes && (isJson || isPng)) ||
                    (!isScenes && isPng))
                {
                    LoadAsset(file, isJson: isJson);
                }
            }
        }
    }

    private void LoadAsset(string file, bool isJson)
    {
        string folderPath = Path.GetDirectoryName(file) ?? _rootFolder;
        string relativeFolder = Path.GetRelativePath(_rootFolder, folderPath).Replace('\\', '/').TrimEnd('/');

        string relativeFilePath = Path.GetRelativePath(_rootFolder, file).Replace('\\', '/').TrimEnd('/');

        string nameNoExt = Path.GetFileNameWithoutExtension(file);

        AssetType type = AssetType.Unknown;
        if (relativeFolder.StartsWith("Objects", StringComparison.OrdinalIgnoreCase))
            type = isJson ? AssetType.Unknown : AssetType.Object;
        else if (relativeFolder.StartsWith("Scenes", StringComparison.OrdinalIgnoreCase))
            type = isJson ? AssetType.Scene : AssetType.Scene;
        else if (relativeFolder.StartsWith("Stages", StringComparison.OrdinalIgnoreCase))
            type = isJson ? AssetType.Unknown : AssetType.Stage;

        if (type == AssetType.Unknown)
        {
            Console.WriteLine($"Ignoring file in incorrect location: {file}");
            return;
        }

        Texture2D tex = null;
        if (!isJson)
        {
            using (var fs = File.OpenRead(file))
            {
                tex = Texture2D.FromStream(_graphicsDevice, fs);
            }
        }

        if (!AssetsByFolder.ContainsKey(relativeFolder))
            AssetsByFolder[relativeFolder] = new List<LoadedAsset>();

        var newAsset = new LoadedAsset
        {
            Name = nameNoExt,
            Path = file,
            RelativeFilePath = relativeFilePath,
            IsJson = isJson,
            Texture = tex,
            Type = type
        };

        AssetsByFolder[relativeFolder].Add(newAsset);
    }

    public LoadedAsset GetStageByName(string stageName)
    {
        return AssetsByFolder.SelectMany(kvp => kvp.Value).FirstOrDefault(a => a.Type == AssetType.Stage && !a.IsJson && a.Name.Equals(stageName, StringComparison.OrdinalIgnoreCase));
    }

    public LoadedAsset GetAssetByName(string nameNoExt)
    {
        return AssetsByFolder.SelectMany(kvp => kvp.Value).FirstOrDefault(a => a.Name.Equals(nameNoExt, StringComparison.OrdinalIgnoreCase));
    }

    public Texture2D LoadTextureFromRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        relativePath = relativePath.Replace('\\', '/');

        string folderKey = Path.GetDirectoryName(relativePath)!.Replace('\\', '/');

        if (!AssetsByFolder.TryGetValue(folderKey, out var list))
            return null;

        var asset = list.FirstOrDefault(a => a.RelativeFilePath.Equals(relativePath, StringComparison.OrdinalIgnoreCase));

        return asset?.Texture;
    }

    public SceneObj LoadSceneFromFile(string fullPath, Texture2D bubbleTex, Texture2D tailTex, SpriteFont font, Texture2D actionBubbleTex, Texture2D actionTailTex)
    {
        if (!Path.IsPathRooted(fullPath))
            fullPath = Path.Combine(_rootFolder, fullPath);

        var opts = new JsonSerializerOptions { IncludeFields = true };
        var json = File.ReadAllText(fullPath);
        var scene = JsonSerializer.Deserialize<SceneObj>(json, opts);

        scene.Stage = LoadTextureFromRelativePath(scene.StageRelativePath);

        foreach (var p in scene.Props)
            p.Image = LoadTextureFromRelativePath(p.AssetRelativePath);

        foreach (var b in scene.Bubbles)
        {
            if (b.Type == BubbleType.Speech)
            {
                b.Image = bubbleTex;
                b.TailImg = tailTex;
            }
            else if (b.Type == BubbleType.ActionPrompt)
            {
                b.Image = actionBubbleTex;
                b.TailImg = actionTailTex;
            }
            b.Font = font;
            b.UpdateSize();
        }

        return scene;
    }

    public void RefreshFolder(string folderPath)
    {
        folderPath = Path.GetFullPath(folderPath);
        var root = Path.GetFullPath(_rootFolder);

        while (folderPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            string rel = Path.GetRelativePath(_rootFolder, folderPath).Replace('\\', '/').TrimEnd('/');
            AssetsByFolder.Remove(rel);

            foreach (var png in Directory.GetFiles(folderPath, "*.png"))
                LoadAsset(png, isJson: false);
            foreach (var jsn in Directory.GetFiles(folderPath, "*.json"))
                LoadAsset(jsn, isJson: true);

            if (string.Equals(folderPath, root, StringComparison.OrdinalIgnoreCase))
                break;

            folderPath = Path.GetDirectoryName(folderPath)!;
        }
    }
}