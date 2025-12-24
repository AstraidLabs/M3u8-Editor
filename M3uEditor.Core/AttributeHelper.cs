using System.Text;

namespace M3uEditor.Core;

public sealed record AttributeEntry(string Name, string Value);

public sealed class AttributeCollection : IEnumerable<AttributeEntry>
{
    private readonly List<AttributeEntry> _entries = new();

    public IEnumerable<string> Keys => _entries.Select(e => e.Name);

    public AttributeEntry? Find(string name) =>
        _entries.FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public void AddOrUpdate(string name, string value)
    {
        var existing = _entries.FindIndex(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing >= 0)
        {
            _entries[existing] = new AttributeEntry(name, value);
        }
        else
        {
            _entries.Add(new AttributeEntry(name, value));
        }
    }

    public void Add(AttributeEntry entry) => _entries.Add(entry);

    public string ToJoinedString(char separator, bool includeSpace = false)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < _entries.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(separator);
                if (includeSpace)
                {
                    builder.Append(' ');
                }
            }

            builder.Append(_entries[i].Name);
            builder.Append('=');
            builder.Append(_entries[i].Value);
        }

        return builder.ToString();
    }

    public IEnumerator<AttributeEntry> GetEnumerator() => _entries.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _entries.GetEnumerator();
}

public static class AttributeHelper
{
    public static AttributeCollection ParseAttributeList(string text, char separator, bool includeEmpty = false)
    {
        var collection = new AttributeCollection();
        if (string.IsNullOrEmpty(text))
        {
            return collection;
        }

        var tokenBuilder = new StringBuilder();
        var inQuotes = false;
        void FlushToken()
        {
            var token = tokenBuilder.ToString();
            tokenBuilder.Clear();
            if (string.IsNullOrWhiteSpace(token) && !includeEmpty)
            {
                return;
            }

            var parts = token.Split('=', 2);
            if (parts.Length == 2)
            {
                collection.Add(new AttributeEntry(parts[0].Trim(), parts[1].Trim()));
            }
        }

        foreach (var ch in text)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
            }

            if (ch == separator && !inQuotes)
            {
                FlushToken();
                continue;
            }

            tokenBuilder.Append(ch);
        }

        FlushToken();
        return collection;
    }
}
