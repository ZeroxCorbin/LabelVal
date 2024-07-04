using CommunityToolkit.Mvvm.ComponentModel;

namespace LabelVal.Run.ViewModels;
public partial class MainWindow : ObservableRecipient
{
    public static string Version => App.Version;

    public RunDatabases RunDatabases { get; set; } = new();
    public ImageResults ImageResults { get; set; } = new();

}
