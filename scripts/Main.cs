using Godot;
using System;

public partial class Main : Node2D
{
    private LineEdit inputField;
    private Button calcButton;
    private Label resultLabel;
    private MathVisualization graph;

    public override void _Ready()
    {
        // 获取界面上的控件
        inputField = GetNode<LineEdit>("UI/InputSection/InputX");
        calcButton = GetNode<Button>("UI/InputSection/BtnCalculate");
        resultLabel = GetNode<Label>("UI/InputSection/ResultLabel");
        graph = GetNode<MathVisualization>("Graph");

        // 连接按钮点击事件
        calcButton.Pressed += OnCalculatePressed;
    }

    private void OnCalculatePressed()
    {
        if (float.TryParse(inputField.Text, out float x))
        {
            if (x < 0)
            {
                float fx = (float)Math.Pow(2, x);
                resultLabel.Text = $"f({x}) = {fx}";
            }
            else
            {
                resultLabel.Text = "无足够条件计算";
            }

            // 传入 x₀ 给图表，用于 D 集合可视化
            graph.InputX0 = x;
            graph.QueueRedraw();
        }
        else
        {
            resultLabel.Text = "无效输入";
            graph.InputX0 = null;
            graph.QueueRedraw();
        }
    }
}