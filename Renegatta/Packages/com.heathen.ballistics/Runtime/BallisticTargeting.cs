using UnityEngine;

namespace Heathen.UnityPhysics
{
    [HelpURL("https://kb.heathen.group/unity/physics/ballistics/ballistic-targeting")]
    [RequireComponent(typeof(BallisticAim))]
    public class BallisticTargeting : MonoBehaviour
    {
        [Tooltip("The target to aim at")]
        public Transform targetTransform;

        /// <summary>
        /// True if a ballistic solution to the target exists.
        /// </summary>
        public bool HasSolution { get; private set; }

        private BallisticAim ballisticAim;

        private void Awake()
        {
            ballisticAim = GetComponent<BallisticAim>();
        }

        private void LateUpdate()
        {
            if (targetTransform && ballisticAim != null)
            {
                HasSolution = ballisticAim.Aim(targetTransform.position);
            }
            else
            {
                HasSolution = false;
            }
        }

        /// <summary>
        /// Updates the current target.
        /// </summary>
        /// <param name="newTarget">The new transform to target.</param>
        public void SetTarget(Transform newTarget)
        {
            targetTransform = newTarget;
        }
    }
}
