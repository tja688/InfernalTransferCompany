/// <summary>
/// ÆõÔ¼½×¶Î½Ó¿Ú
/// </summary>
public interface IContractStage
{
    void Enter(HeContractContext ctx);
    void Update();
    void Exit();
    bool IsCompleted { get; }
    bool HasFailed { get; }
    string StageName { get; }
}