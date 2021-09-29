using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using RealityProgrammer.CSStandard.Interpreter.Expressions;
using RealityProgrammer.CSStandard.Utilities;
using RealityProgrammer.CSStandard.Interpreter.Exceptions;

namespace RealityProgrammer.CSStandard.Interpreter {
    public class ConditionalSearchInterpreter {
        public class ExclusiveIdentifierExpression : BaseExpression {
            public LexerToken Name { get; protected set; }

            public ExclusiveIdentifierExpression(LexerToken name) {
                Name = name;
            }

            public override string ToString() {
                return "ExclusiveIdentifierExpression(" + Name + ")";
            }

            public override object Evaluate(IInterpreter interpreter) {
                if (interpreter is Interpreter excIntr) {
                    switch (Name.Type) {
                        case TokenType.Iterator:
                            return excIntr.IteratingTarget;

                        case TokenType.IteratorIndex:
                            return excIntr.IteratingIndex;

                        case TokenType.This:
                            return excIntr.NativeObject;
                    }
                }

                throw new ArgumentException("ExclusiveIdentifierExpression can only be evaluated by " + typeof(Interpreter).FullName);
            }
        }
        public class Scanner {
            private static readonly Dictionary<string, TokenType> _keywords = new Dictionary<string, TokenType>() {
                { "true", TokenType.True },
                { "false", TokenType.False },
                { "null", TokenType.Null },
                { "and", TokenType.And },
                { "or", TokenType.Or },
                { "__iterator__", TokenType.Iterator },
                { "__iteratorIndex__", TokenType.IteratorIndex },
                { "__this__", TokenType.This },
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

                AddToken(TokenType.EOP);
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

                    case '!': AddToken(Match('=') ? TokenType.BangEqual : TokenType.Bang); break;
                    case '=': AddToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal); break;
                    case '<': AddToken(Match('=') ? TokenType.LessEqual : TokenType.Less); break;
                    case '>': AddToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater); break;

                    case '&':
                        if (Match('&')) {
                            AddToken(TokenType.And);
                            break;
                        }
                        ThrowUnexpectedCharacter('&');
                        break;

                    case '|':
                        if (Match('|')) {
                            AddToken(TokenType.Or);
                            break;
                        }
                        throw new ArgumentException($"Unexpected character '{c}' (Character code: {(short)c}) at position {position - 1}");

                    case ' ':
                    case '\n':
                    case '\r':
                    case '\t':
                        break;

                    case '"':
                        while (Peek() != '"' && !IsEnd()) {
                            Advance();
                        }

                        if (IsEnd()) {
                            throw new UnterminatedStringException($"Unterminated string at position " + start + " to " + (position - 1) + ".");
                        }

                        Advance();
                        AddToken(TokenType.String, SourceProgram.Substring(start + 1, position - start - 2));

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

                    if (!isFloatingPoint) {
                        isFloatingPoint = true;
                    } else {
                        throw new Exception("Multiple decimal seperator symbol");
                    }

                    while (char.IsDigit(Peek())) Advance();
                }

