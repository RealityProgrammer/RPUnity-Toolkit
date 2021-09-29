using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealityProgrammer.CSStandard.Interpreter {
    public class LexerToken {
        public TokenType Type { get; protected set; }
        public object Literal { get; protected set; }
        public string Lexeme { get; protected set; }

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
                sb.Append(", ").Append(Lexeme);
            }

            sb.Append(")");

            return sb.ToString();
        }
    }
}
