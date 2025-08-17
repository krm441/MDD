using UnityEngine;
using UnityEngine.UI;

public class Colorize
{

    // Color 

    public static Colorize Red = new Colorize(Color.red);
    public static Colorize Yellow = new Colorize(Color.yellow);
    public static Colorize Green = new Colorize(Color.green);
    public static Colorize Blue = new Colorize(Color.blue);
    public static Colorize Cyan = new Colorize(Color.cyan);
    public static Colorize Magenta = new Colorize(Color.magenta);

    // Hex 

    public static Colorize Orange = new Colorize("#FFA500");
    public static Colorize Olive = new Colorize("#808000");
    public static Colorize Purple = new Colorize("#800080");
    public static Colorize DarkRed = new Colorize("#8B0000");
    public static Colorize DarkGreen = new Colorize("#006400");
    public static Colorize DarkOrange = new Colorize("#FF8C00");
    public static Colorize Gold = new Colorize("#FFD700");

    private readonly string _prefix;

    private const string Suffix = "</color>";

    // Convert Color to HtmlString
    private Colorize(Color color)
    {
        _prefix = $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>";
    }
    // Use Hex Color
    private Colorize(string hexColor)
    {
        _prefix = $"<color={hexColor}>";
    }

    public static string operator %(string text, Colorize color)
    {
        return color._prefix + text + Suffix;
    }


}

public static class Console
{
    private static bool enabled = true;
    public static void Disable() => enabled = false;
    public static void Enable() => enabled = true;

    public static void ScrLog(params object[] args)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        string msg = BuildMessage(args);
        if (enabled) Debug.Log(msg);

        if (ConsoleUIManager.Instance != null)
            ConsoleUIManager.Instance.Append(msg);        
#endif
    }

    public static void ScrLoopLog(params object[] args)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        string msg = BuildMessage(args, false);
        //if (enabled) Debug.Log(msg);

        if (ConsoleUIManager.Instance != null)
            ConsoleUIManager.Instance.Append(msg);
#endif
    }

    public static void Log(params object[] args)
    {
        //return;
#if UNITY_EDITOR
        if (enabled)
            Debug.Log(BuildMessage(args));
#endif
    }

    public static void LoopLog(params object[] args)
    {
#if UNITY_EDITOR
        if (enabled)
            Debug.Log(BuildMessage(args, false));
#endif
    }

    public static void Warn(params object[] args)
    {
#if UNITY_EDITOR
        if (enabled) Debug.LogWarning(BuildMessage(args));
#endif
    }

    public static void Error(params object[] args)
    {
#if UNITY_EDITOR
        if (enabled) Debug.LogError(BuildMessage(args));
#endif
    }
    private static string BuildMessage(object[] args, bool logTime = true)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        // Add timestamp
        if (logTime)
        {
            string time = System.DateTime.Now.ToString("HH:mm:ss.fff");
            sb.Append($"[{time}] ");
        }

        foreach (var arg in args)
        {
            if (arg == null)
                sb.Append("<null>");
            else
                sb.Append(arg.ToString());

            sb.Append(" ");
        }

        return sb.ToString().TrimEnd(); // remove trailing space
    }

}
