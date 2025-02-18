// Original Version created by Jake Carter - 2023/03/15.
// Modified by Muhammad Humza Butt - 2025/02/18.
// Modification is allowed. Crediting is required.
// Version 1.1.2 - Pagination and UI improvements, Find & Replace Shaders.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ShaderTool : EditorWindow
{
    // --- Shader Search Options ---
    private Shader selectedShader;
    private bool includeEmbeddedMaterials = false;
    private bool onlyEmbeddedMaterials = false;

    // --- Shader Replacement Options ---
    private Shader replaceFromShader;
    private Shader replaceToShader;

    // --- Material Search Results ---
    private List<Material> foundMaterials = new();
    private List<Editor> materialPreviews = new();

    // --- Pagination & UI Management ---
    private Vector2 paginationScrollPos;
    private Vector2 scrollPos;
    private bool showResults = false;
    private int currentPage = 0;
    private const int materialsPerPage = 10;
    private const int buttonsPerRow = 10;
    private int startIndex = 0;
    private int endIndex = 0;
    private int pageCount = 0;
    private string currentPageTxt;

    // --- GUI Dimensions ---
    private int heightSelectedShaderPicker;
    private readonly int heightSelectedShaderPickerInitial = 50;
    private readonly int heightSelectedShaderPickerResults = 25;
    private int heightFindMaterialsBtn;
    private readonly int heightFindMaterialsBtnInitial = 100;
    private readonly int heightFindMaterialsBtnResults = 25;

    // --- Custom GUI Styles ---
    private GUIStyle headerStyle;
    private GUIStyle sectionHeaderStyle;
    private GUIStyle boxStyle;
    private GUIStyle linkStyle;
    private GUIStyle previewStyle;
    private GUIStyle tabStyle;
    private GUIStyle customButtonStyle;

    // --- Tab Management ---
    private enum Tab
    {
        Find,
        Replace
    }
    private Tab currentTab = Tab.Find;
    private readonly string[] tabNames = { "Find Materials", "Replace Shaders" };

    private bool guiInitialized = false;

    [MenuItem("Tools/Search & Replace Shaders", false, 22)]
    public static void ShowWindow()
    {
        ShaderTool window = GetWindow<ShaderTool>(false, "Shader Tool");
        window.minSize = new Vector2(300, 300);
        window.titleContent = new GUIContent("Shader Tool");
        window.Show();
        EditorApplication.delayCall += () =>
        {
            window.position = new Rect((Screen.width - 700) * 0.5f, (Screen.height - 580) * 0.5f, 700, 580);
        };
    }

    private void CreateGUI()
    {
        // Initialize custom styles
        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter
        };

        sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft
        };

        boxStyle = new GUIStyle("box")
        {
            padding = new RectOffset(10, 10, 10, 10)
        };

        linkStyle = new GUIStyle(EditorStyles.linkLabel)
        {
            alignment = TextAnchor.MiddleCenter
        };

        previewStyle = new GUIStyle(); // for material previews

        // Initialize default shaders (customize defaults as needed)
        selectedShader = Shader.Find("Standard");
        replaceFromShader = Shader.Find("Universal Render Pipeline/Lit");
        replaceToShader = Shader.Find("Standard");

        // Set initial GUI heights
        heightSelectedShaderPicker = heightSelectedShaderPickerInitial;
        heightFindMaterialsBtn = heightFindMaterialsBtnInitial;

        // Initialize pagination (will create empty previews)
        UpdatePaginationAndPreviews();

        // Add tab style
        tabStyle = new GUIStyle(EditorStyles.toolbarButton)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            fixedHeight = 30,
            alignment = TextAnchor.MiddleCenter
        };

        // Add custom button style
        customButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            fixedHeight = 30,
            alignment = TextAnchor.MiddleCenter
        };
    }

    private void OnGUI()
    {
        if (!guiInitialized)
        {
            CreateGUI();
            guiInitialized = true;
        }

        GUILayout.Space(10);
        DrawHeader();

        GUILayout.Space(10);
        DrawTabs();

        GUILayout.Space(10);
        EditorGUILayout.BeginVertical(boxStyle);
        switch (currentTab)
        {
            case Tab.Find:
                DrawFindMaterialsSection();
                if (showResults)
                {
                    GUILayout.Space(10);
                    DrawResultsSection();
                }
                break;
            case Tab.Replace:
                DrawShaderReplacementSection();
                break;
        }
        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();
        DrawFooter();
    }

    /// <summary>
    /// Draws the header of the tool.
    /// </summary>
    private void DrawHeader()
    {
        GUILayout.Label("Shader Finder & Replacer Tool", headerStyle);
        EditorGUILayout.HelpBox("This tool helps you locate materials using a specific shader and optionally replace them. Configure the options below and click the appropriate button.", MessageType.Info);
    }

    /// <summary>
    /// Draws the tab selection interface.
    /// </summary>
    private void DrawTabs()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        // Draw tab buttons
        GUI.changed = false;
        currentTab = (Tab)GUILayout.Toolbar((int)currentTab, tabNames, tabStyle, GUILayout.MinWidth(300));
        if (GUI.changed)
        {
            // Reset results view when switching tabs
            showResults = false;
            GUI.FocusControl(null);
        }
        
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Draws the search section for finding materials.
    /// </summary>
    private void DrawFindMaterialsSection()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        GUILayout.Label("Find Materials by Shader", sectionHeaderStyle);
        GUILayout.Space(5);

        // Shader selection field
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Target Shader:", GUILayout.Width(100));
        selectedShader = EditorGUILayout.ObjectField(selectedShader, typeof(Shader), false,
            GUILayout.Height(heightSelectedShaderPicker)) as Shader;
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);
        // Options for embedded materials
        EditorGUILayout.BeginHorizontal();
        includeEmbeddedMaterials = GUILayout.Toggle(includeEmbeddedMaterials,
            new GUIContent("Include Embedded Materials", "Include materials embedded in assets (such as model materials)"),
            GUILayout.ExpandWidth(true));
        if (includeEmbeddedMaterials)
        {
            onlyEmbeddedMaterials = GUILayout.Toggle(onlyEmbeddedMaterials,
                new GUIContent("Only Embedded", "Show only embedded materials"), GUILayout.ExpandWidth(true));
        }
        else
        {
            onlyEmbeddedMaterials = false;
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);
        if (GUILayout.Button("Find Materials", customButtonStyle, GUILayout.Height(heightFindMaterialsBtn)))
        {
            FindMaterials();
        }
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Draws the shader replacement section.
    /// </summary>
    private void DrawShaderReplacementSection()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        GUILayout.Label("Replace Shaders", sectionHeaderStyle);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox("This will search through all materials in your project and replace the specified shader with a new one.", MessageType.Info);
        GUILayout.Space(10);

        replaceFromShader = EditorGUILayout.ObjectField("Old Shader:", replaceFromShader, typeof(Shader), false) as Shader;
        replaceToShader = EditorGUILayout.ObjectField("New Shader:", replaceToShader, typeof(Shader), false) as Shader;

        GUILayout.Space(10);
        EditorGUI.BeginDisabledGroup(replaceFromShader == null || replaceToShader == null);
        if (GUILayout.Button("Replace Shaders", customButtonStyle))
        {
            if (EditorUtility.DisplayDialog("Confirm Shader Replacement",
                $"Are you sure you want to replace all materials using '{replaceFromShader.name}' with '{replaceToShader.name}'?\n\nThis operation can be undone with Edit > Undo.",
                "Yes, Replace", "Cancel"))
            {
                FindAndReplaceMaterialsWithShader(replaceFromShader.name, replaceToShader.name);
            }
        }
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Draws the searchable materials results along with pagination controls.
    /// </summary>
    private void DrawResultsSection()
    {
        // Selection Buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select All - This Page", customButtonStyle))
            SelectAllMaterialsOnPage();
        if (GUILayout.Button("Select All - All Pages", customButtonStyle))
            SelectAllMaterials();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        // Pagination controls in a single line with navigation
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        
        // Previous page button
        EditorGUI.BeginDisabledGroup(currentPage <= 0);
        if (GUILayout.Button("◄", customButtonStyle, GUILayout.Width(30), GUILayout.Height(25)))
        {
            currentPage--;
            // Adjust scroll position to show previous pages
            if (currentPage * 30 < paginationScrollPos.x)
            {
                paginationScrollPos.x = currentPage * 30;
            }
            UpdatePaginationAndPreviews();
        }
        EditorGUI.EndDisabledGroup();

        // Page info
        GUILayout.Label($"Page {currentPage + 1} / {pageCount}", EditorStyles.boldLabel, GUILayout.Width(100));
        
        // Pagination scroll view
        float viewWidth = position.width - 200; // Adjust width accounting for other elements
        EditorGUILayout.BeginHorizontal(GUILayout.Width(viewWidth));
        paginationScrollPos = EditorGUILayout.BeginScrollView(
            paginationScrollPos,
            false, // Disable horizontal scrollbar
            false, // Disable vertical scrollbar
            GUILayout.Height(50)
        );
        
        EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
        for (int i = 0; i < pageCount; i++)
        {
            bool isCurrentPage = i == currentPage;
            GUI.enabled = !isCurrentPage;
            
            if (GUILayout.Button((i + 1).ToString(), customButtonStyle, GUILayout.Width(30), GUILayout.Height(25)))
            {
                currentPage = i;
                UpdatePaginationAndPreviews();
            }
            
            GUI.enabled = true;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndHorizontal();

        // Next page button
        EditorGUI.BeginDisabledGroup(currentPage >= pageCount - 1);
        if (GUILayout.Button("►", customButtonStyle, GUILayout.Width(30), GUILayout.Height(25)))
        {
            currentPage++;
            // Adjust scroll position to show next pages
            float maxScroll = (pageCount * 30) - viewWidth;
            if ((currentPage + 1) * 30 > paginationScrollPos.x + viewWidth)
            {
                paginationScrollPos.x = Mathf.Min((currentPage * 30), maxScroll);
            }
            UpdatePaginationAndPreviews();
        }
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label($"Found Materials: {foundMaterials.Count}", EditorStyles.boldLabel);

        // Material List (scrollable)
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
        for (int i = startIndex; i < endIndex; i++)
        {
            Material mat = foundMaterials[i];
            if (mat == null)
            {
                UpdatePaginationAndPreviews();
                continue;
            }
            EditorGUILayout.BeginHorizontal();
            
            // Material Preview
            Rect previewRect = GUILayoutUtility.GetRect(128, 128, GUILayout.ExpandWidth(false));
            if (materialPreviews.Count > i && materialPreviews[i] != null)
            {
                materialPreviews[i].OnPreviewGUI(previewRect, previewStyle);
            }
            
            // Material info and selection button
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(mat));
            if (GUILayout.Button(mat.name, customButtonStyle, GUILayout.Height(80)))
            {
                SelectMaterial(mat);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
        }
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Draws the tool footer with credits.
    /// </summary>
    private void DrawFooter()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("v1.1.2 Created by Muhammad Humza Butt", linkStyle, GUILayout.ExpandWidth(false)))
            Application.OpenURL("https://github.com/humza-13");
        GUILayout.FlexibleSpace();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Original Version  by Jake Carter", linkStyle, GUILayout.ExpandWidth(false)))
            Application.OpenURL("https://jcfolio.weebly.com/");
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Updates pagination variables and creates the material previews for the current page.
    /// </summary>
    private void UpdatePaginationAndPreviews()
    {
        if (showResults)
        {
            startIndex = currentPage * materialsPerPage;
            endIndex = Mathf.Min(startIndex + materialsPerPage, foundMaterials.Count);
            pageCount = Mathf.CeilToInt(foundMaterials.Count / (float)materialsPerPage);
            currentPageTxt = $"Page {currentPage + 1} / {pageCount}";

            // Refresh material previews for items on the current page
            materialPreviews.Clear();
            materialPreviews = new Editor[foundMaterials.Count].ToList();
            for (int i = startIndex; i < endIndex; i++)
            {
                Material mat = foundMaterials[i];
                Editor previewEditor = Editor.CreateEditor(mat);
                materialPreviews[i] = previewEditor;
            }

            // Use the compact heights when results are shown
            heightSelectedShaderPicker = heightSelectedShaderPickerResults;
            heightFindMaterialsBtn = heightFindMaterialsBtnResults;
        }
        else
        {
            heightSelectedShaderPicker = heightSelectedShaderPickerInitial;
            heightFindMaterialsBtn = heightFindMaterialsBtnInitial;
        }
    }

    /// <summary>
    /// Finds all materials using the currently selected shader.
    /// </summary>
    private void FindMaterials()
    {
        FindMaterialsWithShader(selectedShader?.name);
        showResults = true;
        currentPage = 0;
        UpdatePaginationAndPreviews();
    }

    /// <summary>
    /// Scans all materials in the Assets folder and collects those using the specified shader.
    /// </summary>
    private void FindMaterialsWithShader(string shaderName)
    {
        if (string.IsNullOrEmpty(shaderName))
            return;

        foundMaterials.Clear();
        string[] allMaterialGuids = AssetDatabase.FindAssets("t:Material");

        foreach (string guid in allMaterialGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!assetPath.StartsWith("Assets/"))
                continue;

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null || mat.shader == null || string.IsNullOrEmpty(mat.shader.name))
                continue;

            if (!mat.shader.name.Equals(shaderName, StringComparison.OrdinalIgnoreCase))
                continue;

            // Check for embedded materials
            if (IsMaterialEmbedded(assetPath))
            {
                if (includeEmbeddedMaterials)
                    foundMaterials.Add(mat);
            }
            else
            {
                if (!onlyEmbeddedMaterials)
                    foundMaterials.Add(mat);
            }
        }
        foundMaterials.Sort((a, b) => a.name.CompareTo(b.name));
    }

    /// <summary>
    /// Determines if the material is embedded (and therefore not directly editable).
    /// </summary>
    private bool IsMaterialEmbedded(string assetPath)
    {
        return AssetDatabase.GetMainAssetTypeAtPath(assetPath) != typeof(Material);
    }

    /// <summary>
    /// Finds all materials using the specified old shader and replaces them with the new shader.
    /// </summary>
    private void FindAndReplaceMaterialsWithShader(string oldShaderName, string newShaderName)
    {
        int replacedCount = 0;
        Shader newShader = Shader.Find(newShaderName);
        if (newShader == null)
        {
            Debug.LogError("New shader not found: " + newShaderName);
            return;
        }

        string[] allMaterialGuids = AssetDatabase.FindAssets("t:Material");
        foreach (string guid in allMaterialGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!assetPath.StartsWith("Assets/"))
                continue;

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null || mat.shader == null)
                continue;

            if (mat.shader.name.Equals(oldShaderName, StringComparison.OrdinalIgnoreCase))
            {
                Undo.RecordObject(mat, "Replace Shader");
                mat.shader = newShader;
                EditorUtility.SetDirty(mat);
                replacedCount++;
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Replaced {replacedCount} materials using shader '{oldShaderName}' with '{newShaderName}'.");
    }

    /// <summary>
    /// Sets the selected material as the active object in the Editor.
    /// </summary>
    private void SelectMaterial(Material mat)
    {
        if (mat != null)
        {
            Selection.activeObject = mat;
            EditorGUIUtility.PingObject(mat);
        }
    }

    /// <summary>
    /// Selects all found materials.
    /// </summary>
    private void SelectAllMaterials()
    {
        Selection.objects = foundMaterials.ToArray();
    }

    /// <summary>
    /// Selects only the materials visible on the current page.
    /// </summary>
    private void SelectAllMaterialsOnPage()
    {
        if (foundMaterials.Count > 0)
        {
            List<Material> materialsOnPage = foundMaterials.GetRange(startIndex, endIndex - startIndex);
            Selection.objects = materialsOnPage.ToArray();
        }
    }
}