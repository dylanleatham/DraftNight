using DraftApp.Api.Data.Enums;
using DraftApp.Api.Hubs;
using DraftApp.Api.Models.Responses;
using DraftApp.Api.Services;
using DraftApp.Engine.Models;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace DraftApp.Api.Tests.Services;

/// <summary>
/// Unit tests for EventNotificationService.
/// </summary>
public class EventNotificationServiceTests
{
    private readonly Mock<IHubContext<EventHub>> mockHubContext;
    private readonly Mock<IHubClients> mockClients;
    private readonly Mock<IClientProxy> mockClientProxy;
    private readonly EventNotificationService service;

    public EventNotificationServiceTests()
    {
        mockHubContext = new Mock<IHubContext<EventHub>>();
        mockClients = new Mock<IHubClients>();
        mockClientProxy = new Mock<IClientProxy>();

        mockHubContext.Setup(x => x.Clients).Returns(mockClients.Object);
        mockClients.Setup(x => x.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);

        service = new EventNotificationService(mockHubContext.Object);
    }

    [Fact]
    public async Task BroadcastEventUpdateAsync_SendsToCorrectGroup()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var snapshot = CreateTestSnapshot(eventId);

        // Act
        await service.BroadcastEventUpdateAsync(eventId, snapshot);

        // Assert
        var expectedGroupName = $"event_{eventId}";
        mockClients.Verify(x => x.Group(expectedGroupName), Times.Once);
    }

    [Fact]
    public async Task BroadcastEventUpdateAsync_SendsEventUpdatedMessage()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var snapshot = CreateTestSnapshot(eventId);

        // Act
        await service.BroadcastEventUpdateAsync(eventId, snapshot);

        // Assert
        mockClientProxy.Verify(
            x => x.SendCoreAsync(
                "EventUpdated",
                It.Is<object?[]>(args => args.Length == 1 && ReferenceEquals(args[0], snapshot)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BroadcastEventUpdateAsync_PassesCancellationToken()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var snapshot = CreateTestSnapshot(eventId);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await service.BroadcastEventUpdateAsync(eventId, snapshot, token);

        // Assert
        mockClientProxy.Verify(
            x => x.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                token),
            Times.Once);
    }

    [Fact]
    public async Task BroadcastEventUpdateAsync_DifferentEvents_SendToDifferentGroups()
    {
        // Arrange
        var eventId1 = Guid.NewGuid();
        var eventId2 = Guid.NewGuid();
        var snapshot1 = CreateTestSnapshot(eventId1);
        var snapshot2 = CreateTestSnapshot(eventId2);

        // Act
        await service.BroadcastEventUpdateAsync(eventId1, snapshot1);
        await service.BroadcastEventUpdateAsync(eventId2, snapshot2);

        // Assert
        mockClients.Verify(x => x.Group($"event_{eventId1}"), Times.Once);
        mockClients.Verify(x => x.Group($"event_{eventId2}"), Times.Once);
    }

    private static EventSnapshotResponse CreateTestSnapshot(Guid eventId) => new()
    {
        Id = eventId,
        Name = "Test Event",
        Status = EventStatus.Setup,
        JoinCode = "ABC123",
        PacksInBox = 36,
        PrizePacks = 12,
        Format = TournamentFormat.RoundRobin,
        TotalRounds = 3,
        CurrentRound = 0,
        PrizesAllocated = false,
        Version = 1,
        Players = [],
        Rounds = [],
        PrizeAllocations = []
    };
}
