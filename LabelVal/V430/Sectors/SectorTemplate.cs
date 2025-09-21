using BarcodeVerification.lib.Extensions;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using Newtonsoft.Json.Linq;
using System.Windows;

namespace LabelVal.V430.Sectors;

public class SectorTemplate : ISectorTemplate
{
    public JObject Original { get; set; }

    public string Name { get; set; }
    public string Username { get; set; }

    public string Version { get; set; }

    public double Top { get; set; }
    public double Left { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double AngleDeg { get; set; }

    public System.Drawing.Point CenterPoint => new System.Drawing.Point((int)(Left + Width / 2), (int)(Top + Height / 2));

    public double Orientation { get; set; }

    public TemplateMatchMode MatchSettings { get; set; }
    public BlemishMaskLayers BlemishMask { get; set; }

    public SectorTemplate(JObject report, string reportId, JObject template, string name, string version)
    {
        if (report == null || template == null)
            return;

        Original = template;
        Version = version;
        Name = name;
        Username = name;

        var ipReport = report.GetParameter<JObject>($"ipReports[uId:{reportId}]");

        var woi = ipReport?.GetParameter<string>("reports[0].woi"); //"0, 0, 1280, 960"
        if (woi != null)
        {
            var woiSplit = woi.Trim('\"').Split(',');
            if (woiSplit.Length == 4)
            {
                Left = double.Parse(woiSplit[0]);
                Top = double.Parse(woiSplit[1]);
                Width = double.Parse(woiSplit[3]);
                Height = double.Parse(woiSplit[2]);
            }
            else
            {
                Top = 0;
                Left = 0;
                Width = report.GetParameter<double>("sensorInfo.width");
                Height = report.GetParameter<double>("sensorInfo.height");
            }
        }

        AngleDeg = 0;
        Orientation = 0;

    }

    public SectorTemplate() { }
}
