using Cqrs.Abstractions;

namespace BigData.Application.TransformMongoToSql;

public sealed class TransformMongoToSqlCommandHandler(IMongoToSqlTransformer transformer) : ICommandHandler<TransformMongoToSqlCommand>
{
    public async Task HandleAsync(TransformMongoToSqlCommand command, CancellationToken cancellationToken)
    {
        await transformer.TransformAsync(cancellationToken);
    }
}