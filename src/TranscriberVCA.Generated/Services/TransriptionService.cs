

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prometheus;
using TranscriberVCA.Generated.Models;

namespace TranscriberVCA.Services;

public class TranscriptionService : ITranscriptionService
{
    private readonly Dictionary<string, TranscriptionResult> _transcriptions = new();
    private readonly Dictionary<string, List<string>> _userTranscriptions = new(); // userId -> list of transcription ids

    public TranscriptionResult StartTranscription(string userId, TranscribeRequest request)
    {
        var id = Guid.NewGuid().ToString().Substring(0, 7);
        var model = (TranscriptionResult.ModelEnum)request.Model;
        var format = (TranscriptionResult.FormatEnum)request.Format;

        var result = new TranscriptionResult
        {
            Id = id,
            YoutubeUrl = request.YoutubeUrl,
            Model = model,
            Format = format,
            Status = TranscriptionResult.StatusEnum.ExtractingEnum,
            CreatedAt = DateTime.UtcNow
        };

        _transcriptions[id] = result;

        if (!_userTranscriptions.ContainsKey(userId))
            _userTranscriptions[userId] = new List<string>();
        _userTranscriptions[userId].Add(id);

        // Метрики
        MetricsService.TranscriptionsStarted.WithLabels(model.ToString(), format.ToString()).Inc();
        MetricsService.TranscriptionsInProgress.Inc();

        _ = Task.Run(async () =>
        {
            var timer = MetricsService.TranscriptionDuration.NewTimer();
            try
            {
                result.Status = TranscriptionResult.StatusEnum.TranscribingEnum;
                await Task.Delay(5000);

                result.Status = TranscriptionResult.StatusEnum.CompletedEnum;
                if (request.Format.ToString() == "json")
                {
                    result.Result = new TranscriptionResultResult
                    {
                        Segments = new List<Segment>
                        {
                            new Segment { Start = 0.0f, End = 5.2f, Text = "Привет, добро пожаловать" },
                            new Segment { Start = 5.2f, End = 10.5f, Text = "Это демо транскрипция" }
                        }
                    };
                }

                MetricsService.TranscriptionsCompleted.WithLabels(model.ToString(), format.ToString()).Inc();
            }
            catch
            {
                result.Status = TranscriptionResult.StatusEnum.ErrorEnum;
                MetricsService.TranscriptionsFailed.Inc();
            }
            finally
            {
                timer.ObserveDuration();
                MetricsService.TranscriptionsInProgress.Dec();
            }
        });

        return result;
    }

    public TranscriptionResult? GetTranscription(string userId, string transcriptionId)
    {
        if (!_transcriptions.TryGetValue(transcriptionId, out var result))
            return null;

        if (!_userTranscriptions.ContainsKey(userId) || !_userTranscriptions[userId].Contains(transcriptionId))
            return null;

        return result;
    }

    public List<HistoryItem> GetHistory(string userId, int limit, int offset)
    {
        if (!_userTranscriptions.ContainsKey(userId))
            return new List<HistoryItem>();

        return _userTranscriptions[userId]
            .Skip(offset)
            .Take(limit)
            .Select(id => _transcriptions[id])
            .Select(t => new HistoryItem
            {
                Id = t.Id,
                YoutubeUrl = t.YoutubeUrl,
                Model = t.Model.ToString(),
                Format = t.Format.ToString(),
                Status = t.Status.ToString(),
                CreatedAt = t.CreatedAt
            })
            .ToList();
    }
}