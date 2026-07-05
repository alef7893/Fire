public class FireNodeSensitive : FireNodeBase
{
    protected override void InitializeNode()
    {
        canReignite = false;
        base.InitializeNode();
    }

    protected override void UpdateFireState(float deltaTime)
    {
        UpdateHeatDependentFire(deltaTime, true);
    }

    protected override bool IsFinalState()
    {
        return state == FireNodeState.Extinguished && !canReignite;
    }
}
