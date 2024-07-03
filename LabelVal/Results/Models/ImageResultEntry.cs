using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.Results.Models;
public class ImageResultEntry
{
    public long TimeDate { get; set; }

    public int LoopCount { get; set; }

    public string SourceImageUID { get; set; }
    public byte[] SourceImage { get; set; }

    public string V275_StoredTemplate { get; set; }
    public string V275_StoredReport { get; set; }
    public string V275_StoredImage { get; set; }
    public string V275_CurrentTemplate { get; set; }
    public string V275_CurrentReport { get; set; }
    public string V275_CurrentImage { get; set; }

    public string V5_StoredTemplate { get; set; }
    public string V5_StoredReport { get; set; }
    public string V5_StoredImage { get; set; }
    public string V5_CurrentTemplate { get; set; }
    public string V5_CurrentReport { get; set; }
    public string V5_CurrentImage { get; set; }
}
