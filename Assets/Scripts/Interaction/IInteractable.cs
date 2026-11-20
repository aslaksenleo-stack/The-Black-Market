namespace BlackMarket.Interaction
{
    /// <summary>
    /// Anything in the world the player can use: the counter, the portal device, shelves, a customer.
    /// Put a component implementing this on the object (or on a parent of its collider).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Short action shown on the interact button, e.g. "Use counter".</summary>
        string Prompt { get; }

        /// <summary>False when the object exists but can't be used right now.</summary>
        bool CanInteract { get; }

        void Interact();
    }
}
