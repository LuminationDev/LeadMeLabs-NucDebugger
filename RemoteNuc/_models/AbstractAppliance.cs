public abstract class AbstractAppliance
{
    public string? type;
    public string? name;
    public string? room;
    public string? id;
    public string? value;
    public string? automationType;

    public abstract bool SetValue(string value);
}