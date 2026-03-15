using UnityEngine;

namespace FastStudios.Demo
{
    public class MoveCamera_DEMO : MonoBehaviour
    {
        public Transform player;

        void Update()
        {
            transform.position = player.transform.position;
        }
    }
}
