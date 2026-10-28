namespace PlenBotLogUploader.Tools;

internal interface IListViewItemInfo<T>
{
    string NameToDisplay { get; }
    string TextToDisplay { get; }
    bool CheckedToDisplay { get; }
    void UpdateItems() { }
}
