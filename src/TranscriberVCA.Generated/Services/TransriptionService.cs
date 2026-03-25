using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prometheus;
using TranscriberVCA.Generated.Models;
using TranscriberVCA.Services;

namespace TranscriberVCA.Generated.Services;

/// <summary>
/// TranscriptionService
/// </summary>
public class TranscriptionService : ITranscriptionService
{
    private static readonly ActivitySource ActivitySource = new("TranscriberVCA.Transcription");
    private readonly Dictionary<string, TranscriptionResult> _transcriptions = new();
    private readonly Dictionary<string, List<string>> _userTranscriptions = new();
    private readonly ILogger<TranscriptionService> _logger;

    /// <summary>
    /// Constructor 
    /// </summary>
    /// <param name="logger"></param>
    public TranscriptionService(ILogger<TranscriptionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// StartTranscription
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    public TranscriptionResult StartTranscription(string userId, TranscribeRequest request)
    {
        using var activity = ActivitySource.StartActivity("TranscriptionService.Start");

        var id = Guid.NewGuid().ToString().Substring(0, 7);
        var model = (TranscriptionResult.ModelEnum)request.Model;
        var format = (TranscriptionResult.FormatEnum)request.Format;

        activity?.SetTag("transcription.id", id);
        activity?.SetTag("transcription.user_id", userId);
        activity?.SetTag("transcription.youtube_url", request.YoutubeUrl);
        activity?.SetTag("transcription.model", model.ToString());
        activity?.SetTag("transcription.format", format.ToString());

        _logger.LogInformation(
            "Transcription {TranscriptionId} started by user {UserId}. URL: {YoutubeUrl}, Model: {Model}, Format: {Format}",
            id, userId, request.YoutubeUrl, model, format);

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

        MetricsService.TranscriptionsStarted.WithLabels(model.ToString(), format.ToString()).Inc();
        MetricsService.TranscriptionsInProgress.Inc();

        // Сохраняем trace context для фоновой задачи
        var parentContext = activity?.Context;

        _ = Task.Run(async () =>
        {
            using var processActivity = ActivitySource.StartActivity(
                "TranscriptionService.Process",
                ActivityKind.Internal,
                parentContext ?? default);

            processActivity?.SetTag("transcription.id", id);
            processActivity?.SetTag("transcription.model", model.ToString());

            var timer = MetricsService.TranscriptionDuration.NewTimer();
            try
            {
                using (var extractActivity = ActivitySource.StartActivity("TranscriptionService.ExtractAudio"))
                {
                    extractActivity?.SetTag("transcription.id", id);
                    result.Status = TranscriptionResult.StatusEnum.TranscribingEnum;
                    _logger.LogInformation("Transcription {TranscriptionId} status changed to {Status}",
                        id, "Transcribing");
                    await Task.Delay(2000);
                }

                using (var transcribeActivity = ActivitySource.StartActivity("TranscriptionService.Transcribe"))
                {
                    transcribeActivity?.SetTag("transcription.id", id);
                    transcribeActivity?.SetTag("transcription.model", model.ToString());
                    await Task.Delay(3000);
                }

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

                processActivity?.SetTag("transcription.status", "completed");
                _logger.LogInformation("Transcription {TranscriptionId} completed successfully", id);

                MetricsService.TranscriptionsCompleted.WithLabels(model.ToString(), format.ToString()).Inc();
            }
            catch (Exception ex)
            {
                result.Status = TranscriptionResult.StatusEnum.ErrorEnum;
                processActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                processActivity?.SetTag("transcription.status", "error");
                _logger.LogError(ex, "Transcription {TranscriptionId} failed with error: {Error}", id, ex.Message);
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

    /// <summary>
    /// GetTranscription
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="transcriptionId"></param>
    /// <returns></returns>
    public TranscriptionResult? GetTranscription(string userId, string transcriptionId)
    {
        using var activity = ActivitySource.StartActivity("TranscriptionService.Get");
        activity?.SetTag("transcription.id", transcriptionId);
        activity?.SetTag("transcription.user_id", userId);

        if (!_transcriptions.TryGetValue(transcriptionId, out var result))
        {
            activity?.SetTag("transcription.found", false);
            _logger.LogWarning("Transcription {TranscriptionId} not found", transcriptionId);
            return null;
        }

        if (!_userTranscriptions.ContainsKey(userId) || !_userTranscriptions[userId].Contains(transcriptionId))
        {
            activity?.SetTag("transcription.authorized", false);
            activity?.SetStatus(ActivityStatusCode.Error, "Unauthorized access");
            _logger.LogWarning(
                "User {UserId} attempted to access transcription {TranscriptionId} without permission",
                userId, transcriptionId);
            return null;
        }

        activity?.SetTag("transcription.found", true);
        activity?.SetTag("transcription.status", result.Status.ToString());

        _logger.LogDebug("User {UserId} retrieved transcription {TranscriptionId}, status: {Status}",
            userId, transcriptionId, result.Status);
        return result;
    }

    /// <summary>
    /// GetHistory
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="limit"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public List<HistoryItem> GetHistory(string userId, int limit, int offset)
    {
        using var activity = ActivitySource.StartActivity("TranscriptionService.GetHistory");
        activity?.SetTag("transcription.user_id", userId);
        activity?.SetTag("transcription.limit", limit);
        activity?.SetTag("transcription.offset", offset);

        if (!_userTranscriptions.ContainsKey(userId))
        {
            activity?.SetTag("transcription.history_count", 0);
            _logger.LogInformation("No history found for user {UserId}", userId);
            return new List<HistoryItem>();
        }

        var items = _userTranscriptions[userId]
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

        activity?.SetTag("transcription.history_count", items.Count);

        _logger.LogInformation("User {UserId} retrieved history: {Count} items (offset={Offset}, limit={Limit})",
            userId, items.Count, offset, limit);
        return items;
    }
}