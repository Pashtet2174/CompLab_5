
using System.Text;
using System.Text.RegularExpressions;

namespace Comp_Lab1
{
    public class Optimizer
    {
        public string RunOptimizations(ConstDeclNode node)
        {
            var sb = new StringBuilder();

            string idName = node.Name;
            string originalString = "";
            
            if (node.ValueNode is StringLiteralNode strNode)
            {
                originalString = strNode.Value;
            }

            bool hasEnclosingQuotes = originalString.StartsWith("\"") && originalString.EndsWith("\"");
            sb.AppendLine("--- Исходный IR (TAC) ---");
            string formattedOriginal = hasEnclosingQuotes ? originalString : $"\"{originalString}\"";
            sb.AppendLine($"t1 = {formattedOriginal}");
            sb.AppendLine($"CONST_VAL {idName}:String = t1");
            sb.AppendLine();

            string coreString = hasEnclosingQuotes 
                ? originalString.Substring(1, originalString.Length - 2) 
                : originalString;

            coreString = coreString.Replace("\\t", " ").Replace("\t", " "); 
            coreString = coreString.Replace("\\n", " ").Replace("\n", " "); 

            coreString = Regex.Replace(coreString, @"\s+", " "); 

            string optimizedString = coreString.Trim();

            sb.AppendLine("--- Результат Оптимизации №1 ---");
            sb.AppendLine($"t1 = \"{optimizedString}\"");
            sb.AppendLine($"CONST_VAL {idName}:String = t1");
            sb.AppendLine();
            sb.AppendLine("--- Результат Оптимизации №2 ---");
            sb.AppendLine($"const val {idName}: String = \"{optimizedString}\"");

            return sb.ToString();
        }
    }
}