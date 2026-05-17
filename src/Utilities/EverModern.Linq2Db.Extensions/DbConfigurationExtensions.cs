using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

namespace EverModern.QueryKit;

public static class DbConfigurationExtensions
{
    extension(MappingSchema mappingSchema)
    {
        public MappingSchema UseConversion<TSource, TTarget>(
             DataType dataType,
             Expression<Func<TSource, TTarget>> convertTo,
             Expression<Func<TTarget, TSource>> convertFrom
         )
        {
            mappingSchema.SetDataType(typeof(TSource), dataType);

            mappingSchema.SetConverter(convertTo.Compile());
            mappingSchema.SetConverter(convertFrom.Compile());

            mappingSchema.SetConvertExpression<TSource, TTarget>(convertTo);
            mappingSchema.SetConvertExpression<TTarget, TSource>(convertFrom);

            // Force ADO.NET parameter binding to use the converted value with the explicit DataType,
            // preventing the driver (e.g. Microsoft.Data.Sqlite) from serializing the raw CLR value.
            var compiledTo = convertTo.Compile();
            mappingSchema.SetConvertExpression<TSource, DataParameter>(
                src => new DataParameter { DataType = dataType, Value = compiledTo(src) }
            );

            return mappingSchema;
        }
    }
}
