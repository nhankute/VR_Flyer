using System;
using System.Collections;
using UnityEngine;
using VRStandardAssets.Common;
using VRStandardAssets.Utils;
using Random = UnityEngine.Random;

namespace VRStandardAssets.Flyer
{
    // This class controls each opponent in the flyer scene
    public class Opponent : MonoBehaviour
    {
        public event Action<Opponent> OnOpponentRemovalDistance;    // This event is triggered when it is far enough behind the camera to be removed.
        public event Action<Opponent> OnOpponentHit;                // This event is triggered when the opponent is hit either the ship or a laser. 


        [SerializeField] private float m_OpponentMinSize = 2f;     // The minimum amount an opponent can be scaled up.
        [SerializeField] private float m_OpponentMaxSize = 4f;     // The maximum amount an opponent can be scaled up.
        [SerializeField] private float m_MinSpeed = 0f;             // The minimum speed the opponent will move towards the camera.
        [SerializeField] private float m_MaxSpeed = 10f;            // The maximum speed the opponent will move towards the camera.
        [SerializeField] private float m_MinRotationSpeed = 10f;   // The minimum speed the opponent will rotate at.
        [SerializeField] private float m_MaxRotationSpeed = 14f;   // The maximum speed the opponent will rotate at.
        [SerializeField] private int m_PlayerDamage = 20;           // The amount of damage the opponent will do to the ship if it hits.
        [SerializeField] private int m_Score = 10;                  // The amount added to the score when the opponent hits either the ship or a laser.

        private Rigidbody m_RigidBody;                              // Reference to the opponent's rigidbody, used to move and rotate it.
        private FlyerHealthController m_FlyerHealthController;      // Reference to the flyer's health script, used to damage it.
        private GameObject m_Flyer;                                 // Reference to the flyer itself, used to determine what was hit.
        private Transform m_Cam;                                    // Reference to the camera so this can be destroyed when it's behind the camera.
        private float m_Speed;                                      // How fast opponent will move towards the camera.
        private Vector3 m_RotationAxis;                             // The axis around which the opponent will rotate.
        private float m_RotationSpeed;                              // How fast the opponent will rotate.


        private const float k_RemovalDistance = 50f;                // How far behind the camera the opponent must be before it is removed.


        public int Score { get { return m_Score; } }

        // laser code
        private bool m_Spawning;
        private ObjectPool m_LaserOpponentObjectPool;
        [SerializeField] private float m_LaserSpawnFrequency = 7f;
        [SerializeField] private Transform m_LaserSpawnPos;

        private void Awake()
        {
            m_RigidBody = GetComponent<Rigidbody>();

            m_FlyerHealthController = FindObjectOfType<FlyerHealthController>();
            m_Flyer = m_FlyerHealthController.gameObject;

            m_LaserOpponentObjectPool = FindObjectOfType<EnvironmentController>().GetLaserOpponentPool();
            
            m_Cam = Camera.main.transform;
        }


        private void Start()
        {
            // Set a random scale for the opponent.
            float scaleMultiplier = Random.Range(m_OpponentMinSize, m_OpponentMaxSize);
            transform.localScale = new Vector3(scaleMultiplier, scaleMultiplier, scaleMultiplier);

            // Set a random rotation for the opponent.
            //transform.rotation = Random.rotation;


            // Set a random speed for the opponent.
            m_Speed = Random.Range(m_MinSpeed, m_MaxSpeed);

            // Setup a random spin for the opponent.
            m_RotationAxis = Random.insideUnitSphere;
            m_RotationSpeed = Random.Range(m_MinRotationSpeed, m_MaxRotationSpeed);

            m_Spawning = true;
            StartCoroutine(SpawnLaserRoutine());
        }


        private void Update()
        {
            // Move and rotate the opponent given the previously set up parameters.
            //m_RigidBody.MoveRotation(m_RigidBody.rotation * Quaternion.AngleAxis(m_RotationSpeed * Time.deltaTime, m_RotationAxis));
            m_RigidBody.MovePosition(m_RigidBody.position - Vector3.forward * m_Speed * Time.deltaTime);

            // If the opponent is far enough behind the camera and something has subscribed to OnopponentRemovalDistance call it.
            if (transform.position.z < m_Cam.position.z - k_RemovalDistance)
                if (OnOpponentRemovalDistance != null)
                    OnOpponentRemovalDistance(this);
        }


        private void OnTriggerEnter(Collider other)
        {
            // Only continue if the opponent has hit the flyer.
            if (other.gameObject != m_Flyer)
                return;

            // Damage the flyer.
            m_FlyerHealthController.TakeDamage(m_PlayerDamage);

            // If the damage didn't kill the flyer add to the score and call the appropriate events.
            if (!m_FlyerHealthController.IsDead)
                Hit();
        }


        private void OnDestroy()
        {
            // Ensure the events are completely unsubscribed when the opponent is destroyed.
            OnOpponentRemovalDistance = null;
            OnOpponentHit = null;
            m_Spawning = false;
        }


        public void Hit()
        {
            // Add to the score.
            SessionData.AddScore(m_Score);

            // If OnOpponentHit has any subscribers call it.
            if (OnOpponentHit != null)
                OnOpponentHit(this);
        }

        // LASER CODE

        private IEnumerator SpawnLaserRoutine()
        {
            // While the environment is spawning, spawn an asteroid and wait for another one.
            do
            {
                SpawnLaser();
                yield return new WaitForSeconds(m_LaserSpawnFrequency);
            }
            while (m_Spawning);
        }

        private void SpawnLaser()
        {
            // Get a laser from the pool.
            GameObject laserGameObject = m_LaserOpponentObjectPool.GetGameObjectFromPool();

            // Set it's position and rotation based on the gun positon.
            laserGameObject.transform.position = m_LaserSpawnPos.position;
            //laserGameObject.transform.rotation = m_LaserSpawnPos.rotation;
            Debug.Log(m_LaserSpawnPos.position);

            // Find the FlyerLaser component of the laser instance.
            OpponentLaser opponentLaser = laserGameObject.GetComponent<OpponentLaser>();

            // Set it's object pool so it knows where to return to.
            opponentLaser.ObjectPool = m_LaserOpponentObjectPool;

            // Restart the laser.
            opponentLaser.Restart();
            Debug.Log("spawning laser...");
        }
    }
}
