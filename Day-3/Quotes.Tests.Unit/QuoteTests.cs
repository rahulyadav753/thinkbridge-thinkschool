using FluentAssertions;
using QuotesApi.Models;

namespace Quotes.Tests.Unit;

public class QuoteTests
{
    [Fact]
    public void Create_ValidAuthorAndText_ReturnsQuote()
    {
        // Arrange
        var author = "Albert Einstein";
        var text = "Life is like riding a bicycle.";

        // Act
        var result = Quote.Create(author, text);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Quote.Should().NotBeNull();
        result.Quote!.Author.Should().Be(author);
        result.Quote.Text.Should().Be(text);
        result.Quote.IsDeleted.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Create_InvalidAuthor_ReturnsRequiredError(string? author)
    {
        // Arrange
        var text = "A valid quote.";

        // Act
        var result = Quote.Create(author, text);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Quote.Should().BeNull();
        result.Error.Should().Be("Author is required.");
    }

    [Fact]
    public void Create_AuthorExceedsMaximumLength_ReturnsFailure()
    {
        // Arrange
        var author = new string('A', Quote.MaxAuthorLength + 1);
        var text = "A valid quote.";

        // Act
        var result = Quote.Create(author, text);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Quote.Should().BeNull();
        result.Error.Should()
            .Be($"Author must be {Quote.MinAuthorLength}-{Quote.MaxAuthorLength} characters.");
    }

    [Fact]
    public void Create_AuthorAtMaximumLength_ReturnsQuote()
    {
        // Arrange
        var author = new string('A', Quote.MaxAuthorLength);
        var text = "A valid quote.";

        // Act
        var result = Quote.Create(author, text);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Quote.Should().NotBeNull();
        result.Quote!.Author.Should().HaveLength(Quote.MaxAuthorLength);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Create_InvalidText_ReturnsRequiredError(string? text)
    {
        // Arrange
        var author = "Albert Einstein";

        // Act
        var result = Quote.Create(author, text);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Quote.Should().BeNull();
        result.Error.Should().Be("Text is required.");
    }

    [Fact]
    public void Create_TextExceedsMaximumLength_ReturnsFailure()
    {
        // Arrange
        var author = "Albert Einstein";
        var text = new string('T', Quote.MaxTextLength + 1);

        // Act
        var result = Quote.Create(author, text);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Quote.Should().BeNull();
        result.Error.Should()
            .Be($"Text must be {Quote.MinTextLength}-{Quote.MaxTextLength} characters.");
    }

    [Fact]
    public void Create_TextAtMaximumLength_ReturnsQuote()
    {
        // Arrange
        var author = "Albert Einstein";
        var text = new string('T', Quote.MaxTextLength);

        // Act
        var result = Quote.Create(author, text);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Quote.Should().NotBeNull();
        result.Quote!.Text.Should().HaveLength(Quote.MaxTextLength);
    }

    [Fact]
    public void Create_TextAndAuthorWithWhitespace_TrimsValues()
    {
        // Arrange
        var author = "  Albert Einstein  ";
        var text = "  Life is like riding a bicycle.  ";

        // Act
        var result = Quote.Create(author, text);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Quote.Should().NotBeNull();
        result.Quote!.Author.Should().Be("Albert Einstein");
        result.Quote.Text.Should().Be("Life is like riding a bicycle.");
    }

    [Fact]
    public void SoftDelete_QuoteNotDeleted_SetsIsDeletedToTrue()
    {
        // Arrange
        var result = Quote.Create(
            "Albert Einstein",
            "Life is like riding a bicycle.");

        var quote = result.Quote!;

        // Act
        quote.SoftDelete();

        // Assert
        quote.IsDeleted.Should().BeTrue();
    }
}