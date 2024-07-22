using LabelVal.Sectors.Interfaces;

namespace LabelVal.V275.Sectors;

public class Template : ITemplate
{
    public V275_REST_lib.Models.Job.Sector V275Sector { get; }

    public string Name { get; set; }
    public string Username { get; set; }
    public int Top { get; set; }
    public System.Drawing.Point CenterPoint { get; set; }
    public string Symbology { get; set; }

    public TemplateMatchMode MatchSettings { get; set; }
    public BlemishMaskLayers BlemishMask { get; set; }

    public Template(V275_REST_lib.Models.Job.Sector sectorTemplate)
    {
        V275Sector = sectorTemplate;

        Name = sectorTemplate.name;
        Username = sectorTemplate.username;
        Top = sectorTemplate.top;
        CenterPoint = new System.Drawing.Point(sectorTemplate.left + sectorTemplate.width / 2, sectorTemplate.top + sectorTemplate.height / 2);

        Symbology = sectorTemplate.symbology;

        if (sectorTemplate.matchSettings != null)
            MatchSettings = new TemplateMatchMode
            {
                MatchMode = sectorTemplate.matchSettings.matchMode,
                UserDefinedDataTrueSize = sectorTemplate.matchSettings.userDefinedDataTrueSize,
                FixedText = sectorTemplate.matchSettings.fixedText
            };

        BlemishMask = new BlemishMaskLayers
        {
            Layers = sectorTemplate.blemishMask?.layers
        };
    }
}
