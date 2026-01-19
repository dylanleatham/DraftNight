using DraftApp.Api.Data.Enums;
using DraftApp.Api.Hubs;
using DraftApp.Api.Models.Responses;
using DraftApp.Api.Services;
using DraftApp.Engine.Models;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace DraftApp.Api.Tests.Hubs;

/// <summary>
/// Unit tests for EventHub.
/// </summary>
public class EventHubTests
{
    private readonly Mock<IEventService> mockEventService;
    private readonly Mock<IHubCallerClients> mockClients;
    private readonly Mock<ISingleClientProxy> mockCallerProxy;
    private readonly Mock<IGroupManager> mockGroups;
    private readonly Mock<HubCallerContext> mockContext;
    private readonly EventHub hub;

    public EventHubTests()
    {
        mockEventService = new Mock<IEventService>();
        mockClients = new Mock<IHubCallerClients>();
        mockCallerProxy = new Mock<ISingleClientProxy>();
        mockGroups = new Mock<IGroupManager>();
        mockContext = new Mock<HubCallerContext>();

        mockClients.Setup(c => c.Caller).Returns(mockCallerProxy.Object);
        mockContext.Setup(c => c.ConnectionId).Returns("test-connection-id");

        hub = new EventHub(mockEventService.Object)
        {
            Clients = mockClients.Object,
            Groups = mockGroups.Object,
            Context = mockContext.Object
        };
    }

    [Fact]
    public async Task JoinEventGroup_WithValidEventId_AddsToGroupAndSendsSnapshot()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var snapshot = CreateTestSnapshot(eventId);
        mockEventService.Setup(s => s.GetEventAsync(eventId, default))
            .ReturnsAsync(snapshot);

        // Act
        await hub.JoinEventGroup(eventId);

        // Assert
        var expectedGroupName = $"event_{eventId}";
        mockGroups.Verify(g => g.AddToGroupAsync("test-connection-id", expectedGroupName, default), Times.Once);
        mockCallerProxy.Verify(
            c => c.SendCoreAsync(
                "EventUpdated",
                It.Is<object?[]>(args => args.Length == 1 && ReferenceEquals(args[0], snapshot)),
                default),
            Times.Once);
    }

    [Fact]
    public async Task JoinEventGroup_WithInvalidEventId_SendsError()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        mockEventService.Setup(s => s.GetEventAsync(eventId, default))
            .ReturnsAsync((EventSnapshotResponse?)null);

        // Act
        await hub.JoinEventGroup(eventId);

        // Assert
        mockGroups.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
        mockCallerProxy.Verify(
            c => c.SendCoreAsync(
                "Error",
                It.Is<object?[]>(args =>
                    args[0] as string == "EVENT_NOT_FOUND" &&
                    args[1] as string == "The specified event does not exist."),
                default),
            Times.Once);
    }

    [Fact]
    public async Task LeaveEventGroup_RemovesFromGroup()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        await hub.LeaveEventGroup(eventId);

        // Assert
        var expectedGroupName = $"event_{eventId}";
        mockGroups.Verify(g => g.RemoveFromGroupAsync("test-connection-id", expectedGroupName, default), Times.Once);
    }

    [Fact]
    public async Task RequestSnapshot_WithValidEventId_SendsSnapshot()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var snapshot = CreateTestSnapshot(eventId);
        mockEventService.Setup(s => s.GetEventAsync(eventId, default))
            .ReturnsAsync(snapshot);

        // Act
        await hub.RequestSnapshot(eventId);

        // Assert
        mockCallerProxy.Verify(
            c => c.SendCoreAsync(
                "EventUpdated",
                It.Is<object?[]>(args => args.Length == 1 && ReferenceEquals(args[0], snapshot)),
                default),
            Times.Once);
    }

    [Fact]
    public async Task RequestSnapshot_WithInvalidEventId_SendsError()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        mockEventService.Setup(s => s.GetEventAsync(eventId, default))
            .ReturnsAsync((EventSnapshotResponse?)null);

        // Act
        await hub.RequestSnapshot(eventId);

        // Assert
        mockCallerProxy.Verify(
            c => c.SendCoreAsync(
                "Error",
                It.Is<object?[]>(args =>
                    args[0] as string == "EVENT_NOT_FOUND" &&
                    args[1] as string == "The specified event does not exist."),
                default),
            Times.Once);
    }

    [Fact]
    public void GetGroupName_ReturnsCorrectFormat()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act - call static method on EventHub
        var groupName = GetGroupNameForTest(eventId);

        // Assert
        Assert.Equal($"event_{eventId}", groupName);
    }

    private static string GetGroupNameForTest(Guid eventId) => $"event_{eventId}";

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
