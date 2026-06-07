using Godot;
using System;
using System.Collections.Generic;

public partial class CatapultLord : Control
{
    // ================================================================
    //  游戏参数
    // ================================================================
    private float x0 = -1.0f;          // 城堡位置（固定）
    private float aimD = 0.0f;         // 当前瞄准的 d 值
    private int maxTurns = 8;          // 最大弹射次数
    private int turnsLeft = 8;         // 剩余次数
    private int score = 0;             // 得分
    private int streak = 0;            // 连续成功次数

    // 已探索的 D(x₀) 边界
    private float? discoveredLower = null;
    private float? discoveredUpper = null;

    // 已发射过的 d 值（用于标记）
    private List<float> launchedD = new List<float>();

    // ================================================================
    //  游戏阶段
    // ================================================================
    private enum Phase { Aiming, Result, Won, Lost }
    private Phase phase = Phase.Aiming;

    private float lastResultD = 0;
    private bool lastResultSuccess = false;
    private float resultTimer = 0f;
    private const float ResultDisplayDuration = 2.5f;

    // D(-1) 标准答案（子问题 1）
    private const float DLower = 0f;
    private const float DUpper = 1.5f;

    // ================================================================
    //  布局 / 绘图参数
    // ================================================================
    private const int DefaultFontSize = 16;
    private float L, R;
    private const float G = 30f;
    private float lCX, dCX, cY;
    private float ScaleX = 50f, ScaleY = 50f;
    private float RangeX = 8f;

    // D-plot 缩放参数
    private float dMin = -2f, dMax = 4f;
    private float gMin = -1f, gMax = 3f;
    private float dScale, gScale, dAxisScreenY;

    // ================================================================
    //  节点引用 (通过 GetNode 自动绑定)
    // ================================================================
    private HSlider AimSlider;
    private Label AimValueLabel;
    private Label TurnsLabel;
    private Label ScoreLabel;
    private Label ResultLabel;
    private Button LaunchBtn;
    private Button RestartBtn;

    // ================================================================
    //  生命周期
    // ================================================================
    public override void _Ready()
    {
        // 获取子节点
        AimSlider = GetNode<HSlider>("AimSlider");
        AimValueLabel = GetNode<Label>("AimValueLabel");
        TurnsLabel = GetNode<Label>("TurnsLabel");
        ScoreLabel = GetNode<Label>("ScoreLabel");
        ResultLabel = GetNode<Label>("ResultLabel");
        LaunchBtn = GetNode<Button>("LaunchBtn");
        RestartBtn = GetNode<Button>("RestartBtn");

        // 连接信号
        AimSlider.ValueChanged += OnAimChanged;
        LaunchBtn.Pressed += OnLaunchPressed;
        RestartBtn.Pressed += OnRestartPressed;
        RestartBtn.Disabled = true;

        // 初始化文字
        LaunchBtn.Text = "⚡ 发射!";
        RestartBtn.Text = "🔄 重新开局";

        ResetGame();
        QueueRedraw();
    }

    public void OnRestartPressed()
    {
        ResetGame();
        UpdateUI();
    }

    public void ResetGame()
    {
        x0 = -1.0f;
        aimD = 0.0f;
        turnsLeft = maxTurns;
        score = 0;
        streak = 0;
        launchedD.Clear();
        discoveredLower = null;
        discoveredUpper = null;
        phase = Phase.Aiming;
        lastResultSuccess = false;
        resultTimer = 0;
        QueueRedraw();
    }

    public void OnAimChanged(double value)
    {
        if (phase != Phase.Aiming) return;
        aimD = (float)value;
        QueueRedraw();
    }

