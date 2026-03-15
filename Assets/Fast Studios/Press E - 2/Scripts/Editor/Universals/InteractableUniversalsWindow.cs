#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FastStudios.EditorTools
{
    public class InteractableUniversalsWindow : EditorWindow
    {
        [Header("UXML")]
        [SerializeField] public VisualTreeAsset visualTree;
        [SerializeField] public VisualTreeAsset InteractableUXML;
        [SerializeField] public VisualTreeAsset UIprefabUXML;
        [SerializeField] public VisualTreeAsset KeyUXML;
        [SerializeField] public VisualTreeAsset PositionLerpUXML;
        [SerializeField] public VisualTreeAsset RotationLerpUXML;
        [SerializeField] public VisualTreeAsset ScaleLerpUXML;
        [SerializeField] public VisualTreeAsset TransformLerpUXML;
        [SerializeField] public StyleSheet universalsUSS;
        bool _isLerpsMode;

        SerializedObject _so;
        VisualElement _rightCol;
        ScrollView _list;
        [HideInInspector] public VisualTreeAsset currentUXML;

        readonly Dictionary<string, (string top, List<string> chain)> _layoutIndex = new();
        readonly Dictionary<string, List<string>> _enumNamesCache = new(StringComparer.Ordinal);
        readonly Dictionary<string, string> _displayLabelByPath = new(StringComparer.Ordinal);
        readonly Dictionary<string, string> _foldoutLabelByName = new(StringComparer.Ordinal);
        readonly Dictionary<string, List<string>> _orderByParentKey = new(StringComparer.Ordinal);

        static string PrefKey(string k) => $"FS.Universals.Foldout.{k}";
        bool GetFoldout(string key, bool deflt) => EditorPrefs.GetBool(PrefKey(key), deflt);
        void SetFoldout(string key, bool open) => EditorPrefs.SetBool(PrefKey(key), open);
        string GetDisplayLabel(string path) => (!string.IsNullOrEmpty(path) && _displayLabelByPath.TryGetValue(path, out var s) && !string.IsNullOrEmpty(s)) ? s : path;

        [MenuItem("Tools/Fast Studios/Press E PRO/Universals Window", priority = 20)]
        public static void Open()
        {
            var w = GetWindow<InteractableUniversalsWindow>("Interactable Universals");
            w.minSize = new Vector2(1000, 620);
            w.Show();
        }

        void CreateGUI()
        {
            if (visualTree == null) visualTree = Resources.Load<VisualTreeAsset>("FastStudios/UXML/UniversalsUXML");
            if (visualTree == null) return;

            var root = visualTree.CloneTree();
            if (universalsUSS) root.styleSheets.Add(universalsUSS);
            rootVisualElement.Add(root);

            var leftCol = root.Q<VisualElement>("LeftCol");
            _rightCol = root.Q<VisualElement>("RightCol");
            if (_rightCol == null) { _rightCol = new VisualElement { name = "RightCol" }; root.Add(_rightCol); }

            _rightCol.Clear();
            _list = new ScrollView(ScrollViewMode.Vertical) { name = "UniversalsList" };
            _list.style.flexGrow = 1;
            _rightCol.Add(_list);

            root.Q<Button>("InteractableButton")?.RegisterCallback<ClickEvent>(_ =>
            {
                _isLerpsMode = false;
                currentUXML = InteractableUXML;
                BuildGroupedView(currentUXML);
            });

            root.Q<Button>("UIPrefabButton")?.RegisterCallback<ClickEvent>(_ =>
            {
                _isLerpsMode = false;
                currentUXML = UIprefabUXML;
                BuildGroupedView(currentUXML);
            });

            root.Q<Button>("KeyButton")?.RegisterCallback<ClickEvent>(_ =>
            {
                _isLerpsMode = false;
                currentUXML = KeyUXML;
                BuildGroupedView(currentUXML);
            });

            root.Q<Button>("LerpsButton")?.RegisterCallback<ClickEvent>(_ =>
            {
                _isLerpsMode = true;
                BuildCompositeLerps();
            });

            root.Q<Button>("ResetButton")?.RegisterCallback<ClickEvent>(_ => Rebootstrap());

            if (currentUXML == null) currentUXML = InteractableUXML;

            _so = new SerializedObject(InteractableUniversalsSO.Instance);
            BuildGroupedView(currentUXML);

            InteractableUniversalsSO.OnChanged -= OnUniversalsChanged;
            InteractableUniversalsSO.OnChanged += OnUniversalsChanged;
        }

        void OnDisable()
        {
            InteractableUniversalsSO.OnChanged -= OnUniversalsChanged;
        }

        void OnUniversalsChanged()
        {
            if (_isLerpsMode) BuildCompositeLerps();
            else BuildGroupedView(currentUXML);
        } 

        void Rebootstrap()
        {
            InteractableUniversalsSO.Instance.entries.Clear();

            InteractableUniversalsSO.GetAllVariablesData();

            InteractableUniversalsSO.NotifyChanged();
        }

        Type GetUXMLType(VisualTreeAsset uxml)
        {
            if (uxml == InteractableUXML) return typeof(Interactable);
            else if (uxml == UIprefabUXML) return typeof(UIPrefab);
            else if (uxml == KeyUXML) return typeof(Key);

            else if (uxml == PositionLerpUXML) return typeof(PositionLerp);
            else if (uxml == RotationLerpUXML) return typeof(RotationLerp);
            else if (uxml == ScaleLerpUXML) return typeof(ScaleLerp);
            else if (uxml == TransformLerpUXML) return typeof(TransformLerp);

            return default;
        }

        void BuildGroupedView(VisualTreeAsset uxml)
        {
            currentUXML = uxml;

            _list.Clear();
            _so = new SerializedObject(InteractableUniversalsSO.Instance);

            var entries = _so.FindProperty("entries");
            if (entries == null || entries.arraySize == 0)
            {
                _list.Add(new HelpBox("Nenhuma entrada universal encontrada.", HelpBoxMessageType.Info));
                return;
            }

            BuildLayoutIndexFromUXML(uxml, entries);

            string ChainKey(string top, List<string> chain) => $"{top}/{string.Join("/", chain)}";

            var groupCount = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < entries.arraySize; i++)
            {
                var e = entries.GetArrayElementAtIndex(i);
                var path = e.FindPropertyRelative("propertyPath").stringValue;
                if (!string.Equals(e.FindPropertyRelative("componentTypeName").stringValue, GetUXMLType(uxml).FullName, StringComparison.Ordinal)) continue;

                (string top, List<string> chain) grp;
                if (!_layoutIndex.TryGetValue(path, out grp))
                    grp = ("Misc", new List<string> { "General" });

                var chain2 = new List<string>(grp.chain);
                chain2.RemoveAll(n => string.Equals(n, "General", StringComparison.Ordinal));
                var key = ChainKey(grp.top, chain2);
                if (!groupCount.ContainsKey(key)) groupCount[key] = 0;
                groupCount[key]++;
            }

            var generalsByTop = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            var groupsByKey = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            var childrenByParent = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var usedTops = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < entries.arraySize; i++)
            {
                var e = entries.GetArrayElementAtIndex(i);
                var path = e.FindPropertyRelative("propertyPath").stringValue;
                if (!string.Equals(e.FindPropertyRelative("componentTypeName").stringValue, GetUXMLType(uxml).FullName, StringComparison.Ordinal)) continue;

                (string top, List<string> chain) grp;
                if (!_layoutIndex.TryGetValue(path, out grp))
                    grp = ("Misc", new List<string> { "General" });

                usedTops.Add(grp.top);

                var eff = new List<string>(grp.chain);
                eff.RemoveAll(n => string.Equals(n, "General", StringComparison.Ordinal));

                while (eff.Count >= 2)
                {
                    var fullKey = ChainKey(grp.top, eff);
                    if (groupCount.TryGetValue(fullKey, out var cnt) && cnt == 1)
                        eff.RemoveAt(eff.Count - 1);
                    else
                        break;
                }

                if (eff.Count == 0)
                {
                    if (!generalsByTop.TryGetValue(grp.top, out var list)) generalsByTop[grp.top] = list = new List<int>();
                    list.Add(i);
                }
                else
                {
                    var key = ChainKey(grp.top, eff);
                    if (!groupsByKey.TryGetValue(key, out var list)) groupsByKey[key] = list = new List<int>();
                    list.Add(i);

                    var parentChain = eff.Take(eff.Count - 1).ToList();
                    var parentKey = ChainKey(grp.top, parentChain);
                    if (!childrenByParent.TryGetValue(parentKey, out var set)) childrenByParent[parentKey] = set = new HashSet<string>(StringComparer.Ordinal);
                    set.Add(eff.Last());
                }
            }

            var sortedTops = usedTops
                .OrderBy(t => TopSortKey(t).tier)
                .ThenBy(t => TopSortKey(t).pref)
                .ThenBy(t => TopSortKey(t).name, StringComparer.Ordinal)
                .ToList();

            var topCache = new Dictionary<string, VisualElement>(StringComparer.Ordinal);
            var containerCache = new Dictionary<string, VisualElement>(StringComparer.Ordinal);

            VisualElement EnsureTop(string top)
            {
                if (topCache.TryGetValue(top, out var ve)) return ve;
                var fold = CreateFoldoutFromTemplate(top, depth: 0);
                _list.Add(fold.root);
                topCache[top] = fold.container;
                return fold.container;
            }

            VisualElement EnsureNested(string top, List<string> chain)
            {
                var parent = EnsureTop(top);
                string key = top;

                for (int i = 0; i < chain.Count; i++)
                {
                    string level = chain[i];
                    key = $"{key}/{level}";
                    if (!containerCache.TryGetValue(key, out var cont))
                    {
                        var fold = CreateFoldoutFromTemplate(level, depth: i + 1);
                        parent.Add(fold.root);
                        cont = fold.container;
                        containerCache[key] = cont;
                    }
                    parent = cont;
                }
                return parent;
            }

            foreach (var top in sortedTops)
            {
                var parent = EnsureTop(top);
                if (generalsByTop.TryGetValue(top, out var idxs))
                {
                    foreach (var idx in idxs)
                    {
                        var e = entries.GetArrayElementAtIndex(idx);
                        parent.Add(MakeEntryElement(e));
                    }
                }
            }

            foreach (var top in sortedTops)
            {
                RenderChildren(top, new List<string>(), EnsureTop(top));
            }

            void RenderChildren(string top, List<string> parentChain, VisualElement parentVE)
            {
                var parentKey = ChainKey(top, parentChain);
                if (!childrenByParent.TryGetValue(parentKey, out var set)) return;

                var desired = new List<string>();

                if (_orderByParentKey.TryGetValue(parentKey, out var fromUxml))
                    desired.AddRange(fromUxml.Where(n => set.Contains(n)));

                desired.AddRange(set.Except(desired).OrderBy(n => NicifyCamel(n), StringComparer.Ordinal));

                foreach (var child in desired)
                {
                    var chain = new List<string>(parentChain); chain.Add(child);
                    var leaf = EnsureNested(top, chain);

                    var key = ChainKey(top, chain);
                    if (groupsByKey.TryGetValue(key, out var idxs))
                    {
                        foreach (var idx in idxs)
                        {
                            var e = entries.GetArrayElementAtIndex(idx);
                            leaf.Add(MakeEntryElement(e));
                        }
                    }

                    RenderChildren(top, chain, leaf);
                }
            }

            FSEditorUI.AutoFoldouts(
                _list, _so,
                (key, deflt) => GetFoldout(key, deflt),
                (key, open) => SetFoldout(key, open),
                FSEditorUI.HiddenClass
            );

            _list.Bind(_so);
        }

        void BuildLayoutIndexFromUXML(VisualTreeAsset uxml, SerializedProperty entries)
        {
            _layoutIndex.Clear();
            if (uxml == null) return;

            var probe = uxml.CloneTree();
            string ChainKey(string top, List<string> chain) => $"{top}/{string.Join("/", chain)}";

            _orderByParentKey.Clear();

            var tabs = probe.Query<VisualElement>().ToList()
                .Where(v => !string.IsNullOrEmpty(v.name) &&
                            (v.name.EndsWith("Tab", StringComparison.Ordinal) || v.name.EndsWith("Tabs", StringComparison.Ordinal)));

            foreach (var t in tabs)
            {
                var topName = TrimTabSuffix(t.name);
                RecordOrderFromTab(t, topName);
            }

            void RecordOrderFromTab(VisualElement tabRoot, string top)
            {
                void Walk(VisualElement node, List<string> chain)
                {
                    foreach (var ch in node.Children())
                    {
                        var nm = ch.name;
                        if (!string.IsNullOrEmpty(nm) && nm.EndsWith("Container", StringComparison.Ordinal))
                        {
                            var baseName = TrimSuffix(nm, "Container");
                            var parentKey = ChainKey(top, chain);

                            if (!_orderByParentKey.TryGetValue(parentKey, out var list))
                                _orderByParentKey[parentKey] = list = new List<string>();
                            if (!list.Contains(baseName)) list.Add(baseName);

                            var next = new List<string>(chain); next.Add(baseName);
                            Walk(ch, next);
                        }
                        else
                        {
                            Walk(ch, chain);
                        }
                    }
                }
                Walk(tabRoot, new List<string>());
            }

            _foldoutLabelByName.Clear();
            var allHeaders = probe.Query<VisualElement>().ToList()
                .Where(v => !string.IsNullOrEmpty(v.name) && v.name.EndsWith("Foldout", StringComparison.Ordinal));

            foreach (var h in allHeaders)
            {
                var baseName = TrimSuffix(h.name, "Foldout");
                var lbl = h.Q<Label>() ?? h.Children().OfType<Label>().FirstOrDefault();
                string label = lbl.text;

                if (lbl.name.EndsWith("OVR")) label = NicifyCamel(baseName);
                if (!string.IsNullOrEmpty(baseName) && lbl != null && !string.IsNullOrEmpty(label))
                    _foldoutLabelByName[baseName] = label;
            }

            for (int i = 0; i < entries.arraySize; i++)
            {
                var e = entries.GetArrayElementAtIndex(i);
                var path = e.FindPropertyRelative("propertyPath").stringValue;
                if (string.IsNullOrEmpty(path)) continue;

                var fieldVE = probe.Q<VisualElement>(path);
                if (fieldVE == null) continue;

                var chain = new List<string>();
                string topName = "Main";

                var cur = fieldVE;
                while (cur != null)
                {
                    if (!string.IsNullOrEmpty(cur.name) &&
                        (cur.name.EndsWith("Tab", StringComparison.Ordinal) || cur.name.EndsWith("Tabs", StringComparison.Ordinal)))
                    {
                        topName = TrimTabSuffix(cur.name);
                        break;
                    }

                    if (!string.IsNullOrEmpty(cur.name) && cur.name.EndsWith("Container", StringComparison.Ordinal))
                    {
                        var nm = TrimSuffix(cur.name, "Container");
                        if (!string.IsNullOrEmpty(nm)) chain.Add(nm);
                    }

                    cur = cur.parent;
                }

                chain.Reverse();
                if (chain.Count == 0) chain.Add("General");
                _layoutIndex[path] = (topName, chain);

                _displayLabelByPath[path] = ExtractDisplayLabel(fieldVE, path);
                _layoutIndex[path] = (topName, chain);
            }
        }

        void BuildCompositeLerps()
        {
            currentUXML = null;

            _list.Clear();
            _so = new SerializedObject(InteractableUniversalsSO.Instance);

            var entries = _so.FindProperty("entries");
            if (entries == null || entries.arraySize == 0)
            {
                _list.Add(new HelpBox("Nenhuma entrada universal encontrada.", HelpBoxMessageType.Info));
                return;
            }

            static string PrefKeyPrefix(string displayName) => displayName.Replace(" ", "").Replace("/", "_");

            BuildLerpBlock(typeof(PositionLerp), PositionLerpUXML, "Position Lerp", PrefKeyPrefix("Position Lerp"), entries);
            BuildLerpBlock(typeof(RotationLerp), RotationLerpUXML, "Rotation Lerp", PrefKeyPrefix("Rotation Lerp"), entries);
            BuildLerpBlock(typeof(ScaleLerp), ScaleLerpUXML, "Scale Lerp", PrefKeyPrefix("Scale Lerp"), entries);
            BuildLerpBlock(typeof(TransformLerp), TransformLerpUXML, "Transform Lerp", PrefKeyPrefix("Transform Lerp"), entries);

            FSEditorUI.AutoFoldouts(
                _list, _so,
                (key, deflt) => GetFoldout(key, deflt),
                (key, open) => SetFoldout(key, open),
                FSEditorUI.HiddenClass
            );

            _list.Bind(_so);
        }

        void BuildLerpBlock(Type componentType, VisualTreeAsset uxml, string displayName, string keyPrefix, SerializedProperty entries)
        {
            var (rootFold, scriptContainer) = CreateFoldoutFromTemplate(displayName, depth: 0);
            _list.Add(rootFold);

            if (uxml == null)
            {
                scriptContainer.Add(new HelpBox($"{displayName}: UXML não atribuído.", HelpBoxMessageType.Warning));
                return;
            }

            BuildLayoutIndexFromUXML(uxml, entries);
            var orderByParentKeyLocal = new Dictionary<string, List<string>>(_orderByParentKey, StringComparer.Ordinal);

            string ChainKey(string top, List<string> chain) => $"{top}/{string.Join("/", chain)}";

            var groupCount = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < entries.arraySize; i++)
            {
                var e = entries.GetArrayElementAtIndex(i);
                if (!string.Equals(e.FindPropertyRelative("componentTypeName").stringValue, componentType.FullName, StringComparison.Ordinal))
                    continue;

                var path = e.FindPropertyRelative("propertyPath").stringValue;
                (string top, List<string> chain) grp;
                if (!_layoutIndex.TryGetValue(path, out grp))
                    grp = ("Misc", new List<string> { "General" });

                var chain2 = new List<string>(grp.chain);
                chain2.RemoveAll(n => string.Equals(n, "General", StringComparison.Ordinal));

                while (chain2.Count >= 2)
                {
                    var fullKey = ChainKey(grp.top, chain2);
                    if (!groupCount.ContainsKey(fullKey)) groupCount[fullKey] = 0;
                    groupCount[fullKey]++;
                    chain2.RemoveAt(chain2.Count - 1);
                }
            }

            var generalsByTop = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            var groupsByKey = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            var childrenByParent = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var usedTops = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < entries.arraySize; i++)
            {
                var e = entries.GetArrayElementAtIndex(i);
                if (!string.Equals(e.FindPropertyRelative("componentTypeName").stringValue, componentType.FullName, StringComparison.Ordinal))
                    continue;

                var path = e.FindPropertyRelative("propertyPath").stringValue;
                (string top, List<string> chain) grp;
                if (!_layoutIndex.TryGetValue(path, out grp))
                    grp = ("Misc", new List<string> { "General" });

                usedTops.Add(grp.top);

                var eff = new List<string>(grp.chain);
                eff.RemoveAll(n => string.Equals(n, "General", StringComparison.Ordinal));

                while (eff.Count >= 2)
                {
                    var fullKey = ChainKey(grp.top, eff);
                    if (groupCount.TryGetValue(fullKey, out var cnt) && cnt == 1)
                        eff.RemoveAt(eff.Count - 1);
                    else break;
                }

                if (eff.Count == 0)
                {
                    if (!generalsByTop.TryGetValue(grp.top, out var list)) generalsByTop[grp.top] = list = new List<int>();
                    list.Add(i);
                }
                else
                {
                    var parentChain = eff.Take(eff.Count - 1).ToList();
                    var parentKey = ChainKey(grp.top, parentChain);
                    var leaf = eff[eff.Count - 1];

                    if (!childrenByParent.TryGetValue(parentKey, out var set)) childrenByParent[parentKey] = set = new HashSet<string>(StringComparer.Ordinal);
                    set.Add(leaf);

                    var listKey = ChainKey(grp.top, eff);
                    if (!groupsByKey.TryGetValue(listKey, out var vec)) groupsByKey[listKey] = vec = new List<int>();
                    vec.Add(i);
                }
            }

            var sortedTops = usedTops
                .OrderBy(t => TopSortKey(t).tier)
                .ThenBy(t => TopSortKey(t).pref)
                .ThenBy(t => TopSortKey(t).name, StringComparer.Ordinal)
                .ToList();

            var topCache = new Dictionary<string, VisualElement>(StringComparer.Ordinal);
            var containerCache = new Dictionary<string, VisualElement>(StringComparer.Ordinal);

            VisualElement EnsureTop(string top)
            {
                if (topCache.TryGetValue(top, out var ve)) return ve;

                var prefixed = $"{keyPrefix}__{top}";
                _foldoutLabelByName[prefixed] = NicifyCamel(top);

                var fold = CreateFoldoutFromTemplate(prefixed, depth: 1);
                scriptContainer.Add(fold.root);
                topCache[top] = fold.container;
                return fold.container;
            }

            VisualElement EnsureChain(string top, List<string> chain)
            {
                var key = ChainKey(top, chain);
                if (containerCache.TryGetValue(key, out var ve)) return ve;

                VisualElement parent = EnsureTop(top);
                for (int i = 0; i < chain.Count; i++)
                {
                    var level = chain[i];
                    var prefixed = $"{keyPrefix}__{level}";
                    _foldoutLabelByName[prefixed] = NicifyCamel(level);

                    var fold = CreateFoldoutFromTemplate(prefixed, depth: i + 2);
                    parent.Add(fold.root);
                    parent = fold.container;
                }

                containerCache[key] = parent;
                return parent;
            }

            foreach (var top in sortedTops)
            {
                var parent = EnsureTop(top);
                if (generalsByTop.TryGetValue(top, out var idxs))
                {
                    foreach (var idx in idxs)
                    {
                        var e = entries.GetArrayElementAtIndex(idx);
                        parent.Add(MakeEntryElement(e));
                    }
                }
            }

            void RenderChildren(string top, List<string> parentChain, VisualElement parentVE)
            {
                var parentKey = ChainKey(top, parentChain);
                if (!childrenByParent.TryGetValue(parentKey, out var set)) return;

                var desired = new List<string>();
                if (orderByParentKeyLocal.TryGetValue(parentKey, out var fromUxml))
                    desired.AddRange(fromUxml.Where(n => set.Contains(n)));

                desired.AddRange(set.Except(desired).OrderBy(n => NicifyCamel(n), StringComparer.Ordinal));

                foreach (var child in desired)
                {
                    var chain = new List<string>(parentChain); chain.Add(child);
                    var leafKey = ChainKey(top, chain);

                    var leafVE = EnsureChain(top, chain);

                    if (groupsByKey.TryGetValue(leafKey, out var idxs))
                    {
                        foreach (var idx in idxs)
                        {
                            var e = entries.GetArrayElementAtIndex(idx);
                            leafVE.Add(MakeEntryElement(e));
                        }
                    }

                    RenderChildren(top, chain, leafVE);
                }
            }

            foreach (var top in sortedTops)
                RenderChildren(top, new List<string>(), EnsureTop(top));
        }




        static string TrimSuffix(string s, string suf)
            => s != null && s.EndsWith(suf, StringComparison.Ordinal) ? s.Substring(0, s.Length - suf.Length) : s;

        static string TrimTabSuffix(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (s.EndsWith("Tabs", StringComparison.Ordinal)) return s.Substring(0, s.Length - 4);
            if (s.EndsWith("Tab", StringComparison.Ordinal)) return s.Substring(0, s.Length - 3);
            return s;
        }

        static readonly string[] kPreferredTopOrder = { "Main", "Hold", "Grab", "Drag", "Inspection" };
        static (int tier, int pref, string name) TopSortKey(string top)
        {
            if (string.Equals(top, "Misc", StringComparison.Ordinal)) return (2, int.MaxValue, top);
            int pref = Array.IndexOf(kPreferredTopOrder, top);
            if (pref >= 0) return (0, pref, top);
            return (1, int.MaxValue, top);
        }

        (VisualElement root, VisualElement container) CreateFoldoutFromTemplate(string name, int depth)
        {
            var vta = visualTree ?? Resources.Load<VisualTreeAsset>("FastStudios/Data/UniversalsUXML");
            var tmpRoot = vta.CloneTree();
            var tpl = tmpRoot.Q<VisualElement>("FoldoutTemplate");
            if (tpl == null)
            {
                var fbRoot = new VisualElement { name = name };
                var header2 = new VisualElement { name = $"{name}Foldout" };
                header2.style.flexDirection = FlexDirection.Row;
                var title = new Label(ComputeFoldoutTitle(name, depth)) { style = { unityFontStyleAndWeight = FontStyle.Bold } };
                header2.Add(title);
                var cont2 = new VisualElement { name = $"{name}Container" };
                fbRoot.Add(header2); fbRoot.Add(cont2);
                ApplyDepthBackground(cont2, depth);
                return (fbRoot, cont2);
            }

            tpl.RemoveFromHierarchy();
            tpl.RemoveFromClassList("Hide");

            var header = tpl.Q<VisualElement>("FoldoutTemplateFoldout") ?? (tpl.childCount > 0 ? tpl.ElementAt(0) as VisualElement : null);
            var label = header?.Q<Label>("FoldoutLabel") ?? header?.Q<Label>();
            var cont = tpl.Q<VisualElement>("FoldoutTemplateContainer") ?? (tpl.childCount > 1 ? tpl.ElementAt(1) as VisualElement : new VisualElement());

            tpl.name = name;
            if (header != null) header.name = $"{name}Foldout";
            if (label != null) label.text = ComputeFoldoutTitle(name, depth);
            if (cont != null) cont.name = $"{name}Container";

            ApplyDepthBackground(cont, depth);
            return (tpl, cont ?? new VisualElement { name = $"{name}Container" });
        }

        void ApplyDepthBackground(VisualElement container, int depth)
        {
            if (container == null) return;
            if (depth <= 1)
            {
                container.style.backgroundColor = new StyleColor(new Color32(0x38, 0x38, 0x38, 0xFF));
            }
        }

        List<string> GetEnumNamesForPath(string componentTypeName, string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath) || string.IsNullOrEmpty(componentTypeName)) return null;

            string cacheKey = componentTypeName + "|" + propertyPath;
            if (_enumNamesCache.TryGetValue(cacheKey, out var cached)) return cached;

            static Type ResolveType(string name)
            {
                var t = Type.GetType(name);
                if (t != null) return t;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    t = asm.GetType(name);
                    if (t != null) return t;
                }
                return null;
            }

            var tComp = ResolveType(componentTypeName);
            if (tComp == null) return null;

            var go = new GameObject("~EnumProbe~");
            try
            {
                var comp = go.AddComponent(tComp) as Component;
                var so = new SerializedObject(comp);
                var sp = so.FindProperty(propertyPath);
                if (sp != null && sp.propertyType == SerializedPropertyType.Enum)
                {
                    var names = (sp.enumDisplayNames != null && sp.enumDisplayNames.Length > 0)
                                ? sp.enumDisplayNames.ToList()
                                : (sp.enumNames?.ToList() ?? new List<string>());
                    _enumNamesCache[cacheKey] = names;
                    return names;
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }

            return null;
        }

        static int ClampIndex(int idx, int count) => count <= 0 ? 0 : Mathf.Clamp(idx, 0, count - 1);

        VisualElement MakeEntryElement(SerializedProperty e)
        {
            var pathProp = e.FindPropertyRelative("propertyPath");
            var typeProp = e.FindPropertyRelative("valueType");
            var loadProp = e.FindPropertyRelative("loadOnCreation");

            var path = pathProp.stringValue;
            var vt = (UniversalValueType)typeProp.enumValueIndex;

            string display = GetDisplayLabel(path);
            if (IsMiscRoot(path) || string.Equals(display, path, StringComparison.Ordinal))
                display = NicifyTitle(path);

            var card = new VisualElement();
            card.AddToClassList("fs-entry");
            card.style.marginTop = 4;
            card.style.marginBottom = 4;
            card.style.paddingLeft = 12;
            card.style.paddingRight = 12;
            card.style.paddingTop = 6;
            card.style.paddingBottom = 6;
            card.style.borderBottomWidth = 0;

            var row = new VisualElement { name = "EntryRow" };
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            card.Add(row);

            var lbl = new Label(display);
            lbl.style.unityFontStyleAndWeight = FontStyle.Normal;
            lbl.style.flexShrink = 0;
            row.Add(lbl);

            var valueVE = MakeValueField(e, vt, path);
            valueVE.style.flexGrow = 1;
            valueVE.style.marginLeft = 8;
            row.Add(valueVE);

            var loc = new Toggle("Load on creation");
            loc.style.marginLeft = 8;
            loc.style.flexShrink = 0;
            loc.BindProperty(loadProp);
            row.Add(loc);

            return card;
        }

        VisualElement MakeValueField(SerializedProperty e, UniversalValueType vt, string path)
        {
            PropertyField PF(string childPropName)
            {
                var pf = new PropertyField(e.FindPropertyRelative(childPropName), string.Empty);
                pf.RegisterCallback<SerializedPropertyChangeEvent>(_ =>
                {
                    _so.ApplyModifiedProperties();
                    UnityEditor.EditorUtility.SetDirty(InteractableUniversalsSO.Instance);
                    InteractableUniversalsSO.NotifyEntryValueChanged(path);
                });
                return pf;
            }

            switch (vt)
            {
                case UniversalValueType.Bool: return PF("boolValue");
                case UniversalValueType.Int: return PF("intValue");
                case UniversalValueType.Float: return PF("floatValue");
                case UniversalValueType.String: return PF("stringValue");
                case UniversalValueType.Color: return PF("colorValue");
                case UniversalValueType.Vector2: return PF("v2");
                case UniversalValueType.Vector3: return PF("v3");
                case UniversalValueType.Vector2Int: return PF("v2i");
                case UniversalValueType.Vector3Int: return PF("v3i");

                case UniversalValueType.Enum:
                    {
                        var pathProp = e.FindPropertyRelative("propertyPath");
                        var idxProp = e.FindPropertyRelative("enumValueIndex");
                        var typeProp = e.FindPropertyRelative("componentTypeName");

                        var names = GetEnumNamesForPath(typeProp?.stringValue, pathProp?.stringValue);

                        if (names != null && names.Count > 0)
                        {
                            var dropdown = new DropdownField(string.Empty, names, ClampIndex(idxProp.intValue, names.Count));
                            dropdown.RegisterValueChangedCallback(_ =>
                            {
                                idxProp.intValue = dropdown.index; _so.ApplyModifiedProperties();
                                UnityEditor.EditorUtility.SetDirty(InteractableUniversalsSO.Instance);
                                InteractableUniversalsSO.NotifyEntryValueChanged(path);
                            });
                            dropdown.TrackPropertyValue(idxProp, _ =>
                            {
                                dropdown.index = ClampIndex(idxProp.intValue, names.Count);
                            });
                            return dropdown;
                        }
                        else
                        {
                            return PF("enumValueIndex");
                        }
                    }

                case UniversalValueType.AnimationCurve: return PF("curve");
                case UniversalValueType.Quaternion:
                    {
                        var qProp = e.FindPropertyRelative("quaternion");
                        var eulerField = new Vector3Field(string.Empty);
                        eulerField.tooltip = "Euler Angles (degrees)";

                        void SyncFromProp()
                        {
                            var q = qProp.quaternionValue;
                            eulerField.SetValueWithoutNotify(q.eulerAngles);
                        }
                        SyncFromProp();

                        eulerField.RegisterValueChangedCallback(evt =>
                        {
                            var euler = evt.newValue;
                            qProp.quaternionValue = Quaternion.Euler(euler);
                            _so.ApplyModifiedProperties();
                            UnityEditor.EditorUtility.SetDirty(InteractableUniversalsSO.Instance);
                            InteractableUniversalsSO.NotifyEntryValueChanged(path);
                        });

                        eulerField.TrackPropertyValue(qProp, _ => SyncFromProp());

                        return eulerField;
                    }

                case UniversalValueType.LayerMask:
                    {
                        return new IMGUIContainer(() =>
                        {
                            var lm = e.FindPropertyRelative("layerMask");
                            EditorGUI.BeginChangeCheck();
                            int v = EditorGUILayout.LayerField(GUIContent.none, lm.intValue);
                            if (EditorGUI.EndChangeCheck()) { lm.intValue = v; _so.ApplyModifiedProperties(); }
                        });
                    }

                default: return new HelpBox($"Tipo não suportado: {vt}", HelpBoxMessageType.Warning);
            }


        }

        static string NicifyCamel(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            s = System.Text.RegularExpressions.Regex.Replace(s, "([a-z0-9])([A-Z])", "$1 $2");

            s = System.Text.RegularExpressions.Regex.Replace(s, "([A-Z])([A-Z][a-z])", "$1 $2");

            return s;
        }

        static string NicifyTitle(string s)
        {
            s = NicifyCamel(s);
            if (string.IsNullOrEmpty(s)) return s;
            if (char.IsLetter(s[0])) s = char.ToUpperInvariant(s[0]) + s.Substring(1);
            return s;
        }

        bool IsMiscRoot(string path)
        {
            if (!_layoutIndex.TryGetValue(path, out var grp)) return false;
            var eff = grp.chain.Where(n => !string.Equals(n, "General", StringComparison.Ordinal)).ToList();
            return string.Equals(grp.top, "Misc", StringComparison.Ordinal) && eff.Count == 0;
        }

        string ComputeFoldoutTitle(string name, int depth)
        {
            if (depth == 0) return NicifyCamel(name);

            if (_foldoutLabelByName.TryGetValue(name, out var fromInspector) && !string.IsNullOrEmpty(fromInspector))
                return fromInspector;

            return NicifyCamel(name);
        }

        string ExtractDisplayLabel(VisualElement fieldVE, string pathFallback)
        {
            if (fieldVE is PropertyField pf && !string.IsNullOrEmpty(pf.label))
                return pf.label;

            var pfChild = fieldVE.Q<PropertyField>();
            if (pfChild != null && !string.IsNullOrEmpty(pfChild.label))
                return pfChild.label;

            var lbl = fieldVE.Q<Label>();
            if (lbl != null && !string.IsNullOrEmpty(lbl.text))
                return lbl.text;

            return ObjectNames.NicifyVariableName(pathFallback ?? string.Empty);
        }
    }
}
#endif
