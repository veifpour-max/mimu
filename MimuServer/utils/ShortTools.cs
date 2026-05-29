namespace LocalMimu.Models;
public static class shTools
{
    public static bool check(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }
        return true;
    }
}

