using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LabelVal.Results.ViewModels
{
    public partial class ReportGenerationViewModel : ObservableObject
    {
        private readonly ExcelReportGenerator _reportGenerator;

        public ReportGenerationViewModel()
        {
            _reportGenerator = new ExcelReportGenerator();
        }

        [RelayCommand]
        private async Task GenerateReport()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Workbook|*.xlsx",
                Title = "Save an Excel File",
                FileName = "PRIME_DM_Comparison_Report.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string outputPath = saveFileDialog.FileName;
                string templatePath = Path.Combine("Excel", "Templates", "PRIME_DM_Comparison_Template.xlsx");

                var input1Data = ParameterData.GetMockData(1);
                var input2Data = ParameterData.GetMockData(2);

                await _reportGenerator.GenerateReport(input1Data, input2Data, templatePath, outputPath);
            }
        }
    }
}