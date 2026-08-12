using FluentAssertions;
using QuotesApi.Services;

namespace Quotes.Tests.Unit;

public class RefreshTokenServiceTests
{
    [Fact]
    public void Generate_WhenCalled_ReturnsNonEmptyToken()
    {
        // Arrange

        // Act
        var token = RefreshTokenService.Generate();

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Generate_WhenCalled_ReturnsTokenWithExpectedBase64Length()
    {
        // Arrange

        // Act
        var token = RefreshTokenService.Generate();

        // Assert
        token.Should().HaveLength(44);
    }

    [Fact]
    public void Generate_WhenCalledTwice_ReturnsDifferentTokens()
    {
        // Arrange

        // Act
        var firstToken = RefreshTokenService.Generate();
        var secondToken = RefreshTokenService.Generate();

        // Assert
        firstToken.Should().NotBe(secondToken);
    }

    [Fact]
    public void Hash_ValidToken_ReturnsNonEmptyHash()
    {
        // Arrange
        var token = "test-refresh-token";

        // Act
        var hash = RefreshTokenService.Hash(token);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Hash_ValidToken_Returns64CharacterSha256Hash()
    {
        // Arrange
        var token = "test-refresh-token";

        // Act
        var hash = RefreshTokenService.Hash(token);

        // Assert
        hash.Should().HaveLength(64);
    }

    [Fact]
    public void Hash_SameToken_ReturnsSameHash()
    {
        // Arrange
        var token = "test-refresh-token";

        // Act
        var firstHash = RefreshTokenService.Hash(token);
        var secondHash = RefreshTokenService.Hash(token);

        // Assert
        firstHash.Should().Be(secondHash);
    }

    [Fact]
    public void Hash_DifferentTokens_ReturnDifferentHashes()
    {
        // Arrange
        var firstToken = "refresh-token-one";
        var secondToken = "refresh-token-two";

        // Act
        var firstHash = RefreshTokenService.Hash(firstToken);
        var secondHash = RefreshTokenService.Hash(secondToken);

        // Assert
        firstHash.Should().NotBe(secondHash);
    }

    [Fact]
    public void Hash_Token_ReturnsHashDifferentFromOriginalToken()
    {
        // Arrange
        var token = "test-refresh-token";

        // Act
        var hash = RefreshTokenService.Hash(token);

        // Assert
        hash.Should().NotBe(token);
    }
}