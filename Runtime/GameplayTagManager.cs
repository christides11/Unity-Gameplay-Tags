using System.Collections.Generic;
using UnityEngine;

namespace GameplayTags
{
    public partial class GameplayTagManager : MonoBehaviour
    {
        public Dictionary<string, GameplayTagAsset> tagToAsset = new Dictionary<string, GameplayTagAsset>();

        public GameplayTagAsset emptyTag;
        
        public virtual GameplayTagAsset RequestTagAsset(string name)
        {
            return tagToAsset.GetValueOrDefault(name, emptyTag);
        }
    }
}