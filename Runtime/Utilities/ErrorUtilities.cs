using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RealityProgrammer.UnityToolkit.Core.Utility {
    public static class ErrorUtilities {
        private static readonly Dictionary<RPCoreErrorCode, string> _errorCodes = new Dictionary<RPCoreErrorCode, string>() {
            { RPCoreErrorCode.NoError, "No error." },
            { RPCoreErrorCode.UnknownErrorCode, "Unknown error thrown by deprecated/buggy extensions." },
            { RPCoreErrorCode.NotSupportedObjectType, "Object's Runtime Type are not supported by this extension." },
            { RPCoreErrorCode.ScanUnexpectedCharacter, "Unexpected scan character(s)." },
            { RPCoreErrorCode.ScanUnterminatedString, "Unterminated string detected while scanning." },
            { RPCoreErrorCode.ScanOverflowNumeric, "Overflow numerical value were detected while scanning." },
        };

        public static string FormatErrorCode(RPCoreErrorCode code) {
            if (_errorCodes.TryGetValue(code, out string error)) {
                return error;
            }

            return _errorCodes[RPCoreErrorCode.UnknownErrorCode] + ". Error code: " + code.ToString();
        }
    }
}