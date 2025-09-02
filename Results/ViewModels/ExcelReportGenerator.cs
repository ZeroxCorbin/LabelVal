using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LabelVal.Results.ViewModels
{
    public class ExcelReportGenerator
    {
        public async Task GenerateReport(
            IEnumerable<ParameterData> input1Data,
            IEnumerable<ParameterData> input2Data,
            string templatePath,
            string outputPath)
        {
            if (!File.Exists(templatePath))
            {
                MessageBox.Show($"Template file not found at: {templatePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var workbook = new XLWorkbook(templatePath))
                {
                    var input1Sheet = workbook.Worksheet("Input1");
                    var input2Sheet = workbook.Worksheet("Input2");

                    if (input1Sheet == null || input2Sheet == null)
                    {
                        MessageBox.Show("The template file must contain 'Input 1' and 'Input 2' sheets.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    PopulateSheet(input1Sheet, input1Data);
                    PopulateSheet(input2Sheet, input2Data);

                    // The 'Results' sheet should update automatically as formulas are recalculated on open.
                    // ClosedXML will preserve the formulas from the template.
                    // If you need to force calculation before saving, you can use:
                    // workbook.RecalculateAllFormulas();

                    await Task.Run(() => workbook.SaveAs(outputPath));

                    MessageBox.Show($"Report successfully generated at:\n{outputPath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while generating the report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // You might want to log the full exception details.
            }
        }

        private void PopulateSheet(IXLWorksheet worksheet, IEnumerable<ParameterData> data)
        {
            // Start from row 2, assuming row 1 is headers
            int row = 2;
            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value = item.Name;
                worksheet.Cell(row, 2).Value = item.Value;
                worksheet.Cell(row, 3).Value = item.Suffix;
                worksheet.Cell(row, 4).Value = item.GradeValue;
                worksheet.Cell(row, 5).Value = item.GradeLetter;
                row++;
            }
        }
    }

    public class ParameterData
    {
        public string Name { get; set; }
        public string? Value { get; set; }
        public string? Suffix { get; set; }
        public double? GradeValue { get; set; }
        public string? GradeLetter { get; set; }

        public static List<ParameterData> GetMockData(int set)
        {
            if (set == 1)
            {
                return new List<ParameterData>
                {
                    new() { Name = "Overall Grade", GradeValue = 4.0, GradeLetter = "A" },
                    new() { Name = "Decode", Value = "Pass" },
                    new() { Name = "Symbol Contrast", GradeValue = 3.5, GradeLetter = "B" },
                    new() { Name = "Modulation", GradeValue = 3.0, GradeLetter = "B" },
                    new() { Name = "X-Dimension", Value = "10.0", Suffix = "mils" }
                };
            }
            else
            {
                return new List<ParameterData>
                {
                    new() { Name = "Overall Grade", GradeValue = 3.0, GradeLetter = "B" },
                    new() { Name = "Decode", Value = "Pass" },
                    new() { Name = "Symbol Contrast", GradeValue = 2.5, GradeLetter = "C" },
                    new() { Name = "Modulation", GradeValue = 2.0, GradeLetter = "C" },
                    new() { Name = "X-Dimension", Value = "10.2", Suffix = "mils" }
                };
            }
        }
    }
}