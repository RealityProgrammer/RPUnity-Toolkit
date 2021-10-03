namespace RealityProgrammer.CSStandard.Interpreter.Exceptions {
    public enum InterpreterRuntimeErrorCode {
        NoError,
        UndefinedErrorCode,

        OperandNotANumber,
        InvalidMinusUnaryOperator,
        InvalidLogicalNotOperator,
    }
}
