using UnityEngine;

namespace QuestDemonMR
{
    public sealed class BillboardToHead : MonoBehaviour
    {
        private Transform _head;

        public void Initialize(Transform head) => _head = head;

        private void LateUpdate()
        {
            if (_head == null && Camera.main != null)
            {
                _head = Camera.main.transform;
            }
            if (_head == null)
            {
                return;
            }

            var flatDirection = _head.position - transform.position;
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(-flatDirection.normalized, Vector3.up);
            }
        }
    }
}
