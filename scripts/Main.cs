using Godot;
using System;

public partial class Main : Node2D
{
    private HSlider x0Slider;
    private Label x0Value;
    private Label resultLabel;
    private Button showSolutionBtn;
    private Label subQuestionContent;
    private Label solutionContent;
    private MathVisualization graph;

    private Button tab1, tab2, tab3;

    private int activeTab = 0; // 0 = 题干模式（不选任何子问题 tab）
    private bool solutionVisible = false;

    public override void _Ready()
    {
        // 获取界面上的控件
        x0Slider = GetNode<HSlider>("UI/InputSection/X0Slider");
        x0Value = GetNode<Label>("UI/InputSection/X0Value");
        showSolutionBtn = GetNode<Button>("UI/ShowSolutionBtn");
        resultLabel = GetNode<Label>("UI/InputSection/ResultLabel");
        subQuestionContent = GetNode<Label>("UI/SubQuestionContent");
        solutionContent = GetNode<Label>("UI/SolutionContent");
        graph = GetNode<MathVisualization>("Graph");

        tab1 = GetNode<Button>("UI/Tabs/Tab1");
        tab2 = GetNode<Button>("UI/Tabs/Tab2");
        tab3 = GetNode<Button>("UI/Tabs/Tab3");

        // 连接事件
        x0Slider.ValueChanged += OnSliderChanged;
        showSolutionBtn.Pressed += OnShowSolutionPressed;
        tab1.Pressed += () => OnTabPressed(1);
        tab2.Pressed += () => OnTabPressed(2);
        tab3.Pressed += () => OnTabPressed(3);

        // 默认：题干模式（无 tab 选中）
        OnTabPressed(0);

        // 触发初始滑块值
        OnSliderChanged(x0Slider.Value);
    }

    private void OnTabPressed(int tabIndex)
    {
        activeTab = tabIndex;
        solutionVisible = false;

        // 更新按钮样式
        tab1.Modulate = tabIndex == 1 ? Colors.White : new Color(0.7f, 0.7f, 0.7f);
        tab2.Modulate = tabIndex == 2 ? Colors.White : new Color(0.7f, 0.7f, 0.7f);
        tab3.Modulate = tabIndex == 3 ? Colors.White : new Color(0.7f, 0.7f, 0.7f);

        // 设置图表模式
        graph.ActiveSubQuestion = tabIndex;
        graph.ShowSolution = false;
        graph.InputX0 = null;
        graph.QueueRedraw();

        if (tabIndex == 0)
        {
            // 题干模式：隐藏子问题内容和解题按钮
            subQuestionContent.Visible = false;
            solutionContent.Visible = false;
            showSolutionBtn.Visible = false;
        }
        else
        {
            // 子问题模式：显示子问题题目，解题按钮可用
            subQuestionContent.Visible = true;
            subQuestionContent.Text = GetSubQuestionStatement(tabIndex);
            solutionContent.Visible = false;
            showSolutionBtn.Visible = true;
            showSolutionBtn.Text = "  显示解题过程";
        }
    }

    private void OnShowSolutionPressed()
    {
        solutionVisible = !solutionVisible;

        solutionContent.Visible = solutionVisible;
        showSolutionBtn.Text = solutionVisible ? "  隐藏解题过程" : "  显示解题过程";

        graph.ShowSolution = solutionVisible;

        if (solutionVisible)
        {
            // 同时传入 x₀ = -1 以匹配 D(-1) 的可视化
            graph.InputX0 = -1.0f;
            solutionContent.Text = GetSolutionText(activeTab);
        }
        else
        {
            graph.InputX0 = (float)x0Slider.Value;
        }

        graph.QueueRedraw();
    }

    private void OnSliderChanged(double value)
    {
        float x = (float)value;
        x0Value.Text = x.ToString("F2");

        // 计算 f(x) 值
        string result;
        if (x < 0)
        {
            float fx = (float)Math.Pow(2, x);
            result = $"f({x:F2}) = {fx:F4}";
        }
        else if (activeTab == 1)  // 子问题(1) 定义了 x≥0
        {
            float fx = 1.0f - x;
            result = $"f({x:F2}) = 1 - {x:F2} = {fx:F4}";
        }
        else
        {
            result = "无足够条件计算";
        }

        resultLabel.Text = result;

        // 传入 x₀ 给图表
        graph.InputX0 = x;
        graph.QueueRedraw();
    }

    /// <summary>
    /// 各子问题的题目描述（仅在切换 tab 时显示）
    /// </summary>
    private string GetSubQuestionStatement(int tabIndex)
    {
        return tabIndex switch
        {
            1 => "子问题 (1): 若 f(x) = 1 - x (x ≥ 0)，求集合 D(-1)",

            2 => "子问题 (2): 若 f(x) 为奇函数，且对于任意满足 x₁x₂ ≠ 0 的 x₁, x₂，\n" +
                 "f(x₁) ≤ f(x₂)，证明：D(x₁) ⊇ D(x₂)\n\n" +
                 "（待实现）",

            3 => "子问题 (3): 若 f(x) 满足：\n" +
                 "(a) 当 f(x₁) ≤ f(x₂) 时，D(x₁) ⊇ D(x₂);\n" +
                 "(b) 当 0 < x < 1 时，f(x) < f(0)\n" +
                 "证明：(i) f(0) ≥ 1; (ii) f(x) 在 (0, +∞) 上单调递增\n\n" +
                 "（待实现）",

            _ => ""
        };
    }

    /// <summary>
    /// 各子问题的完整解答过程（点击"显示解题过程"后展示）
    /// </summary>
    private string GetSolutionText(int tabIndex)
    {
        return tabIndex switch
        {
            1 => "【解答过程】\n\n" +
                 "已知: f(x) = { 2^x, x < 0\n" +
                 "            { 1-x, x ≥ 0\n\n" +
                 "求 D(-1) = {d ∈ R | f(-1+d) > f(-1)}\n\n" +
                 "Step 1: 计算 f(-1)\n" +
                 "  f(-1) = 2⁻¹ = 0.5\n\n" +
                 "Step 2: 分情况讨论\n" +
                 "  ① 当 -1+d < 0（即 d < 1）时\n" +
                 "     f(-1+d) = 2⁻¹ᐩᵈ\n" +
                 "     2⁻¹ᐩᵈ > 0.5 = 2⁻¹\n" +
                 "     ∵ 2^x 单调递增\n" +
                 "     ∴ -1+d > -1 → d > 0\n" +
                 "     结合 d < 1，得 0 < d < 1\n\n" +
                 "  ② 当 -1+d ≥ 0（即 d ≥ 1）时\n" +
                 "     f(-1+d) = 1-(-1+d) = 2-d\n" +
                 "     2-d > 0.5 → d < 1.5\n" +
                 "     结合 d ≥ 1，得 1 ≤ d < 1.5\n\n" +
                 "Step 3: 合并结果\n" +
                 "  D(-1) = (0, 1) ∪ [1, 1.5) = (0, 1.5)\n\n" +
                 "答案: D(-1) = (0, 1.5)",

            2 => "子问题 (2) 待实现",

            3 => "子问题 (3) 待实现",

            _ => ""
        };
    }
}