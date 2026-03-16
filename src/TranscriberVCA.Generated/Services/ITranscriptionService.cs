using System.Collections.Generic;
using TranscriberVCA.Generated.Models;

namespace TranscriberVCA.Services;

public interface ITranscriptionService
{
    TranscriptionResult StartTranscription(string userId, TranscribeRequest request);
    TranscriptionResult? GetTranscription(string userId, string transcriptionId);
    List<HistoryItem> GetHistory(string userId, int limit, int offset);
}