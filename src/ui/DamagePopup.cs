namespace Rebirth.UI;

/// <summary>敌人受击时的世界空间伤害数字，存活 1 秒后消失。不挂在敌人节点上，避免击杀后立刻被释放。</summary>
public partial class DamagePopup : Node2D
{
    const float Lifetime = 1f;

    string _text = "";
    Color _color = Colors.White;
    int _fontSize = 14;
    float _age;

    public void Show(DamageResult result)
    {
        if (result.IsDodged)
        {
            _text = "闪避";
            _color = new Color(0.72f, 0.78f, 0.86f);
            _fontSize = 14;
        }
        else if (result.IsCrit)
        {
            _text = Mathf.RoundToInt(result.Amount).ToString();
            _color = new Color(1f, 0.78f, 0.22f);
            _fontSize = 20;
        }
        else
        {
            _text = Mathf.RoundToInt(result.Amount).ToString();
            _color = new Color(1f, 0.95f, 0.88f);
            _fontSize = 14;
        }

        ZIndex = 80;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        _age += dt;
        Position += new Vector2(0f, -42f) * dt;
        var fade = Mathf.Clamp(1f - _age / Lifetime, 0f, 1f);
        Modulate = new Color(1f, 1f, 1f, fade);
        if (_age >= Lifetime)
        {
            QueueFree();
        }
    }

    public override void _Draw()
    {
        var font = ThemeDB.FallbackFont;
        var size = font.GetStringSize(_text, HorizontalAlignment.Left, -1, _fontSize);
        DrawString(
            font,
            new Vector2(-size.X * 0.5f, 0f),
            _text,
            HorizontalAlignment.Left,
            -1,
            _fontSize,
            _color);
    }
}
