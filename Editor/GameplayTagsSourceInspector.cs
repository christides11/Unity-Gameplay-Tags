using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameplayTags
{
    [CustomEditor(typeof(GameplayTagsSource))]
    public class GameplayTagsSourceInspector : Editor
    {
        public VisualTreeAsset m_InspectorUXML;

        private VisualElement rootElement;
        
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement myInspector = new VisualElement();
            rootElement = myInspector;
            
            if (m_InspectorUXML != null)
            {
                var tr = target as GameplayTagsSource;
                var so = new SerializedObject(target);
                VisualElement uxmlContent = m_InspectorUXML.CloneTree();
                var addTagButton = uxmlContent.Q<Button>(name: "AddNewTag");
                addTagButton.clicked += AttemptAddNewTag;
                var tagPathTextField = uxmlContent.Q<TextField>(name: "TagFolderLocation");
                tagPathTextField.BindProperty(so.FindProperty(nameof(GameplayTagsSource.newTagLocation)));
                myInspector.Add(uxmlContent);

                var tagelement = new GameplayTagsListElement();
                tagelement.style.flexGrow = 1;
                tagelement.style.height = 300;
                tagelement.style.marginLeft = -14;
                tagelement.Initialize(target as GameplayTagsSource);
                tagelement.OnAddSubTagRequest.AddListener(HandleAddSubTagRequest);
                tr.OnTagCreated.AddListener(tagelement.RefreshTagList);
                myInspector.Add(tagelement);
            }
            
            return myInspector;
        }

        private void AttemptAddNewTag()
        {
            var tf = rootElement.Q<TextField>(name: "TagName");
            (target as GameplayTagsSource).TryCreateTag(tf.value);
        }

        private void HandleAddSubTagRequest(GameplayTagAsset arg0)
        {
            var atf = rootElement.Q<Foldout>(name: "AddTagFoldout");
            atf.value = true;
            var tf = rootElement.Q<TextField>(name: "TagName");
            tf.value = arg0.GetFullTag() + ".";
        }
    }
}