using System.Collections;
using System.Linq;
using System.Collections.Generic;
using RealityProgrammer.CSStandard.Interpreter.Exceptions;
using RealityProgrammer.CSStandard.Interpreter.Expressions;
using RealityProgrammer.CSStandard.Utilities;
using System;
using UnityEngine;

namespace RealityProgrammer.CSStandard.Interpreter {
    public class EquationCalculateInterpreter {
        private class Scanner {
            private static readonly Dictionary<string, TokenType> _keywords = new Dictionary<string, TokenType>() {
                { "true", TokenType.True },
                { "false", TokenType.False },
                { "and", TokenType.And },
                { "or", TokenType.Or },
            };

            private int start = 0;
            private int position = 0;

            public string SourceProgram { get; protected set; }
            public List<LexerToken> TokenContainer { get; protected set; } = new List<LexerToken>();

            public Scanner(string source) {
                SourceProgram = source;
            }

            public virtual void Scan() {
                start = 0;
                position = 0;

                while (!IsEnd()) {
                    start = position;
                    ScanToken();
                }

                TokenContainer.Add(new LexerToken(TokenType.EOF, null, string.Empty));
            }

            void ScanToken() {
                char c = Advance();

                switch (c) {
                    case '(': AddToken(TokenType.LeftParenthesis); break;
                    case ')': AddToken(TokenType.RightParenthesis); break;

                    case ',': AddToken(TokenType.Comma); break;
                    case '.': AddToken(TokenType.Dot); break;

                    case '+': AddToken(TokenType.Plus); break;
                    case '-': AddToken(TokenType.Minus); break;
                    case '*': AddToken(TokenType.Star); break;
                    case '/': AddToken(TokenType.Slash); break;
                    case '%': AddToken(TokenType.Percentage); break;

                    case '!': AddToken(Match('=') ? TokenType.BangEqual : TokenType.Bang); break;
                    case '=': AddToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal); break;
                    case '<':
                        if (Match('=')) {
                            AddToken(TokenType.LessEqual);
                        } else if (Match('<')) {
                            AddToken(TokenType.BitwiseLeftShift);
                        } else {
                            AddToken(TokenType.Less);
                        }
                        break;
                    case '>':
                        if (Match('=')) {
                            AddToken(TokenType.GreaterEqual);
                        } else if (Match('>')) {
                            AddToken(TokenType.BitwiseRightShift);
                        } else {
                            AddToken(TokenType.Greater);
                        }
                        break;

                    case '&':
                        if (Match('&')) {
                            AddToken(TokenType.And);
                        } else {
                            AddToken(TokenType.BitwiseAnd);
                        }
                        break;

                    case '|':
                        if (Match('|')) {
                            AddToken(TokenType.Or);
                        } else {
                            AddToken(TokenType.BitwiseOr);
                        }
                        break;

                    case '^':
                        AddToken(TokenType.BitwiseXor);
                        break;

                    case ' ':
                    case '\n':
                    case '\r':
                    case '\t':
                        break;

                    default:
                        if (char.IsDigit(c)) {
                            HandleNumbers();
                        } else if (IsAlpha(c)) {
                            while (IsAlphaNumeric(Peek())) Advance();

                            if (!_keywords.TryGetValue(SourceProgram.Substring(start, position - start), out var type)) {
                                type = TokenType.Identifier;
                            }

                            AddToken(type);
                        } else {
                            ThrowUnexpectedCharacter(c);
                        }
                        break;
                }
            }

            void ThrowUnexpectedCharacter(char c) {
                throw new ArgumentException($"Unexpected character '{c}' (Character code: {(short)c}) at position {position - 1}");
            }

            void HandleNumbers() {
                while (char.IsDigit(Peek())) Advance();

                bool isFloatingPoint = false;
                if (Peek() == '.') {
                    Advance();
                    isFloatingPoint = true;

                    while (char.IsDigit(Peek())) Advance();
                }

                if (isFloatingPoint) {
                    AddNumberToken(double.Parse(SourceProgram.Substring(start, position - start)));
                } else {
                    AddNumberToken(long.Parse(SourceProgram.Substring(start, position - start)));
                }
            }

            private bool IsEnd() {
                return position >= SourceProgram.Length;
            }

            private char Advance() {
                return SourceProgram[position++];
            }

            private bool Match(char c) {
                if (IsEnd()) return false;
                if (SourceProgram[position] != c) return false;

                position++;
                return true;
            }

