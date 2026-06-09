using System.Drawing;

namespace Common
{
    public static class ClientCommands
    {
        public const string CreateWorld = "CREATE";
        public const string JoinWorld = "JOIN";
        public const string PlacePixel = "PIXEL";
        public const string ListWorlds = "LIST";
    }
    public static class ServerCommands
    {
        public const string WorldState = "STATE";
        public const string PixelPlaced = "PIXEL_OK";
        public const string Error = "ERROR";
        public const string WorldList = "WORLDS";
    }
    public class WorldState
    {
        public int Id {  get; set; }
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public byte [,] Pixels { get; set; }
        public Dictionary<string, int> PlayerScores { get; set; } = new();
    }

    public static class ColorPalette
    {
        public static readonly Color[] Colors = new Color[]
        {
            Color.Black,
            Color.White,
            Color.Red,
            Color.Green,
            Color.Blue,
            Color.Magenta,
            Color.Yellow,
            Color.Cyan,
        };
        public static Color GetColor(byte index) => Colors[index % Colors.Length];
    }
}
