using System.Reflection;
using IdExtractor.SharedKernel.Auditing;
using Serilog.Core;
using Serilog.Events;

namespace IdExtractor.Api.Infrastructure.Logging;

/// <summary>
/// Ensures anything logged with Serilog's destructuring operator (<c>@</c>) never
/// surfaces a property marked <see cref="SensitiveAttribute"/> — the same attribute
/// modules use to keep extracted PII out of the audit log.
/// </summary>
public sealed class SensitiveDestructuringPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        var properties = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0)
            .ToList();

        if (properties.TrueForAll(p => p.GetCustomAttribute<SensitiveAttribute>() is null))
        {
            result = null!;
            return false;
        }

        var logProperties = properties.Select(property =>
        {
            var propertyValue = property.GetCustomAttribute<SensitiveAttribute>() is not null
                ? new ScalarValue("[REDACTED]")
                : propertyValueFactory.CreatePropertyValue(property.GetValue(value), destructureObjects: true);
            return new LogEventProperty(property.Name, propertyValue);
        });

        result = new StructureValue(logProperties, value.GetType().Name);
        return true;
    }
}
