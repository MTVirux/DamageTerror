namespace DamageTerror.Models;

public class PopoutWindowPin
{
    public bool Pinned { get; set; }
    public Vector2 Pos { get; set; } = new(100, 100);
    public Vector2 Size { get; set; } = new(350, 400);
}
