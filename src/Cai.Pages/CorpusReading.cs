using Cai.Delivery;
using Cai.Scoring;

namespace Cai.Pages;

/// <summary>
/// The corpus read as one measurement, on a stated instant: what was FOUND across every measured codebase,
/// folded from the same signed deliveries the scores come from.
/// </summary>
/// <remarks>
/// <para>★★ EVERY FIGURE HERE IS A COUNT OF CODEBASES. A repository carrying 985 known-vulnerable
/// components is ONE affected codebase, not 985 votes; only <see cref="Findings"/> and its severity
/// siblings count findings. A fold that got this the other way round would publish a corpus shaped like
/// its single worst repository, and no wording on the page could undo it.</para>
///
/// <para>★★ "NOT MEASURED" IS NEVER FOLDED INTO "CLEAN". A codebase whose dependency graph nobody could
/// resolve is not a codebase with no vulnerabilities — it is one that was not asked. It leaves the
/// denominator and is counted in <see cref="VulnUnmeasured"/>, so a page states its share over what was
/// actually measured. This is how a corpus comes to report 3,464 codebases measured when it measured
/// 1,588, and it is the one mistake the whole shape of this type exists to prevent.</para>
///
/// <para>★ A DELIVERY THAT CARRIES NO READING IS IN NO DENOMINATOR AT ALL. Silence from a producer is not
/// a clean bill of health; <see cref="SecurityReadings"/> says how many of <see cref="Codebases"/> said
/// anything.</para>
/// </remarks>
public sealed record CorpusReading
{
    private CorpusReading(
        DateTimeOffset takenAt,
        int codebases,
        IReadOnlyList<SecurityReading> readings,
        AdvisoryCut advisories,
        OriginCut origins)
    {
        TakenAt = takenAt;
        Codebases = codebases;
        SecurityReadings = readings.Count;
        Advisories = advisories;
        Origins = origins;

        VulnMeasurable = readings.Count(r => r.VulnMeasurable);
        VulnUnmeasured = readings.Count(r => r.VulnUnmeasured);
        VulnScanFailed = readings.Count(r => r.VulnScanFailed);
        VulnAffected = readings.Count(r => r.VulnAffected);
        VulnHighOrCritical = readings.Count(r => r.VulnHighOrCritical);
        VulnCritical = readings.Count(r => r.VulnCritical);

        Findings = readings.Sum(r => r.Findings);
        FindingsCritical = readings.Sum(r => r.FindingsCritical);
        FindingsHigh = readings.Sum(r => r.FindingsHigh);
        FindingsMedium = readings.Sum(r => r.FindingsMedium);
        FindingsLow = readings.Sum(r => r.FindingsLow);

        DisclosureMeasured = readings.Count(r => r.DisclosureMeasured);
        DisclosureUnmeasured = readings.Count(r => r.DisclosureUnmeasured);
        DisclosurePolicy = readings.Count(r => r.DisclosurePolicy);
        DisclosureContact = readings.Count(r => r.DisclosureContact);

        SecretsHistoryMeasured = readings.Count(r => r.SecretsHistoryMeasured);
        SecretsHistoryAffected = readings.Count(r => r.SecretsHistoryAffected);
        SecretsHistoryFindings = readings.Sum(r => r.SecretsHistoryFindings);
        SecretsCurrentMeasured = readings.Count(r => r.SecretsCurrentMeasured);
        SecretsCurrentAffected = readings.Count(r => r.SecretsCurrentAffected);

        SupplyChainMeasurable = readings.Count(r => r.SupplyChainMeasurable);
        Sbom = readings.Count(r => r.Sbom);
        Provenance = readings.Count(r => r.Provenance);
        Signing = readings.Count(r => r.Signing);
        PinnedActions = readings.Count(r => r.PinnedActions);
        NoneOfFour = readings.Count(r => r.NoneOfFour);
    }

    /// <summary>The instant the corpus was read. ★ Not the instant a page was built.</summary>
    public DateTimeOffset TakenAt { get; }

    /// <summary>Every measured codebase in the corpus, whether or not it said anything about security.</summary>
    public int Codebases { get; }

