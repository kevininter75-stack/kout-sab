using UnityEngine;

namespace KoutSab.Fruits
{
    /// <summary>
    /// Affiche un letchi généré par code. [ExecuteAlways] pour que le maillage
    /// existe aussi hors mode jeu : sans ça, la scène serait vide dans l'éditeur
    /// et impossible à cadrer ou à capturer.
    ///
    /// Le maillage est reconstruit à chaque changement de réglage dans
    /// l'inspecteur — c'est ce qui permet de caler la forme à l'œil.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class LetchiSpikeView : MonoBehaviour
    {
        [SerializeField] private LetchiShape shape = LetchiShape.Default;

        [Tooltip("Rotation lente, pour juger la silhouette sous tous les angles.")]
        [SerializeField] private float spinDegreesPerSecond = 18f;

        private Mesh generatedMesh;

        private void OnEnable()
        {
            Rebuild();
        }

        private void OnValidate()
        {
            // OnValidate part du thread de l'inspecteur ; reconstruire un maillage
            // ici est sûr, mais pas de le détruire pendant la sérialisation.
            if (isActiveAndEnabled)
            {
                Rebuild();
            }
        }

        private void Update()
        {
            if (Application.isPlaying && spinDegreesPerSecond != 0f)
            {
                transform.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime, Space.Self);
            }
        }

        private void OnDisable()
        {
            DisposeMesh();
        }

        public void Rebuild()
        {
            DisposeMesh();

            generatedMesh = LetchiMeshBuilder.Build(shape);
            GetComponent<MeshFilter>().sharedMesh = generatedMesh;
        }

        private void DisposeMesh()
        {
            if (generatedMesh == null)
            {
                return;
            }

            // Un maillage créé par code n'appartient à aucun asset : sans
            // destruction explicite, il fuit à chaque reconstruction.
            if (Application.isPlaying)
            {
                Destroy(generatedMesh);
            }
            else
            {
                DestroyImmediate(generatedMesh);
            }

            generatedMesh = null;
        }
    }
}
