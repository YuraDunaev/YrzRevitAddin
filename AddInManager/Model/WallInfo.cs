namespace RevitAddinManager.View;

public partial class FrmAddInManager
{
    public class WallInfo
    {
        public int Id { get; set; }
        public string FamilyType { get; set; }
        public double AreaM2 { get; set; }
        public double RevitArea { get; set; }
        public string Level { get; set; }
    }
}