                string digits = SourceProgram.Substring(start, position - start);
                if (isFloatingPoint) {
                    char peek = Peek();

                    switch (peek) {
                        case 'u':
                        case 'U':
                        case 'l':
                        case 'L':
                            throw new InvalidNumericLiteralException("Integer literals cannot be applied to floating point.");

                        case 'f':
                            AddToken(TokenType.Number, float.Parse(digits));
                            Advance();
                            break;

                        case 'd':
                            AddToken(TokenType.Number, double.Parse(digits));
                            Advance();
                            break;

                        default:
                            AddToken(TokenType.Number, double.Parse(digits));
                            break;

                        case 'm':
                            AddToken(TokenType.Number, decimal.Parse(digits));
                            Advance();
                            break;
                    }
                } else {
                    bool isLong = false, isUnsigned = false;

                    bool isChecking = true;
                    while (isChecking) {
                        char peek = Peek();
                        switch (peek) {
                            case 'u':
                            case 'U':
                                if (!isUnsigned) {
                                    isUnsigned = true;
                                    Advance();
                                } else {
                                    ThrowUnexpectedCharacter(peek);
                                }
                                break;

                            case 'l':
                            case 'L':
                                if (!isLong) {
                                    isLong = true;
                                    Advance();
                                } else {
                                    ThrowUnexpectedCharacter(peek);
                                }
                                break;

                            case 'f':
                            case 'd':
                            case 'm':
                                throw new InvalidNumericLiteralException("Floating point number require a decimal seperator '.' somewhere or at the end.");

                            default:
                                isChecking = false;
                                break;
                        }
                    }

                    if (isUnsigned) {
                        if (isLong) {
                            AddNumberToken(ulong.Parse(digits));
                        } else {
                            AddNumberToken(uint.Parse(digits));
                        }
                    } else {
                        if (isLong) {
                            AddNumberToken(long.Parse(digits));
                        } else {
                            AddNumberToken(int.Parse(digits));
                        }
                    }
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

            private char PeekNext() {
                if (position + 1 >= SourceProgram.Length) return '\0';

                return SourceProgram[position + 1];
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
        public class Lexer {
            private readonly List<LexerToken> input;

            private int current = 0;

            public Lexer(List<LexerToken> input) {
                this.input = input;
            }

            public BaseExpression StartLexing() {
                current = 0;

                return Expression();
            }

            private BaseExpression Expression() {
                return Or();
            }

            private BaseExpression Or() {
                var expression = And();

                while (Match(TokenType.Or)) {
                    expression = new LogicalExpression(expression, Previous(), And());
                }

                return expression;
            }

            private BaseExpression And() {
                var expression = Equality();

                while (Match(TokenType.And)) {
                    expression = new LogicalExpression(expression, Previous(), Equality());
                }

                return expression;
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

                while (Match(TokenType.Star, TokenType.Slash)) {
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

                        expr = HandleCallExpression(expr, previous);
                    } else if (Match(TokenType.Dot)) {
                        LexerToken name;

                        if (Check(TokenType.Identifier)) {
                            name = Advance();
                        } else {
                            throw new Exception("Expected identifier after member access '.'");
                        }

                        if (Check(TokenType.LeftParenthesis)) {
                            var previous = Previous();
                            Advance();

                            List<BaseExpression> parameters = CollectParameterExpressions();
                            expr = new FunctionCallExpression(expr, new IdentifierExpression(previous), parameters);
                            Advance();
                        } else {
                            expr = new GetExpression(expr, new IdentifierExpression(name));
                        }
                    } else {
                        break;
                    }
                }

                return expr;
            }

            private FunctionCallExpression HandleCallExpression(BaseExpression expr, LexerToken name) {
                List<BaseExpression> parameters = CollectParameterExpressions();

                return new FunctionCallExpression(expr, new IdentifierExpression(name), parameters);
            }

            private List<BaseExpression> CollectParameterExpressions() {
                List<BaseExpression> parameters = new List<BaseExpression>();

                if (!Check(TokenType.RightParenthesis)) {
                    do {
                        parameters.Add(Expression());
                    } while (Match(TokenType.Comma));
                }

                return parameters;
            }

            private BaseExpression Primary() {
                if (Match(TokenType.True)) return new LiteralExpression(true);
                if (Match(TokenType.False)) return new LiteralExpression(false);
                if (Match(TokenType.Null)) return new LiteralExpression(null);

                if (Match(TokenType.Number, TokenType.String)) {
                    return new LiteralExpression(Previous().Literal);
                }

                if (Match(TokenType.Iterator) || Match(TokenType.IteratorIndex) || Match(TokenType.This)) {
                    return new ExclusiveIdentifierExpression(Previous());
                }
                if (Match(TokenType.Identifier)) {
                    return new IdentifierExpression(Previous());
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
                return Peek().Type == TokenType.EOP;
            }

            private LexerToken Peek() {
                return input[current];
            }

            private LexerToken Previous() {
                return input[current - 1];
            }
        }
        public class Interpreter : IInterpreter {
            public BaseExpression Expression { get; protected set; }
            public object NativeObject { get; protected set; }

            private Dictionary<string, MethodInfo> _cachedMethods;
            private Dictionary<string, FieldInfo> _cachedFields;
            private Dictionary<string, PropertyInfo> _cachedProperties;

            public Interpreter() {
                _cachedMethods = new Dictionary<string, MethodInfo>();
                _cachedFields = new Dictionary<string, FieldInfo>();
                _cachedProperties = new Dictionary<string, PropertyInfo>();
            }

            public Interpreter(BaseExpression expression, object native) : base() {
                Expression = expression;
                NativeObject = native;
            }

            public void ModifyExpression(BaseExpression newExpr) {
                Expression = newExpr;
            }

            public void ModifyNativeObject(object native) {
                NativeObject = native;
            }

            public int IteratingIndex;
            public object IteratingTarget;
            public List<T> Filter<T>(List<T> original) {
                List<T> ret = new List<T>();

                for (int i = 0; i < original.Count; i++) {
                    IteratingIndex = i;
                    IteratingTarget = original[i];

                    object output = Evaluate(Expression);

                    if (output is bool boolean) {
                        if (boolean) {
                            ret.Add(original[i]);
                        }
                    } else {
                        throw new ArgumentException("The output of program is not boolean type. Index: " + i + ". Expected return value: Boolean. Returned: " + output);
                    }
                }

                return ret;
            }

            public bool CheckQualify<T>(T input, int index) {
                IteratingTarget = input;
                IteratingIndex = index;

                object output = Evaluate(Expression);

                if (output is bool boolean) {
                    return boolean;
                } else {
                    throw new ArgumentException("The output of program is not boolean type. Expected return value: Boolean. Returned: " + output);
                }
            }

            public object Interpret() {
                return Evaluate(Expression);
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

            public object EvaluateGroupingExpression(GroupingExpression expression) {
                return Evaluate(expression.Expression);
            }

            public object EvaluateLiteralExpression(LiteralExpression expression) {
                return expression.Literal;
            }

            public object EvaluateUnaryExpression(UnaryExpression expression) {
                object value = Evaluate(expression.Expression);

                if (expression.Operator.Type == TokenType.Minus) {
                    switch (value) {
                        case int intValue: return -intValue;
                        case uint uintValue: return -uintValue;
                        case char charValue: return -charValue;
                        case long longValue: return -longValue;
                        case decimal decimalValue: return -decimalValue;
                        case double doubleValue: return -doubleValue;
                        case float floatValue: return -floatValue;
                        case short shortValue: return -shortValue;
                        case ushort ushortValue: return -ushortValue;
                        case byte byteValue: return -byteValue;
                        case sbyte sbyteValue: return -sbyteValue;
                        default:
                            if (value != null) {
                                var method = value.GetType().GetMethod("op_UnaryNegation", BindingFlags.Public | BindingFlags.Static);

                                if (method != null) {
                                    return method.Invoke(null, new object[1] { value });
                                }
                            }

                            throw new InterpreterErrorCodeException(InterpreterRuntimeErrorCode.InvalidMinusUnaryOperator);
                    }
                } else if (expression.Operator.Type == TokenType.Bang) {
                    return !IsTruthy(value);
                }

                return null;
            }

            private bool IsTruthy(object obj) {
                if (obj == null) return false;
                if (obj is bool b) return b;
                
                var method = obj.GetType().GetMethod("op_LogicalNot", BindingFlags.Public | BindingFlags.Static);
                if (method != null) {
                    return (bool)method.Invoke(null, new object[1] { obj });
                }

                throw new InterpreterErrorCodeException(InterpreterRuntimeErrorCode.InvalidLogicalNotOperator);
            }

            public object Evaluate(BaseExpression expression) {
                return expression.Evaluate(this);
            }

            public object EvaluateLogicalExpression(LogicalExpression expression) {
                object left = Evaluate(expression.Left);

                switch (expression.Operator.Type) {
                    case TokenType.Or:
                        return IsTruthy(left) || IsTruthy(Evaluate(expression.Right));

                    case TokenType.And:
                        return IsTruthy(left) && IsTruthy(Evaluate(expression.Right));
                }

                return false;
            }

            public object EvaluateGetExpression(GetExpression expression) {
                var obj = Evaluate(expression.Expression);

                if (obj != null) {
                    Type type = obj.GetType();

                    string name = expression.Variable.Name.Lexeme;
                    if (_cachedFields.TryGetValue(name, out FieldInfo fieldInfo)) {
                        return fieldInfo.GetValue(obj);
                    } else {
                        if (_cachedProperties.TryGetValue(name, out PropertyInfo propertyInfo)) {
                            return propertyInfo.GetValue(obj);
                        }
                    }

                    fieldInfo = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);

                    if (fieldInfo != null) {
                        _cachedFields.Add(name, fieldInfo);
                        return fieldInfo.GetValue(obj);
                    } else {
                        var propertyInfo = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);

                        if (propertyInfo != null) {
                            _cachedProperties.Add(name, propertyInfo);
                            return propertyInfo.GetValue(obj);
                        } else {
                            throw new Exception("Undefined property \"" + name + "\" of object type " + type.FullName);
                        }
                    }
                }

                throw new NullReferenceException("Trying to interpret property " + expression.Variable.Name.Lexeme + " of null object");
            }

            public object EvaluateCallExpression(FunctionCallExpression expression) {
                object evaluate = Evaluate(expression.Target);

                if (evaluate != null) {
                    object[] parameters = new object[expression.Parameters.Count];

                    for (int i = 0; i < parameters.Length; i++) {
                        parameters[i] = Evaluate(expression.Parameters[i]);
                    }

                    string name = expression.MethodName.Name.Lexeme;

                    if (_cachedMethods.TryGetValue(name, out MethodInfo methodInfo)) {
                        return methodInfo.Invoke(evaluate, parameters);
                    } else {
                        methodInfo = evaluate.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, parameters.Select(x => x.GetType()).ToArray(), null);

                        if (methodInfo != null) {
                            if (methodInfo.ReturnType == typeof(void)) {
                                throw new ArgumentException("Trying to evaluate method \"" + name + "\" which has no return value");
                            }

                            _cachedMethods.Add(name, methodInfo);
                            return methodInfo.Invoke(evaluate, parameters);
                        }

                        throw new NullReferenceException("Trying to call instance method \"" + name + "\" which doesn't exists.");
                    }
                }

                throw new NullReferenceException("Trying to call instance method \"" + expression.MethodName.Name.Lexeme + "\" on null object");
            }
        }

