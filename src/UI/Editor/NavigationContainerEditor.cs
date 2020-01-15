#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace ModIO.UI.EditorCode
{
    [CustomEditor(typeof(NavigationContainer), true)]
    public class NavigationContainerEditor : UnityEditor.UI.SelectableEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUILayout.Space();

            if(GUILayout.Button("Add Container Elements to Children"))
            {
                Undo.SetCurrentGroupName("Add Container Elements to Children");

                NavigationContainer thisNavContainer = this.target as NavigationContainer;

                Selectable[] selectableChildren = thisNavContainer.GetComponentsInChildren<Selectable>();

                foreach(var child in selectableChildren)
                {
                    if(child.gameObject == thisNavContainer.gameObject) { continue; }

                    NavigationContainerElement element = child.gameObject.GetComponent<NavigationContainerElement>();
                    if(element == null)
                    {
                        Undo.AddComponent<NavigationContainerElement>(child.gameObject);
                    }
                }
            }
        }
    }
}

#endif // UNITY_EDITOR
