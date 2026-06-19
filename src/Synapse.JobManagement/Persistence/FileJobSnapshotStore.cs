namespace Synapse.JobManagement.Persistence;

public class FileJobSnapshotStore : IJobSnapshotStore
{
    private readonly string _dir;
    public FileJobSnapshotStore(string directory)
    {
        _dir = directory;
        Directory.CreateDirectory(_dir);
    }
    
    public async Task<string> SaveSnapshotAsync(Guid jobId, JobInfo snapshot, CancellationToken ct = default)
    {
        if (snapshot.LastSnapshotIteration == snapshot.IterationCount)
            return ""; // no changes since last snapshot, skip saving

        snapshot.LastSnapshotIteration = snapshot.IterationCount;
        var name = snapshot.Config.Name;
        
        if (String.IsNullOrWhiteSpace(name)) name =  jobId.ToString();

        var i = 1;
        var filePath = Path.Combine(_dir, $"{name}.json");
        while (Path.Exists(filePath))
        {
            filePath = Path.Combine(_dir, $"{name}-{i}.json");
            i++;
        }
        var json = System.Text.Json.JsonSerializer.Serialize(snapshot, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json, ct);
        
        return filePath;
    }
}
