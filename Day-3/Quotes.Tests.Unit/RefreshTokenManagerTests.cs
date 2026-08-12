using FluentAssertions;
using QuotesApi.Models;
using QuotesApi.Services;

namespace Quotes.Tests.Unit;

public class RefreshTokenManagerTests
{
    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }

    [Fact]
    public void IsReuseDetected_ActiveToken_ReturnsFalse()
    {
        // Arrange
        var clock = new FakeClock
        {
            UtcNow = new DateTimeOffset(
                2026, 8, 12, 10, 0, 0, TimeSpan.Zero)
        };

        var manager = new RefreshTokenManager(clock);

        var token = new RefreshToken
        {
            Token = "token",
            UserId = 1,
            ExpiresAt = clock.UtcNow.UtcDateTime.AddDays(7)
        };

        // Act
        var result = manager.IsReuseDetected(token);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsReuseDetected_RevokedWithoutReplacement_ReturnsFalse()
    {
        // Arrange
        var clock = new FakeClock
        {
            UtcNow = new DateTimeOffset(
                2026, 8, 12, 10, 0, 0, TimeSpan.Zero)
        };

        var manager = new RefreshTokenManager(clock);

        var token = new RefreshToken
        {
            Token = "token",
            UserId = 1,
            ExpiresAt = clock.UtcNow.UtcDateTime.AddDays(7),
            RevokedAt = clock.UtcNow.UtcDateTime
        };

        // Act
        var result = manager.IsReuseDetected(token);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsReuseDetected_RevokedAndReplaced_ReturnsTrue()
    {
        // Arrange
        var clock = new FakeClock
        {
            UtcNow = new DateTimeOffset(
                2026, 8, 12, 10, 0, 0, TimeSpan.Zero)
        };

        var manager = new RefreshTokenManager(clock);

        var token = new RefreshToken
        {
            Token = "old-token",
            UserId = 1,
            ExpiresAt = clock.UtcNow.UtcDateTime.AddDays(7),
            RevokedAt = clock.UtcNow.UtcDateTime,
            ReplacedByToken = "new-token"
        };

        // Act
        var result = manager.IsReuseDetected(token);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RevokeTokenFamily_ActiveTokens_SetsRevokedAt()
    {
        // Arrange
        var now = new DateTimeOffset(
            2026, 8, 12, 10, 0, 0, TimeSpan.Zero);

        var clock = new FakeClock
        {
            UtcNow = now
        };

        var manager = new RefreshTokenManager(clock);

        var tokens = new[]
        {
            new RefreshToken
            {
                Token = "token-1",
                UserId = 1,
                ExpiresAt = now.UtcDateTime.AddDays(7)
            },
            new RefreshToken
            {
                Token = "token-2",
                UserId = 1,
                ExpiresAt = now.UtcDateTime.AddDays(7)
            }
        };

        // Act
        manager.RevokeTokenFamily(tokens);

        // Assert
        tokens.Should().OnlyContain(
            token => token.RevokedAt == now.UtcDateTime);
    }

    [Fact]
    public void RevokeTokenFamily_AlreadyRevokedToken_DoesNotChangeRevokedAt()
    {
        // Arrange
        var now = new DateTimeOffset(
            2026, 8, 12, 10, 0, 0, TimeSpan.Zero);

        var oldRevokedAt = now.UtcDateTime.AddHours(-1);

        var clock = new FakeClock
        {
            UtcNow = now
        };

        var manager = new RefreshTokenManager(clock);

        var token = new RefreshToken
        {
            Token = "token",
            UserId = 1,
            ExpiresAt = now.UtcDateTime.AddDays(7),
            RevokedAt = oldRevokedAt
        };

        // Act
        manager.RevokeTokenFamily(new[] { token });

        // Assert
        token.RevokedAt.Should().Be(oldRevokedAt);
    }

    [Fact]
    public void IsExpired_ExpiredToken_ReturnsTrue()
    {
        // Arrange
        var now = new DateTimeOffset(
            2026, 8, 12, 10, 0, 0, TimeSpan.Zero);

        var clock = new FakeClock
        {
            UtcNow = now
        };

        var manager = new RefreshTokenManager(clock);

        var token = new RefreshToken
        {
            Token = "token",
            UserId = 1,
            ExpiresAt = now.UtcDateTime.AddMinutes(-1)
        };

        // Act
        var result = manager.IsExpired(token);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_FutureToken_ReturnsFalse()
    {
        // Arrange
        var now = new DateTimeOffset(
            2026, 8, 12, 10, 0, 0, TimeSpan.Zero);

        var clock = new FakeClock
        {
            UtcNow = now
        };

        var manager = new RefreshTokenManager(clock);

        var token = new RefreshToken
        {
            Token = "token",
            UserId = 1,
            ExpiresAt = now.UtcDateTime.AddMinutes(1)
        };

        // Act
        var result = manager.IsExpired(token);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_TokenExpiresExactlyNow_ReturnsTrue()
    {
        // Arrange
        var now = new DateTimeOffset(
            2026, 8, 12, 10, 0, 0, TimeSpan.Zero);

        var clock = new FakeClock
        {
            UtcNow = now
        };

        var manager = new RefreshTokenManager(clock);

        var token = new RefreshToken
        {
            Token = "token",
            UserId = 1,
            ExpiresAt = now.UtcDateTime
        };

        // Act
        var result = manager.IsExpired(token);

        // Assert
        result.Should().BeTrue();
    }
}