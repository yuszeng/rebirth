namespace Rebirth.UI;

/// <summary>战斗单位头顶的世界空间血条，存活期间始终显示。</summary>
public partial class WorldHealthBar : Node2D
{
    const float BarWidth = 28f;
    const float BarHeight = 4f;
    const float TopPadding = 6f;
    const int TextFontSize = 10; // 血条数字字号（世界空间尽量小）
    const float TextGap = 1f; // 文字与血条间距

    Health? _health;

    /// <summary>绑定 Health 并定位到实体上方。</summary>
    public void Bind(Health health, float entityRadius)
    {
        _health = health;
        // 文字在血条上方，整体再抬高一点
        Position = new Vector2(0f, -(entityRadius + TopPadding + BarHeight + TextFontSize + TextGap));
        health.Changed -= OnHealthChanged; // 取消订阅健康变化事件
        health.Changed += OnHealthChanged; // 订阅健康变化事件
        UpdateVisibility(); // 更新可见性
        QueueRedraw(); // 重新绘制
    }

    void OnHealthChanged(float _, float __) // 健康变化事件处理
    {
        UpdateVisibility(); // 更新可见性
        QueueRedraw(); // 重新绘制
    }

    void UpdateVisibility() // 更新可见性
    {
        if (_health == null) // 如果健康不存在则直接返回
        {
            Visible = false; // 设置可见性为 false
            return;
        }

        Visible = !_health.IsDead; // 存活期间始终显示
    }

    public override void _Draw() // 绘制血条
    {
        if (_health == null || _health.IsDead)
        {
            return; // 如果健康不存在或已死亡则直接返回
        }

        var ratio = _health.Maximum > 0f
            ? Mathf.Clamp(_health.Current / _health.Maximum, 0f, 1f)
            : 0f; // 计算血条比例
        var halfW = BarWidth * 0.5f;
        DrawRect(new Rect2(-halfW, 0f, BarWidth, BarHeight), new Color(0.08f, 0.08f, 0.08f, 0.75f)); // 背景
        DrawRect(new Rect2(-halfW, 0f, BarWidth * ratio, BarHeight), new Color(0.86f, 0.28f, 0.32f, 0.95f)); // 前景

        // 文字：与 HUD 一致，显示「当前/最大」
        var text = $"{Mathf.RoundToInt(_health.Current)}/{Mathf.RoundToInt(_health.Maximum)}";
        var font = ThemeDB.FallbackFont; // 获取默认字体
        var textSize = font.GetStringSize(text, HorizontalAlignment.Left, -1, TextFontSize); // 获取文本大小
        var textPos = new Vector2(-textSize.X * 0.5f, -TextGap); // 水平居中，贴在血条上方
        DrawString(
            font,  // 字体
            textPos, // 文本位置
            text,  // 文本
            HorizontalAlignment.Left, // 水平对齐
            -1, // 最大宽度
            TextFontSize, // 字号
            Colors.White // 颜色
        ); // 绘制文本
        DrawString(
            font, 
            new Vector2(-textSize.X * 0.6f, -TextGap - TextFontSize), 
            "基础怪物", 
            HorizontalAlignment.Left, 
            -1, 
            TextFontSize, 
            Colors.White
        ); // 绘制文本
    }

    public override void _ExitTree() // 退出树时取消订阅健康变化事件
    {
        if (_health != null)
        {
            _health.Changed -= OnHealthChanged; // 取消订阅健康变化事件
        }
    }
}
