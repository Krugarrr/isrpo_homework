using System;
using System.Collections.Generic;
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
        var id = Guid.NewGuid().ToString().Substring(0, 7);
        var model = (TranscriptionResult.ModelEnum)request.Model;
        var format = (TranscriptionResult.FormatEnum)request.Format;

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

        _ = Task.Run(async () =>
        {
            var timer = MetricsService.TranscriptionDuration.NewTimer();
            try
            {
                _logger.LogInformation("Transcription {TranscriptionId} status changed to {Status}",
                    id, "Transcribing");
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

                _logger.LogInformation(
                    "Transcription {TranscriptionId} completed successfully. Duration: {Duration}ms",
                    id, timer.ObserveDuration().TotalMilliseconds);

                MetricsService.TranscriptionsCompleted.WithLabels(model.ToString(), format.ToString()).Inc();
            }
            catch (Exception ex)
            {
                result.Status = TranscriptionResult.StatusEnum.ErrorEnum;
                _logger.LogError(ex,
                    "Transcription {TranscriptionId} failed with error: {Error}",
                    id, ex.Message);
                MetricsService.TranscriptionsFailed.Inc();
                timer.ObserveDuration();
            }
            finally
            {
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
        if (!_transcriptions.TryGetValue(transcriptionId, out var result))
        {
            _logger.LogWarning("Transcription {TranscriptionId} not found", transcriptionId);
            return null;
        }

        if (!_userTranscriptions.ContainsKey(userId) || !_userTranscriptions[userId].Contains(transcriptionId))
        {
            _logger.LogWarning(
                "User {UserId} attempted to access transcription {TranscriptionId} without permission",
                userId, transcriptionId);
            return null;
        }

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
        if (!_userTranscriptions.ContainsKey(userId))
        {
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

        _logger.LogInformation("User {UserId} retrieved history: {Count} items (offset={Offset}, limit={Limit})",
            userId, items.Count, offset, limit);
        return items;
    }
}