namespace Nexus.Api.Features.Runs;

public interface IRandomProvider
{
    double NextDouble();
    int Next(int minInclusive, int maxExclusive);
}

public class DefaultRandomProvider : IRandomProvider
{
    public double NextDouble() => Random.Shared.NextDouble();
    public int Next(int minInclusive, int maxExclusive) => Random.Shared.Next(minInclusive, maxExclusive);
}
