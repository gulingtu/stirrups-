using Godot;
using System;

public partial class MathVisualization : Control
{
	[Export]
	public float ScaleX { get; set; } = 50.0f; // X 轴缩放比例
	[Export]
	public float ScaleY { get; set; } = 50.0f; // Y 轴缩放比例
	[Export]
	public float RangeX { get; set; } = 10.0f; // X 轴显示范围
	[Export]
	public Color GraphColor { get; set; } = Colors.Blue;
	[Export]
	public Color GridColor { get; set; } = Colors.Gray;
	[Export]
	public Color AxisColor { get; set; } = Colors.Black;

	private const int DefaultFontSize = 16;

	private string[] _functionDescription = new[]
	{
		"函数 f(x) 的定义域为 R，且当 x < 0 时，f(x) = 2^x",
		"对于任意 x₀ ∈ R，定义集合 D(x₀) = {d ∈ R | f(x₀ + d) > f(x₀)}",
		"",
		"(1) 若 f(x) = 1 - x (x ≥ 0)，求集合 D(-1)",
		"",
		"(2) 若 f(x) 为奇函数，且对于任意满足 x₁x₂ ≠ 0 的 x₁, x₂，f(x₁) ≤ f(x₂)，",
		"    证明：D(x₁) ⊇ D(x₂)",
		"",
		"(3) 若 f(x) 满足以下两个条件：",
		"    (a) 当 f(x₁) ≤ f(x₂) 时，D(x₁) ⊇ D(x₂)",
		"    (b) 当 0 < x < 1 时，f(x) < f(0)",
        "    证明：(i) f(0) ≥ 1; (ii) f(x) 在 (0, +∞) 上单调递增"
	};

	public override void _Ready()
	{
		QueueRedraw();
	}

	public override void _Draw()
	{
		// 绘制网格
		DrawGrid();
		
		// 绘制坐标轴
		DrawAxes();
		
		// 绘制函数图像（x < 0 时，f(x) = 2^x）
		DrawFunction();
		
		// 绘制标签
		DrawLabels();
	}

	private void DrawGrid()
	{
		var size = Size;
		float centerX = size.X / 2;
		float centerY = size.Y / 2;

		// 绘制垂直网格线
		for (float x = -RangeX; x <= RangeX; x += 1.0f)
		{
			float screenX = centerX + x * ScaleX;
			DrawLine(new Vector2(screenX, 0), new Vector2(screenX, size.Y), GridColor * new Color(1, 1, 1, 0.3f), 1.0f);
		}

		// 绘制水平网格线
		for (float y = -RangeX; y <= RangeX; y += 1.0f)
		{
			float screenY = centerY - y * ScaleY;
			DrawLine(new Vector2(0, screenY), new Vector2(size.X, screenY), GridColor * new Color(1, 1, 1, 0.3f), 1.0f);
		}
	}

	private void DrawAxes()
	{
		var size = Size;
		float centerX = size.X / 2;
		float centerY = size.Y / 2;

		// X 轴
		DrawLine(new Vector2(0, centerY), new Vector2(size.X, centerY), AxisColor, 2.0f);
		
		// Y 轴
		DrawLine(new Vector2(centerX, 0), new Vector2(centerX, size.Y), AxisColor, 2.0f);

		// 绘制箭头
		DrawArrow(new Vector2(size.X - 10, centerY - 5), new Vector2(size.X, centerY), AxisColor);
		DrawArrow(new Vector2(centerX + 5, 10), new Vector2(centerX, 0), AxisColor);

		// 绘制轴标签
		DrawString(DefaultFontSize * 1.2f, new Vector2(size.X - 25, centerY + 20), "x");
		DrawString(DefaultFontSize * 1.2f, new Vector2(centerX + 15, 20), "y");
	}

	private void DrawArrow(Vector2 start, Vector2 end, Color color)
	{
		float arrowSize = 10.0f;
		float angle = (end - start).Angle();
		
		DrawLine(start, end, color, 2.0f);
		DrawLine(end, end + Vector2.Right.Rotated(angle - Mathf.Pi / 6) * arrowSize, color, 2.0f);
		DrawLine(end, end + Vector2.Right.Rotated(angle + Mathf.Pi / 6) * arrowSize, color, 2.0f);
	}

	private void DrawFunction()
	{
		var size = Size;
		float centerX = size.X / 2;
		float centerY = size.Y / 2;

		// 绘制 x < 0 时的函数图像：f(x) = 2^x
		Vector2? prevPoint = null;
		
		for (float x = -RangeX; x < 0; x += 0.05f)
		{
			float y = (float)Math.Pow(2, x);
			
			float screenX = centerX + x * ScaleX;
			float screenY = centerY - y * ScaleY;
			
			Vector2 currentPoint = new Vector2(screenX, screenY);
			
			if (prevPoint.HasValue)
			{
				DrawLine(prevPoint.Value, currentPoint, GraphColor, 3.0f);
			}
			
			prevPoint = currentPoint;
		}

		// 绘制 x >= 0 时的示例函数（这里用虚线表示可能的延伸）
		prevPoint = null;
		for (float x = 0; x <= RangeX; x += 0.05f)
		{
			// 示例：假设 f(x) = 1（仅用于可视化）
			float y = 1.0f;
			
			float screenX = centerX + x * ScaleX;
			float screenY = centerY - y * ScaleY;
			
			Vector2 currentPoint = new Vector2(screenX, screenY);
			
			if (prevPoint.HasValue)
			{
				DrawLine(prevPoint.Value, currentPoint, Colors.Red * new Color(1, 1, 1, 0.5f), 2.0f, true);
			}
			
			prevPoint = currentPoint;
		}
	}

	private void DrawLabels()
	{
		var size = Size;
		float centerX = size.X / 2;
		float centerY = size.Y / 2;

		// 绘制原点标签
		DrawString(DefaultFontSize, new Vector2(centerX - 15, centerY + 20), "O");

		// 绘制 x 轴刻度标签
		for (int x = (int)-RangeX; x <= (int)RangeX; x++)
		{
			if (x == 0) continue;
			float screenX = centerX + x * ScaleX;
			DrawString(DefaultFontSize * 0.8f, new Vector2(screenX - 5, centerY + 20), x.ToString());
		}

		// 绘制 y 轴刻度标签
		for (int y = (int)-RangeX; y <= (int)RangeX; y++)
		{
			if (y == 0) continue;
			float screenY = centerY - y * ScaleY;
			DrawString(DefaultFontSize * 0.8f, new Vector2(centerX + 10, screenY + 5), y.ToString());
		}

		// 绘制函数表达式
		DrawString(DefaultFontSize * 1.1f, new Vector2(10, 30), "f(x) = 2^x (x < 0)");
		DrawString(DefaultFontSize * 1.1f, new Vector2(10, 50), "f(x) = ? (x ≥ 0)");
	}

	private void DrawString(float fontSize, Vector2 pos, string text)
	{
		DrawString(ThemeDB.FallbackFont, pos, text, HorizontalAlignment.Left, -1, (int)fontSize, Colors.Black);
	}
}
