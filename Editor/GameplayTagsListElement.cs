using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace GameplayTags
{
    public class GameplayTagsListElement : VisualElement
    {
        [SerializeField] private GameplayTagsSource tagsSource;
        [SerializeField] private List<TreeViewItemData<GameplayTagListDataObject.TagReferenceData>> treeViewItems;
        [SerializeField] private GameplayTagListDataObject dataObject;
        [SerializeField] private SerializedObject dataSerializedObject;

        public VisualTreeAsset m_listElementUXML;

        public bool showToggle = true;

        public UnityEvent<GameplayTagAsset> OnAddSubTagRequest = new UnityEvent<GameplayTagAsset>();
        
        public GameplayTagsListElement()
        {
            m_listElementUXML = Resources.Load<VisualTreeAsset>("GameplayTags/UXML_TagListItem");
        }

        public GameplayTagsListElement(bool initialize)
        {
            m_listElementUXML = Resources.Load<VisualTreeAsset>("GameplayTags/UXML_TagListItem");

            if (tagsSource == null)
            {
                string[] interactionGUIDs = AssetDatabase.FindAssets("t:GameplayTagsSource", new[] { "Assets" });

                foreach (var guid in interactionGUIDs)
                {
                    var gotAsset =
                        AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as GameplayTagsSource;
                    if (gotAsset == null) continue;
                    tagsSource = gotAsset;
                    break;
                }
            }

            if (tagsSource == null) return;
            Initialize(tagsSource);
        }

        public void Initialize(GameplayTagsSource tagsSource)
        {
            this.Clear();
            
            dataObject = ScriptableObject.CreateInstance<GameplayTagListDataObject>();
            
            Debug.Log("Tags List Initialize");
            this.tagsSource = tagsSource;
            BuildTagMap(tagsSource);
            dataSerializedObject = new SerializedObject(dataObject);
            BuildTagMapProperties();
            treeViewItems = new List<TreeViewItemData<GameplayTagListDataObject.TagReferenceData>>();

            var treeView = new TreeView();
            this.Add(treeView);
            
            for (int i = 0; i < dataObject.tagMap.Count; i++)
            {
                var subItemsData = BuildTreeRecursive(dataObject.tagMap[i]);
                var treeItemData = new TreeViewItemData<GameplayTagListDataObject.TagReferenceData>(dataObject.tagMap[i].id, dataObject.tagMap[i], subItemsData);
                treeViewItems.Add(treeItemData);
            }

            Func<VisualElement> makeItem = () =>
            {
                var e = new VisualElement();
                m_listElementUXML.CloneTree(e);
                ToolbarMenu tm = e.Q<ToolbarMenu>();
                tm.menu.AppendAction("Add Sub Tag", action =>
                {
                    var tv = this.Q<TreeView>();
                    var selfElement = e;
                    var item = tv.GetItemDataForIndex<GameplayTagListDataObject.TagReferenceData>(int.Parse(selfElement.name));
                    OnAddSubTagRequest.Invoke(item.tagAsset);
                });
                //tm.menu.AppendAction("Duplicate Tag", action => { });
                tm.menu.AppendSeparator();
                tm.menu.AppendAction("Select Exact Tag", action =>
                {
                    var tv = this.Q<TreeView>();
                    var selfElement = e;
                    var item = tv.GetItemDataForIndex<GameplayTagListDataObject.TagReferenceData>(int.Parse(selfElement.name));
                });
                tm.menu.AppendSeparator();
                tm.menu.AppendAction("Rename Tag", action => { });
                tm.menu.AppendAction("Copy Name to Clipboard", action => { });
                return e;
            };

            Action<VisualElement, int> bindItem = (e, i) =>
            {
                var item = treeView.GetItemDataForIndex<GameplayTagListDataObject.TagReferenceData>(i);
                var id = treeView.GetIdForIndex(i);
                e.name = i.ToString();
                var toggle = e.Q<Toggle>();
                var toggleButton = toggle.Q<VisualElement>("unity-checkmark");
                var toggleLabel = toggle.Q<Label>();
                toggleButton.style.display = new StyleEnum<DisplayStyle>(showToggle ? DisplayStyle.Flex : DisplayStyle.None);
                if (showToggle == false) toggleLabel.style.marginLeft = 0;
                toggle.name = i.ToString();
                toggle.label = $"{item.tagAsset.tag}";
                toggle.UnregisterValueChangedCallback(WhenToggleValueChanged);
                toggle.RegisterValueChangedCallback(WhenToggleValueChanged);
                
                toggle.Unbind();
                toggle.BindProperty(item.sp.FindPropertyRelative("selected"));
            };

            treeView.SetRootItems(treeViewItems);
            treeView.makeItem = makeItem;
            treeView.bindItem = bindItem;
            treeView.selectionType = SelectionType.None;
            treeView.Rebuild();

            // Callback invoked when the user double clicks an item
            treeView.itemsChosen += (selectedItems) =>
            {
                Debug.Log("Items chosen: " + string.Join(", ", selectedItems));
            };

            // Callback invoked when the user changes the selection inside the TreeView
            treeView.selectedIndicesChanged += (selectedIndices) =>
            {
                var log = "IDs selected: ";
                foreach (var index in selectedIndices)
                {
                    log += $"{treeView.GetIdForIndex(index)}, ";
                }

                Debug.Log(log.TrimEnd(',', ' '));
            };
        }

        public void RefreshTagList()
        {
            Initialize(tagsSource);
        }
        
        private void WhenToggleValueChanged(ChangeEvent<bool> evt)
        {
            var treeView = this.Q<TreeView>();
            var index = int.Parse((evt.target as Toggle).name);
            var item = treeView.GetItemDataForIndex<GameplayTagListDataObject.TagReferenceData>(index);
            if (evt.newValue == true)
            {
                item.SelectToRoot();
            }
            else
            {
                item.DeselectChildren();
            }
            
            dataObject.EvaluateSelectedTags();
        }
        
        private List<TreeViewItemData<GameplayTagListDataObject.TagReferenceData>> BuildTreeRecursive(GameplayTagListDataObject.TagReferenceData trd)
        {
            if (trd.tagAsset == null || trd.tagAsset.childTags == null || trd.tagAsset.childTags.Count == 0)
                return null;
            var subItemsData = new List<TreeViewItemData<GameplayTagListDataObject.TagReferenceData>>();
            for (var w = 0; w < trd.childTags.Length; w++)
            {
                subItemsData.Add(new TreeViewItemData<GameplayTagListDataObject.TagReferenceData>(trd.childTags[w].id, trd.childTags[w],
                    BuildTreeRecursive(trd.childTags[w])));
            }

            return subItemsData;
        }

        private void BuildTagMap(GameplayTagsSource tagsSource)
        {
            dataObject.tagMap = new List<GameplayTagListDataObject.TagReferenceData>();

            int idCnt = 0;
            var baseTags = tagsSource.GetBaseTags();
            for (int i = 0; i < baseTags.Count; i++)
            {
                var trd = new GameplayTagListDataObject.TagReferenceData()
                {
                    id = idCnt++,
                    selected = false,
                    tagAsset = baseTags[i],
                    parent = null,
                    childTags = new GameplayTagListDataObject.TagReferenceData[baseTags[i].childTags.Count]
                };
                dataObject.tagMap.Add(trd);
                
                for (int w = 0; w < baseTags[i].childTags.Count; w++)
                {
                    trd.childTags[w] = BuildChildMapRecursive(baseTags[i].childTags[w], ref idCnt);
                    trd.childTags[w].parent = trd;
                }
            }
        }

        private GameplayTagListDataObject.TagReferenceData BuildChildMapRecursive(GameplayTagAsset tagAsset, ref int idCnt)
        {
            var trd = new GameplayTagListDataObject.TagReferenceData()
            {
                id = idCnt++,
                selected = false,
                tagAsset = tagAsset,
                childTags = new GameplayTagListDataObject.TagReferenceData[tagAsset.childTags.Count]
            };

            for (int w = 0; w < tagAsset.childTags.Count; w++)
            {
                if (tagAsset.childTags[w] == null) continue;
                trd.childTags[w] = BuildChildMapRecursive(tagAsset.childTags[w], ref idCnt);
                trd.childTags[w].parent = trd;
            }

            return trd;
        }

        private void BuildTagMapProperties()
        {
            for (int i = 0; i < dataObject.tagMap.Count; i++)
            {
                var trd = dataObject.tagMap[i];
                var sp = dataSerializedObject.FindProperty("tagMap").GetArrayElementAtIndex(i);
                trd.sp = sp;
                BuildTagMapPathsRecursive(trd, sp);
            }
        }

        private void BuildTagMapPathsRecursive(GameplayTagListDataObject.TagReferenceData dataObjectTag, SerializedProperty sp)
        {
            dataObjectTag.propertyPath = sp.propertyPath;

            for (int i = 0; i < dataObjectTag.childTags.Length; i++)
            {
                if(dataObjectTag.childTags[i] == null) continue;
                var childSp = sp.FindPropertyRelative("childTags").GetArrayElementAtIndex(i);
                dataObjectTag.childTags[i].propertyPath = childSp.propertyPath;
                dataObjectTag.childTags[i].sp = childSp;
                BuildTagMapPathsRecursive(dataObjectTag.childTags[i], childSp);
            }
        }
    }
}