    public void OnLaunchPressed()
    {
        if (phase != Phase.Aiming || turnsLeft <= 0) return;
        if (launchedD.Contains(aimD)) return; // 已经试过了

        // 执行弹射
        float x = x0 + aimD;
        bool isInD = false;
        if (x < 0)
        {
            float fx = (float)Math.Pow(2, x);
            float fx0 = (float)Math.Pow(2, x0);
            isInD = fx > fx0;
        }
        else
        {
            // 子问题 1: f(x) = 1 - x (x ≥ 0)
            float fx = 1.0f - x;
            float fx0 = (float)Math.Pow(2, x0);
            isInD = fx > fx0;
        }

        launchedD.Add(aimD);

        // 更新 D(x₀) 发现边界
        UpdateDiscoveredBounds();

        // 更新得分
        if (isInD)
        {
            streak++;
            int bonus = streak >= 3 ? 5 : 0;
            score += 10 + bonus;
        }
        else
        {
            streak = 0;
            score = Math.Max(0, score - 3);
        }

        lastResultD = aimD;
        lastResultSuccess = isInD;
        turnsLeft--;

        phase = Phase.Result;
        resultTimer = ResultDisplayDuration;

        // 检查胜负
        CheckWinLose();

        QueueRedraw();
        UpdateUI();
    }

    private void UpdateDiscoveredBounds()
    {
        // 重新扫描已发射的 d 值，找出 D 的边界
        float? newLower = null;
        float? newUpper = null;

        // 已知 D(-1) = (0, 1.5)，所以我们只需要从发射数据中推断
        // 下界: 最大的 d ∈ D 中的最小值
        // 上界: 最小的 d ∉ D 中的最大值
        float maxSuccessBelow = float.MinValue;
        float minFailAbove = float.MaxValue;

        foreach (float d in launchedD)
        {
            float x = x0 + d;
            bool inD;
            if (x < 0)
                inD = (float)Math.Pow(2, x) > (float)Math.Pow(2, x0);
            else
                inD = (1.0f - x) > (float)Math.Pow(2, x0);

            if (inD)
            {
                if (d > maxSuccessBelow) maxSuccessBelow = d;
            }
            else
            {
                if (d < minFailAbove) minFailAbove = d;
            }
        }

        if (maxSuccessBelow > float.MinValue && maxSuccessBelow > 0)
            newLower = DLower;
        if (minFailAbove < float.MaxValue)
            newUpper = minFailAbove;
        // 如果一直没有失败，用最高成功值近似
        if (!newUpper.HasValue && maxSuccessBelow > float.MinValue)
            newUpper = maxSuccessBelow + 0.1f;

        discoveredLower = newLower;
        discoveredUpper = newUpper;
    }

    private void CheckWinLose()
    {
        // 胜: D(-1) 上下界均被发现
        if (discoveredLower.HasValue && discoveredUpper.HasValue)
        {
            if (Math.Abs(discoveredLower.Value - DLower) < 0.15f &&
                Math.Abs(discoveredUpper.Value - DUpper) < 0.15f)
            {
                phase = Phase.Won;
                return;
            }
        }

        // 负: 用完所有次数
        if (turnsLeft <= 0)
        {
            phase = Phase.Lost;
        }
    }

    public override void _Process(double delta)
    {
        if (phase == Phase.Result)
        {
            resultTimer -= (float)delta;
            if (resultTimer <= 0)
            {
                if (turnsLeft <= 0)
                    phase = Phase.Lost;
                else
                    phase = Phase.Aiming;
                
                CheckWinLose();
                QueueRedraw();
                UpdateUI();
            }
            else
            {
                // 持续刷新以显示动画
                QueueRedraw();
            }
        }
    }

    private void UpdateUI()
    {
        if (TurnsLabel != null) TurnsLabel.Text = $"剩余弹射: {turnsLeft}";
        if (ScoreLabel != null) ScoreLabel.Text = $"得分: {score}";
        if (AimValueLabel != null) AimValueLabel.Text = $"d = {aimD:F2}";
        if (ResultLabel != null)
        {
            if (phase == Phase.Result)
            {
                if (lastResultSuccess)
                {
                    string streakMsg = streak >= 3 ? $" 连击 x{streak}!" : "";
                    ResultLabel.Text = $"✓ 安全着陆! +10{streakMsg}";
                    ResultLabel.Modulate = Colors.Green;
                }
                else
                {
                    ResultLabel.Text = "✗ 坠落! -3";
                    ResultLabel.Modulate = Colors.Red;
                }
            }
            else if (phase == Phase.Won)
            {
                ResultLabel.Text = $"🏆 发现 D(-1) = (0, {DUpper})! 得分: {score}";
                ResultLabel.Modulate = Colors.Yellow;
            }
            else if (phase == Phase.Lost)
            {
                float lower = discoveredLower ?? 0;
                float upper = discoveredUpper ?? 0;
                ResultLabel.Text = $"次数用尽。D(-1) 实际为 (0, {DUpper})，你发现了 ({lower:F1}, {upper:F1})";
                ResultLabel.Modulate = Colors.Orange;
            }
            else
            {
                ResultLabel.Text = "调瞄准 d，点击发射";
                ResultLabel.Modulate = Colors.White;
            }
        }
        if (LaunchBtn != null) LaunchBtn.Disabled = (phase != Phase.Aiming);
        if (RestartBtn != null) RestartBtn.Disabled = false;
    }