        public Scanner ScannerInstance { get; protected set; }
        public Lexer LexerInstance { get; protected set; }
        public Interpreter InterpreterInstance { get; protected set; }

        public ConditionalSearchInterpreter() {
            InterpreterInstance = new Interpreter();
        }

        public void Clear() {
            InterpretExpression = null;
        }

        public void InitializeProgram(string program) {
            ScannerInstance = new Scanner(program);
            ScannerInstance.Scan();

            LexerInstance = new Lexer(ScannerInstance.TokenContainer);
        }

        public BaseExpression backfield_Expression;
        public BaseExpression InterpretExpression {
            get => backfield_Expression;
            set {
                backfield_Expression = value;
            }
        }
        public void Lexing() {
            try {
                InterpretExpression = LexerInstance.StartLexing();
            } catch (Exception e) {
                InterpretExpression = null;
                throw e;
            }
        }

        public List<T> InterpretFilter<T>(List<T> input, object native) {
            InterpreterInstance.ModifyExpression(InterpretExpression);
            InterpreterInstance.ModifyNativeObject(native);

            return InterpreterInstance.Filter(input);
        }

        public bool CheckQualify<T>(T input, int index, object native) {
            InterpreterInstance.ModifyExpression(InterpretExpression);
            InterpreterInstance.ModifyNativeObject(native);

            return InterpreterInstance.CheckQualify(input, index);
        }

        private static readonly Type voidType = typeof(void);
        public Type GetOutputType {
            get {
                if (InterpretExpression == null) return voidType;

                object output = InterpreterInstance.Evaluate(InterpretExpression);

                if (output == null) return voidType;

                return output.GetType();
            }
        }

        public bool IsValid {
            get {
                return InterpretExpression != null;
            }
        }
    }
}