    /// <summary>How many of them carried a security reading at all — the population behind every figure below.</summary>
    public int SecurityReadings { get; }

    /// <summary>Codebases whose dependency graph a scanner resolved.</summary>
    public int VulnMeasurable { get; }

    /// <summary>Codebases nobody could look at.</summary>
    public int VulnUnmeasured { get; }

    /// <summary>Of those, the ones where a scanner ran and fell over.</summary>
    public int VulnScanFailed { get; }

    /// <summary>Codebases carrying at least one known-vulnerable component.</summary>
    public int VulnAffected { get; }

    /// <summary>Codebases carrying at least one High or Critical.</summary>
    public int VulnHighOrCritical { get; }

    /// <summary>Codebases carrying at least one Critical.</summary>
    public int VulnCritical { get; }

    /// <summary>Vulnerability findings across the corpus. ★ The only family of figures here that counts findings.</summary>
    public long Findings { get; }

    /// <summary>Critical findings.</summary>
    public long FindingsCritical { get; }

    /// <summary>High findings.</summary>
    public long FindingsHigh { get; }

    /// <summary>Medium findings.</summary>
    public long FindingsMedium { get; }

    /// <summary>Low findings.</summary>
    public long FindingsLow { get; }

    /// <summary>Codebases where a disclosure policy was actually looked for.</summary>
    public int DisclosureMeasured { get; }

    /// <summary>Codebases where it never was — an era fact about the surveys, never about the codebases.</summary>
    public int DisclosureUnmeasured { get; }

    /// <summary>Codebases publishing a disclosure policy.</summary>
    public int DisclosurePolicy { get; }

    /// <summary>Codebases whose policy names a reporting contact.</summary>
    public int DisclosureContact { get; }

    /// <summary>Codebases whose full history was scanned for secrets.</summary>
    public int SecretsHistoryMeasured { get; }

    /// <summary>Of those, the ones that ever committed one.</summary>
    public int SecretsHistoryAffected { get; }

    /// <summary>Historical secret findings across the corpus.</summary>
    public long SecretsHistoryFindings { get; }

    /// <summary>Codebases whose current files were scanned for secrets.</summary>
    public int SecretsCurrentMeasured { get; }

    /// <summary>Of those, the ones still carrying one.</summary>
    public int SecretsCurrentAffected { get; }

    /// <summary>Codebases with a release pipeline the four supply-chain controls could be judged against.</summary>
    public int SupplyChainMeasurable { get; }

    /// <summary>Of those, the ones publishing an SBOM.</summary>
    public int Sbom { get; }

    /// <summary>Of those, the ones publishing build provenance.</summary>
    public int Provenance { get; }

    /// <summary>Of those, the ones signing their releases.</summary>
    public int Signing { get; }

    /// <summary>Of those, the ones pinning their CI actions.</summary>
    public int PinnedActions { get; }

    /// <summary>Of those, the ones doing none of the four.</summary>
    public int NoneOfFour { get; }

    /// <summary>The advisory and package cut, carrying the size of its own blind spot.</summary>
    public AdvisoryCut Advisories { get; }

    /// <summary>Where the corpus comes from, carrying its own denominators.</summary>
    public OriginCut Origins { get; }

    /// <summary>Folds every delivery's security reading into one reading of the corpus.</summary>
    /// <param name="records">One record per measured codebase.</param>
    /// <param name="takenAt">The instant the corpus was read.</param>
    public static CorpusReading From(IEnumerable<SurveyRecord> records, DateTimeOffset takenAt)
    {
        ArgumentNullException.ThrowIfNull(records);

        var all = records.ToList();
        var readings = all.Select(r => r.Latest.Evidence.SecurityReading).OfType<SecurityReading>().ToList();
        return new CorpusReading(takenAt, all.Count, readings, AdvisoryCut.Of(readings), OriginCut.Of(all));
    }
}

