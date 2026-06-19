namespace Synapse.GUI.Services;

// public class TextValueStore : ITextValueStore
// {
//     private readonly IJobManager _jobManager;
//     ConcurrentDictionary<object, string> _dict = new();
//
//     public TextValueStore(IJobManager jobManager)
//     {
//         _jobManager = jobManager;
//     }
//     
//     public void SetTextForJob(Guid jobId, object key, string text)
//     {
//         if (_jobManager.JobExists(jobId))
//         {
//             _dict.AddOrUpdate((jobId, key), text, (k, v) => text);    
//         }
//     }
//
//     public bool TryGetTextForJob(Guid jobId, object key)
//     {
//         _dict.TryRemove((jobId, key), out var textFromDict);
//         return textFromDict ?? string.Empty;
//         
//         if (!_dict.TryGetValue(jobId, out var jobDict)) return false;
//         return jobDict.TryGetValue(key, out text);
//     }
// }
