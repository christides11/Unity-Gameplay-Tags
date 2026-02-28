using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameplayTags
{
    [CreateAssetMenu(menuName = "Gameplay Tags/Tags Source")]
    public class GameplayTagsSource : ScriptableObject
    {
        public string newTagLocation;
        public List<GameplayTagAsset> GameplayTags = new List<GameplayTagAsset>();

        [NonSerialized] public UnityEvent OnTagCreated = new UnityEvent();

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            for (int i = GameplayTags.Count - 1; i >= 0; i--)
            {
                if(GameplayTags[i] == null) GameplayTags.RemoveAt(i);
            }
        }
#endif

        public List<GameplayTagAsset> GetBaseTags()
        {
            List<GameplayTagAsset> baseTags = new List<GameplayTagAsset>();
            for (int i = 0; i < GameplayTags.Count; i++)
            {
                if (GameplayTags[i] == null || GameplayTags[i].parentTag != null) continue;
                baseTags.Add(GameplayTags[i]);
            }

            return baseTags;
        }

#if UNITY_EDITOR
        public bool TryCreateTag(string tagName)
        {
            var tagParts = tagName.Split('.');
            for (int i = 0; i < tagParts.Length; i++)
            {
                if (string.IsNullOrEmpty(tagParts[i])) return false;
            }
            if (tagParts.Length == 0) return false;
            var baseTags = GetBaseTags();

            int matchedParts = 0;
            GameplayTagAsset branchAsset = null;
            for (int i = 0; i < baseTags.Count; i++)
            {
                if (baseTags[i].tag != tagParts[0]) continue;
                matchedParts++;
                branchAsset = baseTags[i];
                FindBranchRecursive(baseTags[i], tagParts, ref branchAsset, ref matchedParts);
                break;
            }
            Debug.Log($"Found {matchedParts}, got branch of {branchAsset?.tag}");

            if (branchAsset == null)
            {
                branchAsset = ScriptableObject.CreateInstance<GameplayTagAsset>();
                branchAsset.tag = tagParts[0];
                AssetDatabase.CreateAsset(branchAsset, $"{newTagLocation}{tagParts[0]}.asset");
                GameplayTags.Add(branchAsset);
                EditorUtility.SetDirty(this);
                matchedParts = 1;
            }

            CreateTagsRecursive(branchAsset, tagParts, ref matchedParts);
            
            OnTagCreated.Invoke();
            return true;
        }
        
        private void FindBranchRecursive(GameplayTagAsset currTag, string[] tagParts, ref GameplayTagAsset branchAsset, ref int matchedParts)
        {
            if(matchedParts >= tagParts.Length) return;

            for (int i = 0; i < currTag.childTags.Count; i++)
            {
                if(currTag.childTags[i].tag != tagParts[matchedParts]) continue;
                matchedParts++;
                branchAsset = currTag.childTags[i];
                FindBranchRecursive(currTag.childTags[i], tagParts, ref branchAsset, ref matchedParts);
                break;
            }
        }

        private void CreateTagsRecursive(GameplayTagAsset branchAsset, string[] tagParts, ref int matchedParts)
        {
            if (matchedParts >= tagParts.Length) return;
            for (int i = 0; i < branchAsset.childTags.Count; i++)
            {
                if(branchAsset.childTags[i].tag != tagParts[matchedParts]) continue;
                matchedParts++;
                CreateTagsRecursive(branchAsset.childTags[i], tagParts, ref matchedParts);
                return;
            }
            
            // No Tag found, create the next one.
            var childAsset = ScriptableObject.CreateInstance<GameplayTagAsset>();
            childAsset.tag = tagParts[matchedParts];
            childAsset.parentTag = branchAsset;
            AssetDatabase.CreateAsset(childAsset, $"{newTagLocation}{branchAsset.name}.{tagParts[matchedParts]}.asset");
            GameplayTags.Add(childAsset);
            EditorUtility.SetDirty(this);
            branchAsset.childTags.Add(childAsset);
            EditorUtility.SetDirty(branchAsset);
            matchedParts++;

            if (matchedParts >= tagParts.Length) return;
            CreateTagsRecursive(childAsset, tagParts, ref matchedParts);
        }
#endif
    }
}