/// <summary>Receives dismiss/cancel from shared crisis and pastoral choice cards.</summary>
public interface IChoiceCardPresenter
{
    void OnChoiceCardDismissed();
    void OnChoiceCardCancelled();
}
