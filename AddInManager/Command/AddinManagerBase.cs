using System.Diagnostics;
using System.IO;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Windows;
using static RevitAddinManager.App;
using RevitAddinManager.ViewModel;
using RevitAddinManager.Model;

#if R25
using AssemblyLoadContext = RevitAddinManager.Model.AssemblyLoadContext;
using System.Runtime.Loader;
#endif

namespace RevitAddinManager.Command;

public sealed class AddinManagerBase
{
 public Result ExecuteCommand(ExternalCommandData data, ref string message, ElementSet elements, bool faceless)
{
    if (FormControl.Instance.IsOpened) return Result.Succeeded;

    if (_activeCmd != null && faceless)
    {
#if R19 || R20 || R21 || R22 || R23 || R24
        return RunActiveCommand(data, ref message, elements);
#else
        return RunActiveCommand(data, ref message, elements);
#endif
    }

    Document doc = data.Application.ActiveUIDocument.Document;
    var frmAddInManager = new View.FrmAddInManager(doc);

    frmAddInManager.LoadLevels();
    frmAddInManager.LoadLevelsFromLinkedModels();
    frmAddInManager.Show();

    return Result.Failed;
}

    public string ActiveTempFolder
    {
        get => _activeTempFolder;
        set => _activeTempFolder = value;
    }

    public Result RunActiveCommand( ExternalCommandData data, ref string message, ElementSet elements)
    {
        var filePath = _activeCmd.FilePath;
        if (!File.Exists(filePath))
        {
            MessageBox.Show("File not found: " + filePath,DefaultSetting.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            return 0;
        }
        Result result =Result.Succeeded;
        return result;
    }

#if R25
    public Result RunActiveCommand(ExternalCommandData data, ref string message, ElementSet elements)
    {
        var filePath = _activeCmd.FilePath;
        if (!File.Exists(filePath))
        {
            MessageBox.Show("File not found: " + filePath,DefaultSetting.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            return 0;
        }
        Result result = Result.Failed;
        var alc = new AssemblyLoadContext(filePath);
        Stream stream = null;
        try
        {
            stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            Assembly assembly = alc.LoadFromStream(stream);
            object instance = assembly.CreateInstance(_activeCmdItem.FullClassName);
            WeakReference alcWeakRef = new WeakReference(alc, trackResurrection: true);
            if (instance is IExternalCommand externalCommand)
            {
                _activeEc = externalCommand;
                result = _activeEc.Execute(data, ref message, elements);
                alc.Unload();
            }
            int counter = 0;
            for (counter = 0; alcWeakRef.IsAlive && (counter < 10); counter++)
            {
                alc = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            stream.Close();
        }
        catch (Exception ex)
        {
            // unload the assembly
            MessageBox.Show(ex.ToString());
            alc?.Unload();
            result = Result.Failed;
            WeakReference alcWeakRef = new WeakReference(alc, trackResurrection: true);
            for (int counter = 0; alcWeakRef.IsAlive && (counter < 10); counter++)
            {
                alc = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            stream?.Close();
            Debug.WriteLine("Assembly unloaded");

        }
        // finally
        // {
        //     alc?.Unload();
        // }
        return result;
    }
#endif

    public static AddinManagerBase Instance
    {
        get
        {
            if (_instance == null)
            {
#pragma warning disable RCS1059 // Avoid locking on publicly accessible instance.
                lock (typeof(AddinManagerBase))
                {
                    if (_instance == null)
                    {
                        _instance = new AddinManagerBase();
                    }
                }
#pragma warning restore RCS1059 // Avoid locking on publicly accessible instance.
            }
            return _instance;
        }
    }

    private AddinManagerBase()
    {
        _addinManager = new AddinManager();

    }

    private string _activeTempFolder = string.Empty;

    private static volatile AddinManagerBase _instance;

    private IExternalCommand _activeEc = null;

    private Addin _activeCmd = null;

    private AddinManager _addinManager;
}