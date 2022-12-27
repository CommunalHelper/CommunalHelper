using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper;

// This class was moved from CommunalHelperModule, so let's keep the same namespace.
public static class Util
{
    public static void Log(LogLevel logLevel, string str)
    {
        Logger.Log(logLevel, "Communal Helper", str);
    }

    public static void Log(string str)
    {
        Log(LogLevel.Debug, str);
    }

    public static bool TryGetPlayer(out Player player)
    {
        player = Engine.Scene?.Tracker?.GetEntity<Player>();
        return player != null;
    }

    private static readonly PropertyInfo[] namedColors = typeof(Color).GetProperties();

    public static Color CopyColor(Color color, float alpha)
    {
        return new Color(color.R, color.G, color.B, (byte)alpha * 255);
    }

    public static Color CopyColor(Color color, int alpha)
    {
        return new Color(color.R, color.G, color.B, alpha);
    }

    public static Color ColorArrayLerp(float lerp, params Color[] colors)
    {
        float m = lerp % colors.Length;
        int fromIndex = (int)Math.Floor(m);
        int toIndex = (fromIndex + 1) % colors.Length;
        float clampedLerp = m - fromIndex;

        return Color.Lerp(colors[fromIndex], colors[toIndex], clampedLerp);
    }

    public static Color TryParseColor(string str, float alpha = 1f)
    {
        foreach (PropertyInfo prop in namedColors)
        {
            if (str.Equals(prop.Name))
            {
                return CopyColor((Color)prop.GetValue(null), alpha);
            }
        }
        return CopyColor(Calc.HexToColor(str.Trim('#')), alpha);
    }

    public static int ToInt(bool b)
    {
        return b ? 1 : 0;
    }

    public static int ToBitFlag(params bool[] b)
    {
        int ret = 0;
        for (int i = 0; i < b.Length; i++)
            ret |= ToInt(b[i]) << i;
        return ret;
    }

    public static float Mod(float x, float m)
    {
        return ((x % m) + m) % m;
    }

    public static Vector2 RandomDir(float length)
    {
        return Calc.AngleToVector(Calc.Random.NextAngle(), length);
    }

    public static string StrTrim(string str)
    {
        return str.Trim();
    }

    public static Vector2 Min(Vector2 a, Vector2 b)
    {
        return new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
    }

    public static Vector2 Max(Vector2 a, Vector2 b)
    {
        return new(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
    }

    public static Rectangle Rectangle(Vector2 a, Vector2 b)
    {
        Vector2 min = Min(a, b);
        Vector2 size = Max(a, b) - min;
        return new((int)min.X, (int)min.Y, (int)size.X, (int)size.Y);
    }

    /// <summary>
    /// Triangle wave function.
    /// </summary>
    public static float TriangleWave(float x)
    {
        return (2 * Math.Abs(Mod(x, 2) - 1)) - 1;
    }

    /// <summary>
    /// Triangle wave between mapped between two values.
    /// </summary>
    /// <param name="x">The input value.</param>
    /// <param name="from">The ouput when <c>x</c> is an even integer.</param>
    /// <param name="to">The output when <c>x</c> is an odd integer.</param>
    public static float MappedTriangleWave(float x, float from, float to)
    {
        return ((from - to) * Math.Abs(Mod(x, 2) - 1)) + to;
    }

    public static float PowerBounce(float x, float p)
    {
        return -(float)Math.Pow(Math.Abs(2 * (Mod(x, 1) - .5f)), p) + 1;
    }

    public static bool Blink(float time, float duration)
    {
        return time % (duration * 2) < duration;
    }
}