            private char Peek() {
                if (IsEnd()) return '\0';

                return SourceProgram[position];
            }

            private bool IsAlpha(char c) {
                return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';
            }

            private bool IsAlphaNumeric(char c) {
                return IsAlpha(c) || char.IsNumber(c);
            }

            void AddNumberToken(object literal) {
                AddToken(TokenType.Number, literal);
            }

            private void AddToken(TokenType type) {
                AddToken(type, null);
            }

            private void AddToken(TokenType type, object literal) {
                TokenContainer.Add(new LexerToken(type, literal, SourceProgram.Substring(start, position - start)));
            }
        }
        private class Lexer {
            private List<LexerToken> input;

            private int current = 0;

            public void FeedTokens(List<LexerToken> input) {
                this.input = input;
            }

            public BaseExpression StartLexing() {
                current = 0;

                return Expression();
            }

            private BaseExpression Expression() {
                return ConditionalOr();
            }

            private BaseExpression ConditionalOr() {
                var expr = ConditionalAnd();

                while (Match(TokenType.Or)) {
                    expr = new BinaryExpression(expr, Previous(), ConditionalAnd());
                }

                return expr;
            }

            private BaseExpression ConditionalAnd() {
                var expr = BitwiseOr();

                while (Match(TokenType.And)) {
                    expr = new BinaryExpression(expr, Previous(), BitwiseOr());
                }

                return expr;
            }

            private BaseExpression BitwiseOr() {
                var expr = BitwiseXor();

                while (Match(TokenType.BitwiseOr)) {
                    expr = new BinaryExpression(expr, Previous(), BitwiseXor());
                }

                return expr;
            }

            private BaseExpression BitwiseXor() {
                var expr = BitwiseAnd();

                while (Match(TokenType.BitwiseXor)) {
                    expr = new BinaryExpression(expr, Previous(), BitwiseAnd());
                }

                return expr;
            }

            private BaseExpression BitwiseAnd() {
                var expr = Equality();

                while (Match(TokenType.BitwiseAnd)) {
                    expr = new BinaryExpression(expr, Previous(), Equality());
                }

                return expr;
            }

            private BaseExpression Equality() {
                var expression = Comparision();

                while (Match(TokenType.EqualEqual, TokenType.BangEqual)) {
                    expression = new BinaryExpression(expression, Previous(), Comparision());
                }

                return expression;
            }

            private BaseExpression Comparision() {
                var expression = Term();

                while (Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual)) {
                    expression = new BinaryExpression(expression, Previous(), Term());
                }

                return expression;
            }

            private BaseExpression Term() {
                var expression = Factor();

                while (Match(TokenType.Minus, TokenType.Plus)) {
                    expression = new BinaryExpression(expression, Previous(), Factor());
                }

                return expression;
            }

            private BaseExpression Factor() {
                var expression = Unary();

                while (Match(TokenType.Star, TokenType.Slash, TokenType.Percentage)) {
                    expression = new BinaryExpression(expression, Previous(), Unary());
                }

                return expression;
            }

            private BaseExpression Unary() {
                if (Match(TokenType.Bang, TokenType.Minus)) {
                    return new UnaryExpression(Previous(), Unary());
                }

                return Call();
            }

            private BaseExpression Call() {
                var expr = Primary();

                while (true) {
                    if (Check(TokenType.LeftParenthesis)) {
                        var previous = Previous();
                        Advance();

                        expr = HandleCallExpression(previous);
                    } else {
                        break;
                    }
                }

                return expr;
            }

            private FunctionCallExpression HandleCallExpression(LexerToken name) {
                List<BaseExpression> parameters = CollectParameterExpressions();

                return new FunctionCallExpression(new IdentifierExpression(name), parameters);
            }

            private List<BaseExpression> CollectParameterExpressions() {
                List<BaseExpression> parameters = new List<BaseExpression>();

                if (!Check(TokenType.RightParenthesis)) {
                    do {
                        parameters.Add(Expression());
                    } while (Match(TokenType.Comma));
                }

                if (Check(TokenType.RightParenthesis)) {
                    Advance();
                } else {
                    throw new UnclosedParenthesesException("Expected close parenthesis ')' after arguments");
                }

                return parameters;
            }

