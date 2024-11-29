using System.Windows;
using RevitAddinManager.Model;
using Autodesk.Revit.DB;
using MahApps.Metro.Controls;
using System.Web.UI.WebControls;
using System.Collections;

namespace RevitAddinManager.View;

public partial class FrmAddInManager : MetroWindow
{
    private Document _document { get; set; }

    public FrmAddInManager(Document doc)
    {
        InitializeComponent();
        App.FrmAddInManager = this;
        Title += DefaultSetting.Version;
        _document = doc;
    }

    public void LoadLevels()
    {
        var levels = new FilteredElementCollector(_document)
            .OfClass(typeof(Level))
            .ToElements()
            .Cast<Level>()
            .ToList();
        StartLevelComboBox.ItemsSource = levels;
        EndLevelComboBox.ItemsSource = levels;
    }
    public void LoadLevelsFromLinkedModels()
    {
        List<Level> linkedLevels = new List<Level>();
        var linkInstances = new FilteredElementCollector(_document)
            .OfClass(typeof(RevitLinkInstance))
            .ToElements()
            .Cast<RevitLinkInstance>();

        foreach (var linkInstance in linkInstances)
        {
            Document linkedDoc = linkInstance.GetLinkDocument();
            if (linkedDoc != null)
            {
                var levels = new FilteredElementCollector(linkedDoc)
                    .OfClass(typeof(Level))
                    .ToElements()
                    .Cast<Level>();
                linkedLevels.AddRange(levels);
            }
        }
        StartLevelLinkedComboBox.ItemsSource = linkedLevels;
        EndLevelLinkedComboBox.ItemsSource = linkedLevels;
    }

    private void CalculateMainDataButton_Click(object sender, RoutedEventArgs e)
    {
        mainWallsDataGrid.ItemsSource = PrepareWallData(StartLevelComboBox.SelectedItem, EndLevelComboBox.SelectedItem);
    }

    private void CalculateLinkedDataButton_Click(object sender, RoutedEventArgs e)
    {
        linkedWallsDataGrid.ItemsSource = PrepareWallData(StartLevelLinkedComboBox.SelectedItem, EndLevelLinkedComboBox.SelectedItem, true);
    }

    private List<WallInfo> PrepareWallData(object fromLevel, object toLevel, bool isLinked = false)
    {
        var startLevel = fromLevel as Level;
        var endLevel = toLevel as Level;
        if (startLevel != null && endLevel != null)
        {
            var walls = GetWallsData(startLevel, endLevel, _document, isLinked);
            return walls;
        }
        else
        {
            MessageBox.Show("Please select valid levels from the dropdowns.");
            return new List<WallInfo>();
        }
    }

    private List<WallInfo> GetWallsData(Level lowerLevel, Level upperLevel, Document doc, bool isLinked = false)
    {
        var wallsInfo = new List<WallInfo>();
        var collector = isLinked 
            ? GetWallsFromLinkedModels(doc) 
            : new FilteredElementCollector(doc).OfClass(typeof(Wall)).Cast<Wall>().ToList<Wall>();

        foreach (Element element in collector)
        {
            var wall = element as Wall;
            if (IsWallWithinLevel(wall, lowerLevel, upperLevel))
            {
                var baseConstraintId = wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT)?.AsElementId() ?? ElementId.InvalidElementId;
                var levelName = isLinked 
                    ? (wall.Document.GetElement(wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT)?.AsElementId()) as Level)?.Name
                    : (doc.GetElement(baseConstraintId) as Level).Name;
                var areaM2 = CalculateTotalFaceAreaM2(wall);
                wallsInfo.Add(new WallInfo
                {
                    Id = wall.Id.IntegerValue,
                    FamilyType = $"{wall.GetType().Name} : {wall.WallType.Name}",
                    AreaM2 = areaM2,
                    RevitArea = Math.Round(UnitUtils.Convert(wall.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED).AsDouble(), UnitTypeId.SquareFeet, UnitTypeId.SquareMeters), 2),
                    Level = levelName
                });
            }
        }
        return wallsInfo;
    }

    private double CalculateTotalFaceAreaM2(Wall selectedWall)
    {
        double totalArea = 0.0;
        IList<Reference> sideFaces = HostObjectUtils.GetSideFaces(selectedWall, ShellLayerType.Exterior);
        foreach (var faceRef in sideFaces)
        {
            Face face = selectedWall.GetGeometryObjectFromReference(faceRef) as Face;
            if (face != null)
            {
                totalArea += face.Area;
            }
        }
        totalArea = UnitUtils.Convert(totalArea, UnitTypeId.SquareFeet, UnitTypeId.SquareMeters);
        return totalArea;
    }

    private List<Wall> GetWallsFromLinkedModels(Document doc)
    {
        List<Wall> walls = new List<Wall>();
        var linkInstances = new FilteredElementCollector(doc)
            .OfClass(typeof(RevitLinkInstance))
            .ToElements();
        foreach (RevitLinkInstance linkInstance in linkInstances)
        {
            Document linkedDoc = linkInstance.GetLinkDocument();
            if (linkedDoc != null)
            {
                var linkedWalls = new FilteredElementCollector(linkedDoc)
                    .OfClass(typeof(Wall))
                    .ToElements();
                foreach (Element element in linkedWalls)
                {
                    if (element is Wall wall)
                    {
                        walls.Add(wall);
                    }
                }
            }
        }
        return walls;
    }
    private bool IsWallWithinLevel(Wall wall, Level levelStart, Level levelEnd)
    {
        ElementId baseConstraintId = wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT)?.AsElementId() ?? ElementId.InvalidElementId;
        Level baseLevel = wall.Document.GetElement(baseConstraintId) as Level;
        if (baseLevel == null)
        {
            return false;
        }
        BoundingBoxXYZ boundingBox = wall.get_BoundingBox(null);
        double wBot = Math.Round(boundingBox.Min.Z, 4);
        double wTop = Math.Round(boundingBox.Max.Z, 4);
        double lBot = Math.Round(Math.Min(levelStart.Elevation, levelEnd.Elevation), 4);
        double lTop = Math.Round(Math.Max(levelStart.Elevation, levelEnd.Elevation), 4);
        return wBot >= lBot && wTop <= lTop;  
    }
}