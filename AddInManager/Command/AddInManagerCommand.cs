using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitAddinManager.Model;

namespace RevitAddinManager.Command;

[Transaction(TransactionMode.Manual)]
public class AddInManagerManual : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        Result result = AddinManagerBase.Instance.ExecuteCommand(commandData, ref message, elements, false);
        return result;
    }
}
