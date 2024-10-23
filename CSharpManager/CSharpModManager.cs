using System.Reflection;
using CSharpManager.IniLib;
using CSharpModBase;
using CSharpModBase.Input;
using CSharpModBase.Utils;
using Mono.Cecil;

namespace CSharpManager;

public sealed class CSharpModManager
{
    private Thread? _loopThread;
    private static string? LoadingModName { get; set; }

    private List<ICSharpMod> LoadedMods { get; } = [];
    private InputManager InputManager { get; } = new();
    private bool IsDevelopMode { get; }

    static CSharpModManager()
    {
        var currentDomain = AppDomain.CurrentDomain;
        currentDomain.AssemblyResolve += AssemblyResolve;
        currentDomain.UnhandledException += OnUnhandledException;

        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    public CSharpModManager()
    {
        InputUtils.InitInputManager(InputManager);
        // load config from ini
        IniReader iniReader = new(Path.Combine(CommonDirs.LoaderDir, "b1cs.ini"));
        IsDevelopMode = iniReader.GetBool("Develop", "Settings");
        Log.Debug($"Develop: {IsDevelopMode}");
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
            return TryLoadDll(Path.Combine(CommonDirs.ModDir, LoadingModName, dllName)) ??
                   TryLoadDll(Path.Combine(CommonDirs.ModDir, "Common", dllName)) ??
                   TryLoadDll(Path.Combine(CommonDirs.LoaderDir, dllName));
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
        if (!Directory.Exists(CommonDirs.ModDir))
        {
            Log.Error($"Mod dir {CommonDirs.ModDir} not exists");
            return;
        }

        string[] dirs = Directory.GetDirectories(CommonDirs.ModDir);
        var ICSharpModType = typeof(ICSharpMod);
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
                if (IsDevelopMode)
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

                foreach (var type in assembly.GetTypes())
                {
                    if (ICSharpModType.IsAssignableFrom(type))
                    {
                        Log.Debug($"Found ICSharpMod: {type}");

                        if (Activator.CreateInstance(type) is ICSharpMod mod)
                        {
                            mod.Init();
                            LoadedMods.Add(mod);
                            Log.Debug($"Loaded mod {mod.Name} {mod.Version}");
                        }
                    }
                }

                LoadingModName = null;
            }
            catch (Exception e)
            {
                Log.Error($"Load {dllPath} failed:");
                Log.Error(e);
            }
        }
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