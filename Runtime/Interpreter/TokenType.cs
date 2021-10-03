namespace RealityProgrammer.CSStandard.Interpreter {
    public enum TokenType {
        LeftParenthesis, RightParenthesis, Dot, Comma, Greater, Less, Equal, Bang,
        Plus, Minus, Star, Slash, Question, Percentage,
        
        BitwiseAnd, BitwiseOr, BitwiseXor, BitwiseLeftShift, BitwiseRightShift, BitwiseComplement,
        BitwiseAndEqual, BitwiseOrEqual, BitwiseXorEqual, BitwiseLeftShiftEqual, BitwiseRightShiftEqual, BitwiseComplementEqual,

        GreaterEqual, LessEqual, EqualEqual, BangEqual,

        Identifier, Number, String,

        True, False, Null, And, Or,

        This,

        EOF,

        // Exclusive Keywords
        Iterator, IteratorIndex,
    }
}
