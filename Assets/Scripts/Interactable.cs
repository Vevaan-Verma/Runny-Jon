using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Interactable : MonoBehaviour {

    [Header("Interacting")]
    public string interactText;

    public void BaseInteract() {

        OnInteract();

    }

    protected abstract void OnInteract();

}