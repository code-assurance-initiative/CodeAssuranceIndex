using System.Text.Json;
using Cai.Web;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// The standard's vocabulary as published at <c>/glossary.jsonld</c> — a schema.org DefinedTermSet.
/// </summary>
public sealed class GlossaryTests
{
    private static JsonElement Published() => JsonDocument.Parse(CaiGlossary.Build()).RootElement;

    [Fact]
    public void It_is_a_schema_org_defined_term_set()
    {
        var set = Published();

        Assert.Equal("https://schema.org", set.GetProperty("@context").GetString());
        Assert.Equal("DefinedTermSet", set.GetProperty("@type").GetString());
        Assert.Equal("https://codeassuranceindex.info/glossary.jsonld", set.GetProperty("url").GetString());
    }

    [Fact]
    public void Every_term_is_published_once_in_order_with_its_code_name_and_gloss()
    {
        var published = Published().GetProperty("hasDefinedTerm").EnumerateArray().ToList();

        Assert.Equal(CaiGlossary.Terms.Count, published.Count);
        foreach (var (term, json) in CaiGlossary.Terms.Zip(published))
        {
            Assert.Equal("DefinedTerm", json.GetProperty("@type").GetString());
            Assert.Equal(term.Key, json.GetProperty("termCode").GetString());
            Assert.Equal(term.En, json.GetProperty("name").GetString());
            Assert.Equal(term.Gloss, json.GetProperty("description").GetString());
        }

        Assert.Equal(published.Count, published.Select(t => t.GetProperty("termCode").GetString()).Distinct().Count());
    }

    /// <summary>
    /// ★ A DANISH NAME ONLY WHERE ONE WAS COINED. A term with no Danish pairing carries no alternateName at
    /// all, rather than an empty or English one a reader would take for a translation.
    /// </summary>
    [Fact]
    public void The_Danish_name_is_published_exactly_where_one_exists()
    {
        var published = Published().GetProperty("hasDefinedTerm").EnumerateArray().ToList();

        foreach (var (term, json) in CaiGlossary.Terms.Zip(published))
        {
            var has = json.TryGetProperty("alternateName", out var da);
            Assert.Equal(term.Da is not null, has);
            if (has)
            {
                Assert.Equal(term.Da, da.GetString());
            }
        }

        Assert.Contains(CaiGlossary.Terms, t => t.Da is not null);
        Assert.Contains(CaiGlossary.Terms, t => t.Da is null);
    }
}
