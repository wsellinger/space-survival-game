namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Tunable mouse-aim crosshair values, loaded from a JSON file next to the executable so
/// they can be edited without recompiling. If the file is missing, a default one is written
/// out so there's always something to open and tweak.
/// </summary>
public class CrosshairConfig
{
    public int SizePixels { get; set; } = 20;
    public float GapRadiusPixels { get; set; } = 4f;
    public float TickLengthPixels { get; set; } = 5f;
    public float ThicknessPixels { get; set; } = 2f;
    public string ColorHex { get; set; } = "#FFFFFFCC";

    public static CrosshairConfig Load(string path) => ConfigLoader.Load<CrosshairConfig>(path);
}
