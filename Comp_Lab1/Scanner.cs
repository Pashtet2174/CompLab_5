using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Comp_Lab1
{
    public enum TokenType
    {
        KeywordConst = 1,  
        KeywordVal = 2,    
        Identifier = 3,     
        StringConstant = 4, 
        Assignment = 10,   
        Semicolon = 16,   
        Error = 99,         
        ErrorOnlyBadChars = 100
    }

    public class Token
    {
        public int Code { get; set; }
        public string TypeName { get; set; }
        public string Value { get; set; }
        public int Line { get; set; }
        public int StartPos { get; set; }
        public int EndPos { get; set; }
    }

    public class Scanner
    {
        private readonly string _source;
        private readonly Dictionary<string, TokenType> _keywords = new Dictionary<string, TokenType> 
        { 
            { "const", TokenType.KeywordConst }, 
            { "val", TokenType.KeywordVal } 
        };

        public Scanner(string source)
        {
            _source = source;
        } 

        public List<Token> Analyze()
        {
            var tokens = new List<Token>();
            int i = 0;
            int currentLine = 1;
            int lineStartPos = 0;

            while (i < _source.Length)
            {
                char c = _source[i];
                int startInLine = i - lineStartPos + 1;

                if (char.IsWhiteSpace(c))
                {
                    if (c == '\n') 
                    { 
                        currentLine++; 
                        lineStartPos = i + 1; 
                    }
                    i++;
                    continue; 
                }

                if (c == '"')
                {
                    int start = i;
                    i++; 
                    
                    while (i < _source.Length && _source[i] != '"' && _source[i] != '\n') 
                    {
                        i++;
                    }

                    if (i < _source.Length && _source[i] == '"')
                    {
                        i++;
                        string val = _source.Substring(start, i - start);
                        tokens.Add(CreateToken(TokenType.StringConstant, Label.TypeString, val, currentLine, startInLine, i - lineStartPos));
                    }
                    else
                    {
                        tokens.Add(CreateToken(TokenType.Error, Label.TypeErrorString, _source.Substring(start, i - start), currentLine, startInLine, i - lineStartPos));
                    }
                    continue;
                }

                if (c == '=')
                {
                    tokens.Add(CreateToken(TokenType.Assignment, Label.TypeAssign, "=", currentLine, startInLine, startInLine));
                    i++; 
                    continue;
                }
                if (c == ';')
                {
                    tokens.Add(CreateToken(TokenType.Semicolon, Label.TypeSemicolon, ";", currentLine, startInLine, startInLine));
                    i++; 
                    continue;
                }

                int wordStart = i;
                while (i < _source.Length)
                {
                    char ch = _source[i];
                    if (char.IsWhiteSpace(ch) || ch == '"' || ch == '=' || ch == ';') break;
                    i++;
                }

                string fullWord = _source.Substring(wordStart, i - wordStart);
                if (string.IsNullOrEmpty(fullWord)) continue;

                Func<char, bool> IsMyValidChar = (ch) => {
                    return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '_';
                };

                int left = 0;
                while (left < fullWord.Length && !IsMyValidChar(fullWord[left]))
                {
                    left++;
                }

                int right = fullWord.Length - 1;
                while (right >= left && !IsMyValidChar(fullWord[right]))
                {
                    right--;
                }

                if (left > right) 
                {
                    tokens.Add(CreateToken(TokenType.ErrorOnlyBadChars, "Недопустимые символы", fullWord, currentLine, startInLine, startInLine + fullWord.Length - 1));
                }
                else
                {
                    if (left > 0)
                    {
                        string leftTrash = fullWord.Substring(0, left);
                        tokens.Add(CreateToken(TokenType.ErrorOnlyBadChars, "Недопустимые символы", leftTrash, currentLine, startInLine, startInLine + left - 1));
                    }

                    string coreWord = fullWord.Substring(left, right - left + 1);
                    int coreStartPos = startInLine + left;
                    int coreEndPos = startInLine + right;

                    if (IsValidIdentifier(coreWord)) 
                    {
                        if (_keywords.TryGetValue(coreWord, out TokenType keywordType))
                            tokens.Add(CreateToken(keywordType, Label.TypeKeyword, coreWord, currentLine, coreStartPos, coreEndPos));
                        else
                            tokens.Add(CreateToken(TokenType.Identifier, Label.TypeIdentifier, coreWord, currentLine, coreStartPos, coreEndPos));
                    }
                    else
                    {
                        tokens.Add(CreateToken(TokenType.Error, Label.TypeErrorSymbol, coreWord, currentLine, coreStartPos, coreEndPos));
                    }
                    
                    if (right < fullWord.Length - 1)
                    {
                        string rightTrash = fullWord.Substring(right + 1);
                        tokens.Add(CreateToken(TokenType.ErrorOnlyBadChars, "Недопустимые символы", rightTrash, currentLine, startInLine + right + 1, startInLine + fullWord.Length - 1));
                    }
                }
                continue;
            }
            return tokens;
        }

        private bool IsValidIdentifier(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;

            if (!((word[0] >= 'a' && word[0] <= 'z') || (word[0] >= 'A' && word[0] <= 'Z')))
            {
                return false;
            }

            for (int k = 1; k < word.Length; k++)
            {
                char ch = word[k];
                bool isLatin = (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z');
                bool isDigit = ch >= '0' && ch <= '9';
                bool isUnderscore = ch == '_';

                if (!isLatin && !isDigit && !isUnderscore)
                {
                    return false;
                }
            }

            return true;
        }

        private Token CreateToken(TokenType type, string name, string val, int line, int start, int end)
        {
            return new Token { Code = (int)type, TypeName = name, Value = val, Line = line, StartPos = start, EndPos = end };
        }
    }
}