namespace RevitAddinManager.Model;

public class Addin : IAddinNode
{
    public List<AddinItem> ItemList
    {
        get => itemList;
        set => itemList = value;
    }

    public string FilePath
    {
        get => filePath;
        set => filePath = value;
    }

    public bool Save
    {
        get => save;
        set => save = value;
    }

    public bool Hidden
    {
        get => hidden;
        set => hidden = value;
    }

    public Addin(string filePath)
    {
        itemList = new List<AddinItem>();
        this.filePath = filePath;
        save = true;
    }

    public Addin(string filePath, List<AddinItem> itemList)
    {
        this.itemList = itemList;
        this.filePath = filePath;
        save = true;
    }

    private List<AddinItem> itemList;

    private string filePath;

    private bool save;

    private bool hidden;
}