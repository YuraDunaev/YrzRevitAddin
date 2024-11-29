using Autodesk.Revit.UI;
using RevitAddinManager.Command;
using RevitAddinManager.View;
using System.Reflection;
using Autodesk.Windows;

namespace RevitAddinManager;

public class App : IExternalApplication
{
    public static FrmAddInManager FrmAddInManager { get; set; }
    public static int ThemId { get; set; } = -1;
    public static DockablePaneId PaneId => new DockablePaneId(new Guid("942D8578-7F25-4DC3-8BD8-585C1DBD3614"));
    public static string PaneName => "Debug/Trace Output";

    public Result OnStartup(UIControlledApplication application)
    {
        CreateRibbonPanel(application);
        application.ControlledApplication.DocumentClosed += DocumentClosed;
        return Result.Succeeded;

    }

    public Result OnShutdown(UIControlledApplication application)
    {
        return Result.Cancelled;
    }

    private static void CreateRibbonPanel(UIControlledApplication application)
    {
        var ribbonPanel = application.CreateRibbonPanel("External Tools");
        var pulldownButtonData = new PulldownButtonData("Options", "Add-in Manager");
        var pulldownButton = (PulldownButton)ribbonPanel.AddItem(pulldownButtonData);
        AddPushButton(pulldownButton, typeof(AddInManagerManual), "Rembox Yrz Add-in");
        var tab = ComponentManager.Ribbon.FindTab("Modify");
        if (tab != null)
        {
            var adwPanel = new Autodesk.Windows.RibbonPanel();
            adwPanel.CopyFrom(GetRibbonPanel(ribbonPanel));
            tab.Panels.Add(adwPanel);
        }

    }
    private static readonly FieldInfo RibbonPanelField = typeof(Autodesk.Revit.UI.RibbonPanel).GetField("m_RibbonPanel", BindingFlags.Instance | BindingFlags.NonPublic);
       
    public static Autodesk.Windows.RibbonPanel GetRibbonPanel(Autodesk.Revit.UI.RibbonPanel panel)
    {
        return RibbonPanelField.GetValue(panel) as Autodesk.Windows.RibbonPanel;
    }

    private static void AddPushButton(PulldownButton pullDownButton, Type command, string buttonText)
    {
        var buttonData = new PushButtonData(command.FullName, buttonText, Assembly.GetAssembly(command).Location, command.FullName);
        pullDownButton.AddPushButton(buttonData);
    }

    private void DocumentClosed(object sender, Autodesk.Revit.DB.Events.DocumentClosedEventArgs e)
    {
        FrmAddInManager?.Close();
    }
}