/// <summary>
/// The advisory and package cut of a corpus reading, carrying the size of its own blind spot.
/// </summary>
/// <remarks>
/// ★★ <see cref="Surveys"/> = <see cref="SurveysComplete"/> + <see cref="SurveysTruncated"/>, always, and
/// the three travel together: a table of advisory counts presented alone reads as a census of the corpus
/// while actually being a census of the part of the corpus small enough to print. The truncated count is
/// how big the unprintable part is.
/// </remarks>
public sealed record AdvisoryCut
{
    private AdvisoryCut(
        int surveys,
        int surveysComplete,
        int surveysTruncated,
        IReadOnlyDictionary<string, AdvisoryStat> byAdvisory,
        IReadOnlyDictionary<string, PackageStat> byPackage)
    {
        Surveys = surveys;
        SurveysComplete = surveysComplete;
        SurveysTruncated = surveysTruncated;
        ByAdvisory = byAdvisory;
        ByPackage = byPackage;
    }

    /// <summary>Codebases whose survey was read for advisories at all — the population behind every figure here.</summary>
    public int Surveys { get; }

    /// <summary>Of those, the ones where every finding the counts describe also appeared as a readable row.</summary>
    public int SurveysComplete { get; }

    /// <summary>Of those, the ones where it did not. Any of these may carry any advisory below uncounted.</summary>
    public int SurveysTruncated { get; }

    /// <summary>Per advisory id, keyed verbatim as the ecosystem writes it.</summary>
    public IReadOnlyDictionary<string, AdvisoryStat> ByAdvisory { get; }

    /// <summary>Per package name, verbatim.</summary>
    public IReadOnlyDictionary<string, PackageStat> ByPackage { get; }

    /// <summary>Folds the advisory lists of every reading that was read for them.</summary>
    internal static AdvisoryCut Of(IReadOnlyList<SecurityReading> readings)
    {
        var read = readings.Where(r => r.AdvisoriesRead).ToList();
        var byAdvisory = new Dictionary<string, Accumulator>(StringComparer.OrdinalIgnoreCase);
        var byPackage = new Dictionary<string, PackageAccumulator>(StringComparer.Ordinal);

        foreach (var survey in read)
        {
            var complete = survey.AdvisoryListComplete;

            // ★ GROUPED PER SURVEY BEFORE ANYTHING IS INCREMENTED. The unit of every count below is a
            //   CODEBASE, so one advisory seen against three versions inside one repository is one.
            foreach (var group in survey.Advisories.GroupBy(a => a.AdvisoryId, StringComparer.OrdinalIgnoreCase))
            {
                if (!byAdvisory.TryGetValue(group.Key, out var advisory))
                {
                    // ★ The FIRST spelling seen is the one published. Ids are collapsed case-insensitively
                    //   where they are counted and never rewritten: a GHSA id is canonically lower case and a
                    //   CVE id upper, and a reader pastes what the page shows into the advisory database.
                    byAdvisory[group.Key] = advisory = new Accumulator(group.First().AdvisoryId);
                }

                advisory.Surveys++;
                if (complete)
                {
                    advisory.SurveysAmongComplete++;
                }

                if (group.All(a => a.Inherited))
                {
                    advisory.SurveysInherited++;
                }

                foreach (var seen in group)
                {
                    advisory.Packages[seen.Package] = advisory.Packages.GetValueOrDefault(seen.Package) + 1;
                }
            }

            foreach (var group in survey.Advisories.GroupBy(a => a.Package, StringComparer.Ordinal))
            {
                if (!byPackage.TryGetValue(group.Key, out var package))
                {
                    byPackage[group.Key] = package = new PackageAccumulator();
                }

                package.Surveys++;
                if (complete)
                {
                    package.SurveysAmongComplete++;
                }

                if (group.All(a => a.Inherited))
                {
                    package.SurveysInherited++;
                }

                foreach (var seen in group)
                {
                    package.Advisories.Add(seen.AdvisoryId);
                }
            }
        }

        return new AdvisoryCut(
            read.Count,
            read.Count(r => r.AdvisoryListComplete),
            read.Count(r => !r.AdvisoryListComplete),
            byAdvisory.ToDictionary(
                kv => kv.Value.Id,
                kv => new AdvisoryStat(
                    kv.Value.Packages.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal)
                        .Select(p => p.Key).FirstOrDefault() ?? "",
                    kv.Value.Packages.Count,
                    kv.Value.Surveys,
                    kv.Value.SurveysInherited,
                    kv.Value.SurveysAmongComplete),
                StringComparer.Ordinal),
            byPackage.ToDictionary(
                kv => kv.Key,
                kv => new PackageStat(
                    kv.Value.Advisories.Count,
                    kv.Value.Surveys,
                    kv.Value.SurveysInherited,
                    kv.Value.SurveysAmongComplete),
                StringComparer.Ordinal));
    }

    private sealed class Accumulator(string id)
    {
        public string Id { get; } = id;

        public Dictionary<string, int> Packages { get; } = new(StringComparer.Ordinal);

        public int Surveys { get; set; }

        public int SurveysInherited { get; set; }

        public int SurveysAmongComplete { get; set; }
    }

    private sealed class PackageAccumulator
    {
        public HashSet<string> Advisories { get; } = new(StringComparer.OrdinalIgnoreCase);

        public int Surveys { get; set; }

        public int SurveysInherited { get; set; }

        public int SurveysAmongComplete { get; set; }
    }
}

