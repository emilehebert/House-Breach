using UnityEngine;
using Cinemachine;

/// <summary>
/// Chaise sur laquelle le joueur peut s'asseoir. Implémente IInteractable pour
/// se brancher sur InteractionSystem (E pour s'asseoir), et IFocusable pour
/// que Echap (centralisé dans MenuManager via FocusStack) fasse lever le joueur.
///
/// Nested behaviour : si le joueur est assis ET en focus sur un écran, un
/// premier Echap sort du focus écran, un second Echap fait lever le joueur —
/// géré automatiquement par l'ordre d'empilement dans FocusStack.
/// </summary>
public class MountableChair : MonoBehaviour, IInteractable, IFocusable
{
    [Header("Configuration")]
    [SerializeField] private Transform sitPoint;   // Où le joueur est téléporté en s'asseyant
    [SerializeField] private Transform standPoint; // Où le joueur atterrit en se levant
    [SerializeField] private string interactText = "S'asseoir";

    [Header("Cinemachine (2.9.7)")]
    [SerializeField] private CinemachineVirtualCamera chairCamera;
    [Tooltip("Doit être plus haut que la vcam de gameplay, mais plus bas que les vcams de focus écran.")]
    [SerializeField] private int chairPriority = 20;
    [SerializeField] private int defaultPriority = 0;
    [Tooltip("Ta vcam de gameplay normale — sert de référence pour copier sa sensibilité de regard sur chairCamera, pour éviter d'avoir à régler les deux séparément.")]
    [SerializeField] private CinemachineVirtualCamera gameplayCamera;

    private GameObject currentPlayer;
    private bool isSitting = false;

    // --- IFocusable : pas besoin du curseur juste pour être assis ---
    public bool WantsCursorVisible => false;

    private void Start()
    {
        if (chairCamera != null)
            chairCamera.Priority = defaultPriority;
    }

    /// <summary>
    /// Copie la sensibilité de regard (vitesse des axes POV) de la vcam de
    /// gameplay vers celle de la chaise, pour ne jamais avoir à les régler
    /// séparément à la main ni les laisser diverger.
    /// </summary>
    private void SyncSensitivity()
    {
        if (chairCamera == null || gameplayCamera == null) return;

        var sourcePov = gameplayCamera.GetCinemachineComponent<CinemachinePOV>();
        var targetPov = chairCamera.GetCinemachineComponent<CinemachinePOV>();
        if (sourcePov == null || targetPov == null) return;

        targetPov.m_HorizontalAxis.m_MaxSpeed = sourcePov.m_HorizontalAxis.m_MaxSpeed;
        targetPov.m_VerticalAxis.m_MaxSpeed = sourcePov.m_VerticalAxis.m_MaxSpeed;
    }

    private void OnDisable()
    {
        // Sécurité si la chaise est désactivée pendant que le joueur est assis
        if (isSitting)
            StandUp(popFromStack: true);
    }

    // -- IInteractable --

    public void Interact()
    {
        if (isSitting) return;

        if (PlayerDriver.Instance != null && PlayerDriver.Instance.CurrentBody != null)
            SitDown(PlayerDriver.Instance.CurrentBody.GameObject);
    }

    public string GetInteractText() => interactText;

    public AnimationClip GetInteractAnimation() => null; // Ajoute un clip ici si tu veux une anim de s'asseoir

    // -- IFocusable : appelé par FocusStack quand cette chaise est au sommet et qu'Echap est pressé --

    public void OnCancelFocus() => StandUp(popFromStack: false); // déjà retiré de la pile par FocusStack

    // -- Chair Logic --

    private void SitDown(GameObject player)
    {
        currentPlayer = player;
        isSitting = true;

        // Snap le joueur sur le point d'assise (corrige le bug où sitPoint n'était jamais utilisé)
        if (sitPoint != null)
        {
            player.transform.position = sitPoint.position;
            player.transform.rotation = sitPoint.rotation;
        }

        // 1. Désactiver la physique (les jambes)
        if (player.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
        if (player.TryGetComponent<CharacterController>(out var cc)) cc.enabled = false;

        // 2. Geler les inputs de déplacement via la méthode réutilisable —
        // contrairement à "movement.enabled = false", ça ne coupe pas
        // gravité/ground-check/collider, donc pas de glitch au lever.
        if (PlayerDriver.Instance != null)
            PlayerDriver.Instance.SetInputEnabled(false);

        // 3. Bascule caméra (sensibilité resynchronisée à chaque fois, au cas où
        // elle aurait changé entre-temps, ex: un slider de sensibilité dans les options)
        SyncSensitivity();
        if (chairCamera != null)
            chairCamera.Priority = chairPriority;

        // 4. S'enregistre comme focus actif — Echap fera lever le joueur
        FocusStack.Push(this);
    }

    /// <summary>Appel externe (ex: bouton UI "se lever"). Retire aussi la chaise de la FocusStack.</summary>
    public void StandUp() => StandUp(popFromStack: true);

    private void StandUp(bool popFromStack)
    {
        if (!isSitting) return;

        // 1. Reset caméra
        if (chairCamera != null)
            chairCamera.Priority = defaultPriority;

        // 2. Replacer le joueur pour ne pas rester coincé dans la chaise
        if (currentPlayer != null)
        {
            bool hadController = currentPlayer.TryGetComponent<CharacterController>(out var cc);
            if (hadController) cc.enabled = false; // le temps de repositionner, évite les collisions parasites

            if (standPoint != null)
                currentPlayer.transform.position = standPoint.position;
            else
                currentPlayer.transform.position = transform.position + transform.forward;

            if (hadController) cc.enabled = true;

            // 3. Réactiver la physique
            if (currentPlayer.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = false;
        }

        // 4. Redonner le contrôle
        if (PlayerDriver.Instance != null)
            PlayerDriver.Instance.SetInputEnabled(true);

        if (popFromStack)
            FocusStack.Pop(this);

        isSitting = false;
        currentPlayer = null;
    }
}