            private BaseExpression Primary() {
                if (Match(TokenType.True)) return new LiteralExpression(1);
                if (Match(TokenType.False)) return new LiteralExpression(0);

                if (Match(TokenType.Number)) {
                    return new LiteralExpression(Previous().Literal);
                }

                if (Match(TokenType.Identifier)) {
                    return new VariableRetrieveExpression(Previous());
                }

                if (Match(TokenType.LeftParenthesis)) {
                    var expression = Expression();

                    if (!Check(TokenType.RightParenthesis)) {
                        throw new UnclosedParenthesesException("Expected a close parenthesis ')'.");
                    }

                    Advance();
                    return new GroupingExpression(expression);
                }

                throw new ArgumentException("Expect an exception at the end of tokens.");
            }

            private bool Match(params TokenType[] types) {
                foreach (TokenType type in types) {
                    if (Check(type)) {
                        Advance();
                        return true;
                    }
                }

                return false;
            }

            private bool Check(TokenType type) {
                if (IsAtEnd()) return false;
                return Peek().Type == type;
            }

            private LexerToken Advance() {
                if (!IsAtEnd()) current++;
                return Previous();
            }

            private bool IsAtEnd() {
                return Peek().Type == TokenType.EOF;
            }

            private LexerToken Peek() {
                return input[current];
            }

