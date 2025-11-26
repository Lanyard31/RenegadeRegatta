using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class EditorFrameLimiter {
    static EditorFrameLimiter() {
        Application.targetFrameRate = 60; // change this to whatever keeps your laptop from screaming
    }
}
