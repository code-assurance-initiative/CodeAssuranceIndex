using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Cai.Tests;

/// <summary>
/// The reference CLI end to end, the way a producer and a buyer use it: mint a key, sign evidence into a
/// delivery, verify it offline — and refuse it once it has been tampered with.
/// </summary>
/// <remarks>
/// ★★ THE CLI IS THE STANDARD'S OWN PROOF THAT ITS NUMBERS ARE CHECKABLE, and no test loaded it: everything it
/// calls was tested, the composition of those calls behind each verb was not. Run IN PROCESS through the
/// assembly's entry point rather than as a child process, so a failure shows its stack and coverage sees it.
/// <para>★ Serialised: the CLI writes to the process-wide Console, which these tests redirect.</para>
/// </remarks>
[Collection(CliCollection.Name)]
public sealed class CliTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("cai-cli-tests").FullName;

    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string Rubrics = Path.Combine(RepoRoot, "rubrics");
    private static readonly string Evidence = Path.Combine(RepoRoot, "examples", "evidence.sample.json");

    [Fact]
    public async Task No_verb_prints_usage_and_exits_2()
    {
        var run = await Cai();

        Assert.Equal(2, run.Exit);
        Assert.Contains("cai — the Code Assurance Index reference tools", run.Err, StringComparison.Ordinal);
    }

    /// <summary>★ A number is only meaningful under the rubric it was folded with, so there is no default.</summary>
    [Fact]
    public async Task Score_refuses_to_fold_without_the_rubric_archive()
    {
        var run = await Cai("score", Evidence);

        Assert.Equal(2, run.Exit);
        Assert.Contains("score needs --rubrics", run.Err, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Score_folds_the_sample_evidence_to_a_headline()
    {
        var run = await Cai("score", Evidence, "--rubrics", Rubrics);

        Assert.True(run.Exit == 0, run.Err);
        Assert.StartsWith("CAI ", run.Out, StringComparison.Ordinal);
        Assert.Contains("rubric ", run.Out, StringComparison.Ordinal);
    }

    /// <summary>
    /// ★★ A NUMBER IS SPELLED THE SAME ON EVERY MACHINE. Under a Danish culture the CLI printed "CAI 70,3" and
    /// "Δ0,01" — and its stdout is what a CI step parses.
    /// </summary>
    [Fact]
    public async Task The_output_uses_a_decimal_point_whatever_the_hosts_culture()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("da-DK");
        try
        {
            var score = await Cai("score", Evidence, "--rubrics", Rubrics);
            var verify = await Cai("verify", Evidence, "--rubrics", Rubrics);

            Assert.Matches(@"^CAI \d+\.\d ", score.Out);
            Assert.Matches(@"Δ\d+\.\d\d", verify.Out);
            Assert.DoesNotMatch(@"\d,\d", score.Out + verify.Out);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public async Task A_missing_input_file_is_a_usage_error_not_a_crash()
    {
        var run = await Cai("score", Path.Combine(_dir, "nope.json"));

        Assert.Equal(2, run.Exit);
        Assert.StartsWith("error:", run.Err, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Keygen_without_a_key_id_says_what_it_needs()
    {
        var run = await Cai("keygen");

        Assert.Equal(2, run.Exit);
        Assert.Contains("keygen needs a keyId", run.Err, StringComparison.Ordinal);
    }

    /// <summary>
    /// ★★ THE ROUND TRIP THE WHOLE DELIVERY SPEC PROMISES: what a producer signs, a buyer holding only the
    /// public key set and the rubric archive verifies offline — and one changed byte of the verdict fails it.
    /// </summary>
    [Fact]
    public async Task A_signed_delivery_verifies_offline_and_a_tampered_one_does_not()
    {
        var pair = Path.Combine(_dir, "pair.json");
        var keys = Path.Combine(_dir, "keys.json");
        var package = Path.Combine(_dir, "package.json");

        var keygen = await Cai("keygen", "cai-test-key", "--out", pair);
        Assert.Equal(0, keygen.Exit);
        await File.WriteAllTextAsync(keys, keygen.Out, TestContext.Current.CancellationToken);

        var sign = await Cai(
            "sign", Evidence, "--key", pair, "--rubrics", Rubrics, "--repo", "acme/api",
            "--commit", "3f9a1c2", "--issued-at", "2026-09-29T12:00:00Z", "--out", package);
        Assert.True(sign.Exit == 0, sign.Err);

        var verify = await Cai("verify-delivery", package, "--keys", keys, "--rubrics", Rubrics);
        Assert.True(verify.Exit == 0, verify.Out + verify.Err);

        var tampered = JsonDocument.Parse(await File.ReadAllTextAsync(package, TestContext.Current.CancellationToken))
            .RootElement.ToString().Replace("acme/api", "acme/evil", StringComparison.Ordinal);
        await File.WriteAllTextAsync(package, tampered, TestContext.Current.CancellationToken);

        var refused = await Cai("verify-delivery", package, "--keys", keys, "--no-reproduce");
        Assert.Equal(1, refused.Exit);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_dir, recursive: true);
        }
        catch (IOException)
        {
            // A held handle on a temp directory is not worth failing a test over.
        }
    }

    private sealed record Run(int Exit, string Out, string Err);

    private static async Task<Run> Cai(params string[] args)
    {
        var entry = Assembly.Load("cai").EntryPoint
            ?? throw new InvalidOperationException("the cai assembly has no entry point");
        var (stdout, stderr) = (Console.Out, Console.Error);
        using var o = new StringWriter();
        using var e = new StringWriter();
        Console.SetOut(o);
        Console.SetError(e);
        try
        {
            var result = entry.Invoke(null, [args]);
            var exit = result switch
            {
                Task<int> task => await task,
                int code => code,
                _ => throw new InvalidOperationException($"unexpected entry point result: {result}"),
            };
            return new Run(exit, o.ToString(), e.ToString());
        }
        finally
        {
            Console.SetOut(stdout);
            Console.SetError(stderr);
        }
    }

    private static string FindRepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Cai.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException("could not find the repository root (Cai.slnx) above the test output");
    }
}

/// <summary>Tests that redirect the process-wide Console run one at a time.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CliCollection
{
    public const string Name = "cli-console";
}
