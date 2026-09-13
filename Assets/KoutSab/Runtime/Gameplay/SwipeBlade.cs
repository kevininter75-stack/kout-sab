using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KoutSab.Gameplay
{
    /// <summary>
    /// La lame : lit le geste du joueur, en garde une courte traînée, et expose
    /// le segment qui vient d'être tracé.
    ///
    /// Les points sont convertis tout de suite en positions MONDE sur le plan de
    /// jeu (z = 0). Travailler en pixels obligerait chaque test de coupe à
    /// reprojeter, et le code dépendrait de la résolution — un swipe identique
    /// n'aurait pas le même effet sur deux téléphones.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class SwipeBlade : MonoBehaviour
    {
        private readonly struct TrailPoint
        {
            public readonly Vector3 World;
            public readonly float Time;

            public TrailPoint(Vector3 world, float time)
            {
                World = world;
                Time = time;
            }
        }

        private readonly List<TrailPoint> points = new List<TrailPoint>();
        private readonly Vector3[] linePositions = new Vector3[GameplayTuning.TrailMaxPoints];

        private Camera playCamera;
        private LineRenderer line;
        private float viewHeight;

        /// <summary>Vrai si le geste de cette frame est assez vif pour couper.</summary>
        public bool IsCutting { get; private set; }

        /// <summary>Extrémités du segment tracé pendant cette frame, en monde.</summary>
        public Vector3 SegmentStart { get; private set; }
        public Vector3 SegmentEnd { get; private set; }

        public void Initialise(Camera camera, float visibleHeight)
        {
            playCamera = camera;
            viewHeight = visibleHeight;

            line = GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.numCapVertices = 4;
            line.positionCount = 0;
        }

        private void Update()
        {
            IsCutting = false;

            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.isPressed)
            {
                points.Clear();
                line.positionCount = 0;
                return;
            }

            Vector3 world = ScreenToPlayPlane(pointer.position.ReadValue());
            float now = Time.time;

            if (points.Count > 0)
            {
                TrailPoint previous = points[points.Count - 1];
                float elapsed = now - previous.Time;

                if (elapsed > 1e-4f)
                {
                    // Vitesse ramenée à la hauteur visible : un geste « rapide »
                    // doit vouloir dire la même chose sur tous les écrans.
                    float speed = Vector3.Distance(world, previous.World) / elapsed / viewHeight;

                    if (speed >= GameplayTuning.BladeMinSpeed)
                    {
                        IsCutting = true;
                        SegmentStart = previous.World;
                        SegmentEnd = world;
                    }
                }
            }

            points.Add(new TrailPoint(world, now));
            TrimTrail(now);
            RenderTrail();
        }

        private void TrimTrail(float now)
        {
            while (points.Count > 0 && now - points[0].Time > GameplayTuning.TrailPointLife)
            {
                points.RemoveAt(0);
            }

            while (points.Count > GameplayTuning.TrailMaxPoints)
            {
                points.RemoveAt(0);
            }
        }

        private void RenderTrail()
        {
            int count = points.Count;
            for (int i = 0; i < count; i++)
            {
                linePositions[i] = points[i].World;
            }

            line.positionCount = count;
            line.SetPositions(linePositions);

            // Le ruban s'affine vers la queue : c'est ce dégradé qui donne
            // l'impression d'une lame en mouvement plutôt que d'un trait collé
            // sous le doigt.
            float width = viewHeight * GameplayTuning.TrailHalfWidth * 2f;
            line.startWidth = 0f;
            line.endWidth = width;
        }

        private Vector3 ScreenToPlayPlane(Vector2 screenPosition)
        {
            // Le plan de jeu est en z = 0 ; la caméra le regarde de -Z.
            Vector3 point = new Vector3(screenPosition.x, screenPosition.y,
                                        -playCamera.transform.position.z);
            return playCamera.ScreenToWorldPoint(point);
        }
    }
}
