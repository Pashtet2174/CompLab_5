using System.Text;

namespace Comp_Lab1;

public abstract class AstNode
{
    public abstract void Print(StringBuilder sb, string indent, bool isLast);
}