using UnityEngine;

namespace KoutSab.Gameplay
{
    /// <summary>
    /// Un morceau en vol : fruit entier ou moitié tranchée.
    ///
    /// La physique est intégrée à la main plutôt que confiée à un Rigidbody.
    /// Raisons : la gravité voulue n'est pas celle du monde (voir
    /// GameplayTuning.FruitGravity), les fruits ne doivent jamais entrer en
    /// collision entre eux, et le micro-gel à l'impact doit pouvoir figer les
    /// morceaux sans toucher à l'horloge globale — qui, elle, continue de faire
    /// avancer le chrono. Un Rigidbody imposerait de lutter contre les trois.
    /// </summary>
    public sealed class FlyingPiece : MonoBehaviour
    {
        private Vector3 velocity;
        private Vector3 spinAxis;
        private float spinSpeed;
        private float ageSeconds;
        private float lifetime;

        public bool IsAlive { get; private set; }

        /// <summary>Repère le fruit entier, seul à pouvoir être tranché.</summary>
        public bool IsWhole { get; private set; }

        public MeshFilter Filter { get; private set; }
        public MeshRenderer Renderer { get; private set; }

        private void Awake()
        {
            Filter = GetComponent<MeshFilter>();
            Renderer = GetComponent<MeshRenderer>();
        }

        public void Launch(Vector3 position, Vector3 initialVelocity, bool whole, float lifeSeconds)
        {
            transform.position = position;
            transform.rotation = Random.rotation;

            velocity = initialVelocity;
            spinAxis = Random.onUnitSphere;
            spinSpeed = Random.Range(GameplayTuning.SpinMin, GameplayTuning.SpinMax);

            ageSeconds = 0f;
            lifetime = lifeSeconds;
            IsWhole = whole;
            IsAlive = true;

            gameObject.SetActive(true);
        }

        /// <summary>Écarte une moitié du plan de coupe, juste après la découpe.</summary>
        public void Push(Vector3 impulse, float extraSpin)
        {
            velocity += impulse;
            spinSpeed += extraSpin;
        }

        public void Step(float deltaTime, float despawnBelowY)
        {
            if (!IsAlive)
            {
                return;
            }

            velocity.y -= GameplayTuning.FruitGravity * deltaTime;
            transform.position += velocity * deltaTime;
            transform.Rotate(spinAxis, spinSpeed * deltaTime, Space.World);

            ageSeconds += deltaTime;

            if (transform.position.y < despawnBelowY || (lifetime > 0f && ageSeconds > lifetime))
            {
                Recycle();
            }
        }

        public void Recycle()
        {
            IsAlive = false;
            gameObject.SetActive(false);
        }
    }
}
