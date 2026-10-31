using System.Security.Cryptography;


string source_path = Path.GetFullPath(args[0]);
string replica_path = Path.GetFullPath(args[1]);
string logFile_path = Path.GetFullPath(args[3]);

// if (args.Length != 4)
// {
//     Console.WriteLine("How to use: FolderSync <source> <replica> <interval_seconds> <log_file>");
//     return;
// }

// if (!int.TryParse(args[2], out int interval))
// {
//     Console.WriteLine("The interval must be a whole number.");
//     return;
// }

// if (interval <= 0)
// {
//     Console.WriteLine("The interval must be greater than 0.");
//     return;
// }

if (args.Length != 4 || !int.TryParse(args[2], out int interval) || interval <= 0)
{
    Console.WriteLine("How to use: FolderSync <source> <replica> <interval_seconds> <log_file>");
    return;
}
if (!Directory.Exists(source_path)) // Check if the source folder exists
{
    Console.WriteLine("The source/main folder does not exist.");
    return;
}

Log($"Started: {source_path} -> {replica_path} (every {interval}s)");

while (true)
{
    try { Sync(); }
    catch (Exception ex) { Log($"ERROR: {ex.Message}"); }
    Thread.Sleep(interval * 1000);
}

void Sync()
{
    Directory.CreateDirectory(replica_path);

    //Create missing folders in replica
    foreach (var dir in Directory.GetDirectories(source_path, "*", SearchOption.AllDirectories))
    {
        string targetFilePath = Path.Combine(replica_path, Path.GetRelativePath(source_path, dir));
        if (!Directory.Exists(targetFilePath))
        {
            Directory.CreateDirectory(targetFilePath);
            Log($"CREATE folder: {targetFilePath}");
        }
    }

    //Copy new or changed files
    foreach (var file in Directory.GetFiles(source_path, "*", SearchOption.AllDirectories))
    {
        string targetFilePath = Path.Combine(replica_path, Path.GetRelativePath(source_path, file));
        bool exists = File.Exists(targetFilePath);
        if (!exists || !SameContent(file, targetFilePath))
        {
            File.Copy(file, targetFilePath, true);
            Log($"{(exists ? "UPDATE" : "COPY")} file: {targetFilePath}");
        }
    }

    //Delete files that dont exist in source
    foreach (var file in Directory.GetFiles(replica_path, "*", SearchOption.AllDirectories))
    {
        if (!File.Exists(Path.Combine(source_path, Path.GetRelativePath(replica_path, file))))
        {
            File.Delete(file);
            Log($"REMOVE file: {file}");
        }
    }

    //Delete folders that no longer exist in source
    foreach (var dir in Directory.GetDirectories(replica_path, "*", SearchOption.AllDirectories)
                                 .OrderByDescending(d => d.Length))
    {
        if (!Directory.Exists(Path.Combine(source_path, Path.GetRelativePath(replica_path, dir))))
        {
            Directory.Delete(dir);
            Log($"REMOVE folder: {dir}");
        }
    }
}

bool SameContent(string a, string b)
{
    if (new FileInfo(a).Length != new FileInfo(b).Length) return false;
    return Hash(a).SequenceEqual(Hash(b));
}

byte[] Hash(string path)
{
    using var md5 = MD5.Create();
    using var stream = File.OpenRead(path);
    return md5.ComputeHash(stream);
}

void Log(string message)
{
    string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";
    Console.WriteLine(line);//console
    File.AppendAllText(logFile_path, line + Environment.NewLine);//log file
}