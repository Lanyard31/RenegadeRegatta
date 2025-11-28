using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class EditorFrameLimiter {
    static EditorFrameLimiter() {
        Application.targetFrameRate = 30; // change this to whatever keeps your laptop from screaming
    }
}
