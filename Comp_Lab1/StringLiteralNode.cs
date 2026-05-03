using System.Text;

namespace Comp_Lab1;

public class StringLiteralNode : AstNode
{
    public string Value { get; set; }

    public override void Print(StringBuilder sb, string indent, bool isLast)
    {
        sb.AppendLine(indent + (isLast ? "└── " : "├── ") + "value: " + Value);
    }
}