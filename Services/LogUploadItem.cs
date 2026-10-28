using System;

namespace PlenBotLogUploader.Services;

public class LogUploadItem
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string TimestampString => Timestamp.ToString("HH:mm:ss");
    public string BossName { get; set; } = "";
    public int BossId { get; set; }
    public bool Success { get; set; }
    public string Duration { get; set; } = "";
    public string Permalink { get; set; } = "";
    public bool IsCm { get; set; }
    public bool IsLegendaryCm { get; set; }
    public string LocalFilePath { get; set; } = "";

    public string ResultBadge => Success ? "SUCCESS" : "FAIL";
    public string ResultBackground => Success ? "#16A34A" : "#DC2626";
    public string ModeBadge => IsLegendaryCm ? "LCM" : IsCm ? "CM" : "Normal";
    public string ModeBackground => IsLegendaryCm ? "#7C3AED" : IsCm ? "#D97706" : "#475569";
}

public class LogMessageItem
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string TimestampString => Timestamp.ToString("HH:mm:ss");
    public string Message { get; set; } = "";
    public string Level { get; set; } = "Info"; // Info, Success, Warning, Error
    public string LevelColor => Level switch
    {
        "Success" => "#22C55E",
        "Warning" => "#F59E0B",
        "Error" => "#EF4444",
        _ => "#94A3B8"
    };
    public string LevelBadge => Level switch
    {
        "Success" => "✔",
        "Warning" => "⚠",
        "Error" => "✖",
        _ => "ℹ"
    };
}
