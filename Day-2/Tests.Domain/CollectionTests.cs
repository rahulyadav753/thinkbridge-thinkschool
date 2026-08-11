using FluentAssertions;
using QuotesApi.Models;

namespace Tests.Domain;

public class CollectionTests
{
    [Fact]
    public void EmptyName_Throws()
    {
        var act = () => new Collection("", 1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NameOver80Characters_Throws()
    {
        var name = new string('A', 81);

        var act = () => new Collection(name, 1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Adding51stItem_Throws()
    {
        var collection = new Collection("My Collection", 1);

        for (var i = 1; i <= 50; i++)
            collection.AddItem(i);

        var act = () => collection.AddItem(51);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DuplicateQuoteId_Throws()
    {
        var collection = new Collection("My Collection", 1);

        collection.AddItem(1);

        var act = () => collection.AddItem(1);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RemovingNonExistentItem_Throws()
    {
        var collection = new Collection("My Collection", 1);

        var act = () => collection.RemoveItem(999);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddingThenRemoving_LeavesZeroItems()
    {
        var collection = new Collection("My Collection", 1);

        collection.AddItem(1);
        collection.RemoveItem(1);

        collection.Items.Should().BeEmpty();
    }
}