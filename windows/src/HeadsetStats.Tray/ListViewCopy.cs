using System.Text;

namespace HeadsetStats.Tray;

/// <summary>Makes a ListView's rows copyable: Ctrl+C / Ctrl+A and a right-click menu.</summary>
internal static class ListViewCopy
{
    /// <param name="valueColumn">If set, adds "Copy value", which copies only this column (e.g. the Value of a property list).</param>
    public static void Attach(ListView list, int? valueColumn = null)
    {
        list.MultiSelect = true;
        list.KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.C) { Copy(list, Selected(list), null); e.Handled = true; }
            if (e.Control && e.KeyCode == Keys.A) { foreach (ListViewItem item in list.Items) item.Selected = true; e.Handled = true; }
        };

        var menu = new ContextMenuStrip();
        var copy = menu.Items.Add("Copy", null, (_, _) => Copy(list, Selected(list), null));
        ToolStripItem? copyValue = valueColumn is { } column
            ? menu.Items.Add("Copy value", null, (_, _) => Copy(list, Selected(list), column))
            : null;
        menu.Items.Add("Copy all", null, (_, _) => Copy(list, list.Items.Cast<ListViewItem>(), null));
        menu.Opening += (_, _) =>
        {
            var any = list.SelectedItems.Count > 0;
            copy.Enabled = any;
            if (copyValue is not null) copyValue.Enabled = any;
        };
        list.ContextMenuStrip = menu;
    }

    private static IEnumerable<ListViewItem> Selected(ListView list) => list.SelectedItems.Cast<ListViewItem>();

    private static void Copy(ListView list, IEnumerable<ListViewItem> items, int? column)
    {
        var rows = items.ToList();
        if (rows.Count == 0) return;
        var sb = new StringBuilder();
        // Header line only when copying whole rows of a multi-column table.
        if (column is null && rows.Count > 1)
            sb.AppendLine(string.Join('\t', list.Columns.Cast<ColumnHeader>().Select(c => c.Text)));
        foreach (var row in rows)
        {
            sb.AppendLine(column is { } c
                ? row.SubItems[c].Text
                : string.Join('\t', row.SubItems.Cast<ListViewItem.ListViewSubItem>().Select(s => s.Text)));
        }
        Clipboard.SetText(sb.ToString().TrimEnd());
    }
}
