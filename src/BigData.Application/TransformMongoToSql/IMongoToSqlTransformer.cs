namespace BigData.Application.TransformMongoToSql;

public interface IMongoToSqlTransformer
{
    Task TransformAsync(CancellationToken cancellationToken);
}