using System.Text.Json;
using EverModern.QueryKit;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;

namespace EverModern.DataProvision.Tests;

public sealed class DeferredQueryableItem
{
    [PrimaryKey]
    [Column]
    public long Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;
}

public abstract class QueryableTestsBase : IDisposable
{
    readonly DataConnection _connection = CreateConnectionAsync().Result;
    const string TableName = "DeferredQueryableItems";

    static async Task<DataConnection> CreateConnectionAsync(
        CancellationToken cancellationToken = default
    )
    {
        var schema = CreateMappingSchema();
        var connectionOptions = new DataOptions()
            .UseSQLite($"Data Source=:memory:")
            .UseMappingSchema(schema);
        var connection = new DataConnection(connectionOptions);

        await connection.ExecuteAsync(
            $"""
            CREATE TABLE IF NOT EXISTS {TableName}
            (
                Id INTEGER NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL
            );
            """
        );

        await connection.ExecuteAsync($"DELETE FROM {TableName};");
        await connection.ExecuteAsync(
            $"INSERT INTO {TableName}(Id, Name) VALUES (1, 'One'), (2, 'Two'), (3, 'Three');"
        );

        connection.BeginTransaction();

        return connection;
    }

    protected DeferredQueryable<DeferredQueryableItem> GetQueryable() =>
        new(
            _connection.GetTable<DeferredQueryableItem>(),
            con => con.GetTable<DeferredQueryableItem>(),
            async _ => await ValueTask.FromResult(_connection)
        );

    public void Dispose() => _connection.Dispose();

    static MappingSchema CreateMappingSchema()
    {
        var mappingSchema = new MappingSchema();
        var builder = new FluentMappingBuilder(mappingSchema);
        builder
            .Entity<DeferredQueryableItem>()
            .HasTableName(TableName)
            .Property(m => m.Id)
            .HasColumnName("Id")
            .Property(m => m.Name)
            .HasColumnName("Name");

        builder.Build();

        return mappingSchema;
    }
}
