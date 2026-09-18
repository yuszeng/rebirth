namespace Rebirth.Core;

/// <summary>
/// 简易文件日志：每次启动创建一个新日志文件，仅保留最新 5 个。
/// 日志目录：user://logs/（见 CurrentLogPath / LogDirectory）。
/// </summary>
public partial class GameLog : Node
{
    public static GameLog Instance { get; private set; } = null!;

    const int MaxLogFiles = 5; // 最多保留的日志文件数
    const string LogFolderName = "logs"; // 相对 user:// 的子目录名

    readonly object _writeLock = new(); // 多线程写入锁（Godot 主线程为主，预留安全）
    string _currentLogPath = ""; // 当前会话日志文件绝对路径

    /// <summary>日志目录绝对路径。</summary>
    public string LogDirectory { get; private set; } = "";

    /// <summary>当前会话日志文件绝对路径。</summary>
    public string CurrentLogPath => _currentLogPath;

    public override void _EnterTree()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
        StartSession();
    }

    public override void _ExitTree()
    {
        if (!string.IsNullOrEmpty(_currentLogPath))
        {
            WriteInternal("INFO", "GameLog", "日志会话结束");
        }
    }

    /// <summary>写入 INFO 级别日志。</summary>
    public static void Info(string message) => Instance?.Write("INFO", message);

    /// <summary>写入 WARN 级别日志。</summary>
    public static void Warn(string message) => Instance?.Write("WARN", message);

    /// <summary>写入 ERROR 级别日志。</summary>
    public static void Error(string message) => Instance?.Write("ERROR", message);

    /// <summary>创建日志目录、新日志文件，并清理旧文件。</summary>
    void StartSession()
    {
        LogDirectory = Path.Combine(OS.GetUserDataDir(), LogFolderName);
        Directory.CreateDirectory(LogDirectory);

        _currentLogPath = CreateLogFilePath(LogDirectory);
        WriteInternal("INFO", "GameLog", $"日志会话开始 -> {_currentLogPath}");
        PruneOldLogs();
    }

    /// <summary>按时间戳生成唯一文件名，格式 yyyy-MM-dd_HH-mm-ss.log。</summary>
    static string CreateLogFilePath(string logDirectory)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var path = Path.Combine(logDirectory, $"{timestamp}.log");
        if (!File.Exists(path))
        {
            return path;
        }

        // 同一秒内重复启动时追加毫秒，避免覆盖
        return Path.Combine(logDirectory, $"{timestamp}-{DateTime.Now:fff}.log");
    }

    /// <summary>按修改时间保留最新 MaxLogFiles 个，删除更早的文件。</summary>
    void PruneOldLogs()
    {
        var files = Directory.GetFiles(LogDirectory, "*.log")
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ToList();

        foreach (var file in files.Skip(MaxLogFiles))
        {
            try
            {
                file.Delete();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[GameLog] 删除旧日志失败: {file.FullName} ({ex.Message})");
            }
        }
    }

    void Write(string level, string message) => WriteInternal(level, "Game", message);

    void WriteInternal(string level, string tag, string message)
    {
        if (string.IsNullOrEmpty(_currentLogPath))
        {
            return;
        }

        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] [{tag}] {message}";
        lock (_writeLock)
        {
            File.AppendAllText(_currentLogPath, line + System.Environment.NewLine);
        }

        // 同步输出到 Godot 编辑器「输出」面板，便于调试
        if (level == "ERROR")
        {
            GD.PrintErr(line);
        }
        else
        {
            GD.Print(line);
        }
    }
}
