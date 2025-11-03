using System.Collections.Generic;
using UnityEngine;

namespace Heathen.UnityPhysics
{
    [HelpURL("https://kb.heathen.group/unity/physics/ballistics/trick-shot-line")]
    [RequireComponent(typeof(LineRenderer))]
    [RequireComponent(typeof(TrickShot))]
    public class TrickShotLine : MonoBehaviour
    {
        [Tooltip("If true, prediction is calculated at Start.")]
        public bool runOnStart = true;

        [Tooltip("If true, prediction is recalculated every frame.")]
        public bool continuousRun = true;

        private TrickShot trickShot;
        private LineRenderer lineRenderer;
        private readonly List<Vector3> trajectory = new();

        private void Awake()
        {
            trickShot = GetComponent<TrickShot>();
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
        }

        private void Start()
        {
            if (runOnStart && !continuousRun)
                trickShot.Predict();

            UpdateLine();
        }

        private void LateUpdate()
        {
            if (continuousRun)
                trickShot.Predict();

            UpdateLine();
        }

        private void UpdateLine()
        {
            trajectory.Clear();

            foreach (var path in trickShot.prediction)
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
    }
}
