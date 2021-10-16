using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MiscellaneousUtilities
{
    public static List<T> RemoveAtIndices<T>(this List<T> list, List<int> indices) {
        var ordered = indices.Distinct().OrderBy(x => x).ToArray();

        if (ordered.Length != 0) {
            int indexToRemove = 0;
            int newIdx = 0;

            for (int originalIdx = 0; originalIdx < list.Count; originalIdx++) {
                if (indexToRemove < ordered.Length && ordered[indexToRemove] == originalIdx) {
                    indexToRemove++;
                } else {
                    list[newIdx++] = list[originalIdx];
                }
            }

            list.RemoveRange(newIdx, list.Count - newIdx);
        }

        return list;
    }

    public static List<T> RemoveAtIndices<T>(this List<T> list, int[] indices) {
        var ordered = indices.Distinct().OrderBy(x => x).ToArray();

        if (ordered.Length != 0) {
            int indexToRemove = 0;
            int newIdx = 0;

            for (int originalIdx = 0; originalIdx < list.Count; originalIdx++) {
                if (indexToRemove < ordered.Length && ordered[indexToRemove] == originalIdx) {
                    indexToRemove++;
                } else {
                    list[newIdx++] = list[originalIdx];
                }
            }

            list.RemoveRange(newIdx, list.Count - newIdx);
        }

        return list;
    }
}