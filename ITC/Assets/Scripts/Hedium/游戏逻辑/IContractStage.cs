/// <summary>
/// 契约阶段接口
/// </summary>
public interface IContractStage
{

    void Enter();
    void Update();
    void Exit();
    bool IsCompleted { get; }
    bool HasFailed { get; }
    string StageName { get; }
}