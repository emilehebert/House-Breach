using System.Collections.Generic;
using UnityEngine;

public class FootstepSoundPlayer : MonoBehaviour
{
    public Transform player;
    public AudioSource audioSource;
    public AudioEvent defaultAudioEvent;

    [SerializeField]
    private List<MaterialAudioEvent> materialAudioEvents;

    [System.Serializable]
    private class MaterialAudioEvent
    {
        public Material[] materials;
        public AudioEvent audioEvent;
    }

    private void Start()
    {
        // Always re-fetch the live SFX source from the persistent AudioManager,
        // instead of relying on a reference dragged in the Inspector.
        audioSource = AudioManager.instance.GetSfxSource();
    }

    public void GetMaterial()
    {
        Debug.DrawRay(player.position, Vector3.down * 1f, Color.green, 2f);

        if (Physics.Raycast(player.position, Vector3.down * 100.0f, out RaycastHit hit))
        {
            Renderer renderer = hit.collider.GetComponent<Renderer>();
            Material material = null;

            if (renderer != null)
            {
                MeshCollider meshCollider = hit.collider as MeshCollider;
                if (meshCollider != null && meshCollider.sharedMesh != null)
                {
                    Mesh mesh = meshCollider.sharedMesh;
                    int triangleIndex = hit.triangleIndex;
                    int submeshIndex = -1;

                    for (int i = 0; i < mesh.subMeshCount; i++)
                    {
                        int[] triangles = mesh.GetTriangles(i);
                        if (triangleIndex * 3 < triangles.Length)
                        {
                            submeshIndex = i;
                            break;
                        }
                        triangleIndex -= triangles.Length / 3;
                    }

                    if (submeshIndex != -1 && submeshIndex < renderer.materials.Length)
                    {
                        material = renderer.materials[submeshIndex];
                    }
                }
                else
                {
                    material = renderer.material;
                }
            }

            if (material != null)
            {
                PlaySoundForMaterial(material);
            }
            else
            {
                Debug.LogWarning("No material found on the hit object.");
            }
        }
    }

    private void PlaySoundForMaterial(Material material)
    {
        string cleanMaterialName = material.name.Replace(" (Instance)", "").Trim();

        foreach (var matEvent in materialAudioEvents)
        {
            foreach (Material mat in matEvent.materials)
            {
                if (mat == null) continue;
                string matName = mat.name.Replace(" (Instance)", "").Trim();

                if (cleanMaterialName == matName)
                {
                    matEvent.audioEvent?.PlayRandom(audioSource);
                    return;
                }
            }
        }

        // Default fallback
        Debug.LogWarning("No AudioEvent found for material: " + cleanMaterialName);
        defaultAudioEvent?.PlayRandom(audioSource);
    }
}
