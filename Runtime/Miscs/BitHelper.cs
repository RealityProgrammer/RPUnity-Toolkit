using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// https://referencesource.microsoft.com/#system.core/system/Collections/Generic/BitHelper.cs
namespace RealityProgrammer.UnityToolkit.Core.Miscs {
    unsafe internal class BitHelper {   // should not be serialized

        private const byte MarkedBitFlag = 1;
        private const byte IntSize = 32;

        private int m_length;

        [System.Security.SecurityCritical]
        private int* m_arrayPtr;

        private int[] m_array;

        private bool useStackAlloc;

        [System.Security.SecurityCritical]
        internal BitHelper(int* bitArrayPtr, int length) {
            this.m_arrayPtr = bitArrayPtr;
            this.m_length = length;
            useStackAlloc = true;
        }

        internal BitHelper(int[] bitArray, int length) {
            this.m_array = bitArray;
            this.m_length = length;
        }

        [System.Security.SecuritySafeCritical]
        internal unsafe void MarkBit(int bitPosition) {
            if (useStackAlloc) {
                int bitArrayIndex = bitPosition / IntSize;
                if (bitArrayIndex < m_length && bitArrayIndex >= 0) {
                    m_arrayPtr[bitArrayIndex] |= (MarkedBitFlag << (bitPosition % IntSize));
                }
            } else {
                int bitArrayIndex = bitPosition / IntSize;
                if (bitArrayIndex < m_length && bitArrayIndex >= 0) {
                    m_array[bitArrayIndex] |= (MarkedBitFlag << (bitPosition % IntSize));
                }
            }
        }

        [System.Security.SecuritySafeCritical]
        internal unsafe bool IsMarked(int bitPosition) {
            if (useStackAlloc) {
                int bitArrayIndex = bitPosition / IntSize;
                if (bitArrayIndex < m_length && bitArrayIndex >= 0) {
                    return ((m_arrayPtr[bitArrayIndex] & (MarkedBitFlag << (bitPosition % IntSize))) != 0);
                }
                return false;
            } else {
                int bitArrayIndex = bitPosition / IntSize;
                if (bitArrayIndex < m_length && bitArrayIndex >= 0) {
                    return ((m_array[bitArrayIndex] & (MarkedBitFlag << (bitPosition % IntSize))) != 0);
                }
                return false;
            }
        }

        internal static int ToIntArrayLength(int n) {
            return n > 0 ? ((n - 1) / IntSize + 1) : 0;
        }
    }
}