using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Media;

namespace MimuGui.Design;

public static class Icons
{
    private static readonly Dictionary<string, Geometry?> Cache = new();

    public static Geometry? Add => Load("plus");
    public static Geometry? Attach => Load("paperclip");
    public static Geometry? Group => Load("users");
    public static Geometry? Close => Load("x");
    public static Geometry? Check => Load("check");
    public static Geometry? DoneAll => Load("checks");
    public static Geometry? Send => Load("paper-plane-fill");
    public static Geometry? ArrowForward => Load("arrow-right");
    public static Geometry? ArrowsClockwise => Load("arrows-clockwise");
    public static Geometry? Gear => Load("gear");

    public static Geometry? Load(string name)
    {
        if (Cache.TryGetValue(name, out var cached))
        {
            return cached;
        }
        try
        {
            var file = Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", name + ".svg");
            var svg = File.ReadAllText(file);
            var matches = Regex.Matches(svg, "d=\"([^\"]+)\"");
            var combined = string.Join(" ", matches.Select(m => m.Groups[1].Value));
            var geometry = Geometry.Parse(combined);
            Cache[name] = geometry;
            return geometry;
        }
        catch
        {
            Cache[name] = null;
            return null;
        }
    }
}
