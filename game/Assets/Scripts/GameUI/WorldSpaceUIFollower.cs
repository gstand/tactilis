using UnityEngine;

namespace GameUI
{
    /// <summary>
    /// </summary>
    public class WorldSpaceUIFollower : MonoBehaviour
    {
        [Header("Follow Settings")]
        [SerializeField] private Transform targetCamera;
        [SerializeField] private float followDistance = 2f;
        [SerializeField] private float heightOffset = 0f;
        [SerializeField] private float smoothSpeed = 5f;

        [Header("Behavior")]
        [SerializeField] private bool followPosition = true;
        [SerializeField] private bool followRotation = true;
        [SerializeField] private bool lockYPosition = false;
        [SerializeField] private float yPosition = 1.5f;

        [Header("Lazy Follow (Recommended for MR)")]
        [SerializeField] private bool useLazyFollow = true;
        [SerializeField] private float lazyFollowThreshold = 0.3f;
        [SerializeField] private float maxDistance = 3f;

        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private bool isFollowing = true;

        private void Start()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main?.transform;
            }

            if (targetCamera != null)
            {
                UpdateTargetTransform();
                transform.position = targetPosition;
                transform.rotation = targetRotation;
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null || !isFollowing) return;

            UpdateTargetTransform();

            if (useLazyFollow)
            {
                LazyFollow();
            }
            else
            {
                DirectFollow();
            }
        }

        private void UpdateTargetTransform()
        {
            Vector3 forward = targetCamera.forward;
            forward.y = 0;
            forward.Normalize();

            targetPosition = targetCamera.position + forward * followDistance;
            targetPosition.y = lockYPosition ? yPosition : targetCamera.position.y + heightOffset;

            targetRotation = Quaternion.LookRotation(targetPosition - targetCamera.position);
        }

        private void DirectFollow()
        {
            if (followPosition)
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
            }

            if (followRotation)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
            }
        }

        private void LazyFollow()
        {
            float distance = Vector3.Distance(transform.position, targetPosition);

            if (distance > lazyFollowThreshold)
            {
                float t = Mathf.Clamp01((distance - lazyFollowThreshold) / (maxDistance - lazyFollowThreshold));
                float speed = Mathf.Lerp(0, smoothSpeed, t);

                if (followPosition)
                {
                    transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * speed);
                }
            }

            if (followRotation)
            {
                Vector3 lookDirection = targetCamera.position - transform.position;
                lookDirection.y = 0;
                if (lookDirection.sqrMagnitude > 0.001f)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(-lookDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * smoothSpeed);
                }
            }
        }

        /// <summary>
        /// </summary>
        public void SetFollowing(bool follow)
        {
            isFollowing = follow;
        }

        /// <summary>
        /// </summary>
        public void SnapToTarget()
        {
            if (targetCamera != null)
            {
                UpdateTargetTransform();
                transform.position = targetPosition;
                transform.rotation = targetRotation;
            }
        }

        /// <summary>
        /// </summary>
        public void SetFollowDistance(float distance)
        {
            followDistance = distance;
        }
    }
}