    // ================================================================
    //  绘图
    // ================================================================
    public override void _Draw()
    {
        if (Size.X < 10 || Size.Y < 10) return;

        // 计算布局
        float totalW = Size.X;
        float totalH = Size.Y;
        L = totalW * 0.52f;        // 左面板（地形）
        R = totalW - L - G;        // 右面板（D-plot）
        lCX = L / 2;
        dCX = L + G + R / 2;
        cY = totalH * 0.45f;       // 略偏下留空间给游戏 UI

        // 清背景
        DrawRect(new Rect2(0, 0, totalW, totalH), new Color(0.12f, 0.12f, 0.15f, 1.0f));

        // 左侧: 地形图
        DrawTerrain();

        // 右侧: D-plot
        DrawDPlot();

        // 游戏 HUD (绘制在图表之上)
        DrawGameHUD();
    }

    // ================================================================
    //  地形图（左侧）
    // ================================================================
    private void DrawTerrain()
    {
        // 网格
        for (float x = -RangeX; x <= RangeX; x += 1f)
        {
            float sx = lCX + x * ScaleX;
            if (sx >= 0 && sx <= L)
                DrawLine(new Vector2(sx, 0), new Vector2(sx, Size.Y), new Color(1, 1, 1, 0.08f), 1f);
        }
        for (float y = -RangeX; y <= RangeX; y += 1f)
        {
            float sy = cY - y * ScaleY;
            if (sy >= 0 && sy <= Size.Y)
                DrawLine(new Vector2(0, sy), new Vector2(L, sy), new Color(1, 1, 1, 0.08f), 1f);
        }

        // 坐标轴
        DrawLine(new Vector2(0, cY), new Vector2(L, cY), Colors.Gray, 2f);
        DrawLine(new Vector2(lCX, 0), new Vector2(lCX, Size.Y), Colors.Gray, 2f);
        DrawString(DefaultFontSize, new Vector2(lCX - 12, cY + 18), "O");

        // f(x) = 2^x (x≥0)
        Vector2? prev = null;
        for (float x = -RangeX; x < 0; x += 0.05f)
        {
            float y = (float)Math.Pow(2, x);
            float sx = lCX + x * ScaleX;
            float sy = cY - y * ScaleY;
            var pt = new Vector2(sx, sy);
            if (prev.HasValue && sx >= 0) DrawLine(prev.Value, pt, Colors.DodgerBlue, 3f);
            prev = pt;
        }

        // x≥0 迷雾（子问题1的 f(x) 是 1-x，用半透明绿色）
        // 先画绿色实线作为实际地形（但被迷雾遮住）
        prev = null;
        for (float x = 0; x <= RangeX; x += 0.05f)
        {
            float y = 1.0f - x;
            float sx = lCX + x * ScaleX;
            float sy = cY - y * ScaleY;
            var pt = new Vector2(sx, sy);
            if (prev.HasValue && sx <= L) DrawLine(prev.Value, pt, new Color(0, 0.7f, 0.3f, 0.3f), 3f);
            prev = pt;
        }

        // 迷雾覆盖（半透明灰色矩形覆盖 x≥0）
        float fogLeft = lCX; // x=0
        if (fogLeft < L)
        {
            Color fogColor = new Color(0.3f, 0.3f, 0.35f, 0.7f);
            DrawRect(new Rect2(fogLeft, 0, L - fogLeft, Size.Y), fogColor);

            // 已探索的点：穿透迷雾显示
            foreach (float d in launchedD)
            {
                float x = x0 + d;
                if (x < 0) continue;
                float fx = 1.0f - x;
                float sx = lCX + x * ScaleX;
                float sy = cY - fx * ScaleY;
                if (sx >= fogLeft && sx <= L)
                {
                    // 在迷雾上开个"窗口" —— 用一个圆形裁剪区域? 简化: 画亮色标记
                    DrawCircle(new Vector2(sx, sy), 5, Colors.LightGreen);
                    DrawLine(new Vector2(sx, sy), new Vector2(sx, cY), new Color(1, 1, 1, 0.3f), 1f);
                }
            }

            // 迷雾标签
            DrawString((int)(DefaultFontSize * 1.2f), new Vector2(fogLeft + 30, cY - 40), "??? 未知地域 ???");
            DrawString(DefaultFontSize * 0.8f, new Vector2(fogLeft + 30, cY - 18), "弹射探索来揭示地形");
        }

        // 城堡位置标记 (x₀)
        float fx0 = (float)Math.Pow(2, x0);
        float sx0 = lCX + x0 * ScaleX;
        float sy0 = cY - fx0 * ScaleY;
        // 城堡图标
        DrawRect(new Rect2(sx0 - 8, sy0 - 16, 16, 20), Colors.SaddleBrown);
        DrawRect(new Rect2(sx0 - 6, sy0 - 20, 12, 6), Colors.Firebrick);
        DrawString(DefaultFontSize * 0.7f, new Vector2(sx0 - 12, sy0 - 24), "城堡");

        // 弹射瞄准指示器
        if (phase == Phase.Aiming)
        {
            float aimX = x0 + aimD;
            float aimFx;
            bool known = aimX < 0;
            if (aimX < 0) aimFx = (float)Math.Pow(2, aimX);
            else aimFx = 1.0f - aimX;

            float aimSx = lCX + aimX * ScaleX;
            float aimSy = cY - aimFx * ScaleY;

            // 瞄准弧线（从城堡到落点）
            float midX = (sx0 + aimSx) / 2;
            float arcHeight = -40 - Math.Abs(aimD) * 8;
            DrawArcLine(sx0, sy0, aimSx, aimSy, arcHeight, Colors.Yellow * new Color(1, 1, 1, 0.7f), 2f);

            // 落点标记
            if (aimX >= 0 && aimSx >= lCX && aimSx <= L)
            {
                // 在迷雾上画一个问号标记
                DrawCircle(new Vector2(aimSx, aimSy), 6, Colors.Yellow);
                DrawString(DefaultFontSize * 0.8f, new Vector2(aimSx + 8, aimSy + 4), "?");
            }
            else if (aimSx >= 0)
            {
                DrawCircle(new Vector2(aimSx, aimSy), 6, Colors.Yellow);
            }

            // d 值标注
            DrawString(DefaultFontSize * 0.7f, new Vector2(aimSx - 15, Math.Min(aimSy, cY - 10) - 10),
                $"d={aimD:F2}");
        }

        // 已发射的弹射标记
        foreach (float d in launchedD)
        {
            float x = x0 + d;
            float fx = x < 0 ? (float)Math.Pow(2, x) : 1.0f - x;
            float sx = lCX + x * ScaleX;
            float sy = cY - fx * ScaleY;

            bool inD = false;
            if (x < 0) inD = fx > (float)Math.Pow(2, x0);
            else inD = fx > (float)Math.Pow(2, x0);

            Color markColor = inD ? Colors.Green : Colors.Red;
            DrawCircle(new Vector2(sx, sy), 4, markColor);
            if (d == lastResultD && phase == Phase.Result)
            {
                // 高亮最新结果
                DrawCircle(new Vector2(sx, sy), 8, markColor * new Color(1, 1, 1, 0.4f));
            }
        }

        // 图例
        int legendY = (int)(Size.Y - 60);
        DrawLine(new Vector2(10, legendY), new Vector2(30, legendY), Colors.DodgerBlue, 3f);
        DrawString(DefaultFontSize * 0.8f, new Vector2(35, legendY - 5), "已知地形 f(x)=2^x");

        DrawRect(new Rect2(10, legendY + 15, 20, 10), new Color(0.3f, 0.3f, 0.35f, 0.7f));
        DrawString(DefaultFontSize * 0.8f, new Vector2(35, legendY + 22), "未知地域 (迷雾)");

        DrawCircle(new Vector2(15, legendY + 40), 4, Colors.Green);
        DrawString(DefaultFontSize * 0.8f, new Vector2(35, legendY + 37), "安全着陆 (∈D)");
        DrawCircle(new Vector2(15, legendY + 55), 4, Colors.Red);
        DrawString(DefaultFontSize * 0.8f, new Vector2(35, legendY + 52), "坠落 (∉D)");

        // 分界线 x=0
        float zeroX = lCX;
        DrawDashedLine(new Vector2(zeroX, 0), new Vector2(zeroX, Size.Y), new Color(0.8f, 0.8f, 0.8f, 0.3f), 1f);
        DrawString(DefaultFontSize * 0.65f, new Vector2(zeroX + 4, cY + 30), "x=0");
    }

