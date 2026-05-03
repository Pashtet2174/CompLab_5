namespace Comp_Lab1;

public class SemanticAnalyzer
{
    private readonly SymbolTable _symbolTable = new SymbolTable();
    public List<SemanticError> Errors { get; } = new List<SemanticError>();
    public List<AstNode> AstRoots { get; } = new List<AstNode>();

    public void Analyze(List<Token> tokens)
    {
        int i = 0;
        while (i < tokens.Count)
        {
            if (tokens[i].Code == (int)TokenType.KeywordConst && i + 2 < tokens.Count)
            {
                var constNode = new ConstDeclNode();
                constNode.Modifiers.Add("const");
                
                if (tokens[i + 1].Code == (int)TokenType.KeywordVal)
                {
                    constNode.Modifiers.Add("val");
                }

                var idToken = tokens[i + 2];
                if (idToken.Code == (int)TokenType.Identifier)
                {
                    constNode.Name = idToken.Value;

                    if (_symbolTable.CheckDuplicate(constNode.Name))
                    {
                        int prevLine = _symbolTable.GetDeclarationLine(constNode.Name);
                        Errors.Add(new SemanticError
                        {
                            Line = idToken.Line,
                            Position = idToken.StartPos,
                            Message = $"Ошибка: идентификатор \"{constNode.Name}\" уже объявлен ранее (строка {prevLine})"
                        });
                    }
                    else
                    {
                        _symbolTable.Declare(constNode.Name, idToken.Line);
                        
                        int assignIdx = i + 3;
                        if (assignIdx < tokens.Count && tokens[assignIdx].Code == (int)TokenType.Assignment)
                        {
                            int valIdx = assignIdx + 1;
                            if (valIdx < tokens.Count)
                            {
                                var valToken = tokens[valIdx];
                                
                                if (valToken.Code == (int)TokenType.StringConstant)
                                {
                                    constNode.ValueNode = new StringLiteralNode { Value = valToken.Value };
                                }
                                else
                                {
                                    Errors.Add(new SemanticError
                                    {
                                        Line = valToken.Line,
                                        Position = valToken.StartPos,
                                        Message = $"Ошибка: несовместимость типов. Ожидается строковый литерал, получено '{valToken.Value}'"
                                    });
                                }
                            }
                        }
                        
                        AstRoots.Add(constNode);
                    }
                }
            }
            i++;
        }
    }
}