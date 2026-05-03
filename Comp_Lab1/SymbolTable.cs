namespace Comp_Lab1;

public class SymbolTable
{
    private readonly Dictionary<string, int> _symbols = new Dictionary<string, int>();

    public bool CheckDuplicate(string name) => _symbols.ContainsKey(name);

    public void Declare(string name, int line)
    {
        if (!CheckDuplicate(name))
        {
            _symbols.Add(name, line);
        }
    }

    public int GetDeclarationLine(string name) => _symbols.ContainsKey(name) ? _symbols[name] : -1;
}