using System.Text;

namespace Comp_Lab1;

public class ConstDeclNode : AstNode
{
    public string Name { get; set; }
    public List<string> Modifiers { get; set; } = new List<string>();
    public AstNode ValueNode { get; set; }

    public override void Print(StringBuilder sb, string indent, bool isLast)
    {
        sb.AppendLine(indent + "ConstDeclNode");
        indent += "    ";
        sb.AppendLine(indent + "├── name: \"" + Name + "\"");
            
        string modifiersStr = string.Join(", ", Modifiers.Select(m => $"\"{m}\""));
        sb.AppendLine(indent + "├── modifiers: [" + modifiersStr + "]");
            
        sb.Append(indent + "└── value: StringLiteralNode\r\n");
        if (ValueNode != null)
        {
            ValueNode.Print(sb, indent + "    ", true);
        }
    }
}