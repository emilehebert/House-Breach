using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Decal))]
public class DecalEditor : Editor
{
    SerializedProperty materialProp;
    SerializedProperty spriteProp;
    SerializedProperty layerMaskProp;
    SerializedProperty maxAngleProp;
    SerializedProperty offsetProp;
    SerializedProperty scatterModeProp;
    SerializedProperty scatterCenterProp;
    SerializedProperty scatterBoundsProp;
    SerializedProperty decalCountProp;
    SerializedProperty randomSeedProp;

    void OnEnable()
    {
        materialProp = serializedObject.FindProperty("material");
        spriteProp = serializedObject.FindProperty("sprite");
        layerMaskProp = serializedObject.FindProperty("layerMask");
        maxAngleProp = serializedObject.FindProperty("maxAngle");
        offsetProp = serializedObject.FindProperty("offset");
        scatterModeProp = serializedObject.FindProperty("scatterMode");
        scatterCenterProp = serializedObject.FindProperty("scatterCenter");
        scatterBoundsProp = serializedObject.FindProperty("scatterBounds");
        decalCountProp = serializedObject.FindProperty("decalCount");
        randomSeedProp = serializedObject.FindProperty("randomSeed");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(materialProp);
        EditorGUILayout.PropertyField(spriteProp);

        DrawSpritePreview(spriteProp.objectReferenceValue as Sprite);

        EditorGUILayout.PropertyField(layerMaskProp);
        EditorGUILayout.PropertyField(maxAngleProp);
        EditorGUILayout.PropertyField(offsetProp);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(scatterModeProp);
        if (scatterModeProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(scatterCenterProp, new GUIContent("Center"));
            EditorGUILayout.PropertyField(scatterBoundsProp, new GUIContent("Scatter Bounds"));
            EditorGUILayout.PropertyField(decalCountProp, new GUIContent("Decal Count"));
            EditorGUILayout.PropertyField(randomSeedProp, new GUIContent("Random Seed"));
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        string buttonLabel = scatterModeProp.boolValue ? "Build (Scatter)" : "Build";
        if (GUILayout.Button(buttonLabel, GUILayout.Height(24)))
        {
            ((Decal)target).Build();
        }
    }

    // Draws the sprite's actual sub-rect from its texture, so atlased sprites
    // preview correctly instead of showing the whole atlas sheet.
    static void DrawSpritePreview(Sprite sprite)
    {
        const float size = 64f;
        Rect rect = GUILayoutUtility.GetRect(size, size);
        rect.x = (EditorGUIUtility.currentViewWidth - size) * 0.5f;
        rect.width = size;

        if (sprite == null || sprite.texture == null)
            return;

        Texture2D texture = sprite.texture;
        Rect spriteRect = sprite.rect;
        Rect uvRect = new Rect(
            spriteRect.xMin / texture.width,
            spriteRect.yMin / texture.height,
            spriteRect.width / texture.width,
            spriteRect.height / texture.height);

        GUI.DrawTextureWithTexCoords(rect, texture, uvRect);
    }
}