            private LexerToken Previous() {
                return input[current - 1];
            }
        }
        private class Interpreter : IInterpreter {
            public BaseExpression Expression { get; protected set; }

            public static readonly Dictionary<string, Func<double[], double>> BuiltinMethods = new Dictionary<string, Func<double[], double>>() {
                { "Sqrt", (p) => Math.Sqrt(p.FirstOrDefault()) },
                { "Abs", (p) => Math.Abs(p.FirstOrDefault()) },
                { "ln", (p) => Math.Log(p.FirstOrDefault()) },
                { "Floor", (p) => Math.Floor(p.FirstOrDefault()) },
                { "Ceiling", (p) => Math.Ceiling(p.FirstOrDefault()) },
                { "Round", (p) => Math.Round(p.FirstOrDefault()) },
                { "Pow", (p) => Math.Pow(p[0], p[1]) },

                { "Sin", (p) => Math.Sin(p.FirstOrDefault()) },
                { "Cos", (p) => Math.Cos(p.FirstOrDefault()) },
                { "Tan", (p) => Math.Tan(p.FirstOrDefault()) },
                { "Cot", (p) => 1 / Math.Tan(p.FirstOrDefault()) },

                { "Asin", (p) => Math.Asin(p.FirstOrDefault()) },
                { "Acos", (p) => Math.Acos(p.FirstOrDefault()) },
                { "Atan", (p) => Math.Atan(p.FirstOrDefault()) },
                { "Atan2", (p) => Math.Atan2(p.FirstOrDefault(), p.ElementAtOrDefault(1)) },

                { "Min", (p) => Math.Min(p.FirstOrDefault(), p.ElementAtOrDefault(1)) },
                { "Max", (p) => Math.Max(p.FirstOrDefault(), p.ElementAtOrDefault(1)) },
                { "Branch", (p) => p[0] == 1 ? p[1] : p[2] },
            };
            public Dictionary<string, Func<double[], double>> UserDefinedMethods { get; protected set; }
            public Dictionary<string, double> UserDefinedConstants { get; protected set; }

            public Dictionary<string, object> VariableDictionary { get; set; }
            public static Dictionary<string, double> BuiltInConstants { get; private set; } = new Dictionary<string, double>() {
                { "PI", Math.PI },
                { "pi", Math.PI },
                { "e", Math.E },
                { "E", Math.E },
                { "Sqrt2", 1.41421356237309504880168872420969807856967187537694807317667973799 },
                { "Sqrt3", 1.732050807568877293527446341505872366942805253810380628055806 },
                { "GoldenRatio", 1.6180339887498948482 },
                { "ConwaysConst", 0.5772156649015328606065120900824024310421 },
                { "Omega", 0.56714329040978387299996866221035554 },
                { "Deg2Rad", 0.01745329252 },
                { "Rad2Deg", 57.295779513 },
            };

            public Interpreter() {
                UserDefinedMethods = new Dictionary<string, Func<double[], double>>();
                VariableDictionary = new Dictionary<string, object>();
            }

            public void FeedExpression(BaseExpression expr) {
                Expression = expr;
            }

            public double Calculate() {
                return (double)Evaluate(Expression);
            }

            private object Evaluate(BaseExpression expr) {
                return expr.Evaluate(this);
            }

            public object EvaluateBinaryExpresison(BinaryExpression expression) {
                dynamic leftObj = Evaluate(expression.Left);
                dynamic rightObj = Evaluate(expression.Right);

                if (leftObj != null && rightObj != null) {
                    try {
                        switch (expression.Operator.Type) {
                            case TokenType.Plus:
                                return leftObj + rightObj;
                            case TokenType.Minus:
                                return leftObj - rightObj;
                            case TokenType.Star:
                                return leftObj * rightObj;
                            case TokenType.Slash:
                                return leftObj / rightObj;
                            case TokenType.Percentage:
                                return leftObj % rightObj;
                            case TokenType.Greater:
                                return leftObj > rightObj;
                            case TokenType.GreaterEqual:
                                return leftObj >= rightObj;
                            case TokenType.Less:
                                return leftObj < rightObj;
                            case TokenType.LessEqual:
                                return leftObj <= rightObj;
                            case TokenType.EqualEqual:
                                return leftObj == rightObj;
                            case TokenType.BangEqual:
                                return leftObj != rightObj;
                            default:
                                throw new BinaryOperatorNotExistsException("Binary Operator cannot be applied between 2 Operands", leftObj, expression.Operator.Lexeme, rightObj);
                        }
                    } catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) {
                        throw new BinaryOperatorNotExistsException("Binary Operator cannot be applied between 2 Operands", leftObj, expression.Operator.Lexeme, rightObj);
                    }
                } else {
                    switch (expression.Operator.Type) {
                        case TokenType.EqualEqual: return leftObj == rightObj;
                        case TokenType.BangEqual: return leftObj != rightObj;
                        default:
                            throw new BinaryOperatorNotExistsException("Binary Operator cannot be applied between 2 Operands", "null", expression.Operator.Lexeme, "null");
                    }
                }
            }

            public object EvaluateCallExpression(FunctionCallExpression expression) {
                string methodName = expression.MethodName.Name.Lexeme;

                if (BuiltinMethods.TryGetValue(methodName, out var func)) {
                    return func(ConvertExpressionToNumber(expression.Parameters));
                } else if (UserDefinedMethods.TryGetValue(methodName, out func)) {
                    return func(ConvertExpressionToNumber(expression.Parameters));
                }

                throw new Exception("Method " + methodName + " cannot be found or not provided yet.");
            }

            public double[] ConvertExpressionToNumber(List<BaseExpression> exprs) {
                double[] arr = new double[exprs.Count];

                for (int i = 0; i < exprs.Count; i++) {
                    arr[i] = ((IConvertible)Evaluate(exprs[i])).ToDouble(null);
                }

                return arr;
            }

            public object EvaluateGetExpression(GetExpression expression) {
                throw new NotImplementedException();
            }

            public object EvaluateGroupingExpression(GroupingExpression expression) {
                return Evaluate(expression.Expression);
            }

            public object EvaluateLiteralExpression(LiteralExpression expression) {
                return expression.Literal;
            }

            public object EvaluateLogicalExpression(LogicalExpression expression) {
                object left = Evaluate(expression.Left);
                long leftTruthy = IsTruthy(left);

                switch (expression.Operator.Type) {
                    case TokenType.Or:
                        if (leftTruthy == 1) {
                            return 1L;
                        } else {
                            return IsTruthy(Evaluate(expression.Right));
                        }

                    case TokenType.And:
                        return IsTruthy(left) * IsTruthy(Evaluate(expression.Right));
                }

                return false;
            }

            public object EvaluateTargetCallExpression(TargetFunctionCallExpression expression) {
                throw new NotImplementedException();
            }

            public object EvaluateUnaryExpression(UnaryExpression expression) {
                object value = Evaluate(expression.Expression);

                if (expression.Operator.Type == TokenType.Minus) {
                    switch (value) {
                        case long longValue: return -longValue;
                        case double doubleValue: return -doubleValue;
                        default:
                            throw new InterpreterErrorCodeException(InterpreterRuntimeErrorCode.OperandNotANumber);
                    }
                } else if (expression.Operator.Type == TokenType.Bang) {
                    return 1L - IsTruthy(value);
                }

                return null;
            }

            private long IsTruthy(object obj) {
                if (obj == null) return 0;

                return (long)Math.Sign(((IConvertible)obj).ToDouble(null));
            }

            public object EvaluateVariableRetrieveExpression(VariableRetrieveExpression expr) {
                if (BuiltInConstants.TryGetValue(expr.Name.Lexeme, out double constant)) {
                    return constant;
                }
                if (VariableDictionary.TryGetValue(expr.Name.Lexeme, out object value)) {
                    return ((IConvertible)value).ToDouble(null);
                }
                if (UserDefinedConstants.TryGetValue(expr.Name.Lexeme, out constant)) {
                    return constant;
                }

                throw new VariableNotExistsException(expr.Name);
            }
        }

        private Scanner ScannerInstance { get; set; }
        private Lexer LexerInstance { get; set; }
        private Interpreter InterpreterInstance { get; set; }

        public EquationCalculateInterpreter() {
            LexerInstance = new Lexer();
            InterpreterInstance = new Interpreter();
        }

        public EquationCalculateInterpreter(string equation) : base() {
            ScannerInstance = new Scanner(equation);
        }

        public void ChangeEquation(string equation) {
            ScannerInstance = new Scanner(equation);
        }

        public void ScanEquation() {
            if (ScannerInstance == null) {
                throw new NullReferenceException("Cannot scan equation as scanner instance is null, which caused by unprovided equation. Call ChangeEquation().");
            }

            ScannerInstance.Scan();
        }

        public BaseExpression InterpretingExpression { get; protected set; }
        public void InitializeLexing() {
            LexerInstance.FeedTokens(ScannerInstance.TokenContainer);
            InterpretingExpression = LexerInstance.StartLexing();
        }

        public void DefineMethod(string name, Func<double[], double> @delegate) {
            if (!Interpreter.BuiltinMethods.ContainsKey(name)) {
                throw new ArgumentException("User defined method with the name of \"" + name + "\" match with one of the built-in methods.");
            }

            if (InterpreterInstance.UserDefinedMethods.ContainsKey(name)) {
                throw new ArgumentException("User defined method \"" + name + "\" is already exists.");
            }

            InterpreterInstance.UserDefinedMethods.Add(name, @delegate);
        }

        public void ReplaceMethod(string name, Func<double[], double> @delegate) {
            if (!InterpreterInstance.UserDefinedMethods.ContainsKey(name)) {
                DefineMethod(name, @delegate);
                return;
            }

            InterpreterInstance.UserDefinedMethods[name] = @delegate;
        }

        public void DefineConstant(string name, double value) {
            if (!Interpreter.BuiltInConstants.ContainsKey(name)) {
                throw new ArgumentException("User defined constant with the name of \"" + name + "\" match with one of the built-in constants.");
            }

            if (InterpreterInstance.UserDefinedConstants.ContainsKey(name)) {
                throw new ArgumentException("User defined constant \"" + name + "\" is already exists.");
            }

            InterpreterInstance.UserDefinedConstants.Add(name, value);
        }

        public double GetConstant(string name) {
            if (!InterpreterInstance.UserDefinedConstants.ContainsKey(name)) {
                throw new ArgumentException("User defined constant \"" + name + "\" is not exists.");
            }

            return InterpreterInstance.UserDefinedConstants[name];
        }

        public void DefineParameter(string name, double value) {
            if (InterpreterInstance.VariableDictionary.ContainsKey(name)) {
                throw new ArgumentException("User defined parameter \"" + name + "\" is already exists.");
            }

            InterpreterInstance.VariableDictionary.Add(name, value);
        }

        public void ModifyParameter(string name, double newValue) {
            if (!InterpreterInstance.VariableDictionary.ContainsKey(name)) {
                DefineParameter(name, newValue);
                return;
            }

            InterpreterInstance.VariableDictionary[name] = newValue;
        }

        public double GetParameter(string name) {
            if (!InterpreterInstance.VariableDictionary.ContainsKey(name)) {
                throw new ArgumentException("User defined parameter \"" + name + "\" is not exists.");
            }

            return Convert.ToDouble(InterpreterInstance.VariableDictionary[name]);
        }

        public double Calculate() {
            InterpreterInstance.FeedExpression(InterpretingExpression);
            return InterpreterInstance.Calculate();
        }

        /// <summary>
        /// Fast code to calculate simple equations, only once, not recommended to calculate complicated equations (such as custom parameters and constants) or calculate same equation multiple times.
        /// </summary>
        /// <param name="equation">Equation with plain numbers, operators and groupings</param>
        /// <returns></returns>
        public static double Calculate(string equation) {
            EquationCalculateInterpreter calculator = new EquationCalculateInterpreter(equation);
            calculator.ScanEquation();
            calculator.InitializeLexing();

            return calculator.Calculate();
        }
    }
}