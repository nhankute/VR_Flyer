using System.Collections;
using UnityEngine;
using VRStandardAssets.Utils;

namespace VRStandardAssets.Flyer
{
    // This script controls the behaviour of each laser instance.
    public class FlyerLaser : MonoBehaviour
    {
        [SerializeField] private float m_Speed = 500f;              // The speed each laser moves forward at.
        [SerializeField] private float m_LaserLifeDuration = 3f;    // How long the laser lasts before it's returned to it's object pool.


        private Rigidbody m_RigidBody;                              // Reference to the rigidbody of the laser.
        private bool m_Hit;                                         // Whether the laser has hit something.


        public ObjectPool ObjectPool { private get; set; }          // The object pool the laser belongs to.


        private void Awake()
        {
            m_RigidBody = GetComponent<Rigidbody>();
        }


        private void Update()
        {
            m_RigidBody.MovePosition(m_RigidBody.position + transform.forward * m_Speed * Time.deltaTime);
        }


        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("hit...");
            // Try and get the asteroid component of the hit collider.
            Asteroid asteroid = other.GetComponent<Asteroid>();
            Opponent opponent = other.GetComponent<Opponent>();

            // If it doesn't exist return.
            if (asteroid == null && opponent == null)
                return;

            // call the Hit function of the asteroid.
            if (asteroid != null)
            {
                asteroid.Hit();
                Debug.Log("asteroid hit...");
            }

            // or the hit function of the opponent
            if (opponent != null)
            {
                opponent.Hit();
                Debug.Log("opponent hit...");
            }

            // The laser has hit something.
            m_Hit = true;

            // Return the laser to the object pool.
            ObjectPool.ReturnGameObjectToPool(gameObject);
        }


        private IEnumerator Timeout()
        {
            // Wait for the life time of the laser.
            yield return new WaitForSeconds (m_LaserLifeDuration);

            // If the laser hasn't hit something return it to the object pool.
            if(!m_Hit)
			    ObjectPool.ReturnGameObjectToPool (gameObject);
        }


        public void Restart()
        {
            // At restart the laser hasn't hit anything.
            m_Hit = false;

            // Start the lifetime timeout.
            StartCoroutine (Timeout ());
        }
    }
}