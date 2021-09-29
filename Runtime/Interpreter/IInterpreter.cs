using RealityProgrammer.CSStandard.Interpreter.Expressions;

namespace RealityProgrammer.CSStandard.Interpreter {
    public interface IInterpreter {
        object EvaluateLiteralExpression(LiteralExpression expression);
        object EvaluateUnaryExpression(UnaryExpression expression);
        object EvaluateBinaryExpresison(BinaryExpression expression);
        object EvaluateGroupingExpression(GroupingExpression expression);
        object EvaluateLogicalExpression(LogicalExpression expression);
        object EvaluateGetExpression(GetExpression expression);
        object EvaluateCallExpression(FunctionCallExpression expression);
    }
}
