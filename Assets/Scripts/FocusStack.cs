using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Permet à n'importe quel état "focus" (assis sur une chaise, en train de
/// regarder un écran, plus tard dans un mini-jeu, etc.) de s'enregistrer comme
/// actif. Un seul point d'entrée global pour Echap/Cancel (dans MenuManager)
/// ne fait réagir QUE le sommet de la pile. Gère aussi la visibilité du
/// curseur de façon centralisée (pause OU un focus qui le demande), pour
/// éviter que plusieurs scripts se marchent dessus sur Cursor.visible.
/// </summary>
public interface IFocusable
{
    /// <summary>Appelé quand cet objet est au sommet de la pile et que Cancel est pressé.</summary>
    void OnCancelFocus();

    /// <summary>Le curseur doit-il être visible/déverrouillé pendant ce focus ? (ex: écran = oui, chaise = non)</summary>
    bool WantsCursorVisible { get; }
}

public static class FocusStack
{
    private static readonly List<IFocusable> stack = new List<IFocusable>();

    public static bool IsActive => stack.Count > 0;

    public static void Push(IFocusable focusable)
    {
        if (!stack.Contains(focusable))
            stack.Add(focusable);

        RefreshCursor();
    }

    public static void Pop(IFocusable focusable)
    {
        stack.Remove(focusable);
        RefreshCursor();
    }

    /// <summary>
    /// À appeler depuis le handler global d'input (MenuManager) quand Cancel est pressé.
    /// Retourne true si quelque chose dans la pile a géré l'appui.
    /// </summary>
    public static bool TryHandleCancel()
    {
        if (stack.Count == 0) return false;

        int lastIndex = stack.Count - 1;
        IFocusable top = stack[lastIndex];
        stack.RemoveAt(lastIndex);

        // top might be a destroyed Unity object — interface references don't
        // get Unity's "destroyed == null" override, so calling straight
        // through could throw instead of failing safely. Well-behaved
        // IFocusables already pop themselves in OnDisable, so this is a
        // last-resort guard, not the primary safety net.
        if (top is Object unityObj && unityObj == null)
        {
            RefreshCursor();
            return true;
        }

        top.OnCancelFocus();
        RefreshCursor();
        return true;
    }

    /// <summary>À appeler lors d'un changement de scène pour éviter les références vers des objets détruits.</summary>
    public static void Clear()
    {
        stack.Clear();
        RefreshCursor();
    }

    /// <summary>
    /// Recalcule si le curseur doit être visible : pause OU un focus actif qui
    /// le demande. Appelé automatiquement par Push/Pop/Clear, et à appeler
    /// aussi depuis MenuManager quand l'état pause change.
    /// </summary>
    public static void RefreshCursor()
    {
        bool shouldShow = GameManager.IsMenuOpen || stack.Any(f => f.WantsCursorVisible);
        Cursor.visible = shouldShow;
        Cursor.lockState = shouldShow ? CursorLockMode.None : CursorLockMode.Locked;
    }
}