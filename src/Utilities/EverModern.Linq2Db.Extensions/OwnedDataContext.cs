using System.Data.Common;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Interceptors;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;

namespace EverModern.QueryKit;

public delegate ValueTask<TConnection> ConnectionFactory<TConnection>(
    CancellationToken cancellationToken = default
)
    where TConnection : IDataContext;

public class OwnedDataContext(IDataContext dataContext, Action onDisposed) : IDataContext
{
    public string ContextName => dataContext.ContextName;

    public Func<ISqlBuilder> CreateSqlBuilder => dataContext.CreateSqlBuilder;

    public Func<DataOptions, ISqlOptimizer> GetSqlOptimizer => dataContext.GetSqlOptimizer;

    public SqlProviderFlags SqlProviderFlags => dataContext.SqlProviderFlags;

    public TableOptions SupportedTableOptions => dataContext.SupportedTableOptions;

    public Type DataReaderType => dataContext.DataReaderType;

    public MappingSchema MappingSchema => dataContext.MappingSchema;

    public bool InlineParameters
    {
        get => dataContext.InlineParameters;
        set => dataContext.InlineParameters = value;
    }

    public List<string> QueryHints => dataContext.QueryHints;

    public List<string> NextQueryHints => dataContext.NextQueryHints;

    public bool CloseAfterUse
    {
        get => dataContext.CloseAfterUse;
        set => dataContext.CloseAfterUse = value;
    }

    public DataOptions Options => dataContext.Options;

    public string? ConfigurationString => dataContext.ConfigurationString;

    public int ConfigurationID => dataContext.ConfigurationID;

    public void AddInterceptor(IInterceptor interceptor) => dataContext.AddInterceptor(interceptor);

    public void AddMappingSchema(MappingSchema mappingSchema) =>
        dataContext.AddMappingSchema(mappingSchema);

    public void Close()
    {
        try
        {
            dataContext.Close();
        }
        finally
        {
            onDisposed?.Invoke();
        }
    }

    public async Task CloseAsync()
    {
        try
        {
            await dataContext.CloseAsync();
        }
        finally
        {
            onDisposed?.Invoke();
        }
    }

    public void Dispose()
    {
        try
        {
            dataContext.Dispose();
        }
        finally
        {
            onDisposed?.Invoke();
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await dataContext.DisposeAsync();
        }
        finally
        {
            onDisposed?.Invoke();
        }
    }

    public IQueryRunner GetQueryRunner(
        Query query,
        IDataContext parametersContext,
        int queryNumber,
        IQueryExpressions expressions,
        object?[]? parameters,
        object?[]? preambles
    ) =>
        dataContext.GetQueryRunner(
            query,
            parametersContext,
            queryNumber,
            expressions,
            parameters,
            preambles
        );

    public Expression GetReaderExpression(
        DbDataReader reader,
        int idx,
        Expression readerExpression,
        Type toType
    ) => dataContext.GetReaderExpression(reader, idx, readerExpression, toType);

    public bool? IsDBNullAllowed(DbDataReader reader, int idx) =>
        dataContext.IsDBNullAllowed(reader, idx);

    public void RemoveInterceptor(IInterceptor interceptor) =>
        dataContext.RemoveInterceptor(interceptor);

    public void SetMappingSchema(MappingSchema mappingSchema) =>
        dataContext.SetMappingSchema(mappingSchema);

    public IDisposable? UseMappingSchema(MappingSchema mappingSchema) =>
        dataContext.UseMappingSchema(mappingSchema);

    public IDisposable? UseOptions(Func<DataOptions, DataOptions> optionsSetter) =>
        dataContext.UseOptions(optionsSetter);
}
