using UnityEngine;

public static class Console
{
    private static bool enabled = true;
    public static void Disable() => enabled = false;
    public static void Enable() => enabled = true;

    public static void Log(params object[] args)
    {
#if UNITY_EDITOR
        if(enabled)
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
