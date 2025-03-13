using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using Newtonsoft.Json.Linq;

namespace LabelVal.V275.Sectors;

public class SectorTemplate : ISectorTemplate
{
    public JObject V275Sector { get; }

    public string Name { get; set; }
    public string Username { get; set; }

    public string Version { get; set; }

    public double Top { get; set; }
    public double Left { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double AngleDeg { get; set; }

    public System.Drawing.Point CenterPoint { get; set; }

    public string SymbologyType { get; set; }

    public double Orientation { get; set; }
    public TemplateMatchMode MatchSettings { get; set; }
    public BlemishMaskLayers BlemishMask { get; set; }

    public SectorTemplate(JObject sectorTemplate, string version)
    {
        Version = version;

        V275Sector = sectorTemplate;

        Name = sectorTemplate["name"]?.ToString();
        Username = sectorTemplate["username"]?.ToString();

        Top = sectorTemplate["top"]?.Value<double>() ?? 0;
        Left = sectorTemplate["left"]?.Value<double>() ?? 0;
        Width = sectorTemplate["width"]?.Value<double>() ?? 0;
        Height = sectorTemplate["height"]?.Value<double>() ?? 0;
        AngleDeg = sectorTemplate["angle"]?.Value<double>() ?? 0;

        CenterPoint = new System.Drawing.Point(
            (int)(sectorTemplate["left"]?.Value<double>() ?? 0 + sectorTemplate["width"]?.Value<double>() ?? 0 / 2),
            (int)(sectorTemplate["top"]?.Value<double>() ?? 0 + sectorTemplate["height"]?.Value<double>() ?? 0 / 2)
        );

        Orientation = sectorTemplate["orientation"]?.Value<double>() ?? 0;

        SymbologyType = sectorTemplate["symbology"]?.ToString();

        if (sectorTemplate["matchSettings"] != null)
        {
            MatchSettings = new TemplateMatchMode
            {
                MatchMode = sectorTemplate["matchSettings"]["matchMode"].Value<int>(),
                UserDefinedDataTrueSize = sectorTemplate["matchSettings"]["userDefinedDataTrueSize"].Value<int>(),
                FixedText = sectorTemplate["matchSettings"]["fixedText"].ToString()
            };
        }

        //BlemishMask = new BlemishMaskLayers
        //{
        //    Layers = sectorTemplate["blemishMask"]?["layers"].ToObject<List<string>>()
        //};
    }

}
