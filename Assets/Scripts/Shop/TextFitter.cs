using UnityEngine;

namespace BlackMarket.Shop
{
    /// <summary>
    /// Scales a TextMesh so its text fits a box of the given size in metres. Font metrics are only known
    /// once Unity has built the text mesh, so this measures on the first frame instead of guessing.
    /// </summary>
    [RequireComponent(typeof(TextMesh))]
    public sealed class TextFitter : MonoBehaviour
    {
        public Vector2 BoxSize = new Vector2(0.8f, 0.5f);

        private System.Collections.IEnumerator Start()
        {
            yield return null;
            var renderer = GetComponent<MeshRenderer>();
            if (renderer == null) yield break;

            // Measure in local space (the text faces along its local Z).
            Vector3 world = renderer.bounds.size;
            Vector3 local = transform.InverseTransformVector(world);
            float width = Mathf.Abs(local.x) * transform.localScale.x;
            float height = Mathf.Abs(local.y) * transform.localScale.y;
            if (width < 1e-4f || height < 1e-4f) yield break;

            float scale = Mathf.Min(BoxSize.x / width, BoxSize.y / height);
            transform.localScale *= scale;
        }
    }
}
