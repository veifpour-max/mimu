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

public static string FormatTime(DateTime time)
    {
        var timingDiff = DateTime.Now - time;

        if (timingDiff.TotalSeconds >= 0 && timingDiff.TotalSeconds < 60)
        {
            return "Только что";
        }
        if (time.Date == DateTime.Now.Date)
        {
            return time.ToString("HH:mm:ss");
        }
        if (time.Date == DateTime.Now.AddDays(-1).Date)
        {
            return time.ToString("dd:MM:yy | HH:mm:ss");
        }
        else
        {
           return time.ToString("dd:MM:yy | HH:mm:ss");  
        }
    }
}



    