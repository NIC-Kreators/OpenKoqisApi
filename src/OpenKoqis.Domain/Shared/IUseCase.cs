namespace OpenKoqis.Domain.Shared;

// ReSharper disable once TypeParameterCanBeVariant
public interface IUseCase<TInput, TOutput>
{
    Task<TOutput> Execute(TInput input);
}
