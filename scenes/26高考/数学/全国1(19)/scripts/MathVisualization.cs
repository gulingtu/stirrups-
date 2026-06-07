using Godot;
using System;
using System.Collections.Generic;

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

	// 由 Main.cs 传入的 x₀ 值
	public float? InputX0 { get; set; } = null;

	// 当前激活的子问题 (0=题干模式, 1=子问题1, 2=子问题2, 3=子问题3)
	public int ActiveSubQuestion { get; set; } = 0;

	// 是否显示解题过程可视化
	public bool ShowSolution { get; set; } = false;

	// ----- 面板布局（在 _Draw 中计算）-----
	private float L;   // 左面板宽度
	private const float G = 30; // 中间间隙
	private float R;   // 右面板宽度
	private float lCX; // 左面板中心 x
	private float dCX; // D-plot 中心 x
	private float cY;  // 垂直中心
	private bool _hasDPlot; // D-plot 是否有效

	public override void _Ready()
	{
		QueueRedraw();
	}

	public override void _Draw()
	{
		// 计算面板分区
		L = Size.X * 0.48f;
		R = Size.X - L - G;
		lCX = L / 2;
		dCX = L + G + R / 2;
		cY = Size.Y / 2;

		// 左侧: f(x) 图
		DrawGridLeft();
		DrawAxesLeft();
		DrawFunction();
		DrawLabels();

		// 右侧: D-plot
		_hasDPlot = false;

		if (ActiveSubQuestion == 1 && ShowSolution && InputX0.HasValue)
		{
			// 子问题(1) 解题模式: x₀ = -1, 完整 D-plot
			DrawDPlot(InputX0.Value, fullInfo: true);
			_hasDPlot = true;
		}
		else if (InputX0.HasValue && ActiveSubQuestion == 0)
		{
			// 题干模式: 部分 D-plot
			DrawDPlot(InputX0.Value, fullInfo: false);
			_hasDPlot = true;
		}

		// D-plot 图例说明
		DrawDPlotLegend();

		// 联动连线 (左右面板之间)
		if (_hasDPlot && InputX0.HasValue && InputX0.Value < 0)
		{
			DrawConnectors(InputX0.Value);
		}

		// 左侧 f-plot 标注 (点, 阈值, 高亮曲线)
		if (ActiveSubQuestion == 1 && ShowSolution)
		{
			DrawFPlotAnnotations(-1.0f, fullInfo: true);
		}
		else if (InputX0.HasValue && ActiveSubQuestion == 0)
		{
			if (InputX0.Value < 0)
				DrawFPlotAnnotations(InputX0.Value, fullInfo: false);
			else
				DrawFPlotX0Unknown(InputX0.Value);
		}
	}

	// ================================================================
	//  左侧: f(x) 图的网格 / 坐标轴
	// ================================================================
	private void DrawGridLeft()
	{
		float left0 = 0;
		float leftW = L;

		for (float x = -RangeX; x <= RangeX; x += 1.0f)
		{
			float sx = lCX + x * ScaleX;
			if (sx >= left0 && sx <= leftW)
				DrawLine(new Vector2(sx, 0), new Vector2(sx, Size.Y), GridColor * new Color(1, 1, 1, 0.3f), 1.0f);
		}
		for (float y = -RangeX; y <= RangeX; y += 1.0f)
		{
			float sy = cY - y * ScaleY;
			if (sy >= 0 && sy <= Size.Y)
				DrawLine(new Vector2(0, sy), new Vector2(leftW, sy), GridColor * new Color(1, 1, 1, 0.3f), 1.0f);
		}
	}

	private void DrawAxesLeft()
	{
		DrawLine(new Vector2(0, cY), new Vector2(L, cY), AxisColor, 2.0f);
		DrawLine(new Vector2(lCX, 0), new Vector2(lCX, Size.Y), AxisColor, 2.0f);

		DrawArrow(new Vector2(L - 10, cY - 5), new Vector2(L, cY), AxisColor);
		DrawArrow(new Vector2(lCX + 5, 10), new Vector2(lCX, 0), AxisColor);

		DrawString(DefaultFontSize * 1.2f, new Vector2(L - 25, cY + 20), "x");
		DrawString(DefaultFontSize * 1.2f, new Vector2(lCX + 15, 20), "y");
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
		// x < 0: f(x) = 2^x
		Vector2? prev = null;
		for (float x = -RangeX; x < 0; x += 0.05f)
		{
			float y = (float)Math.Pow(2, x);
			float sx = lCX + x * ScaleX;
			float sy = cY - y * ScaleY;
			var pt = new Vector2(sx, sy);
			if (prev.HasValue) DrawLine(prev.Value, pt, GraphColor, 3.0f);
			prev = pt;
		}

		// x >= 0
		if (ActiveSubQuestion == 1)
		{
			prev = null;
			for (float x = 0; x <= RangeX; x += 0.05f)
			{
				float y = 1.0f - x;
				float sx = lCX + x * ScaleX;
				float sy = cY - y * ScaleY;
				var pt = new Vector2(sx, sy);
				if (prev.HasValue) DrawLine(prev.Value, pt, Colors.Green, 3.0f);
				prev = pt;
			}
		}
		else
		{
			prev = null;
			for (float x = 0; x <= RangeX; x += 0.05f)
			{
				float y = 1.0f;
				float sx = lCX + x * ScaleX;
				float sy = cY - y * ScaleY;
				var pt = new Vector2(sx, sy);
				if (prev.HasValue)
					DrawLine(prev.Value, pt, Colors.Red * new Color(1, 1, 1, 0.5f), 2.0f, true);
				prev = pt;
			}
		}
	}

	private void DrawLabels()
	{
		DrawString(DefaultFontSize, new Vector2(lCX - 15, cY + 20), "O");
		for (int x = (int)-RangeX; x <= (int)RangeX; x++)
		{
			if (x == 0) continue;
			float sx = lCX + x * ScaleX;
			if (sx >= 0 && sx <= L)
				DrawString(DefaultFontSize * 0.8f, new Vector2(sx - 5, cY + 20), x.ToString());
		}
		for (int y = (int)-RangeX; y <= (int)RangeX; y++)
		{
			if (y == 0) continue;
			float sy = cY - y * ScaleY;
			if (sy >= 0 && sy <= Size.Y)
				DrawString(DefaultFontSize * 0.8f, new Vector2(lCX + 10, sy + 5), y.ToString());
		}

		// 图例
		DrawLine(new Vector2(10, L - 65), new Vector2(30, L - 65), GraphColor, 3.0f);
		DrawString(DefaultFontSize * 0.9f, new Vector2(35, L - 70), "f(x) = 2^x (x < 0)");
		if (ActiveSubQuestion == 1)
		{
			DrawLine(new Vector2(10, L - 50), new Vector2(30, L - 50), Colors.Green, 3.0f);
			DrawString(DefaultFontSize * 0.9f, new Vector2(35, L - 55), "f(x) = 1 - x (x ≥ 0)");
		}
		else
		{
			DrawDashedLine(new Vector2(10, L - 50), new Vector2(30, L - 50), Colors.Red * new Color(1, 1, 1, 0.5f), 2.0f);
			DrawString(DefaultFontSize * 0.9f, new Vector2(35, L - 55), "f(x) = ? (x ≥ 0)");
		}
	}

	// ================================================================
	//  D-plot: g(d) = f(x₀+d) - f(x₀) 的独立坐标系
	// ================================================================
	private void DrawDPlot(float x0, bool fullInfo)
	{
		if (x0 >= 0)
		{
			DrawDPlotEmpty("x₀ ≥ 0: 无法确定 D(x₀)");
			return;
		}

		// ---- 计算 d 范围和 g(d) 值 ----
		float dMin = Math.Min(-2, -x0 - 1);
		float dMax = Math.Max(3, -x0 + 2);
		if (dMax - dMin < 3) { dMax = dMin + 3; }

		// 采样 g(d) 值
		List<float> gVals = new List<float>();
		for (float d = dMin; d <= dMax; d += 0.04f)
		{
			float x = x0 + d;
			float fx;
			if (x < 0)
				fx = (float)Math.Pow(2, x);
			else if (fullInfo)
				fx = 1.0f - x;
			else
				continue; // 未知, 跳过
			float g = fx - (float)Math.Pow(2, x0);
			gVals.Add(g);
		}

		// 确定 g 轴范围
		float gMin = 0, gMax = 0;
		if (gVals.Count > 0)
		{
			gMin = gVals[0]; gMax = gVals[0];
			foreach (var g in gVals)
			{
				if (g < gMin) gMin = g;
				if (g > gMax) gMax = g;
			}
		}
		// 扩展范围确保能看到 g=0
		if (gMin > -0.5f) gMin = -0.5f;
		if (gMax < 0.5f) gMax = 0.5f;
		float gRange = Math.Max(gMax - gMin, 1.0f);

		float dScale = R * 0.8f / (dMax - dMin);
		float gScale = Size.Y * 0.7f / gRange;

		float left0 = L + G; // D-plot 左边界
		float right1 = Size.X; // 右边界
		float dAxisY = cY + gRange / 2 * gScale * 0.2f; // 稍微偏下让 g 的负值也有空间

		// ---- 背景 ----
		DrawRect(new Rect2(left0, 0, right1 - left0, Size.Y), new Color(0.95f, 0.98f, 1.0f, 0.3f));

		// ---- 网格 ----
		for (float d = Mathf.Floor(dMin); d <= dMax; d += 1.0f)
		{
			float sx = dCX + (d - 0) * dScale;
			if (sx >= left0 && sx <= right1)
				DrawLine(new Vector2(sx, 0), new Vector2(sx, Size.Y), GridColor * new Color(1, 1, 1, 0.2f), 1.0f);
		}
		for (float g = Mathf.Floor(gMin); g <= gMax; g += 0.5f)
		{
			if (Mathf.Abs(g) < 0.01f) continue;
			float sy = dAxisY - g * gScale;
			if (sy >= 0 && sy <= Size.Y)
				DrawLine(new Vector2(left0, sy), new Vector2(right1, sy), GridColor * new Color(1, 1, 1, 0.2f), 1.0f);
		}

		// ---- 坐标轴 ----
		// d 轴 (g=0 的水平线)
		float dAxisScreenY = dAxisY;
		DrawLine(new Vector2(left0, dAxisScreenY), new Vector2(right1, dAxisScreenY), AxisColor, 2.0f);
		DrawArrow(new Vector2(right1 - 10, dAxisScreenY - 5), new Vector2(right1, dAxisScreenY), AxisColor);
		DrawString(DefaultFontSize * 1.0f, new Vector2(right1 - 20, dAxisScreenY + 18), "d");

		// g 轴 (d=0 的竖直线)
		float gAxisX = dCX + (0 - 0) * dScale; // d=0 位置
		DrawLine(new Vector2(gAxisX, 0), new Vector2(gAxisX, Size.Y), AxisColor, 2.0f);
		DrawArrow(new Vector2(gAxisX + 5, 10), new Vector2(gAxisX, 0), AxisColor);
		DrawString(DefaultFontSize * 1.0f, new Vector2(gAxisX + 15, 15), "g(d)");

		// ---- d 轴刻度 ----
		for (float d = Mathf.Floor(dMin); d <= dMax; d += 1.0f)
		{
			float sx = dCX + (d - 0) * dScale;
			if (sx >= left0 && sx <= right1)
			{
				DrawLine(new Vector2(sx, dAxisScreenY - 4), new Vector2(sx, dAxisScreenY + 4), AxisColor, 1.0f);
				DrawString(DefaultFontSize * 0.75f, new Vector2(sx - 5, dAxisScreenY + 12), d.ToString("F0"));
			}
		}
		// g=0 点标注
		DrawString(DefaultFontSize * 0.75f, new Vector2(gAxisX + 5, dAxisScreenY + 5), "0");

		// ---- g(d) 曲线: 已知部分 (实线) ----
		Vector2? prevPt = null;
		for (float d = dMin; d <= dMax; d += 0.04f)
		{
			float x = x0 + d;
			float fx;
			if (x < 0) { fx = (float)Math.Pow(2, x); }
			else if (fullInfo) { fx = 1.0f - x; }
			else continue;

			float g = fx - (float)Math.Pow(2, x0);
			float sx = dCX + (d - 0) * dScale;
			float sy = dAxisScreenY - g * gScale;
			var pt = new Vector2(sx, sy);
			if (prevPt.HasValue && sx >= left0 && sx <= right1)
				DrawLine(prevPt.Value, pt, DSetColor, 3.0f);
			prevPt = pt;
		}

		// ---- g(d) 曲线: 未知部分 (虚线/问号) ----
		if (!fullInfo)
		{
			// 从 x₀+d=0 (d=-x₀) 往右, 函数未知
			float splitD = -x0;
			float splitX = dCX + (splitD - 0) * dScale;
			// 画一段灰色虚线表示未知
			for (float d = splitD; d <= dMax; d += 0.08f)
			{
				float sx = dCX + (d - 0) * dScale;
				if (sx >= left0 && sx <= right1 && sx >= splitX)
				{
					DrawString(DefaultFontSize * 1.2f, new Vector2(sx, dAxisScreenY - 50 + 10 * (d % 0.3f < 0.15f ? 0 : 1)), "?");
					if (d > splitD + 0.5f) break; // 只在分界线附近画几个问号
				}
			}
		}

		// ---- 高亮 D(x₀): g(d) > 0 的区域 (展示子集构成) ----
		float dStart = 0;
		float dBoundary = -x0; // 分界点 d = -x₀ (对应函数分界 x=0)
		float dEnd;
		if (fullInfo && x0 < 0)
		{
			float dRightLimit = 1 - x0 - (float)Math.Pow(2, x0);
			dEnd = Math.Min(dRightLimit, dMax);
		}
		else if (!fullInfo)
		{
			dEnd = dBoundary;
		}
		else
		{
			dEnd = dMax;
		}

		float barY = dAxisScreenY + 5;
		float barH = 10;

		// 左子区间: (0, -x₀) — x₀+d < 0 时, f(x₀+d) = 2^{x₀+d}
		float hlL1 = dCX + (dStart - 0) * dScale;
		float hlR1 = dCX + (dBoundary - 0) * dScale;
		hlR1 = Math.Min(hlR1, dCX + (dEnd - 0) * dScale);

		if (hlR1 > hlL1)
		{
			Color subColor1 = new Color(1.0f, 0.6f, 0.0f, 0.85f); // 橙色
			DrawRect(new Rect2(hlL1, barY, hlR1 - hlL1, barH), subColor1);
			DrawRect(new Rect2(hlL1, barY, hlR1 - hlL1, barH), subColor1, false, 1.5f);
			// 标注: d∈(0, -x₀)
			float mid1 = (hlL1 + hlR1) / 2;
			if (fullInfo)
				DrawString(DefaultFontSize * 0.7f, new Vector2(mid1 - 40, barY - 4), $"d∈(0, {dBoundary:F1})");
			else
				DrawString(DefaultFontSize * 0.7f, new Vector2(mid1 - 40, barY - 4), $"d∈(0, {dBoundary:F1})?");
		}

		// 右子区间: [-x₀, dRightLimit) — x₀+d ≥ 0 时, f(x₀+d) = 1-(x₀+d)
		float hlL2 = dCX + (dBoundary - 0) * dScale;
		float hlR2 = dCX + (dEnd - 0) * dScale;

		if (hlR2 > hlL2 && fullInfo)
		{
			Color subColor2 = new Color(0.2f, 0.7f, 1.0f, 0.85f); // 蓝色
			DrawRect(new Rect2(hlL2, barY, hlR2 - hlL2, barH), subColor2);
			DrawRect(new Rect2(hlL2, barY, hlR2 - hlL2, barH), subColor2, false, 1.5f);
			// 标注: d∈[-x₀, dRightLimit)
			float mid2 = (hlL2 + hlR2) / 2;
			DrawString(DefaultFontSize * 0.7f, new Vector2(mid2 - 40, barY - 4), $"d∈[{dBoundary:F1}, {dEnd:F1})");
		}

		// ---- D(x₀) 总标注 ----
		float totalLeft = hlL1;
		string dLabel = fullInfo ? $"D({x0:F1}) = (0, {dBoundary:F1}) ∪ [{dBoundary:F1}, {dEnd:F1})" : $"D({x0:F1}) ⊇ (0, {dBoundary:F1})";
		DrawString(DefaultFontSize * 0.85f, new Vector2(totalLeft, barY + 22), dLabel);

		// ---- 分界线 d = -x₀ (强调显示) ----
		float splitSX = dCX + (dBoundary - 0) * dScale;
		if (splitSX >= left0 && splitSX <= right1)
		{
			DrawDashedLine(new Vector2(splitSX, 0), new Vector2(splitSX, Size.Y), new Color(0.3f, 0.3f, 0.3f, 0.7f), 2.0f);
			DrawString(DefaultFontSize * 0.75f, new Vector2(splitSX + 5, dAxisScreenY - 2), $"d={dBoundary:F1}");
			// 在底部加说明
			DrawString(DefaultFontSize * 0.65f, new Vector2(splitSX - 30, Size.Y - 8), "↑ x=0 在 f 图中的对应分界");
		}

		// ---- 验证示例 (直接在 D-plot 上标点) ----
		float[] exD = fullInfo
			? new float[] { -x0 / 2, -x0, -0.3f }
			: new float[] { (-x0) / 2, -0.3f };

		foreach (float d in exD)
		{
			float x = x0 + d;
			bool known = x < 0 || fullInfo;
			if (!known) continue;

			float fx = (x < 0) ? (float)Math.Pow(2, x) : 1.0f - x;
			float g = fx - (float)Math.Pow(2, x0);
			float sx = dCX + (d - 0) * dScale;
			float sy = dAxisScreenY - g * gScale;

			bool inD = g > 0;
			Color c = inD ? Colors.Green : Colors.Red;
			DrawCircle(new Vector2(sx, sy), 5, c);
			DrawLine(new Vector2(sx, sy), new Vector2(sx, dAxisScreenY), c * new Color(1, 1, 1, 0.4f), 1.0f);

			string mark = inD ? "✓" : "✗";
			DrawString(DefaultFontSize * 0.65f, new Vector2(sx + 6, sy - 4), mark);
		}

		// ---- d=0 起点标记 ----
		float zeroX = gAxisX;
		DrawCircle(new Vector2(zeroX, dAxisScreenY), 4, DSetColor);
		DrawString(DefaultFontSize * 0.7f, new Vector2(zeroX - 20, dAxisScreenY - 12), "d=0 (起点)");
	}

	private void DrawDPlotEmpty(string msg)
	{
		float left0 = L + G;
		DrawRect(new Rect2(left0, 0, Size.X - left0, Size.Y), new Color(0.95f, 0.98f, 1.0f, 0.3f));
		DrawString(DefaultFontSize, new Vector2(left0 + 20, cY), msg);
	}

	private void DrawDPlotLegend()
	{
		float left0 = L + G;
		float y = Size.Y - 20;

		if (!_hasDPlot)
		{
			DrawString(DefaultFontSize * 0.8f, new Vector2(left0 + 10, y), "输入 x₀ 后显示 D(x₀) 图");
			return;
		}

		// 图例: g(d) 曲线
		DrawLine(new Vector2(left0 + 10, y - 25), new Vector2(left0 + 30, y - 25), DSetColor, 3.0f);
		DrawString(DefaultFontSize * 0.7f, new Vector2(left0 + 35, y - 30), "g(d) = f(x₀+d) - f(x₀)");

		DrawRect(new Rect2(left0 + 10, y - 18, 20, 8), DSetColor * new Color(1, 1, 1, 0.7f));
		DrawRect(new Rect2(left0 + 10, y - 18, 20, 8), DSetColor, false, 1.0f);
		DrawString(DefaultFontSize * 0.7f, new Vector2(left0 + 35, y - 15), "D(x₀) = {d | g(d) > 0}");
	}

	// ================================================================
	//  联动连线: 连接 f-plot 和 D-plot 的关键点
	// ================================================================
	private void DrawConnectors(float x0)
	{
		// 连线 A: x₀ (f-plot) → d=0 (D-plot)
		float fX0 = lCX + x0 * ScaleX;
		float d0X = dCX; // d=0 就是 D-plot 的 g 轴
		float connY = cY + 50; // 在 x 轴下方

		// 水平虚线连接
		DrawDashedLine(new Vector2(fX0, connY), new Vector2(d0X, connY), DSetColor * new Color(1, 1, 1, 0.5f), 1.5f);

		// f-plot 端标注
		DrawString(DefaultFontSize * 0.65f, new Vector2(fX0, connY - 14), $"x₀={x0:F1}");
		// D-plot 端标注
		DrawString(DefaultFontSize * 0.65f, new Vector2(d0X - 5, connY - 14), "d=0");

		// 连线 B: x=0 (f-plot) → d=-x₀ (D-plot)
		if (x0 < 0)
		{
			float f0X = lCX;
			float splitD = -x0;
			float dScaleD = R * 0.8f / (Math.Max(3, -x0 + 2) - Math.Min(-2, -x0 - 1));
			float dSplitX = dCX + (splitD - 0) * dScaleD;

			float connY2 = cY + 70;

			DrawDashedLine(new Vector2(f0X, connY2), new Vector2(dSplitX, connY2), new Color(0.5f, 0.5f, 0.5f, 0.5f), 1.5f);
			DrawString(DefaultFontSize * 0.65f, new Vector2(f0X, connY2 - 14), "x=0");
			DrawString(DefaultFontSize * 0.65f, new Vector2(dSplitX - 10, connY2 - 14), $"d={splitD:F1}");

			// 在间隙处加说明文字
			float gapMid = (f0X + dSplitX) / 2;
			if (gapMid > L && gapMid < L + G) gapMid = L + G / 2;
			DrawString(DefaultFontSize * 0.6f, new Vector2(gapMid - 30, connY2 - 28), "函数分界");
		}
	}

	// ================================================================
	//  f-plot 上的标注: P(x₀,f(x₀)), 阈值线, 高亮曲线
	// ================================================================
	private void DrawFPlotAnnotations(float x0, bool fullInfo)
	{
		if (x0 >= 0) return;

		float fx0 = (float)Math.Pow(2, x0);
		float sx0 = lCX + x0 * ScaleX;
		float sy0 = cY - fx0 * ScaleY;

		// P 点
		DrawCircle(new Vector2(sx0, sy0), 6, Colors.Red);
		DrawString(DefaultFontSize * 0.9f, new Vector2(sx0 + 10, sy0 - 8), $"P({x0:F1}, {fx0:F3})");

		// 阈值线
		DrawDashedLine(new Vector2(0, sy0), new Vector2(L, sy0), new Color(0.5f, 0.5f, 0.5f, 0.6f), 1.0f);
		DrawString(DefaultFontSize * 0.8f, new Vector2(5, sy0 - 3), $"y = f({x0:F2}) = {fx0:F3}");

		// 高亮曲线: f(x) > f(x₀) 的区段
		float highlightEnd = fullInfo ? 0.5f : 0;
		Vector2? prev = null;
		for (float x = x0; x <= highlightEnd; x += 0.04f)
		{
			float y = (x < 0) ? (float)Math.Pow(2, x) : (1.0f - x);
			float sx = lCX + x * ScaleX;
			float sy = cY - y * ScaleY;
			var pt = new Vector2(sx, sy);
			if (prev.HasValue)
			{
				Color c = (x < 0) ? new Color(0.2f, 0.6f, 1.0f, 0.9f) : new Color(0.0f, 0.8f, 0.2f, 0.9f);
				DrawLine(prev.Value, pt, c, 5.0f);
			}
			prev = pt;
		}
	}

	private void DrawFPlotX0Unknown(float x0)
	{
		float sx0 = lCX + x0 * ScaleX;
		DrawDashedLine(new Vector2(sx0, 0), new Vector2(sx0, Size.Y), new Color(0.8f, 0.5f, 0.5f, 0.5f), 1.5f);
		DrawString(DefaultFontSize, new Vector2(Math.Min(sx0, L - 160), cY / 2),
			$"x₀ = {x0:F2} ≥ 0, 未知 f(x₀), 无法确定 D(x₀)");
		DrawString(DefaultFontSize * 0.8f, new Vector2(sx0 - 20, cY + 25), "?");
	}

	// ================================================================
	//  工具: 虚线 / 文字
	// ================================================================
	private void DrawDashedLine(Vector2 from, Vector2 to, Color color, float width)
	{
		float dashLen = 8.0f;
		float gapLen = 5.0f;
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

	private void DrawString(float fontSize, Vector2 pos, string text)
	{
		DrawString(ThemeDB.FallbackFont, pos, text, HorizontalAlignment.Left, -1, (int)fontSize, Colors.Black);
	}
}