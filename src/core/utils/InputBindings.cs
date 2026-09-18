namespace Rebirth.Core;

/// <summary>运行时注册输入映射，避免依赖 project.godot 手动配置。</summary>
public static class InputBindings
{
    public static void Ensure()
    {
        BindKeys("move_left", [Key.A, Key.Left]); // 左移
        BindKeys("move_right", [Key.D, Key.Right]); // 右移
        BindKeys("move_up", [Key.W, Key.Up]); // 上移
        BindKeys("move_down", [Key.S, Key.Down]); // 下移
        BindKeys("restart", [Key.R]); // 重新开始
        BindKeys("pause", [Key.Escape]); // 暂停菜单
        BindKeys("attack_mode_menu", [Key.E]); // 攻击方式选择
    }

    /// <summary>将多个按键绑定到同一 InputMap 动作。</summary>
    static void BindKeys(StringName action, Key[] keys)
    {
        if (!InputMap.HasAction(action)) // 如果动作不存在则添加动作
        {
            InputMap.AddAction(action); // 添加动作
        }

        foreach (var key in keys) // 遍历按键列表
        {
            var ev = new InputEventKey { PhysicalKeycode = key }; // 物理键码
            if (!InputMap.ActionHasEvent(action, ev)) // 如果动作不存在则添加事件
            {
                InputMap.ActionAddEvent(action, ev); // 添加事件
            }
        }
    }
}