/// <summary>
/// What one advisory could be SEEN to do across the corpus — a floor, and every field is shaped so a reader
/// is told it is one.
/// </summary>
/// <remarks>
/// ★★ <see cref="Surveys"/> IS A LOWER BOUND AND MUST NEVER BE PUBLISHED AS A TOTAL. Individual advisories
/// survive only in a survey's readable list, which a producer may have had to truncate, and the truncation
/// bites hardest on the repositories carrying the most vulnerabilities — so the error is not noise, it is
/// systematic and always in the same direction.
/// <para>★ WHICH IS WHY <see cref="SurveysAmongComplete"/> EXISTS BESIDE IT. A share of the codebases whose
/// list was COMPLETE is exact and checkable, because that population is not censored. A share of the whole
/// corpus cannot be: its numerator is a floor and its denominator is not, and no amount of wording fixes a
/// percentage built out of two different kinds of number.</para>
/// </remarks>
/// <param name="Package">The package this advisory was most often seen against.</param>
/// <param name="Packages">Distinct packages it was seen against. Normally 1; more than 1 means the same
/// advisory id reached the corpus under more than one package name, and the page has to say so rather than
/// silently picking one.</param>
/// <param name="Surveys">Codebases where it was VISIBLE. A floor.</param>
/// <param name="SurveysInherited">Of those, the ones where every sighting was inherited rather than chosen.</param>
/// <param name="SurveysAmongComplete">Of the codebases whose advisory list was complete, the ones carrying it.</param>
public sealed record AdvisoryStat(
    string Package,
    int Packages,
    int Surveys,
    int SurveysInherited,
    int SurveysAmongComplete);

/// <summary>How widespread one vulnerable package is, on the same floor-and-complete footing as an advisory.</summary>
/// <param name="Advisories">Distinct advisories seen raised against it.</param>
/// <param name="Surveys">Codebases where it was visible carrying at least one advisory. A floor.</param>
/// <param name="SurveysInherited">Of those, the ones where it was inherited rather than chosen.</param>
/// <param name="SurveysAmongComplete">Of the codebases whose advisory list was complete, the ones carrying it.</param>
public sealed record PackageStat(
    int Advisories,
    int Surveys,
    int SurveysInherited,
    int SurveysAmongComplete);

