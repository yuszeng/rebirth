namespace Rebirth.UI;

/// <summary>把伤害飘字生成在世界坐标。只展示打到敌人的结算，不改伤害数值。</summary>
public partial class DamagePopupLayer : Node2D
{
    public override void _Ready()
    {
        Name = "DamagePopupLayer";
        ZIndex = 80;
        ProcessMode = ProcessModeEnum.Always;
        EventBus.Instance.DamageApplied += OnDamageApplied;
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance == null)
        {
            return;
        }

        EventBus.Instance.DamageApplied -= OnDamageApplied;
    }

    void OnDamageApplied(DamageResult result)
    {
        if (result.Request.Target is not Enemy enemy || !GodotObject.IsInstanceValid(enemy))
        {
            return;
        }

        var popup = new DamagePopup();
        AddChild(popup);
        var jitter = GameRng.Instance.Range(-12f, 12f);
        popup.GlobalPosition = enemy.GlobalPosition + new Vector2(jitter, -(enemy.Radius + 10f));
        popup.Show(result);
    }
}
