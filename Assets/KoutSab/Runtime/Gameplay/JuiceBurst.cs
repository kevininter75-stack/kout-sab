using UnityEngine;

namespace KoutSab.Gameplay
{
    /// <summary>
    /// Gerbe de jus projetée au moment de la coupe.
    ///
    /// Un système de particules par gerbe, tous créés au démarrage et recyclés.
    /// Shuriken plutôt que VFX Graph : le VFX Graph exige des compute shaders,
    /// absents de WebGL. Deux implémentations à maintenir pour un gain invisible
    /// à ces volumes ne vaut pas la peine.
    ///
    /// Les particules partent DANS LE PLAN de la coupe, pas dans toutes les
    /// directions : du jus qui gicle perpendiculairement à la lame trahit le
    /// geste qui vient d'être fait, et c'est ce qui rend le coup lisible.
    /// </summary>
    public sealed class JuiceBurst : MonoBehaviour
    {
        private ParticleSystem system;
        private ParticleSystem.EmitParams emitParams;

        public bool IsBusy => system != null && system.IsAlive(true);

        public void Configure(Material particleMaterial, float fruitRadius)
        {
            system = gameObject.AddComponent<ParticleSystem>();

            // Le système doit être arrêté avant toute configuration : Unity
            // verrouille certains modules sur un système en cours de lecture.
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.48f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(fruitRadius * 6f, fruitRadius * 26f);
            main.startSize = new ParticleSystem.MinMaxCurve(fruitRadius * 0.10f, fruitRadius * 0.34f);
            main.gravityModifier = 0.22f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false; // émission uniquement à la demande

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false; // direction imposée à chaque particule

            // Les gouttes ralentissent en vol : sans traînée, elles filent tout
            // droit et ressemblent à des étincelles plutôt qu'à du liquide.
            ParticleSystem.LimitVelocityOverLifetimeModule drag = system.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.dampen = 0.22f;

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.15f));

            var renderer = GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Projette la gerbe depuis <paramref name="origin"/>, dans le plan de
        /// coupe défini par sa normale.
        /// </summary>
        public void Play(Vector3 origin, Vector3 planeNormal, Color juiceColor, int count)
        {
            gameObject.SetActive(true);
            transform.position = origin;

            // Repère du plan de coupe : deux axes perpendiculaires à la normale.
            Vector3 axisU = Vector3.Normalize(Vector3.Cross(planeNormal,
                Mathf.Abs(planeNormal.y) < 0.9f ? Vector3.up : Vector3.right));
            Vector3 axisV = Vector3.Cross(planeNormal, axisU);

            for (int i = 0; i < count; i++)
            {
                float angle = Random.value * Mathf.PI * 2f;
                Vector3 inPlane = axisU * Mathf.Cos(angle) + axisV * Mathf.Sin(angle);

                // Une pincée de dispersion hors plan : une gerbe parfaitement
                // plate se lit comme un disque de carton.
                Vector3 direction = (inPlane + planeNormal * Random.Range(-0.22f, 0.22f)).normalized;

                emitParams = default;
                emitParams.position = origin;
                emitParams.velocity = direction * Random.Range(system.main.startSpeed.constantMin,
                                                               system.main.startSpeed.constantMax);
                emitParams.startColor = juiceColor;
                emitParams.applyShapeToPosition = false;

                system.Emit(emitParams, 1);
            }

            system.Play(false);
        }

        public void RecycleIfDone()
        {
            if (gameObject.activeSelf && !IsBusy)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
