using Prometheus;

namespace TranscriberVCA.Services;

public static class MetricsService
{
    public static readonly Counter TranscriptionsStarted = Metrics.CreateCounter(
        "transcriber_transcriptions_started_total",
        "Total number of transcription requests",
        new CounterConfiguration
        {
            LabelNames = new[] { "model", "format" }
        });
    
    public static readonly Counter TranscriptionsCompleted = Metrics.CreateCounter(
        "transcriber_transcriptions_completed_total",
        "Total number of completed transcriptions",
        new CounterConfiguration
        {
            LabelNames = new[] { "model", "format" }
        });
    
    public static readonly Counter TranscriptionsFailed = Metrics.CreateCounter(
        "transcriber_transcriptions_failed_total",
        "Total number of failed transcriptions");
    
    public static readonly Gauge TranscriptionsInProgress = Metrics.CreateGauge(
        "transcriber_transcriptions_in_progress",
        "Number of transcriptions currently being processed");
    
    public static readonly Counter UsersRegistered = Metrics.CreateCounter(
        "transcriber_users_registered_total",
        "Total number of registered users");
    
    public static readonly Counter LoginsTotal = Metrics.CreateCounter(
        "transcriber_logins_total",
        "Total number of login attempts",
        new CounterConfiguration
        {
            LabelNames = new[] { "status" }
        });
    
    public static readonly Histogram TranscriptionDuration = Metrics.CreateHistogram(
        "transcriber_transcription_duration_seconds",
        "Duration of transcription processing in seconds",
        new HistogramConfiguration
        {
            Buckets = new[] { 0.5, 1.0, 2.0, 5.0, 10.0, 30.0, 60.0 }
        });
    
    public static readonly Counter HttpRequestsTotal = Metrics.CreateCounter(
        "transcriber_http_requests_total",
        "Total HTTP requests",
        new CounterConfiguration
        {
            LabelNames = new[] { "method", "endpoint", "status_code" }
        });
}