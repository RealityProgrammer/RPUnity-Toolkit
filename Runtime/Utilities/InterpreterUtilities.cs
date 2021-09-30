using System.Text;
using RealityProgrammer.CSStandard.Interpreter.Expressions;

namespace RealityProgrammer.CSStandard.Utilities {
    public static class InterpreterUtilities {
        public const string TreeVerticalBranch = "║  ";
        public const string TreeLastBranch = "╚══";
        public const string TreeNonLastBranch = "╠══";

        public static string PrintExpressionTree(BaseExpression expr) {
            StringBuilder sb = new StringBuilder();
            ShowExpressionTree(expr, sb);

            return sb.ToString();
        }

        public static void ShowExpressionTree(BaseExpression node, StringBuilder output, string indent = "", bool isLast = true) {
            var marker = isLast ? TreeLastBranch : TreeNonLastBranch;

            output.Append(indent);
            output.Append(marker);

            switch (node) {
                case LiteralExpression literal:
                    output.Append(literal);
                    output.AppendLine();
                    break;

                case UnaryExpression unary:
                    output.AppendLine(unary.GetType().Name);
                    indent += isLast ? "   " : TreeVerticalBranch;

                    output.Append(indent);
                    output.Append(TreeNonLastBranch);
                    output.AppendLine("Operator:" + unary.Operator.Type);
                    ShowExpressionTree(unary.Expression, output, indent, true);
                    break;

                case BinaryExpression binary:
                    output.AppendLine(binary.GetType().Name);
                    indent += isLast ? "   " : TreeVerticalBranch;
                    ShowExpressionTree(binary.Left, output, indent, false);
                    output.Append(indent);
                    output.Append(TreeNonLastBranch);
                    output.AppendLine("Operator:" + binary.Operator.Type);

                    ShowExpressionTree(binary.Right, output, indent, true);
                    break;

                case LogicalExpression logical:
                    output.AppendLine(logical.GetType().Name);
                    indent += isLast ? "   " : TreeVerticalBranch;
                    ShowExpressionTree(logical.Left, output, indent, false);
                    output.Append(indent);
                    output.Append(TreeNonLastBranch);
                    output.AppendLine("Operator:" + logical.Operator.Type);

                    ShowExpressionTree(logical.Right, output, indent, true);
                    break;

                case GroupingExpression group:
                    output.AppendLine(group.GetType().Name);
                    indent += isLast ? "   " : TreeVerticalBranch;
                    ShowExpressionTree(group.Expression, output, indent, true);
                    break;

                case GetExpression get:
                    output.AppendLine(get.GetType().Name);
                    indent += isLast ? "   " : TreeVerticalBranch;
                    ShowExpressionTree(get.Expression, output, indent, false);
                    ShowExpressionTree(get.Variable, output, indent, true);
                    break;

                case IdentifierExpression identifier:
                    output.AppendLine(identifier.GetType().Name);
                    indent += isLast ? "   " : TreeVerticalBranch;
                    output.Append(indent);
                    output.Append(TreeLastBranch);
                    output.AppendLine("Name: " + identifier.Name.Lexeme);
                    break;

                case FunctionCallExpression call:
                    output.AppendLine(call.GetType().Name);
                    indent += isLast ? "   " : TreeVerticalBranch;

                    if (call.Parameters.Count == 0) {
                        ShowExpressionTree(call.MethodName, output, indent, true);
                    } else {
                        ShowExpressionTree(call.MethodName, output, indent, false);

                        output.Append(indent);
                        output.Append(TreeLastBranch);
                        output.AppendLine("Parameters (" + call.Parameters.Count + "):");

                        indent += "   ";
                        for (int i = 0; i < call.Parameters.Count; i++) {
                            ShowExpressionTree(call.Parameters[i], output, indent, i == call.Parameters.Count - 1);
                        }
                    }
                    break;

                case TargetFunctionCallExpression targetCall:
                    output.AppendLine(targetCall.GetType().Name);
                    indent += isLast ? "   " : TreeVerticalBranch;

                    if (targetCall.Parameters.Count == 0) {
                        ShowExpressionTree(targetCall.Target, output, indent, false);
                        ShowExpressionTree(targetCall.MethodName, output, indent, true);
                    } else {
                        ShowExpressionTree(targetCall.Target, output, indent, false);
                        ShowExpressionTree(targetCall.MethodName, output, indent, false);

                        output.Append(indent);
                        output.Append(TreeLastBranch);
                        output.AppendLine("Parameters (" + targetCall.Parameters.Count + "):");

                        indent += "   ";
                        for (int i = 0; i < targetCall.Parameters.Count; i++) {
                            ShowExpressionTree(targetCall.Parameters[i], output, indent, i == targetCall.Parameters.Count - 1);
                        }
                    }
                    break;

                case TargetExpression target:
                    output.AppendLine(target.GetType().Name);
                    indent += isLast ? "   " : TreeVerticalBranch;
                    output.Append(indent);
                    output.Append(TreeLastBranch);
                    output.AppendLine("Name: " + target.Token.Type);
                    break;

                case VariableRetrieveExpression varRet:
                    output.AppendLine(varRet.GetType().Name);
                    indent += isLast ? "   " : TreeVerticalBranch;

                    output.Append(indent);
                    output.Append(TreeLastBranch);
                    output.AppendLine("Name: " + varRet.Name.Lexeme);
                    break;

                case Interpreter.ConditionalSearchInterpreter.ExclusiveIdentifierExpression excIdentifier:
                    output.AppendLine(excIdentifier.GetType().Name);
                    indent += isLast ? "   " : TreeVerticalBranch;
                    output.Append(indent);
                    output.Append(TreeLastBranch);
                    output.AppendLine("Name: " + excIdentifier.Name);
                    break;
            }
        }
    }
}
