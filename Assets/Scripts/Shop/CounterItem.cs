using System;
using BlackMarket.Art;
using BlackMarket.Interaction;
using BlackMarket.Items;
using UnityEngine;

namespace BlackMarket.Shop
{
    /// <summary>
    /// The 3D item lying on the counter during a deal. Tap it (or look at it and press the button) to
    /// inspect it closely. Shows the real model, including any crack if the item is flawed.
    /// </summary>
    public sealed class CounterItem : MonoBehaviour, IInteractable
    {
        private BoxCollider box;

        public ItemInstance Item { get; private set; }

        /// <summary>The rendered model (child), which the inspector rotates.</summary>
        public Transform Model { get; private set; }

        public bool Interactable { get; set; } = true;

        public string Prompt => "Inspect " + (Item != null ? Item.Definition.Name : "item");
        public bool CanInteract => Interactable && isActiveAndEnabled;

        public event Action<CounterItem> Used;

        public static CounterItem Create(ItemInstance item, Vector3 position, Quaternion rotation)
        {
            var root = new GameObject("CounterItem_" + item.Definition.Id);
            root.transform.SetPositionAndRotation(position, rotation);
            var counterItem = root.AddComponent<CounterItem>();
            counterItem.Item = item;

            GameObject model = ArtLibrary.Current.CreateItemObject(item.Definition.ModelId, item.HasHiddenFlaw, root.transform, "Model");
            counterItem.Model = model.transform;

            // A collider a little larger than the model makes small items easy to tap.
            Bounds b = ArtLibrary.RenderBounds(model);
            counterItem.box = root.AddComponent<BoxCollider>();
            counterItem.box.center = root.transform.InverseTransformPoint(b.center);
            counterItem.box.size = Vector3.Max(b.size * 1.3f, Vector3.one * 0.14f);
            return counterItem;
        }

        public void Interact()
        {
            Used?.Invoke(this);
        }

        /// <summary>Visual centre of the model, in world space.</summary>
        public Vector3 Center => ArtLibrary.RenderBounds(Model.gameObject).center;
    }
}
