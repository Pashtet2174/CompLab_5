using System.Collections.Generic;
using System.Linq;

namespace Comp_Lab1
{
    public class ParserError
    {
        public Token Token { get; set; }
        public string Message { get; set; }
    }

    public class Parser
    {
        private readonly List<Token> _tokens;
        public List<ParserError> Errors { get; } = new List<ParserError>();
        
        private readonly TokenType[] _expectedSequence = 
        {
            TokenType.KeywordConst,
            TokenType.KeywordVal,
            TokenType.Identifier,
            TokenType.Assignment,
            TokenType.StringConstant,
            TokenType.Semicolon
        };

        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
        }

        public void Analyze()
        {
            int i = 0;

            while (i < _tokens.Count)
            {
                while (i < _tokens.Count && 
                       _tokens[i].Code == (int)TokenType.ErrorOnlyBadChars) 
                {
                    Errors.Add(new ParserError { 
                        Token = _tokens[i], 
                        Message = string.Format(Label.ErrLexical, _tokens[i].TypeName, _tokens[i].Value)
                    });
                    i++;
                }
                if (i >= _tokens.Count) break;
                if (_tokens[i].Code == (int)TokenType.Semicolon)
                {
                    Errors.Add(new ParserError
                    {
                        Token = _tokens[i],
                        Message = string.Format(Label.ErrMissingElement, GetTokenName(_expectedSequence[0]))
                    });
                    i++; 
                    continue; 
                }
                int state = 0;
                bool wasStarted = false; 
                while (state < _expectedSequence.Length && i < _tokens.Count)
                {
                    var currentToken = _tokens[i];
                    if (_expectedSequence[state] == TokenType.Semicolon && 
                        currentToken.Code == (int)TokenType.KeywordConst)
                    {
                        Errors.Add(new ParserError
                        {
                            Token = _tokens[i - 1], 
                            Message = string.Format(Label.ErrMissingElement, ";")
                        });
                        wasStarted = false; 
                        break; 
                    }
                    if (currentToken.Code == (int)TokenType.ErrorOnlyBadChars)
                    {
                        Errors.Add(new ParserError { 
                            Token = currentToken, 
                            Message = string.Format(Label.ErrLexical, currentToken.TypeName, currentToken.Value) 
                        });

                        if (currentToken.Value.StartsWith("\"") && _expectedSequence[state] == TokenType.StringConstant)
                        {
                            state++;
                        }
                        i++;
                        wasStarted = true;
                        continue;
                    }

                    if (currentToken.Code == (int)_expectedSequence[state])
                    {
                        state++;
                        i++;
                        wasStarted = true; 
                    }
                    else
                    {
                        bool foundExpectedAhead = false;
                        int lookaheadIndex = i;

                        while (lookaheadIndex < _tokens.Count)
                        {
                            if (_tokens[lookaheadIndex].Code == (int)_expectedSequence[state])
                            {
                                foundExpectedAhead = true;
                                break;
                            }

                            if (_tokens[lookaheadIndex].Code == (int)TokenType.Semicolon)
                            {
                                break;
                            }

                            lookaheadIndex++;
                        }

                        if (foundExpectedAhead)
                        {
                            for (int k = i; k < lookaheadIndex; k++)
                            {
                                if (_tokens[k].Code == (int)TokenType.ErrorOnlyBadChars)
                                {
                                    Errors.Add(new ParserError {
                                        Token = _tokens[k],
                                        Message = string.Format(Label.ErrLexical, _tokens[k].TypeName, _tokens[k].Value)
                                    });
                                }
                                else
                                {
                                    Errors.Add(new ParserError {
                                        Token = _tokens[k],
                                        Message = string.Format(Label.ErrExtraElement, _tokens[k].Value, GetTokenName(_expectedSequence[state]))
                                    });
                                }
                            }
                            i = lookaheadIndex; 
                        }
                        else
                        {
                            Errors.Add(new ParserError {
                                Token = currentToken,
                                Message = string.Format(Label.ErrMissingElement, GetTokenName(_expectedSequence[state]))
                            });
                            
                            bool advanceI = false;
                            int nextAnchorStateIndex = -1;
                            int anchorTokenIndex = -1;
                            
                            for (int s = state + 1; s < _expectedSequence.Length; s++)
                            {
                                if (_expectedSequence[s] == TokenType.Assignment || _expectedSequence[s] == TokenType.Semicolon)
                                {
                                    for (int k = i; k < _tokens.Count; k++)
                                    {
                                        if (_tokens[k].Code == (int)_expectedSequence[s])
                                        {
                                            anchorTokenIndex = k;
                                            break;
                                        }
                                    }

                                    if (anchorTokenIndex != -1)
                                    {
                                        nextAnchorStateIndex = s;
                                        break;
                                    }
                                }
                            }

                            if (anchorTokenIndex != -1)
                            {
                                int statesToAnchor = nextAnchorStateIndex - state;
                                int tokensToAnchor = anchorTokenIndex - i;
                                if (tokensToAnchor >= statesToAnchor)
                                {
                                    advanceI = true;
                                }
                            }
                            else
                            {
                                if (currentToken.Code != (int)TokenType.Semicolon)
                                {
                                    advanceI = true;
                                }   
                            }

                            state++; 
                            wasStarted = true;
                            
                            if (advanceI)
                            {
                                i++;
                            }
                        }
                    }
                }

                if (wasStarted && state < _expectedSequence.Length && i >= _tokens.Count)
                {
                    var lastToken = _tokens.LastOrDefault() ?? new Token { Line = 1, StartPos = 1, EndPos = 1 };
                    Errors.Add(new ParserError 
                    {
                        Token = lastToken,
                        Message = string.Format(Label.ErrUnexpectedEOF, GetTokenName(_expectedSequence[state]))
                    });
                }
            }
        }
        private string GetTokenName(TokenType type)
        {
            switch (type)
            {
                case TokenType.KeywordConst: return "const";
                case TokenType.KeywordVal: return "val";
                case TokenType.Identifier: return Label.TypeIdentifier;
                case TokenType.Assignment: return "=";
                case TokenType.StringConstant: return Label.TypeString;
                case TokenType.Semicolon: return ";";
                default: return type.ToString();
            }
        }
    }
}