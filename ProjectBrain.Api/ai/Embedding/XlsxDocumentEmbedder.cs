using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ProjectBrain.AI.Embedding;

/// <summary>
/// Embedder for Excel files (.xlsx)
/// </summary>
public class XlsxDocumentEmbedder : BaseDocumentEmbedder
{
    public XlsxDocumentEmbedder(ILogger<XlsxDocumentEmbedder> logger) : base(logger)
    {
    }

    public override IEnumerable<string> SupportedExtensions => new[] { ".xlsx" };

    public override async Task<List<DocumentPage>> ExtractTextAsync(Stream stream, string filename)
    {
        Logger.LogInformation("Extracting text from XLSX file: {Filename}", filename);
        
        stream.Position = 0;
        var pages = new List<DocumentPage>();
        
        using var spreadsheetDocument = SpreadsheetDocument.Open(stream, false);
        var workbookPart = spreadsheetDocument.WorkbookPart;
        
        if (workbookPart == null)
        {
            Logger.LogWarning("Could not read workbook from XLSX file: {Filename}", filename);
            return pages;
        }
        
        var sharedStringTable = workbookPart.SharedStringTablePart?.SharedStringTable;
        var sheets = workbookPart.Workbook?.Sheets;
        
        if (sheets == null)
        {
            return pages;
        }
        
        var title = Path.GetFileNameWithoutExtension(filename);
        var pageNumber = 1;
        
        foreach (var sheet in sheets.Elements<Sheet>())
        {
            var sheetName = sheet.Name?.Value ?? $"Sheet{pageNumber}";
            var worksheetPart = (WorksheetPart?)workbookPart.GetPartById(sheet.Id?.Value ?? string.Empty);
            
            if (worksheetPart?.Worksheet == null)
                continue;
            
            var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
            if (sheetData == null)
                continue;
            
            var sheetContent = new System.Text.StringBuilder();
            sheetContent.AppendLine($"Sheet: {sheetName}");
            
            foreach (var row in sheetData.Elements<Row>())
            {
                var rowValues = new List<string>();
                
                foreach (var cell in row.Elements<Cell>())
                {
                    var cellValue = GetCellValue(cell, sharedStringTable);
                    if (!string.IsNullOrWhiteSpace(cellValue))
                    {
                        rowValues.Add(cellValue);
                    }
                }
                
                if (rowValues.Count > 0)
                {
                    sheetContent.AppendLine(string.Join(" | ", rowValues));
                }
            }
            
            pages.Add(new DocumentPage
            {
                PageNumber = pageNumber++,
                Content = sheetContent.ToString(),
                Title = pageNumber == 2 ? title : $"{title} - {sheetName}"
            });
        }
        
        Logger.LogInformation("Extracted {PageCount} pages (sheets) from XLSX file: {Filename}", pages.Count, filename);
        return pages;
    }
    
    private string GetCellValue(Cell cell, SharedStringTable? sharedStringTable)
    {
        if (cell.CellValue == null)
            return string.Empty;
        
        var cellValue = cell.CellValue.Text;
        
        // If the cell contains a shared string, look it up
        if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString && sharedStringTable != null)
        {
            if (int.TryParse(cellValue, out var index) && index < sharedStringTable.ChildElements.Count)
            {
                var sharedStringItem = sharedStringTable.ChildElements[index] as SharedStringItem;
                return sharedStringItem?.Text?.Text ?? string.Empty;
            }
        }
        
        return cellValue;
    }
}

