using RealityProgrammer.CSStandard.Interpreter.Expressions;
using System.Collections.Generic;

namespace RealityProgrammer.CSStandard.Interpreter {
    public interface IInterpreter {
        Dictionary<string, object> VariableDictionary { get; set; }

        object EvaluateVariableRetrieveExpression(VariableRetrieveExpression expr);
        object EvaluateLiteralExpression(LiteralExpression expr);
        object EvaluateUnaryExpression(UnaryExpression expr);
        object EvaluateBinaryExpresison(BinaryExpression expr);
        object EvaluateGroupingExpression(GroupingExpression expr);
        object EvaluateLogicalExpression(LogicalExpression expr);
        object EvaluateGetExpression(GetExpression expr);
        object EvaluateCallExpression(FunctionCallExpression expr);
        object EvaluateTargetCallExpression(TargetFunctionCallExpression expr);
    }
}
