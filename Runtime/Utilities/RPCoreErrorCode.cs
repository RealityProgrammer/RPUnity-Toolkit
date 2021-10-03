namespace RealityProgrammer.UnityToolkit.Core.Utility {
    public enum RPCoreErrorCode : uint {
        NoError, UnknownErrorCode,

        NotSupportedObjectType,
        ScanUnexpectedCharacter,
        ScanUnterminatedString,
        ScanOverflowNumeric,
    }
}