namespace IdExtractor.SharedKernel.Auditing;

/// <summary>
/// Marks a property as sensitive so <see cref="AuditSaveChangesInterceptor"/> excludes
/// its value from the audit log payload. Apply to any property that can hold extracted
/// document PII (names, document numbers, dates of birth, etc.).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SensitiveAttribute : Attribute;