/// <summary>
/// Where the corpus comes FROM, as far as anything can honestly say — and the denominators that make it
/// readable.
/// </summary>
/// <remarks>
/// <para>★★ THE DENOMINATORS ARE THE WHOLE POINT. The only origin signal that exists is the owning forge
/// account's free-text profile line, and about half of owners leave it blank. So the country map never
/// travels alone: it travels with how many owners were considered, how many declared anything and how many
/// resolved, because a country share taken over the accounts that happen to fill in a profile field
/// measures who fills in a profile field, not where software is written.</para>
///
/// <para>★★ CODEBASES PER COUNTRY IS NOT A HEADCOUNT. One prolific account can put forty codebases in a
/// country that has three people in it, so <see cref="CodebasesByCountry"/> and
/// <see cref="OwnersByCountry"/> are carried separately and a page shows both.</para>
///
/// <para>★ AN UNRESOLVED OWNER IS ABSENT FROM THE MAP, never mapped to a placeholder — which is how an
/// "unknown" bucket becomes a country on a chart.</para>
/// </remarks>
public sealed record OriginCut
{
    private OriginCut(
        int codebases,
        int codebasesDeclared,
        int codebasesResolved,
        int ownersConsidered,
        int ownersDeclared,
        int ownersResolved,
        IReadOnlyDictionary<string, int> codebasesByCountry,
        IReadOnlyDictionary<string, int> ownersByCountry)
    {
        Codebases = codebases;
        CodebasesDeclared = codebasesDeclared;
        CodebasesResolved = codebasesResolved;
        OwnersConsidered = ownersConsidered;
        OwnersDeclared = ownersDeclared;
        OwnersResolved = ownersResolved;
        CodebasesByCountry = codebasesByCountry;
        OwnersByCountry = ownersByCountry;
    }

    /// <summary>Every codebase asked about — THE denominator for anything said about codebases.</summary>
    public int Codebases { get; }

    /// <summary>Of those, the ones whose owner put something in the field, placeable or not.</summary>
    public int CodebasesDeclared { get; }

    /// <summary>Of those, the ones whose owner resolved to a country.</summary>
    public int CodebasesResolved { get; }

    /// <summary>Distinct owners across those codebases — THE denominator for anything said about owners.</summary>
    /// <remarks>
    /// ★ An owner is <c>(forge, account)</c>. The same name on two forges is two owners: an account is only
    /// unique within a provider, and folding them would put one account's codebases in another's country.
    /// </remarks>
    public int OwnersConsidered { get; }

    /// <summary>Of those, how many wrote something in the field.</summary>
    public int OwnersDeclared { get; }

    /// <summary>Of those, how many resolved to a country.</summary>
    public int OwnersResolved { get; }

    /// <summary>Codebases per country. ★ Not a headcount.</summary>
    public IReadOnlyDictionary<string, int> CodebasesByCountry { get; }

    /// <summary>Distinct owners per country.</summary>
    public IReadOnlyDictionary<string, int> OwnersByCountry { get; }

    /// <summary>Folds every subject's origin into the corpus's.</summary>
    internal static OriginCut Of(IReadOnlyList<SurveyRecord> records)
    {
        var owners = new Dictionary<string, SubjectOrigin?>(StringComparer.Ordinal);
        var codebasesByCountry = new Dictionary<string, int>(StringComparer.Ordinal);
        var declaredCodebases = 0;
        var resolvedCodebases = 0;

        foreach (var record in records)
        {
            var subject = record.Latest.Subject;
            var origin = subject.Origin;

            if (origin?.Declared == true)
            {
                declaredCodebases++;
            }

            if (origin?.Country is { Length: > 0 } country)
            {
                resolvedCodebases++;
                codebasesByCountry[country] = codebasesByCountry.GetValueOrDefault(country) + 1;
            }

            // ★ An owner is (forge, account). The first origin seen for an owner is the one kept: two
            //   deliveries about one account's repositories describe one account, and a later one saying
            //   nothing must not erase what an earlier one said.
            var key = OwnerKey(subject);
            if (!owners.TryGetValue(key, out var known) || known is null)
            {
                owners[key] = origin ?? known;
            }
        }

        var ownersByCountry = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var country in owners.Values.Select(o => o?.Country).OfType<string>())
        {
            ownersByCountry[country] = ownersByCountry.GetValueOrDefault(country) + 1;
        }

        return new OriginCut(
            records.Count,
            declaredCodebases,
            resolvedCodebases,
            owners.Count,
            owners.Values.Count(o => o?.Declared == true),
            owners.Values.Count(o => o?.Country is { Length: > 0 }),
            codebasesByCountry,
            ownersByCountry);
    }

    /// <summary>The owner's identity: the forge and the account, because an account is only unique within one.</summary>
    private static string OwnerKey(DeliverySubject subject)
    {
        var repository = subject.Repository ?? "";
        var slash = repository.LastIndexOf('/');
        var account = slash > 0 ? repository[..slash] : repository;
        return $"{subject.Host}/{account}";
    }
}
