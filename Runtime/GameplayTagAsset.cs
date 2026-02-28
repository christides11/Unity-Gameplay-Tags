using System.Collections.Generic;
using UnityEngine;

namespace GameplayTags
{
    [CreateAssetMenu(menuName = "Gameplay Tags/Tag")]
    public partial class GameplayTagAsset : ScriptableObject
    {
        public GameplayTagAsset parentTag;
        public List<GameplayTagAsset> childTags = new List<GameplayTagAsset>();

        public string tag;
        [TextArea]
        public string description;

        public string GetFullTag()
        {
            List<string> parentStrings = new List<string>();
            GameplayTagAsset nextParentTag = parentTag;
            while (nextParentTag != null)
            {
                parentStrings.Add(nextParentTag.tag);
                nextParentTag = nextParentTag.parentTag;
            }
            parentStrings.Reverse();
            parentStrings.Add(tag);
            return string.Join(".", parentStrings);
        }
        
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(tag);
        }
    }
}