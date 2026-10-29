using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CardCreator.Editor
{
    internal sealed class CardArtworkLibrary : VisualElement
    {
        private readonly Action<Texture2D, bool> assign;
        private readonly List<string> paths = new List<string>();
        private readonly VisualElement grid;
        private readonly Label pageLabel;
        private readonly Button previous, next;
        private string search = "";
        private string category = "Characters";
        private int page;
        private const int PageSize = 9;

        internal CardArtworkLibrary(Action<Texture2D, bool> assign)
        {
            this.assign = assign;
            AddToClassList("art-library");
            var searchField = new TextField { label = "Search assets" };
            searchField.RegisterValueChangedCallback(e => { search = e.newValue; page = 0; Refresh(); });
            Add(searchField);
            var filter = new DropdownField("Collection", new List<string> { "Characters", "Backgrounds", "UI icons", "All" }, 0);
            filter.RegisterValueChangedCallback(e => { category = e.newValue; page = 0; Refresh(); });
            Add(filter);
            var hint = new Label("Click a thumbnail to assign it. Backgrounds fill the card; other assets use the character layer.");
            hint.AddToClassList("studio-hint");
            Add(hint);
            grid = new VisualElement();
            grid.AddToClassList("art-library__grid");
            Add(grid);
            var navigation = new VisualElement();
            navigation.AddToClassList("studio-row");
            previous = new Button(() => { page--; Refresh(); }) { text = "Previous" };
            next = new Button(() => { page++; Refresh(); }) { text = "Next" };
            pageLabel = new Label();
            pageLabel.AddToClassList("art-library__page");
            navigation.Add(previous); navigation.Add(pageLabel); navigation.Add(next);
            Add(navigation);
            Add(new Button(Reload) { text = "Refresh asset library" });
            Reload();
        }

        private void Reload()
        {
            paths.Clear();
            string[] roots = { "Assets/CardCreator", "Assets/Aekashics Librarium Static Battlers" };
            var validRoots = roots.Where(AssetDatabase.IsValidFolder).ToArray();
            if (validRoots.Length > 0)
                paths.AddRange(AssetDatabase.FindAssets("t:Texture2D", validRoots)
                    .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.OrdinalIgnoreCase));
            page = 0;
            Refresh();
        }

        private static bool IsBackground(string path)
            => path.IndexOf("background", StringComparison.OrdinalIgnoreCase) >= 0;
        private static bool IsIcon(string path)
            => path.IndexOf("icon", StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("/OnlineAssets/", StringComparison.OrdinalIgnoreCase) >= 0;

        private void Refresh()
        {
            if (grid == null) return;
            var filtered = paths.Where(p => Path.GetFileNameWithoutExtension(p).IndexOf(search ?? "", StringComparison.OrdinalIgnoreCase) >= 0)
                .Where(p => category == "All" || (category == "Backgrounds" ? IsBackground(p)
                    : category == "UI icons" ? IsIcon(p) : !IsBackground(p) && !IsIcon(p))).ToList();
            int pages = Mathf.Max(1, Mathf.CeilToInt(filtered.Count / (float)PageSize));
            page = Mathf.Clamp(page, 0, pages - 1);
            grid.Clear();
            foreach (string path in filtered.Skip(page * PageSize).Take(PageSize))
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var tile = new Button(() => { if (texture != null) assign(texture, IsBackground(path)); });
                tile.tooltip = path + (texture != null ? $"\n{texture.width} × {texture.height} px" : "");
                tile.AddToClassList("art-library__tile");
                var thumbnail = new Image { image = texture, scaleMode = ScaleMode.ScaleToFit };
                thumbnail.AddToClassList("art-library__thumbnail");
                tile.Add(thumbnail);
                var name = new Label(Path.GetFileNameWithoutExtension(path));
                name.AddToClassList("art-library__name");
                tile.Add(name);
                grid.Add(tile);
            }
            if (filtered.Count == 0) grid.Add(new Label("No matching assets in this collection."));
            pageLabel.text = $"{page + 1} / {pages} · {filtered.Count} assets";
            previous.SetEnabled(page > 0); next.SetEnabled(page < pages - 1);
        }
    }
}

