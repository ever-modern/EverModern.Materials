using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;

namespace EverModern.DataProvision;

public static class ModelCreationExtensions
{
    public static void UseConversion<TSharpType, TDbType>(
        this ModelBuilder modelBuilder,
        Expression<Func<TSharpType, TDbType>> sharpToDb,
        Expression<Func<TDbType, TSharpType>> dbToSharp)
    {
        var decimalPropertyEntities = modelBuilder.Model.GetEntityTypes()
            .Select(t =>
            {
                var entityPropertiesOfType = new
                {
                    Entity = t.ClrType,
                    DecimalProperties = t.GetMembers().OfType<IMutablePropertyBase>().Where(m => m.ClrType == typeof(TSharpType)).ToArray()
                };
                return entityPropertiesOfType;
            })
            .Where(e => e.DecimalProperties.Any())
            .ToArray();

        var converter = new ValueConverter<TSharpType, TDbType>(sharpToDb, dbToSharp);

        foreach (var entity in decimalPropertyEntities)
        {
            foreach (var decimalProperty in entity.DecimalProperties)
            {
                modelBuilder.Entity(entity.Entity).Property(decimalProperty.Name).HasConversion(converter);
            }
        }
    }

    public static void EnumAsInt<TEnum>(this ModelBuilder modelBuilder)
        => modelBuilder.UseConversion<TEnum, int>(en => Convert.ToInt32(en), num => (TEnum)Enum.ToObject(typeof(TEnum), num));
}
