using System;

namespace Synapse.GUI.Services;

public interface ITextValueStore
{
    void SetTextForJob(Guid jobId, object key, string text);
    string GetTextForJob(Guid jobId, object key);
}