// Current process only. No token or password is saved to disk.
public static class LoginSession
{
    public static string Account { get; private set; }
    public static string Token { get; private set; }
    public static void Set(string account, string token) { Account = account; Token = token; }
    public static void Clear() { Account = null; Token = null; }
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { Account = null; Token = null; }
}
