using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealityProgrammer.CSStandard.Interpreter {
    public enum TokenType {
        LeftParenthesis, RightParenthesis, Dot, Comma, Greater, Less, Equal, Bang,
        Plus, Minus, Star, Slash, Question,
        
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
