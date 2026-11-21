using System;
using BlackMarket.Interaction;
using UnityEngine;

namespace BlackMarket.Shop
{
    /// <summary>
    /// Something on the counter the player can use: the bell (call the next visitor) or the ledger
    /// (open the trade ledger). It only announces that it was used; the scene controller decides what happens.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CounterInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string prompt = "Use counter";

        public string Prompt => prompt;
        public bool CanInteract => isActiveAndEnabled;

        public event Action Used;

        public void Configure(string newPrompt)
        {
            prompt = newPrompt;
        }

        public void Interact()
        {
            Used?.Invoke();
        }
    }
}