    // ================================================================
    //  D-plot（右侧）
    // ================================================================
    private void DrawDPlot()
    {
        float left0 = L + G;
        float right1 = Size.X;

        // 背景
        DrawRect(new Rect2(left0, 0, right1 - left0, Size.Y), new Color(0.08f, 0.1f, 0.15f, 1f));

        // 计算 d 范围
        dMin = -1f;
        dMax = 3.5f;
        float gRange = 3f;

        dScale = R * 0.8f / (dMax - dMin);
        gScale = Size.Y * 0.35f / gRange;
        dAxisScreenY = cY + 50;

        // 网格
        for (float d = Mathf.Floor(dMin); d <= dMax; d += 0.5f)
        {
            float sx = dCX + (d - 0) * dScale;
            if (sx >= left0 && sx <= right1)
                DrawLine(new Vector2(sx, 0), new Vector2(sx, Size.Y), new Color(1, 1, 1, 0.08f), 1f);
        }
        for (float g = -gRange; g <= gRange; g += 0.5f)
        {
            float sy = dAxisScreenY - g * gScale;
            if (sy >= 0 && sy <= Size.Y)
                DrawLine(new Vector2(left0, sy), new Vector2(right1, sy), new Color(1, 1, 1, 0.08f), 1f);
        }

        // 坐标轴
        DrawLine(new Vector2(left0, dAxisScreenY), new Vector2(right1, dAxisScreenY), Colors.Gray, 2f);
        float gAxisX = dCX;
        DrawLine(new Vector2(gAxisX, 0), new Vector2(gAxisX, Size.Y), Colors.Gray, 2f);
        DrawString(DefaultFontSize * 0.9f, new Vector2(right1 - 20, dAxisScreenY + 18), "d");
        DrawString(DefaultFontSize * 0.9f, new Vector2(gAxisX + 10, 15), "g(d)");

        // d 轴刻度
        for (float d = Mathf.Floor(dMin); d <= dMax; d += 0.5f)
        {
            float sx = dCX + (d - 0) * dScale;
            if (sx >= left0 && sx <= right1)
            {
                DrawLine(new Vector2(sx, dAxisScreenY - 3), new Vector2(sx, dAxisScreenY + 3), Colors.Gray, 1f);
                DrawString(DefaultFontSize * 0.65f, new Vector2(sx - 6, dAxisScreenY + 12), d.ToString("F1"));
            }
        }
        DrawString(DefaultFontSize * 0.7f, new Vector2(gAxisX + 4, dAxisScreenY + 5), "0");

        // g(d) 曲线
        Vector2? prevPt = null;
        for (float d = dMin; d <= dMax; d += 0.04f)
        {
            float x = x0 + d;
            float fx;
            if (x < 0) fx = (float)Math.Pow(2, x);
            else fx = 1.0f - x;

            float g = fx - (float)Math.Pow(2, x0);
            float sx = dCX + (d - 0) * dScale;
            float sy = dAxisScreenY - g * gScale;
            var pt = new Vector2(sx, sy);
            if (prevPt.HasValue && sx >= left0 && sx <= right1)
                DrawLine(prevPt.Value, pt, Colors.Orange, 3f);
            prevPt = pt;
        }

        // g=0 参考线
        float zeroGY = dAxisScreenY;
        DrawDashedLine(new Vector2(left0, zeroGY), new Vector2(right1, zeroGY), new Color(0.5f, 0.5f, 0.5f, 0.4f), 1f);

        // 已发射的标记
        foreach (float d in launchedD)
        {
            float x = x0 + d;
            float fx = x < 0 ? (float)Math.Pow(2, x) : 1.0f - x;
            float g = fx - (float)Math.Pow(2, x0);
            float sx = dCX + (d - 0) * dScale;
            float sy = dAxisScreenY - g * gScale;

            bool inD = g > 0;
            DrawCircle(new Vector2(sx, sy), 4, inD ? Colors.Green : Colors.Red);
            DrawLine(new Vector2(sx, sy), new Vector2(sx, dAxisScreenY), new Color(1, 1, 1, 0.2f), 1f);
        }

        // 当前瞄准标记
        if (phase == Phase.Aiming)
        {
            float x = x0 + aimD;
            float fx = x < 0 ? (float)Math.Pow(2, x) : 1.0f - x;
            float g = fx - (float)Math.Pow(2, x0);
            float sx = dCX + (aimD - 0) * dScale;
            float sy = dAxisScreenY - g * gScale;

            DrawCircle(new Vector2(sx, sy), 6, Colors.Yellow);
            DrawLine(new Vector2(sx, dAxisScreenY), new Vector2(sx, sy), Colors.Yellow * new Color(1, 1, 1, 0.3f), 1f);

            string gLabel = g > 0 ? "g>0 ✓" : "g≤0 ✗";
            DrawString(DefaultFontSize * 0.7f, new Vector2(sx + 8, sy - 4), gLabel);
        }

        // 已发现的 D(x₀) 高亮条
        float barY = dAxisScreenY + 5;
        float barH = 8;
        float hlLeft = dCX + (DLower - 0) * dScale;

        if (discoveredUpper.HasValue)
        {
            float hlRight = dCX + (discoveredUpper.Value - 0) * dScale;
            hlRight = Math.Min(hlRight, dCX + (dMax - 0) * dScale);
            hlLeft = Math.Max(hlLeft, dCX + (dMin - 0) * dScale);

            if (hlRight > hlLeft)
            {
                DrawRect(new Rect2(hlLeft, barY, hlRight - hlLeft, barH), new Color(1, 0.6f, 0, 0.6f));
                DrawRect(new Rect2(hlLeft, barY, hlRight - hlLeft, barH), Colors.Orange, false, 1.5f);
            }
        }

        // 目标区间（淡色参考）
        float targetLeft = dCX + (DLower - 0) * dScale;
        float targetRight = dCX + (DUpper - 0) * dScale;
        if (targetRight > targetLeft)
        {
            DrawRect(new Rect2(targetLeft, barY + 12, targetRight - targetLeft, 3), new Color(0, 1, 0, 0.3f));
        }

        // 标签
        string discLabel = "";
        if (discoveredLower.HasValue && discoveredUpper.HasValue)
            discLabel = $"发现: ({discoveredLower.Value:F1}, {discoveredUpper.Value:F1})";
        else if (discoveredLower.HasValue)
            discLabel = $"发现: ({discoveredLower.Value:F1}, ?)";
        else
            discLabel = "尚未发现 D(x₀)";

        DrawString(DefaultFontSize * 0.75f, new Vector2(left0 + 10, barY + 28), discLabel);
        DrawString(DefaultFontSize * 0.65f, new Vector2(left0 + 10, barY + 45), $"目标: (0, {DUpper:F1})");
        DrawString(DefaultFontSize * 0.65f, new Vector2(left0 + 10, barY + 60), $"剩余: {turnsLeft} 次");

        // 分界线 d = -x₀
        float splitD = 1f; // -(-1) = 1
        float splitX = dCX + (splitD - 0) * dScale;
        if (splitX >= left0 && splitX <= right1)
        {
            DrawDashedLine(new Vector2(splitX, 0), new Vector2(splitX, Size.Y), new Color(0.5f, 0.5f, 0.5f, 0.4f), 1.5f);
            DrawString(DefaultFontSize * 0.65f, new Vector2(splitX + 4, dAxisScreenY - 2), "d=1 (分界)");
        }
    }

