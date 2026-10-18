using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Graves.Core.Editor
{
    /// <summary>
    /// The authoring window: one place to create, find, edit and delete every definition in the
    /// project, including those a plugin introduced.
    /// </summary>
    /// <remarks>
    /// <b>One window, on purpose.</b> The detail panel is an <see cref="InspectorElement"/> bound to
    /// the selected asset, which means it draws whatever that ScriptableObject exposes, including a
    /// plugin type this window has never heard of. Adding a field to a definition or a module costs
    /// zero editor code - that is the property that has to hold if plugin five is to be as cheap to
    /// write as plugin one.
    /// </remarks>
    public class GravesWindow : EditorWindow
    {
        private List<DefinitionCatalogue.CategoryEntry> _categories = new List<DefinitionCatalogue.CategoryEntry>();
        private List<GameDefinition> _visible = new List<GameDefinition>();
        private int _categoryIndex;
        private string _search = string.Empty;

        private Toolbar _tabStrip;
        private readonly List<ToolbarToggle> _tabButtons = new List<ToolbarToggle>();
        private ListView _definitionList;

        // The manual is a tab like the others rather than a separate window, because the point of it
        // is to be one click from the thing being authored. A second window is a thing you go and
        // find, which is what the folder already was.
        // Kept in EditorPrefs rather than in a field, because a field dies with a domain reload and
        // the whole point is that the window comes back where it was. SessionState would survive a
        // reload and not a restart, and coming back tomorrow on the tab you left is the behaviour
        // that was actually asked for.
        private const string LastTabKey = "graves.window.lastTab";
        private const string LastManualKey = "graves.window.lastManual";

        private ToolbarToggle _manualTab;
        private readonly List<string> _manualPages = new List<string>();
        private string _manualSelected;
        private bool _manualMode;
        private VisualElement _detail;
        private Label _status;

        /// <summary>Opens the window, focusing it if already open.</summary>
        [MenuItem("Graves/Authoring Window %#g")]
        public static void Open()
        {
            var window = GetWindow<GravesWindow>();
            window.titleContent = new GUIContent("Graves");
            window.minSize = new Vector2(760f, 420f);
            window.Show();
        }

        private void CreateGUI()
        {
            _categories = DefinitionCatalogue.DiscoverCategories();

            rootVisualElement.Add(BuildToolbar());
            rootVisualElement.Add(BuildCategoryTabs());

            // A TwoPaneSplitView is fully populated BEFORE being parented. It validates that it has
            // exactly two children as soon as it is attached to a panel, so adding an empty split and
            // filling it afterwards throws "TwoPaneSplitView needs exactly 2 children" and leaves the
            // window unusable.
            var split = new TwoPaneSplitView(0, 260f, TwoPaneSplitViewOrientation.Horizontal)
            {
                // Unity persists a divider position, a scroll offset and a selection for any element
                // given a view data key, and forgets all three for any element without one. That is
                // the whole of "the window remembered where I left it" - a dragged divider survives
                // a domain reload, a recompile and closing the editor.
                viewDataKey = "graves.window.split"
            };
            split.style.flexGrow = 1f;
            split.Add(BuildListPane());
            split.Add(BuildDetailPane());

            rootVisualElement.Add(split);

            _status = new Label { style = { paddingLeft = 6f, paddingTop = 2f, paddingBottom = 2f } };
            _status.style.unityFontStyleAndWeight = FontStyle.Italic;
            rootVisualElement.Add(_status);

            // Only now that every pane exists is it safe to choose anything, because choosing
            // rebuilds the list and the detail, and both must exist first.
            RestoreLastTab();
        }

        private VisualElement BuildToolbar()
        {
            var toolbar = new Toolbar();

            var rescan = new ToolbarButton(() =>
            {
                // Rebuilt rather than refreshed, because a rescan can find a definition kind that
                // did not exist a moment ago - a plugin just compiled - and that needs a new tab.
                _categories = DefinitionCatalogue.DiscoverCategories();

                if (_tabStrip != null && _tabStrip.parent != null)
                {
                    var host = _tabStrip.parent;
                    int at = host.IndexOf(_tabStrip);
                    host.Remove(_tabStrip);
                    host.Insert(at, BuildCategoryTabs());
                }

                DefinitionCatalogue.RebuildRegistry();
                SelectCategory(_categoryIndex);
                SetStatus("Rescanned. Registry rebuilt from disk.");
            }) { text = "Rescan" };
            toolbar.Add(rescan);

            var validate = new ToolbarButton(() =>
            {
                var registry = DefinitionCatalogue.RebuildRegistry();
                var problems = registry.Validate();

                // Checked here as well as in the report, because two plugins colliding on a state key
                // corrupts saves and nothing else in the project would ever say so.
                problems.AddRange(DefinitionCatalogue.ValidateStateKeys());

                if (problems.Count == 0)
                {
                    SetStatus("Registry is sound. " + registry.Definitions.Count + " definitions, " +
                              "every uid unique.");
                    return;
                }

                foreach (var problem in problems) Debug.LogWarning("[Graves] " + problem, registry);
                SetStatus(problems.Count + " integrity problem(s) - see the console.");
            }) { text = "Validate" };
            toolbar.Add(validate);

            var report = new ToolbarButton(() =>
            {
                CoreDiagnostics.CopyToClipboard();
                SetStatus("Diagnostic report copied to the clipboard, and logged to the console.");
            })
            {
                text = "Report",
                tooltip = "Copies a plain-text description of this installation - version, assembly " +
                          "boundaries, installed plugins, authored data and any integrity problems. " +
                          "Paste it into a bug report from a project nobody else can run."
            };
            toolbar.Add(report);

            toolbar.Add(new ToolbarSpacer());

            var create = new ToolbarButton(CreateSelectedType) { text = "New" };
            toolbar.Add(create);

            var duplicate = new ToolbarButton(() =>
            {
                var selected = Selected();
                if (selected == null) { SetStatus("Nothing selected to duplicate."); return; }

                var copy = DefinitionCatalogue.Duplicate(selected);
                RefreshList();
                SelectDefinition(copy);
                SetStatus("Duplicated '" + selected.DisplayName + "' with a new identity.");
            }) { text = "Duplicate" };
            toolbar.Add(duplicate);

            var delete = new ToolbarButton(DeleteSelected) { text = "Delete" };
            toolbar.Add(delete);

            var spacer = new ToolbarSpacer { flex = true };
            toolbar.Add(spacer);

            var search = new ToolbarSearchField();
            search.style.width = 220f;
            search.RegisterValueChangedCallback(evt =>
            {
                _search = evt.newValue ?? string.Empty;
                RefreshList();
            });
            toolbar.Add(search);

            return toolbar;
        }

        /// <summary>
        /// The row of category tabs across the top - Entities, Items, and whatever a plugin has
        /// added.
        /// </summary>
        /// <remarks>
        /// <b>Tabs rather than a third pane.</b> There are a handful of categories and there always
        /// will be: a category is a kind of definition, and a project with twenty of those has other
        /// problems. Spending a whole vertical pane on a four-item list costs the width that the list
        /// and the inspector actually need.
        ///
        /// Graves' shape, taken from an authoring window he had already built and preferred, and it
        /// is the right call for the same reason his was: the thing being authored deserves the room,
        /// not the navigation.
        ///
        /// The tabs are built from what the catalogue discovered, so a plugin that adds a definition
        /// kind gets a tab with no edit here - the same promise the detail panel already makes about
        /// fields.
        /// </remarks>
        private VisualElement BuildCategoryTabs()
        {
            _tabStrip = new Toolbar();
            _tabButtons.Clear();

            for (int i = 0; i < _categories.Count; i++)
            {
                int index = i;
                var button = new ToolbarToggle { text = _categories[i].Category };
                button.style.minWidth = 90f;
                button.style.unityTextAlign = TextAnchor.MiddleCenter;

                button.RegisterValueChangedCallback(change =>
                {
                    // A toggle turning itself off would leave no category chosen and an empty list
                    // with no way back, so clicking the active tab simply re-asserts it.
                    if (!change.newValue && _categoryIndex == index) { button.SetValueWithoutNotify(true); return; }
                    if (change.newValue) SelectCategory(index);
                });

                _tabButtons.Add(button);
                _tabStrip.Add(button);
            }

            if (_categories.Count == 0)
            {
                _tabStrip.Add(new Label("No definition kinds found - install a plugin, or check the core compiled.")
                {
                    style = { paddingLeft = 8f, paddingTop = 3f, opacity = 0.6f }
                });
            }

            _tabStrip.Add(new ToolbarSpacer { flex = true });

            var manual = new ToolbarToggle { text = "Manual" };
            manual.style.minWidth = 90f;
            manual.style.unityTextAlign = TextAnchor.MiddleCenter;
            manual.tooltip = "The project's own documentation, rendered here so it is one click from " +
                             "the thing being authored rather than in a folder nobody has open.";
            manual.RegisterValueChangedCallback(change =>
            {
                if (!change.newValue && _manualMode) { manual.SetValueWithoutNotify(true); return; }
                if (change.newValue) SelectManual();
            });

            _manualTab = manual;
            _tabStrip.Add(manual);

            return _tabStrip;
        }

        /// <summary>
        /// Switches the window to showing documentation instead of definitions.
        /// </summary>
        /// <remarks>
        /// The same two panes serve both: a list on the left, the thing itself on the right. A
        /// separate window would have been easier and would also have been another place to go, and
        /// the reason these documents go unread is that they are already somewhere else.
        /// </remarks>
        private void SelectManual()
        {
            _manualMode = true;
            EditorPrefs.SetString(LastTabKey, LastManualKey);

            for (int i = 0; i < _tabButtons.Count; i++) _tabButtons[i].SetValueWithoutNotify(false);
            if (_manualTab != null) _manualTab.SetValueWithoutNotify(true);

            _manualPages.Clear();
            _manualPages.AddRange(ManualPage.FindPages());

            _definitionList.itemsSource = _manualPages;
            _definitionList.Rebuild();

            if (_manualPages.Count > 0)
            {
                _manualSelected = _manualPages[0];
                _definitionList.SetSelectionWithoutNotify(new[] { 0 });
            }
            else
            {
                _manualSelected = null;
            }

            RebuildDetail();
            // Names the roots that actually resolved rather than the ones this build hopes for, so a
            // consuming project sees the framework's own folder and not two it does not have.
            var roots = ManualPage.Roots();
            var names = new List<string>();
            foreach (var root in roots)
            {
                names.Add(System.IO.Path.GetFileName(
                    root.TrimEnd(System.IO.Path.DirectorySeparatorChar,
                                 System.IO.Path.AltDirectorySeparatorChar)));
            }

            SetStatus(_manualPages.Count + " document" + (_manualPages.Count == 1 ? string.Empty : "s") +
                      (names.Count == 0 ? "." : " in " + string.Join(" and ", names) + "."));
        }

        /// <summary>
        /// Reopens on whatever was last being looked at.
        /// </summary>
        /// <remarks>
        /// The stored tab is a category NAME rather than an index, because an index means something
        /// different the moment a plugin adds or removes a definition kind - and reopening on a
        /// different tab than the one you left, silently, is worse than reopening on the first.
        /// </remarks>
        private void RestoreLastTab()
        {
            var last = EditorPrefs.GetString(LastTabKey, string.Empty);

            if (last == LastManualKey) { SelectManual(); return; }

            for (int i = 0; i < _categories.Count; i++)
            {
                if (_categories[i].Category != last) continue;
                SelectCategory(i);
                return;
            }

            SelectCategory(0);
        }

        /// <summary>Chooses a category, updates the tabs to match, and rebuilds the list.</summary>
        private void SelectCategory(int _index)
        {
            if (_categories.Count == 0) return;

            _categoryIndex = Mathf.Clamp(_index, 0, _categories.Count - 1);
            _manualMode = false;
            EditorPrefs.SetString(LastTabKey, _categories[_categoryIndex].Category);
            if (_manualTab != null) _manualTab.SetValueWithoutNotify(false);

            for (int i = 0; i < _tabButtons.Count; i++)
            {
                _tabButtons[i].SetValueWithoutNotify(i == _categoryIndex);
            }

            RefreshList();
        }

        private VisualElement BuildListPane()
        {
            _definitionList = new ListView
            {
                viewDataKey = "graves.window.list",
                itemsSource = _visible,
                fixedItemHeight = 22f,
                selectionType = SelectionType.Single,
                makeItem = () => new Label { style = { paddingLeft = 8f, paddingTop = 3f } },
                bindItem = (element, index) =>
                {
                    if (_manualMode)
                    {
                        ((Label)element).text = index < _manualPages.Count
                                              ? ManualPage.TitleFor(_manualPages[index])
                                              : string.Empty;
                        return;
                    }

                    ((Label)element).text = index < _visible.Count && _visible[index] != null
                                          ? _visible[index].DisplayName
                                          : "(missing)";
                }
            };

            _definitionList.selectionChanged += _ =>
            {
                if (_manualMode)
                {
                    int index = _definitionList.selectedIndex;
                    _manualSelected = index >= 0 && index < _manualPages.Count ? _manualPages[index] : null;
                }
                RebuildDetail();
            };
            return _definitionList;
        }

        private VisualElement BuildDetailPane()
        {
            _detail = new ScrollView
            {
                viewDataKey = "graves.window.detail",
                style = { flexGrow = 1f }
            };
            return _detail;
        }

        /// <summary>
        /// Redraws the right-hand panel for the current selection: the definition's own inspector,
        /// then one inspector per attached module, then the means to add another.
        /// </summary>
        private void RebuildDetail()
        {
            // Guarded because UI Toolkit notifies selection synchronously, so this can be reached
            // from a pane that was built before the rest of the window existed.
            if (_detail == null) return;

            _detail.Clear();

            if (_manualMode)
            {
                if (string.IsNullOrEmpty(_manualSelected))
                {
                    // Says WHY rather than just that it is empty. A blank manual with no message is
                    // what a fresh install of this framework showed the first time anyone tried it,
                    // and silence gave the reader no way to know the documents existed at all.
                    _detail.Add(new Label(ManualPage.EmptyExplanation())
                    {
                        style =
                        {
                            paddingLeft = 10f, paddingRight = 14f, paddingTop = 10f,
                            whiteSpace = WhiteSpace.Normal, opacity = 0.75f
                        }
                    });
                    return;
                }

                ManualPage.Render(_detail, _manualSelected);
                return;
            }

            var definition = Selected();
            if (definition == null)
            {
                _detail.Add(new Label("Nothing selected.")
                {
                    style = { paddingLeft = 8f, paddingTop = 8f, opacity = 0.6f }
                });
                return;
            }

            _detail.Add(Header(definition.DisplayName));

            var uid = new Label("uid  " + definition.Uid)
            {
                style = { paddingLeft = 8f, paddingBottom = 6f, opacity = 0.5f, fontSize = 10f }
            };
            uid.tooltip = "Permanent identity. Saves and cross-references point at this, never at the " +
                          "name, so renaming is always safe.";
            _detail.Add(uid);

            _detail.Add(new InspectorElement(definition));

            _detail.Add(Header("Modules"));

            var ordering = new Label("Attached in this order when the entity wakes. A module that " +
                                     "reads what another installs should sit below it.")
            {
                style = { paddingLeft = 8f, paddingBottom = 4f, opacity = 0.55f,
                          whiteSpace = WhiteSpace.Normal, fontSize = 11f }
            };
            _detail.Add(ordering);

            var modules = definition.Modules;
            if (modules.Count == 0)
            {
                _detail.Add(new Label("No modules. The core knows what this is, but nothing yet knows " +
                                      "what it does.")
                {
                    style = { paddingLeft = 8f, paddingBottom = 4f, opacity = 0.6f, whiteSpace = WhiteSpace.Normal }
                });
            }

            for (int i = 0; i < modules.Count; i++)
            {
                var module = modules[i];
                if (module == null)
                {
                    _detail.Add(new Label("Missing module - its plugin is not installed.")
                    {
                        style = { paddingLeft = 8f, color = new Color(0.9f, 0.5f, 0.3f) }
                    });
                    continue;
                }

                // Collapsed by default. A definition can carry a lot of these - the test player
                // carries nineteen - and opening every one of them turns the panel into a wall that
                // has to be scrolled past to reach anything.
                var foldout = new Foldout
                {
                    text = "  " + (i + 1) + ".  " + Readable(module.GetType().Name),
                    value = false
                };

                foldout.tooltip = module.GetType().FullName + "  from " +
                                  module.GetType().Assembly.GetName().Name;

                var origin = new Label(module.GetType().Assembly.GetName().Name)
                {
                    style = { paddingLeft = 16f, opacity = 0.5f, fontSize = 10f }
                };
                origin.tooltip = "The plugin this module came from. Deleting that plugin leaves this " +
                                 "module unresolvable, and the definition reports it rather than " +
                                 "quietly dropping the authored values.";
                foldout.Add(origin);

                foldout.Add(new InspectorElement(module));

                var captured = module;
                var atIndex = i;

                var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };

                var up = new Button(() =>
                {
                    if (!DefinitionCatalogue.MoveModule(definition, captured, -1)) return;
                    RebuildDetail();
                    SetStatus("Moved " + Readable(captured.GetType().Name) + " earlier.");
                })
                { text = "Move up" };
                up.SetEnabled(atIndex > 0);
                row.Add(up);

                var down = new Button(() =>
                {
                    if (!DefinitionCatalogue.MoveModule(definition, captured, 1)) return;
                    RebuildDetail();
                    SetStatus("Moved " + Readable(captured.GetType().Name) + " later.");
                })
                { text = "Move down" };
                down.SetEnabled(atIndex < modules.Count - 1);
                row.Add(down);

                row.Add(new Button(() =>
                {
                    if (!EditorUtility.DisplayDialog(
                            "Remove module",
                            "Remove " + captured.GetType().Name + " from '" + definition.DisplayName +
                            "'? Its authored values are destroyed with it.",
                            "Remove", "Cancel")) return;

                    DefinitionCatalogue.RemoveModule(definition, captured);
                    RebuildDetail();
                    SetStatus("Removed " + captured.GetType().Name + ".");
                })
                { text = "Remove" });

                foldout.Add(row);
                _detail.Add(foldout);
            }

            _detail.Add(new Button(() => ShowAddModuleMenu(definition)) { text = "Add module" });
        }

        private void ShowAddModuleMenu(GameDefinition _definition)
        {
            var types = DefinitionCatalogue.DiscoverModuleTypes();
            var menu = new GenericMenu();

            if (types.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No module types - install a plugin"));
            }

            foreach (var type in types)
            {
                var captured = type;
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(type.Name)), false, () =>
                {
                    DefinitionCatalogue.AddModule(_definition, captured);
                    RebuildDetail();
                    SetStatus("Added " + captured.Name + " to '" + _definition.DisplayName + "'.");
                });
            }

            menu.ShowAsContext();
        }

        private void CreateSelectedType()
        {
            if (_categories.Count == 0)
            {
                SetStatus("No definition types found. Nothing declares a category.");
                return;
            }

            var entry = _categories[Mathf.Clamp(_categoryIndex, 0, _categories.Count - 1)];
            var created = DefinitionCatalogue.Create(entry.Type, "New " + entry.Type.Name);

            DefinitionCatalogue.RebuildRegistry();
            RefreshList();
            SelectDefinition(created);
            SetStatus("Created '" + created.DisplayName + "'.");
        }

        private void DeleteSelected()
        {
            var selected = Selected();
            if (selected == null) { SetStatus("Nothing selected to delete."); return; }

            var label = selected.DisplayName;
            if (!EditorUtility.DisplayDialog(
                    "Delete definition",
                    "Delete '" + label + "'?\n\nAnything saved against it will stop resolving - which " +
                    "is deliberate: the core reports a missing definition rather than silently " +
                    "substituting another one.",
                    "Delete", "Cancel")) return;

            if (!DefinitionCatalogue.Delete(selected))
            {
                SetStatus("Could not delete '" + label + "'.");
                return;
            }

            DefinitionCatalogue.RebuildRegistry();
            RefreshList();
            SetStatus("Deleted '" + label + "'.");
        }

        private void RefreshList()
        {
            _visible.Clear();

            if (_categories.Count > 0)
            {
                var entry = _categories[Mathf.Clamp(_categoryIndex, 0, _categories.Count - 1)];
                var all = DefinitionCatalogue.LoadAll(entry.Type);

                foreach (var definition in all)
                {
                    if (string.IsNullOrEmpty(_search) ||
                        definition.DisplayName.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _visible.Add(definition);
                    }
                }
            }

            if (_definitionList != null)
            {
                _definitionList.itemsSource = _visible;
                _definitionList.Rebuild();
            }

            RebuildDetail();
        }

        private void SelectDefinition(GameDefinition _definition)
        {
            if (_definition == null || _definitionList == null) return;
            var index = _visible.IndexOf(_definition);
            if (index >= 0) _definitionList.selectedIndex = index;
        }

        private GameDefinition Selected()
        {
            if (_definitionList == null) return null;
            var index = _definitionList.selectedIndex;
            return index >= 0 && index < _visible.Count ? _visible[index] : null;
        }

        private void SetStatus(string _message)
        {
            if (_status != null) _status.text = _message;
        }

        /// <summary>
        /// Turns a type name into something a person reads rather than parses.
        /// </summary>
        /// <remarks>
        /// The trailing "Module" goes because every one of them has it and it carries nothing, and
        /// the remaining words are separated. This is presentation only - nothing is ever looked up
        /// by the result.
        /// </remarks>
        private static string Readable(string _typeName)
        {
            if (string.IsNullOrEmpty(_typeName)) return string.Empty;

            var trimmed = _typeName.EndsWith("Module") && _typeName.Length > 6
                        ? _typeName.Substring(0, _typeName.Length - 6)
                        : _typeName;

            var built = new System.Text.StringBuilder(trimmed.Length + 6);
            for (int i = 0; i < trimmed.Length; i++)
            {
                if (i > 0 && char.IsUpper(trimmed[i]) && !char.IsUpper(trimmed[i - 1])) built.Append(' ');
                built.Append(trimmed[i]);
            }
            return built.ToString();
        }

        private static Label Header(string _text)
        {
            return new Label(_text)
            {
                style =
                {
                    paddingLeft = 6f, paddingTop = 8f, paddingBottom = 2f,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
        }
    }
}
