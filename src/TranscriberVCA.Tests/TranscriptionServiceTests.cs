using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TranscriberVCA.Generated.Models;
using TranscriberVCA.Generated.Services;
using Xunit;

namespace TranscriberVCA.Tests;

public class TranscriptionServiceTests
{
    private readonly TranscriptionService _service;

    public TranscriptionServiceTests()
    {
        var logger = new Mock<ILogger<TranscriptionService>>();
        _service = new TranscriptionService(logger.Object);
    }

    [Fact]
    public void StartTranscription_ReturnsResultWithId()
    {
        var request = new TranscribeRequest
        {
            YoutubeUrl = "https://youtube.com/watch?v=test1",
            Model = TranscribeRequest.ModelEnum.BaseEnum,
            Format = TranscribeRequest.FormatEnum.TxtEnum
        };

        var result = _service.StartTranscription("olyakub", request);

        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.YoutubeUrl.Should().Be("https://youtube.com/watch?v=test1");
        result.Status.Should().Be(TranscriptionResult.StatusEnum.TranscribingEnum);
    }

    [Fact]
    public void StartTranscription_SetsCorrectModelAndFormat()
    {
        var request = new TranscribeRequest
        {
            YoutubeUrl = "https://youtube.com/watch?v=test2",
            Model = TranscribeRequest.ModelEnum.LargeEnum,
            Format = TranscribeRequest.FormatEnum.JsonEnum
        };

        var result = _service.StartTranscription("olyakub", request);

        result.Model.Should().Be(TranscriptionResult.ModelEnum.LargeEnum);
        result.Format.Should().Be(TranscriptionResult.FormatEnum.JsonEnum);
    }

    [Fact]
    public void GetTranscription_ExistingId_ReturnsResult()
    {
        var request = new TranscribeRequest
        {
            YoutubeUrl = "https://youtube.com/watch?v=test3",
            Model = TranscribeRequest.ModelEnum.BaseEnum,
            Format = TranscribeRequest.FormatEnum.TxtEnum
        };
        var started = _service.StartTranscription("olyakub", request);

        var result = _service.GetTranscription("olyakub", started.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(started.Id);
    }

    [Fact]
    public void GetTranscription_NonExistentId_ReturnsNull()
    {
        var result = _service.GetTranscription("olyakub", "nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public void GetTranscription_WrongUser_ReturnsNull()
    {
        var request = new TranscribeRequest
        {
            YoutubeUrl = "https://youtube.com/watch?v=test4",
            Model = TranscribeRequest.ModelEnum.BaseEnum,
            Format = TranscribeRequest.FormatEnum.TxtEnum
        };
        var started = _service.StartTranscription("olyakub", request);

        var result = _service.GetTranscription("sashakrug", started.Id);

        result.Should().BeNull();
    }

    [Fact]
    public void GetHistory_NoTranscriptions_ReturnsEmptyList()
    {
        var result = _service.GetHistory("deadinside", 10, 0);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetHistory_WithTranscriptions_ReturnsCorrectCount()
    {
        var request = new TranscribeRequest
        {
            YoutubeUrl = "https://youtube.com/watch?v=test5",
            Model = TranscribeRequest.ModelEnum.BaseEnum,
            Format = TranscribeRequest.FormatEnum.TxtEnum
        };

        _service.StartTranscription("skibidimaniac", request);
        _service.StartTranscription("skibidimaniac", request);
        _service.StartTranscription("skibidimaniac", request);

        var result = _service.GetHistory("skibidimaniac", 10, 0);

        result.Should().HaveCount(3);
    }

    [Fact]
    public void GetHistory_WithLimitAndOffset_ReturnsCorrectSlice()
    {
        var request = new TranscribeRequest
        {
            YoutubeUrl = "https://youtube.com/watch?v=test6",
            Model = TranscribeRequest.ModelEnum.BaseEnum,
            Format = TranscribeRequest.FormatEnum.TxtEnum
        };

        for (int i = 0; i < 5; i++)
            _service.StartTranscription("chromekiller", request);

        var result = _service.GetHistory("chromekiller", 2, 1);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task StartTranscription_CompletesAfterProcessing()
    {
        var request = new TranscribeRequest
        {
            YoutubeUrl = "https://youtube.com/watch?v=test7",
            Model = TranscribeRequest.ModelEnum.BaseEnum,
            Format = TranscribeRequest.FormatEnum.TxtEnum
        };

        var started = _service.StartTranscription("blablabla", request);

        await Task.Delay(6000);

        var result = _service.GetTranscription("blablabla", started.Id);

        result.Should().NotBeNull();
        result!.Status.Should().Be(TranscriptionResult.StatusEnum.CompletedEnum);
    }
}