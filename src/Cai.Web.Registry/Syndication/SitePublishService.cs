using System.Globalization;
using System.Text.Json;
using Cai.Delivery;
using Cai.Pages;

namespace Cai.Web.Registry;

/// <summary>What one sweep did.</summary>
/// <param name="Built">Pages the standard composed and stands behind.</param>
/// <param name="Changed">Of those, the ones whose content the site did not already hold.</param>
/// <param name="Withdrawn">Pages the site was serving that nothing stands behind any more.</param>
/// <param name="Failed">Pages that could not be pushed.</param>
public sealed record SiteSweep(int Built, int Changed, int Withdrawn, int Failed);

/// <summary>
/// Composes every page the standard stands behind and reconciles the site against them.
/// </summary>
/// <remarks>
/// <para>★★ WITHDRAWAL IS BY RECONCILIATION, NOT BY EVENT. A subject whose owner withdraws publication
/// raises nothing here; the only way to know which pages are now orphans is to compare what the site serves
/// against what the standard would compose today.</para>
///
/// <para>★★ WHICH IS WHY EVERY "COULD NOT FIND OUT" FAILS CLOSED, IN BOTH DIRECTIONS. A site that could not
/// be listed and a corpus that composed nothing are each indistinguishable, from here, from "there is
/// nothing" — and a reconciliation against nothing deletes everything the site holds under these roots. So
/// a null listing withdraws nothing, and a sweep that composed no pages withdraws nothing.</para>
///
/// <para>★★ OWNERSHIP IS ASSERTED BY ADDRESS, AND BY WHOLE SEGMENTS. The site serves pages this system did
/// not write — other producers', the standard's own hand-authored prose — and a sweep that deleted every
/// path it did not recognise would take theirs with it. A path that merely shares a PREFIX is not ours:
/// <c>surveys-of-other-things</c> is a different page from anything under <c>surveys/</c>.</para>
/// </remarks>
public sealed class SitePublishService(
    IRegistryStore store,
    ISiteSyndication syndication,
    SiteSweepMemory memory,
    TimeProvider clock,
    ILogger<SitePublishService> logger)
{
    /// <summary>The roots this system publishes under, and is therefore allowed to withdraw from.</summary>
    private static readonly string[] Roots = [SurveyPageBuilder.Root, CorpusSheetBuilder.Root];

    /// <summary>
    /// Whether <paramref name="path"/> is a page this system owns, and may therefore withdraw.
    /// </summary>
    /// <remarks>
    /// ★ WHOLE SEGMENTS. <c>state-of-the-corpus-elsewhere</c> starts with the corpus root and is not under
    /// it; deleting it because of a string prefix would be this system reaching outside its own namespace.
    /// </remarks>
    public static bool Owns(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var trimmed = path.Trim('/');
        return Roots.Any(root =>
            string.Equals(trimmed, root, StringComparison.Ordinal)
            || trimmed.StartsWith(root + "/", StringComparison.Ordinal));
    }

    /// <summary>Compose everything, push it, and reconcile what the site serves against it.</summary>
    public async Task<SiteSweep> SweepAsync(CancellationToken cancellationToken)
    {
        var takenAt = clock.GetUtcNow();
        var records = PublishedRecords();

        // ★ ASKED ONCE, BEFORE ANYTHING IS PUSHED, so the reconciliation is a statement about the site as it
        //   was when this sweep started rather than about the site this sweep has been editing.
        var serving = await ServingAsync(cancellationToken).ConfigureAwait(false);

        if (records.Count == 0)
        {
            // ★★ NOTHING COMPOSED, NOTHING WITHDRAWN. "No subject is published" and "the registry could not
            //    be read" are indistinguishable from here, and only one of them means the site should be
            //    emptied — so neither does.
            logger.LogInformation("Site sweep: no published subject, nothing composed and nothing withdrawn.");
            var nothing = new SiteSweep(0, 0, 0, 0);
            memory.Record(nothing, takenAt);
            return nothing;
        }

        var pages = Compose(records, takenAt);
        var pushed = await PushAsync(pages, cancellationToken).ConfigureAwait(false);

        // ★ THE WANTED SET IS EVERY PAGE THIS SWEEP COMPOSED, not every page it managed to push. A page whose
        //   push failed is still a page the standard stands behind, and withdrawing it because a single PUT
        //   returned 503 would delete the site one outage at a time.
        var wanted = pages.Select(p => p.Path).ToHashSet(StringComparer.Ordinal);
        var withdrawn = serving is null
            ? 0
            : await WithdrawOrphansAsync(wanted, serving, cancellationToken).ConfigureAwait(false);

        RecordReading(records, takenAt);

        var sweep = new SiteSweep(pages.Count, pushed.Changed, withdrawn, pushed.Failed);

        // ★ REMEMBERED BEFORE IT IS LOGGED. A log line on a deployed host is not a surface anyone reads on a
        //   normal day, and "the publisher stopped" looks exactly like "nothing changed" from outside.
        memory.Record(sweep, takenAt);
        logger.LogInformation(
            "Site sweep: {Built} built, {Changed} changed, {Withdrawn} withdrawn, {Failed} failed.",
            sweep.Built, sweep.Changed, sweep.Withdrawn, sweep.Failed);
        return sweep;
    }

    /// <summary>
    /// Every published subject's deliveries, as the standard reads them.
    /// </summary>
    /// <remarks>
    /// ★ A SUBJECT WHOSE PACKAGE WILL NOT PARSE IS SKIPPED AND SAID SO, never guessed at. The registry stores
    /// the artifact verbatim; if one of them cannot be read back, that is a fact worth a log line rather than
    /// a page composed from half a delivery.
    /// </remarks>
    private List<SurveyRecord> PublishedRecords()
    {
        var records = new List<SurveyRecord>();

        foreach (var subject in store.ListPublishedSubjects())
        {
            var payloads = new List<DeliveryPayload>();
            foreach (var delivery in store.ListByOwnerAndRepositories(subject.OwnerOrgId, [subject.Repository]))
            {
                if (Parse(delivery) is { } payload)
                {
                    payloads.Add(payload);
                }
            }

            if (payloads.Count > 0)
            {
                records.Add(SurveyRecord.From(payloads));
            }
        }

        return records;
    }

    private DeliveryPayload? Parse(DeliveryRecord delivery)
    {
        try
        {
            return DeliveryPackage.Parse(delivery.PackageJson).Payload;
        }
        catch (JsonException e)
        {
            logger.LogWarning(
                e, "Delivery {DeliveryId} could not be read back and is not on any page.", delivery.DeliveryId);
            return null;
        }
    }

    /// <summary>Every page the standard publishes about this corpus.</summary>
    private List<SurveyPage> Compose(IReadOnlyList<SurveyRecord> records, DateTimeOffset takenAt)
    {
        var pages = new List<SurveyPage>();

        // One portrait per subject.
        pages.AddRange(records.Select(SurveyPageBuilder.Build).OfType<SurveyPage>());

        // The catalogue: the index over the field guides, and the guides themselves.
        pages.Add(SurveyIndexBuilder.Build(records, takenAt));
        pages.AddRange(SurveyCorpus.Fields(records).Select(FieldGuideBuilder.Build).OfType<SurveyPage>());

        // The corpus read as one document, with everything under its own root.
        var history = store.ListCorpusReadings().Select(Dated).ToList();
        pages.Add(CorpusSheetBuilder.Build(records, takenAt, history));
        pages.AddRange(CorpusGroupPages.Build(records, takenAt));
        pages.AddRange(CorpusAdvisoryPages.Build(CorpusReading.From(records, takenAt), takenAt));

        return pages;
    }

    private static DatedReading Dated(CorpusReadingRecord row) => new(
        DateTimeOffset.TryParse(row.TakenAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var at)
            ? at
            : DateTimeOffset.MinValue,
        row.Codebases,
        row.MedianCai);

    /// <summary>Push each page, counting real change and never stopping on one failure.</summary>
    private async Task<(int Changed, int Failed)> PushAsync(
        IReadOnlyList<SurveyPage> pages, CancellationToken cancellationToken)
    {
        var changed = 0;
        var failed = 0;

        foreach (var page in pages)
        {
            try
            {
                if (await syndication.PublishAsync(page, cancellationToken).ConfigureAwait(false))
                {
                    changed++;
                }
            }
            catch (HttpRequestException e)
            {
                failed++;
                logger.LogWarning(e, "Page {Path} could not be published.", page.Path);
            }
        }

        return (changed, failed);
    }

    /// <summary>Withdraw the pages under this system's own roots that nothing stands behind any more.</summary>
    private async Task<int> WithdrawOrphansAsync(
        IReadOnlySet<string> wanted, IReadOnlyList<string> serving, CancellationToken cancellationToken)
    {
        var doomed = serving
            .Select(p => p.Trim('/'))
            .Where(p => Owns(p) && !wanted.Contains(p))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var withdrawn = 0;
        foreach (var path in doomed)
        {
            try
            {
                if (await syndication.WithdrawAsync(path, cancellationToken).ConfigureAwait(false))
                {
                    withdrawn++;
                }
            }
            catch (HttpRequestException e)
            {
                logger.LogWarning(e, "Page {Path} could not be withdrawn.", path);
            }
        }

        return withdrawn;
    }

    private async Task<IReadOnlyList<string>?> ServingAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await syndication.ListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException e)
        {
            // ★ NULL, NOT EMPTY. The question was unanswerable, and an unanswerable question withdraws
            //   nothing — see the type's own remarks.
            logger.LogWarning(e, "The site could not be listed; nothing will be withdrawn this sweep.");
            return null;
        }
    }

    /// <summary>
    /// Record what this sweep published, as the point the trend line is drawn through.
    /// </summary>
    /// <remarks>
    /// ★ ONE SWEEP, ONE READING, and a sweep that published nothing records nothing — a zero point would be
    /// a reading of an empty corpus that never happened.
    /// </remarks>
    private void RecordReading(IReadOnlyList<SurveyRecord> records, DateTimeOffset takenAt)
    {
        var scores = records.Select(r => r.Latest.Verdict.Cai).ToList();
        var reading = CorpusReading.From(records, takenAt);

        store.RecordCorpusReading(new CorpusReadingRecord(
            takenAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            records.Count,
            scores.Count == 0 ? null : SurveyCorpus.Median([.. scores]),
            JsonSerializer.Serialize(new
            {
                codebases = records.Count,
                securityReadings = reading.SecurityReadings,
                vulnMeasurable = reading.VulnMeasurable,
                vulnAffected = reading.VulnAffected,
                disclosureMeasured = reading.DisclosureMeasured,
                disclosurePolicy = reading.DisclosurePolicy,
            })));
    }
}
