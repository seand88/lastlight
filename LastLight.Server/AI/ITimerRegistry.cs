namespace LastLight.Server.AI;

public interface ITimerRegistry
{
    void RegisterTimer(string actionId, float interval);
    void UnregisterTimer(string actionId);
    void ClearTimers();
}
