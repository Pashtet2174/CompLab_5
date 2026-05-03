using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace Comp_Lab1
{
    public partial class AstVisualizerForm : Form
    {
        private readonly List<AstNode> _astRoots;
        private readonly SKControl _skControl;

        public AstVisualizerForm(List<AstNode> astRoots)
        {
            _astRoots = astRoots;
            Text = "Визуализация AST";
            Width = 900;
            Height = 700;
            StartPosition = FormStartPosition.CenterScreen;

            _skControl = new SKControl { Dock = DockStyle.Fill };
            _skControl.PaintSurface += OnPaintSurface;
            Controls.Add(_skControl);
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
{
    var canvas = e.Surface.Canvas;
    canvas.Clear(SKColors.White);

    if (_astRoots == null || !_astRoots.Any()) return;

    using var linePaint = new SKPaint
    {
        Color = SKColors.LightGray,
        StrokeWidth = 2,
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        PathEffect = SKPathEffect.CreateDash(new float[] { 4, 2 }, 0)
    };

    float startY = 50;
    float treeGap = 150; 
    float currentX = 100; 

    foreach (var root in _astRoots)
    {
        float treeWidth = MeasureTreeWidth(root);

        DrawNode(canvas, root, currentX + treeWidth / 2f, startY, treeWidth * 0.8f, linePaint);

        currentX += treeWidth + treeGap;
    }
}

private float MeasureTreeWidth(AstNode node)
{
    if (node == null) return 0;

    float minNodeWidth = 140f; 

    if (node is ConstDeclNode c)
    {
        int branchCount = 2 + (c.ValueNode != null ? 1 : 0);
        
        float modsWidth = Math.Max(minNodeWidth, c.Modifiers.Count * 80f);
        float idWidth = minNodeWidth;
        float valueWidth = c.ValueNode != null ? MeasureTreeWidth(c.ValueNode) : 0;

        return modsWidth + idWidth + valueWidth + 40f; 
    }
    else if (node is StringLiteralNode)
    {
        return minNodeWidth;
    }

    return minNodeWidth;
}

private void DrawNode(SKCanvas canvas, AstNode node, float x, float y, float spread, SKPaint linePaint)
{
    if (node == null) return;

    using var bodyPaint = new SKPaint { Color = new SKColor(235, 245, 255), Style = SKPaintStyle.Fill, IsAntialias = true };
    using var borderPaint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f, IsAntialias = true };
    using var textPaint = new SKPaint { Color = SKColors.Black, TextSize = 12, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Arial") };
    using var valuePaint = new SKPaint { Color = new SKColor(255, 255, 225), Style = SKPaintStyle.Fill, IsAntialias = true };

    var visualChildren = new List<(string Title, AstNode RealNode, float Weight)>();

    if (node is ConstDeclNode c)
    {
        visualChildren.Add(("Modifiers", null, Math.Max(1f, c.Modifiers.Count * 0.6f)));
        visualChildren.Add(("Identifier", null, 1f));
        if (c.ValueNode != null) 
            visualChildren.Add((c.ValueNode.GetType().Name.Replace("Node", ""), c.ValueNode, 1.2f));
    }
    else if (node is StringLiteralNode s)
    {
        visualChildren.Add(("Value", null, 1f));
    }

    string mainTitle = node is ConstDeclNode ? "ConstDecl" : node.GetType().Name.Replace("Node", "");
    float nodeW = 120, nodeH = 40;
    var rect = new SKRect(x - nodeW / 2, y, x + nodeW / 2, y + nodeH);
    canvas.DrawRoundRect(rect, 5, 5, bodyPaint);
    canvas.DrawRoundRect(rect, 5, 5, borderPaint);
    DrawCenteredText(canvas, mainTitle, x, y + 25, textPaint);

    if (visualChildren.Any())
    {
        float childY = y + 100;
        float totalWeight = visualChildren.Sum(vc => vc.Weight);
        float currentChildX = x - spread / 2f;

        foreach (var child in visualChildren)
        {
            float childSpace = (child.Weight / totalWeight) * spread;
            float centerX = currentChildX + childSpace / 2f;

            canvas.DrawLine(x, y + nodeH, centerX, childY, linePaint);

            if (child.RealNode != null)
            {
                DrawNode(canvas, child.RealNode, centerX, childY, childSpace * 0.9f, linePaint);
            }
            else
            {
                var cRect = new SKRect(centerX - nodeW / 2, childY, centerX + nodeW / 2, childY + nodeH);
                canvas.DrawRoundRect(cRect, 5, 5, bodyPaint);
                canvas.DrawRoundRect(cRect, 5, 5, borderPaint);
                DrawCenteredText(canvas, child.Title, centerX, childY + 25, textPaint);

                float leafY = childY + 80;
                if (child.Title == "Modifiers" && node is ConstDeclNode cn)
                {
                    float modStartX = centerX - (cn.Modifiers.Count - 1) * 40;
                    foreach (var m in cn.Modifiers)
                    {
                        DrawLeafNode(canvas, centerX, childY + nodeH, modStartX, leafY, m, linePaint, valuePaint, borderPaint, textPaint);
                        modStartX += 80;
                    }
                }
                else if (child.Title == "Identifier" && node is ConstDeclNode cin)
                {
                    DrawLeafNode(canvas, centerX, childY + nodeH, centerX, leafY, cin.Name, linePaint, valuePaint, borderPaint, textPaint);
                }
                else if (child.Title == "Value" && node is StringLiteralNode sn)
                {
                    DrawLeafNode(canvas, centerX, childY + nodeH, centerX, leafY, $"\"{sn.Value}\"", linePaint, valuePaint, borderPaint, textPaint);
                }
            }
            currentChildX += childSpace;
        }
    }
}

        private void DrawLeafNode(SKCanvas canvas, float parentX, float parentY, float leafX, float leafY, string text, 
            SKPaint linePaint, SKPaint bodyPaint, SKPaint borderPaint, SKPaint textPaint)
        {
            float leafW = 70;
            float leafH = 30;
            canvas.DrawLine(parentX, parentY, leafX, leafY, linePaint);
            var rect = new SKRect(leafX - leafW / 2, leafY, leafX + leafW / 2, leafY + leafH);
            canvas.DrawRoundRect(rect, 3, 3, bodyPaint);
            canvas.DrawRoundRect(rect, 3, 3, borderPaint);
            string displayInterpetation = text.Length > 12 ? text.Substring(0, 10) + ".." : text;
            DrawCenteredText(canvas, displayInterpetation, leafX, leafY + 20, textPaint);
        }

        private void DrawCenteredText(SKCanvas canvas, string text, float x, float y, SKPaint paint)
        {
            float width = paint.MeasureText(text);
            canvas.DrawText(text, x - width / 2, y, paint);
        }
        
    }
}