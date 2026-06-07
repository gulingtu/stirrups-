using Godot;
using System;

public partial class MathVisualization : Control
{
	[Export]
	public float ScaleX { get; set; } = 50.0f;
	[Export]
	public float ScaleY { get; set; } = 50.0f;
	[Export]
	public float RangeX { get; set; } = 10.0f;
	[Export]
	public Color GraphColor { get; set; } = Colors.Blue;
	[Export]
	public Color GridColor { get; set; } = Colors.Gray;
	[Export]
	public Color AxisColor { get; set; } = Colors.Black;
	[Export]
	public Color DSetColor { get; set; } = Colors.Orange;

	private const int DefaultFontSize = 16;

	// 由 Main.cs 传入的 x₀ 值，用于 D 集合可视化
	public float? InputX0 { get; set; } = null;

	public override void _Ready()
	{
		QueueRedraw();
	}

	public override void _Draw()
	{
		DrawGrid();
		DrawAxes();
		DrawFunction();
		DrawLabels();

		// 如果有 x₀ 输入，绘制 D 集合可视化
		if (InputX0.HasValue)
		{
			DrawDSetVisualization(InputX0.Value);
		}
	}

	private void DrawGrid()
	{
		var size = Size;
		float centerX = size.X / 2;
		float centerY = size.Y / 2;

		for (float x = -RangeX; x <= RangeX; x += 1.0f)
		{
			float screenX = centerX + x * ScaleX;
			DrawLine(new Vector2(screenX, 0), new Vector2(screenX, size.Y), GridColor * new Color(1, 1, 1, 0.3f), 1.0f);
		}

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

		DrawLine(new Vector2(0, centerY), new Vector2(size.X, centerY), AxisColor, 2.0f);
		DrawLine(new Vector2(centerX, 0), new Vector2(centerX, size.Y), AxisColor, 2.0f);

		DrawArrow(new Vector2(size.X - 10, centerY - 5), new Vector2(size.X, centerY), AxisColor);
		DrawArrow(new Vector2(centerX + 5, 10), new Vector2(centerX, 0), AxisColor);

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

		// x < 0 时：f(x) = 2^x（蓝色实线）
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

		// x >= 0 时：无足够条件，红色占位虚线（y = 1 仅做视觉参考）
		prevPoint = null;
		for (float x = 0; x <= RangeX; x += 0.05f)
		{
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

		DrawString(DefaultFontSize, new Vector2(centerX - 15, centerY + 20), "O");

		for (int x = (int)-RangeX; x <= (int)RangeX; x++)
		{
			if (x == 0) continue;
			float screenX = centerX + x * ScaleX;
			DrawString(DefaultFontSize * 0.8f, new Vector2(screenX - 5, centerY + 20), x.ToString());
		}

		for (int y = (int)-RangeX; y <= (int)RangeX; y++)
		{
			if (y == 0) continue;
			float screenY = centerY - y * ScaleY;
			DrawString(DefaultFontSize * 0.8f, new Vector2(centerX + 10, screenY + 5), y.ToString());
		}

		DrawLine(new Vector2(10, 10), new Vector2(30, 10), GraphColor, 3.0f);
		DrawString(DefaultFontSize * 0.9f, new Vector2(35, 5), "f(x) = 2^x (x < 0)");

		DrawDashedLineSegment(new Vector2(10, 25), new Vector2(30, 25), Colors.Red * new Color(1, 1, 1, 0.5f), 2.0f);
		DrawString(DefaultFontSize * 0.9f, new Vector2(35, 20), "f(x) = ? (x ≥ 0)");
	}

	/// <summary>
	/// 绘制 D 集合可视化：
	/// - 在 x₀ 处画竖虚线
	/// - 若 x₀ < 0，在 f(x₀) 处画水平虚线、标记点 (x₀, f(x₀))
	/// - 在 x 轴上高亮已知的 D(x₀) 区间
	/// - 标注 D(x₀) 的数学定义
	/// </summary>
	private void DrawDSetVisualization(float x0)
	{
		var size = Size;
		float centerX = size.X / 2;
		float centerY = size.Y / 2;

		float screenX0 = centerX + x0 * ScaleX;

		// 1. 在 x₀ 处画橙色竖虚线
		DrawDashedLine(new Vector2(screenX0, 0), new Vector2(screenX0, size.Y), DSetColor, 1.5f);
		DrawString(DefaultFontSize * 0.9f, new Vector2(screenX0 - 15, size.Y - 5), $"x₀={x0:F2}");

		if (x0 < 0)
		{
			// 可以计算 f(x₀) = 2^(x₀)
			float fx0 = (float)Math.Pow(2, x0);
			float screenY0 = centerY - fx0 * ScaleY;

			// 2. 在 f(x₀) 处画橙色水平虚线
			DrawDashedLine(new Vector2(0, screenY0), new Vector2(size.X, screenY0), DSetColor, 1.5f);

			// 3. 标记圆点 (x₀, f(x₀))
			DrawCircle(new Vector2(screenX0, screenY0), 5, DSetColor);
			DrawString(DefaultFontSize * 0.9f, new Vector2(screenX0 + 10, screenY0 - 10), $"({x0:F2}, {fx0:F4})");

			// 4. 高亮已知 D(x₀) 区间：f(x) > f(x₀) 且 x < 0（已知区域）
			// 对于 x₀ < 0, f(x) = 2^x 严格递增, 所以 f(x) > f(x₀) 等价于 x > x₀
			// 已知区域中：x₀ < x < 0, 对应 d = x - x₀, 即 0 < d < -x₀
			float highlightLeft = Math.Max(-RangeX, x0);
			float highlightRight = 0;
			float dRight = -x0;

			// 在 x 轴下方画高亮条，表示已知 D(x₀) 范围
			float barY = centerY + 10;
			float barHeight = 8;
			float hlScreenLeft = centerX + highlightLeft * ScaleX;
			float hlScreenRight = centerX + highlightRight * ScaleX;

			if (hlScreenRight > hlScreenLeft)
			{
				DrawRect(new Rect2(hlScreenLeft, barY, hlScreenRight - hlScreenLeft, barHeight), DSetColor * new Color(1, 1, 1, 0.6f));
				DrawRect(new Rect2(hlScreenLeft, barY, hlScreenRight - hlScreenLeft, barHeight), DSetColor, false, 1.5f);
			}

			// 在 x 轴上方高亮函数曲线段（f(x) > f(x₀) 的已知部分）
			Vector2? prevPt = null;
			for (float x = highlightLeft; x <= highlightRight; x += 0.05f)
			{
				float y = (float)Math.Pow(2, x);
				float sx = centerX + x * ScaleX;
				float sy = centerY - y * ScaleY;
				Vector2 pt = new Vector2(sx, sy);
				if (prevPt.HasValue)
				{
					DrawLine(prevPt.Value, pt, DSetColor, 4.0f);
				}
				prevPt = pt;
			}

			// 5. 标注 D(x₀) 信息
			string dSetText = $"D({x0:F2}) = (0, {-x0:F2})  （已知部分）";
			DrawString(DefaultFontSize, new Vector2(10, size.Y - 30), dSetText);

			// 在 D 集合区间上方加文字标注
			float labelX = (hlScreenLeft + hlScreenRight) / 2;
			DrawString(DefaultFontSize * 0.8f, new Vector2(labelX - 40, barY - 5), "D(x₀)");
		}
		else // x0 >= 0
		{
			// 无法计算 f(x₀)
			float labelPosX = Math.Min(screenX0, size.X - 100);
			DrawString(DefaultFontSize, new Vector2(labelPosX, centerY / 2), $"f({x0:F2}) = ?  无足够条件");

			// 在 x 轴上标记"未知区域"
			DrawString(DefaultFontSize * 0.8f, new Vector2(screenX0 - 20, centerY + 25), "?");
		}
	}

	/// <summary>
	/// 绘制虚线（通过短线段模拟）
	/// </summary>
	private void DrawDashedLine(Vector2 from, Vector2 to, Color color, float width)
	{
		float dashLen = 8.0f;
		float gapLen = 5.0f;
		Vector2 direction = (to - from).Normalized();
		float totalLen = from.DistanceTo(to);
		float drawn = 0;

		while (drawn < totalLen)
		{
			float segEnd = Math.Min(drawn + dashLen, totalLen);
			Vector2 segFrom = from + direction * drawn;
			Vector2 segTo = from + direction * segEnd;
			DrawLine(segFrom, segTo, color, width);
			drawn += dashLen + gapLen;
		}
	}

	/// <summary>
	/// 绘制一小段虚线（用于图例中的虚线展示）
	/// </summary>
	private void DrawDashedLineSegment(Vector2 from, Vector2 to, Color color, float width)
	{
		DrawDashedLine(from, to, color, width);
	}

	private void DrawString(float fontSize, Vector2 pos, string text)
	{
		DrawString(ThemeDB.FallbackFont, pos, text, HorizontalAlignment.Left, -1, (int)fontSize, Colors.Black);
	}
}
