var target = Argument("target", "Build");
var config = Argument("c", "Release");

Task("Build").Does(() =>
{
    var proj = GetFiles("./src/*.csproj").FirstOrDefault()?.FullPath;

    var gamePath = "C:/Program Files/Epic Games/AmongUs/BepInEx/plugins/";

    var settings = new DotNetBuildSettings {
        Configuration = config,
        NoIncremental = true,
        MSBuildSettings = new DotNetMSBuildSettings()
    };
    
    if (config.Equals("Debug", StringComparison.OrdinalIgnoreCase)) {
        settings.MSBuildSettings.WithProperty("DefineConstants", "DEBUG");
    }

    DotNetBuild(proj, settings);

    var projFolder = System.IO.Path.GetDirectoryName(proj);
    var outDll = $"{projFolder}/bin/{config}/net6.0/GNC.dll";

    var xml = System.Xml.Linq.XDocument.Load(proj);
    var ver = xml.Descendants("VersionPrefix").FirstOrDefault()?.Value ?? "6.9";
    var finalName = $"GNCv{ver}.dll";

    if (FileExists(outDll)) {
        if (DirectoryExists(gamePath)) {
            foreach(var f in GetFiles(gamePath + "GNCv*.dll")) DeleteFile(f);
            
            CopyFile(outDll, gamePath + finalName);
            Information("--- Собрано: {0} ---", finalName);
            Information("--- Конфигурация: {0} ---", config);
        } else {
            Warning("Папка игры не найдена. DLL собрана, но не скопирована.");
        }
    } else {
        Error("Выходной файл DLL не найден по пути: " + outDll);
    }
});

RunTarget(target);