    // ================================================================
    //  游戏 HUD
    // ================================================================
    private void DrawGameHUD()
    {
        int hudY = (int)(Size.Y - 30);

        // 阶段标题
        string title = "第1关 · 弹射领主";
        if (phase == Phase.Won) title += " 🏆 通关!";
        else if (phase == Phase.Lost) title += " ⏰ 次数用尽";
        DrawString((int)(DefaultFontSize * 1.3f), new Vector2(10, 25), title);

        // 进度条
        float progW = L * 0.6f;
        float progX = L * 0.2f;
        float progY = 45f;
        float progH = 8f;

        // 发现进度
        float progress = 0;
        if (discoveredLower.HasValue && discoveredUpper.HasValue)
        {
            float found = discoveredUpper.Value - discoveredLower.Value;
            float target = DUpper - DLower;
            progress = Math.Min(1f, found / target);
        }

        DrawRect(new Rect2(progX, progY, progW, progH), new Color(0.2f, 0.2f, 0.2f, 1f));
        DrawRect(new Rect2(progX, progY, progW * progress, progH), Colors.Green);
        DrawRect(new Rect2(progX, progY, progW, progH), Colors.White, false, 1f);
        DrawString(DefaultFontSize * 0.7f, new Vector2(progX + 5, progY + progH + 15),
            $"D(x₀) 探索进度: {progress * 100:F0}%");

        // 连击显示
        if (streak >= 2 && phase == Phase.Aiming)
        {
            DrawString((int)(DefaultFontSize * 1.1f), new Vector2(L * 0.4f, progY + 35),
                $"🔥 {streak} 连击!", Colors.Orange);
        }

        // 游戏结束覆盖层
        if (phase == Phase.Won || phase == Phase.Lost)
        {
            Color overlayColor = phase == Phase.Won
                ? new Color(0, 0.3f, 0, 0.3f)
                : new Color(0.3f, 0, 0, 0.3f);
            DrawRect(new Rect2(0, 0, Size.X, Size.Y), overlayColor);

            string msg = phase == Phase.Won
                ? $"🎉 通关! D(-1) = (0, {DUpper})  得分: {score}"
                : $"😞 失败。实际 D(-1) = (0, {DUpper})";

            DrawString((int)(DefaultFontSize * 1.5f), new Vector2(Size.X * 0.3f, Size.Y * 0.5f), msg,
                phase == Phase.Won ? Colors.Yellow : Colors.White);
            DrawString(DefaultFontSize, new Vector2(Size.X * 0.35f, Size.Y * 0.55f + 25),
                "点击「重新开局」再试一次");
        }
    }

