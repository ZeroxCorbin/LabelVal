using SQLite;

namespace LabelVal.Run;
[StoreAsText]
public enum RunStates
{
    Idle,
    Running,
    Paused,
    Stopped,
    Complete,
    Error
}
