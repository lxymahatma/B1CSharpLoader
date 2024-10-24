using System.Reflection;
using CSharpModBase;
using CSharpModBase.Input;
using HarmonyLib;
using Mono.Cecil;
using static CSharpModBase.Common;

namespace CSharpManager;

public class CSharpModManager
{
    private static readonly Harmony HarmonyInstance = new("CSharpModManager");
    private Thread? _loopThread;
    private static string? LoadingModName { get; set; }

    public List<ICSharpMod> LoadedMods { get; } = [];
    public InputManager InputManager { get; } = new();
    public bool Develop { get; set; }

    static CSharpModManager()
    {
        var currentDomain = AppDomain.CurrentDomain;
        currentDomain.AssemblyResolve += AssemblyResolve;
        currentDomain.UnhandledException += OnUnhandledException;

        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    public CSharpModManager()
    {
        Utils.InitInputManager(InputManager);
        // load config from ini
        Ini iniFile = new(Path.Combine(LoaderDir, "b1cs.ini"));
        Develop = iniFile.GetValue("Develop", "Settings", "1").Trim() == "1";
        Log.Debug($"Develop: {Develop}");
    }

    private static Assembly? TryLoadDll(string path)
    {
        if (File.Exists(path))
        {
            return Assembly.LoadFrom(path);
        }

        return null;
    }

    private static Assembly? AssemblyResolve(object sender, ResolveEventArgs args)
    {
        try
        {
            if (LoadingModName == null)
            {
                return null;
            }

            var dllName = $"{new AssemblyName(args.Name).Name}.dll";
            return TryLoadDll(Path.Combine(ModDir, LoadingModName, dllName)) ??
                   TryLoadDll(Path.Combine(ModDir, "CommonDirs", dllName)) ??
                   TryLoadDll(Path.Combine(LoaderDir, dllName));
        }
        catch (Exception e)
        {
            Log.Error($"Load assembly {args.Name} failed:");
            Log.Error(e);
        }

        return Assembly.Load(args.Name);
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Log.Error("UnhandledException:");
        Log.Error((Exception)e.ExceptionObject);
    }

    private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error("UnobservedTaskException:");
        Log.Error(e.Exception);
    }

    public void LoadMods()
    {
        LoadedMods.Clear();
        if (!Directory.Exists(ModDir))
        {
            Log.Error($"Mod dir {ModDir} does not exist");
            return;
        }

        string[] dirs = Directory.GetDirectories(ModDir);
        var interfaceType = typeof(ICSharpMod);
        foreach (var dir in dirs)
        {
            LoadingModName = Path.GetFileName(dir);
            var dllPath = Path.Combine(dir, $"{LoadingModName}.dll");
            if (!File.Exists(dllPath))
            {
                continue;
            }

            try
            {
                Log.Debug($"======== Loading {dllPath} ========");
                Assembly assembly;
                if (Develop)
                {
                    using var assemblyDef = AssemblyDefinition.ReadAssembly(dllPath);
                    assemblyDef.Name.Name += DateTime.Now.ToString("_yyyyMMdd_HHmmssffff");
                    using MemoryStream stream = new();
                    assemblyDef.Write(stream);
                    assembly = Assembly.Load(stream.GetBuffer());
                }
                else
                {
                    assembly = Assembly.LoadFrom(dllPath);
                }

                LoadMod(assembly, interfaceType);
                LoadingModName = null;
            }
            catch (Exception e)
            {
                Log.Error($"Load {dllPath} failed:");
                Log.Error(e);
            }
        }
    }

    private void LoadMod(Assembly assembly, Type interfaceType)
    {
        foreach (var inheritedType in assembly.GetTypes().Where(interfaceType.IsAssignableFrom))
        {
            Log.Debug($"Found ICSharpMod: {inheritedType}");

            if (Activator.CreateInstance(inheritedType) is not ICSharpMod mod)
            {
                return;
            }

            mod.Init();
            LoadedMods.Add(mod);
            Log.Debug($"Loaded mod {mod.Name} {mod.Version}");
        }

        var harmony = new Harmony($"{assembly.FullName}");
        harmony.PatchAll(assembly);
    }

    public void ReloadMods()
    {
        Log.Debug("ReloadMods");
        InputManager.Clear();
        foreach (var mod in LoadedMods)
        {
            try
            {
                mod.DeInit();
            }
            catch (Exception e)
            {
                Log.Error($"DeInit {mod.Name} failed:");
                Log.Error(e);
            }
        }

        // Unpatch everything inside appdomain
        HarmonyInstance.UnpatchAll();
        LoadMods();
    }

    public void StartLoop()
    {
        InputManager.RegisterBuiltinKeyBind(ModifierKeys.Control, Key.F5, ReloadMods);
        _loopThread = new Thread(Loop)
        {
            // IsBackground = true,
        };
        _loopThread.Start();
    }

    private void Loop()
    {
        while (true)
        {
            InputManager.Update();
            Thread.Sleep(10); // 10ms
        }
    }
}