    // ================================================================
    //  绘图工具
    // ================================================================
    private void DrawDashedLine(Vector2 from, Vector2 to, Color color, float width)
    {
        float dashLen = 8f;
        float gapLen = 5f;
        Vector2 dir = (to - from).Normalized();
        float total = from.DistanceTo(to);
        float drawn = 0;
        while (drawn < total)
        {
            float segEnd = Math.Min(drawn + dashLen, total);
            DrawLine(from + dir * drawn, from + dir * segEnd, color, width);
            drawn += dashLen + gapLen;
        }
    }

    private void DrawArcLine(float x1, float y1, float x2, float y2, float height, Color color, float width)
    {
        // 二次贝塞尔弧线
        int steps = 20;
        Vector2 p0 = new Vector2(x1, y1);
        Vector2 p2 = new Vector2(x2, y2);
        Vector2 p1 = new Vector2((x1 + x2) / 2, (y1 + y2) / 2 + height);

        Vector2? prev = null;
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float inv = 1 - t;
            Vector2 pt = inv * inv * p0 + 2 * inv * t * p1 + t * t * p2;
            if (prev.HasValue) DrawLine(prev.Value, pt, color, width);
            prev = pt;
        }
    }

    private void DrawString(float fontSize, Vector2 pos, string text)
    {
        DrawString(ThemeDB.FallbackFont, pos, text, HorizontalAlignment.Left, -1, (int)fontSize, Colors.White);
    }

    private void DrawString(float fontSize, Vector2 pos, string text, Color color)
    {
        DrawString(ThemeDB.FallbackFont, pos, text, HorizontalAlignment.Left, -1, (int)fontSize, color);
    }
}