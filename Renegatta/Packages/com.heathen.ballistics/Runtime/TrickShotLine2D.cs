using System.Collections.Generic;
using UnityEngine;

namespace Heathen.UnityPhysics
{
    [HelpURL("https://kb.heathen.group/unity/physics/ballistics/trick-shot-line")]
    [RequireComponent(typeof(LineRenderer))]
    [RequireComponent(typeof(TrickShot2D))]
    public class TrickShotLine2D : MonoBehaviour
    {
        [Tooltip("If true, prediction is calculated at Start.")]
        public bool runOnStart = true;

        [Tooltip("If true, prediction is recalculated every frame.")]
        public bool continuousRun = true;

        private TrickShot2D trickShot2D;
        private LineRenderer lineRenderer;
        private readonly List<Vector3> trajectory = new();

        private void Awake()
        {
            trickShot2D = GetComponent<TrickShot2D>();
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
        }

        private void Start()
        {
            if (runOnStart && !continuousRun)
                trickShot2D.Predict();

            UpdateLine();
        }

        private void LateUpdate()
        {
            if (continuousRun)
                trickShot2D.Predict();

            UpdateLine();
        }

        private void UpdateLine()
        {
            trajectory.Clear();

            foreach (var path in trickShot2D.prediction)
            {
                if (path.steps == null || path.steps.Length == 0)
                    continue;

                foreach (var step in path.steps)
                    trajectory.Add(step.position);
            }

            lineRenderer.positionCount = trajectory.Count;
            if (trajectory.Count > 0)
                lineRenderer.SetPositions(trajectory.ToArray());
        }

#if UNITY_EDITOR
        [ContextMenu("Draw Line")]
        private void EditorUpdateLine()
        {
            if (trickShot2D == null)
                trickShot2D = GetComponent<TrickShot2D>();

            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();

            trickShot2D.Predict();
            UpdateLine();
        }
#endif
    }
}
