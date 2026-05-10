using MongoDB.Driver;
using Moq;

namespace Nexus.Api.Tests;

public static class MongoMocks
{
    public static IAsyncCursor<T> Cursor<T>(IEnumerable<T> items)
    {
        var list = items.ToList();
        var mock = new Mock<IAsyncCursor<T>>();
        mock.Setup(c => c.Current).Returns(list);
        var moveNext = mock.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()));
        var moveNextAsync = mock.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()));
        if (list.Count > 0)
        {
            moveNext.Returns(true).Returns(false);
            moveNextAsync.ReturnsAsync(true).ReturnsAsync(false);
        }
        else
        {
            moveNext.Returns(false);
            moveNextAsync.ReturnsAsync(false);
        }
        return mock.Object;
    }

    public static void SetupFindReturning<T>(this Mock<IMongoCollection<T>> col, IEnumerable<T> items)
    {
        col
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<T>>(),
                It.IsAny<FindOptions<T, T>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Cursor(items));
    }

    public static void SetupFindOneAndUpdateReturning<T>(this Mock<IMongoCollection<T>> col, T? returned)
    {
        col
            .Setup(c => c.FindOneAndUpdateAsync<T>(
                It.IsAny<FilterDefinition<T>>(),
                It.IsAny<UpdateDefinition<T>>(),
                It.IsAny<FindOneAndUpdateOptions<T, T>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(returned!);
    }
}
