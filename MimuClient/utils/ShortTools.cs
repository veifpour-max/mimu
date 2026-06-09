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
        // тяжелее кода не писал, очень тяжело реально, ошибаешься как на первом дне. я долбаеб конченный вот откуда я мог заболеть да еще и так хреново
        var timingDiff = DateTime.Now - time;

        if (timingDiff.TotalSeconds >= 0 && timingDiff.TotalSeconds < 60)
        {
            return "Только что";
        }
        if (time.Date == DateTime.Now.Date)
        {
            return time.ToString("HH:mm");
        }
        if (time.Date == DateTime.Now.AddDays(-1).Date)
        {
            return "Вчера";
        }
        else
        {
           return time.ToString("dd.MM.yy");  
        }
        // я чувствую будто у меня -88 iq, даже применять в program.cs не хочу нахуй так жить блять
    }
}

    
    

    








    