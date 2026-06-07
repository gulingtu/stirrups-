using Godot;
using System;

public partial class Main : Node2D
{
	private LineEdit inputField;
	private Button calcButton;
	private Label resultLabel;

	public override void _Ready()
	{
		// 获取界面上的控件
		inputField = GetNode<LineEdit>("UI/InputSection/InputX");
		calcButton = GetNode<Button>("UI/InputSection/BtnCalculate");
		resultLabel = GetNode<Label>("UI/InputSection/ResultLabel");

		// 连接按钮点击事件
		calcButton.Pressed += OnCalculatePressed;
	}

	private void OnCalculatePressed()
	{
		if (float.TryParse(inputField.Text, out float x))
		{
			float fx = CalculateFunction(x);
			resultLabel.Text = $"f({x}) = {fx}";
		}
		else
		{
			resultLabel.Text = "无效输入";
		}
	}

	// 根据题目定义计算函数值
	// 当 x < 0 时，f(x) = 2^x
	// 当 x >= 0 时，根据题目要求的不同部分使用不同的函数
	private float CalculateFunction(float x)
	{
		if (x < 0)
		{
			// 当 x < 0 时，f(x) = 2^x
			return (float)Math.Pow(2, x);
		}
		else
		{
			// 当 x >= 0 时，根据题目要求，有多种可能的定义
			// 对于问题(1)：若 f(x) = 1 - x，则返回 1 - x
			// 对于问题(2)和(3)：需要根据具体条件定义
			// 这里我们以问题(1)为例实现 f(x) = 1 - x
			return 1 - x;
		}
	}
}
