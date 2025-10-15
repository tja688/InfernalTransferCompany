using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DirectorUI.Editor
{
    [CustomEditor(typeof(UIDirector))]
    public class UIDirectorEditor : UnityEditor.Editor
    {
        private SerializedProperty transitionTicketsProperty;
        private SerializedProperty startingViewIdProperty;
        private SerializedProperty tweenPlayerProperty;

        private void OnEnable()
        {
            transitionTicketsProperty = serializedObject.FindProperty("transitionTickets");
            startingViewIdProperty = serializedObject.FindProperty("startingViewId");
            tweenPlayerProperty = serializedObject.FindProperty("tweenPlayer");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(tweenPlayerProperty);
            EditorGUILayout.PropertyField(transitionTicketsProperty, includeChildren: true);

            DrawStartingViewSelector();

            serializedObject.ApplyModifiedProperties();

            DrawRuntimePreview();
        }

        private void DrawStartingViewSelector()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Starting View", EditorStyles.boldLabel);

            var ids = CollectViewIds();
            if (ids.Count == 0)
            {
                EditorGUILayout.HelpBox("場景中尚未找到任何 UIView。可在 Play 模式或場景中放置後重試。", MessageType.Info);
                EditorGUILayout.PropertyField(startingViewIdProperty);
                return;
            }

            var display = new string[ids.Count];
            for (int i = 0; i < ids.Count; i++)
            {
                display[i] = string.IsNullOrEmpty(ids[i]) ? "<None>" : ids[i];
            }

            var currentValue = startingViewIdProperty.stringValue;
            var currentIndex = Mathf.Max(0, ids.IndexOf(currentValue));
            var newIndex = EditorGUILayout.Popup(new GUIContent("Starting View Id"), currentIndex, display);
            startingViewIdProperty.stringValue = ids[newIndex];
        }

        private List<string> CollectViewIds()
        {
            var result = new List<string> { string.Empty };
            foreach (var view in UnityEngine.Object.FindObjectsOfType<UIView>(true))
            {
                if (view == null) continue;
                if (string.IsNullOrEmpty(view.ViewId)) continue;
                if (!result.Contains(view.ViewId))
                {
                    result.Add(view.ViewId);
                }
            }
            return result;
        }

        private void DrawRuntimePreview()
        {
            if (!Application.isPlaying) return;

            var director = (UIDirector)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime State", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Current View", director.CurrentView ? director.CurrentView.ViewId : "None");
            EditorGUILayout.LabelField("Is Transitioning", director.IsTransitioning ? "True" : "False");
        }
    }
}
