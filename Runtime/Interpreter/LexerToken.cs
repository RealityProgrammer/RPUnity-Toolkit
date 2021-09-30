using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealityProgrammer.CSStandard.Interpreter {
    public struct LexerToken {
        public TokenType Type { get; set; }
        public object Literal { get; set; }
        public string Lexeme { get; set; }

        public LexerToken(TokenType type, object literal, string lexeme) {
            Type = type;
            Literal = literal;
            Lexeme = lexeme;
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder(GetType().Name).Append("(").Append(Type);

            if (Literal != null) {
                sb.Append(", ").Append(Literal);
            }

            if (!string.IsNullOrEmpty(Lexeme)) {
                sb.Append(", \"").Append(Lexeme).Append("\"");
            }

            sb.Append(")");

            return sb.ToString();
        }
    }
}
