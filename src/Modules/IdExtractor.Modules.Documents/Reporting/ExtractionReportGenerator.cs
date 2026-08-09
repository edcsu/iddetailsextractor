using IdExtractor.Modules.Documents.Persistence;
using NetPdf.Fluent;
using NetPdf.Layout;

namespace IdExtractor.Modules.Documents.Reporting;

/// <summary>Renders a one-page PDF report: the submitted document image, document
/// type, extracted fields with confidences, MRZ validity, and timestamps.</summary>
public sealed class ExtractionReportGenerator
{
    private static readonly TextStyle TitleStyle = new() { FontSize = 18, Bold = true };
    private static readonly TextStyle BodyStyle = new() { FontSize = 11 };
    private static readonly TextStyle HeaderStyle = new() { FontSize = 10, Bold = true };
    private static readonly TextStyle CellStyle = new() { FontSize = 10 };

    public byte[] Generate(ExtractionRecord record, byte[] sourceImageBytes)
    {
        var document = Document.Create(doc => doc
            .Page(page => page
                .Size(PageSizes.A4)
                .Margin(36)
                .Content(content => content.Column(column =>
                {
                    column.Spacing(8);

                    column.Item().Text($"Extraction Report — {record.Id}", TitleStyle);
                    column.Item().Text($"Document type: {record.DocumentType} ({record.DocumentTypeConfidence:P0} confidence)", BodyStyle);
                    column.Item().Text($"Extracted: {record.CreatedAtUtc:u}", BodyStyle);
                    column.Item().Text(
                        $"MRZ valid: {(record.Mrz is null ? "N/A (no MRZ detected)" : record.Mrz.Valid.ToString())}",
                        BodyStyle);

                    column.Item().Image(ImageSource.FromBytes(sourceImageBytes));

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(140);
                            columns.RelativeColumn(1);
                            columns.ConstantColumn(80);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Field", HeaderStyle);
                            header.Cell().Text("Value", HeaderStyle);
                            header.Cell().Text("Confidence", HeaderStyle);
                        });

                        foreach (var (name, field) in record.Fields)
                        {
                            table.Cell().Text(name, CellStyle);
                            table.Cell().Text(field.Value, CellStyle);
                            table.Cell().Text($"{field.Confidence:P0}", CellStyle);
                        }
                    });
                }))));

        return document.ToBytes();
    }
}
