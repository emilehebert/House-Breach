using UnityEngine;
using HauntedPSX.RenderPipelines.PSX.Runtime;  // <-- your runtime pipeline namespace

public static class PSXRuntimeMaterialUtils
{
    /// <summary>
    /// Walks all child Renderers of 'go' and forces their materials into the given
    /// PSX RenderQueueType (e.g. UIOverlayOpaque or UIOverlayTransparent).
    /// </summary>
    public static void SetRenderQueue(GameObject go,
                                      PSXRenderQueue.RenderQueueType queueType,
                                      int offset = 0,
                                      bool alphaClip = false)
    {
        // Compute the actual integer queue once
        int q = PSXRenderQueue.ChangeType(queueType, offset, alphaClip);

        foreach (var rend in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = rend.materials;
            for (int i = 0; i < mats.Length; i++)
                mats[i].renderQueue = q;
        }
    }
}
