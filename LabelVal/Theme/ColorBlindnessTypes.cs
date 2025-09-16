using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.Theme;

public enum ColorBlindnessType
{
    [Description("")]
    None,
    [Description("Protanopia/Deuteranopia")]
    RedGreen,
    [Description("Tritanopia")]
    BlueYellow,
    [Description("Achromatopsia")]
    Monochrome
}