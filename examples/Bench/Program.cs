using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ason;

Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║        ASON-CSharp vs JSON Comprehensive Benchmark          ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
Console.WriteLine();

const int iterations = 100;
Console.WriteLine($"Iterations per test: {iterations}");

// Section 1: Flat struct
Console.WriteLine();
Console.WriteLine("┌─────────────────────────────────────────────┐");
Console.WriteLine("│  Section 1: Flat Struct (schema-driven vec) │");
Console.WriteLine("└─────────────────────────────────────────────┘");

foreach (var count in new[] { 100, 500, 1000, 5000 })
{
    var r = BenchFlat(count, iterations);
    Console.WriteLine($"  Flat struct × {count} (8 fields)");
    Console.WriteLine($"    Serialize:   JSON {r.jSer,8:F2}ms | ASON {r.aSer,8:F2}ms | ratio {r.jSer / r.aSer:F2}x {(r.jSer >= r.aSer ? "✓ ASON faster" : "")}");
    Console.WriteLine($"    Deserialize: JSON {r.jDe,8:F2}ms | ASON {r.aDe,8:F2}ms | ratio {r.jDe / r.aDe:F2}x {(r.jDe >= r.aDe ? "✓ ASON faster" : "")}");
    Console.WriteLine($"    BIN encode:  ASON {r.bSer,8:F2}ms | ratio {r.jSer / r.bSer:F2}x vs JSON ser {(r.jSer >= r.bSer ? "✓" : "")}");
    Console.WriteLine($"    Size:        JSON {r.jB,8} B | ASON {r.aB,8} B ({(1.0 - (double)r.aB / r.jB) * 100:F0}% smaller) | BIN {r.bB} B ({(1.0 - (double)r.bB / r.jB) * 100:F0}% smaller)");
    Console.WriteLine();
}

// Section 2: 5-level deep
Console.WriteLine("┌──────────────────────────────────────────────────────────┐");
Console.WriteLine("│  Section 2: 5-Level Deep Nesting (Company hierarchy)    │");
Console.WriteLine("└──────────────────────────────────────────────────────────┘");

foreach (var count in new[] { 10, 50, 100 })
{
    var r = BenchDeep(count, iterations);
    Console.WriteLine($"  5-level deep × {count} (Company>Division>Team>Project>Task)");
    Console.WriteLine($"    Serialize:   JSON {r.jSer,8:F2}ms | ASON {r.aSer,8:F2}ms | ratio {r.jSer / r.aSer:F2}x {(r.jSer >= r.aSer ? "✓ ASON faster" : "")}");
    Console.WriteLine($"    Deserialize: JSON {r.jDe,8:F2}ms | ASON {r.aDe,8:F2}ms | ratio {r.jDe / r.aDe:F2}x {(r.jDe >= r.aDe ? "✓ ASON faster" : "")}");
    Console.WriteLine($"    BIN encode:  ASON {r.bSer,8:F2}ms | ratio {r.jSer / r.bSer:F2}x vs JSON ser {(r.jSer >= r.bSer ? "✓" : "")}");
    Console.WriteLine($"    Size:        JSON {r.jB,8} B | ASON {r.aB,8} B ({(1.0 - (double)r.aB / r.jB) * 100:F0}% smaller) | BIN {r.bB} B ({(1.0 - (double)r.bB / r.jB) * 100:F0}% smaller)");
    Console.WriteLine();
}

// Section 3: Large payload
Console.WriteLine("┌──────────────────────────────────────────────┐");
Console.WriteLine("│  Section 3: Large Payload (10k records)      │");
Console.WriteLine("└──────────────────────────────────────────────┘");
{
    Console.WriteLine("  (10 iterations for large payload)");
    var r = BenchFlat(10000, 10);
    Console.WriteLine($"  Flat struct × 10000 (8 fields)");
    Console.WriteLine($"    Serialize:   JSON {r.jSer,8:F2}ms | ASON {r.aSer,8:F2}ms | ratio {r.jSer / r.aSer:F2}x {(r.jSer >= r.aSer ? "✓ ASON faster" : "")}");
    Console.WriteLine($"    Deserialize: JSON {r.jDe,8:F2}ms | ASON {r.aDe,8:F2}ms | ratio {r.jDe / r.aDe:F2}x {(r.jDe >= r.aDe ? "✓ ASON faster" : "")}");
    Console.WriteLine($"    BIN encode:  ASON {r.bSer,8:F2}ms | ratio {r.jSer / r.bSer:F2}x vs JSON ser {(r.jSer >= r.bSer ? "✓" : "")}");
    Console.WriteLine($"    Size:        JSON {r.jB,8} B | ASON {r.aB,8} B ({(1.0 - (double)r.aB / r.jB) * 100:F0}% smaller) | BIN {r.bB} B ({(1.0 - (double)r.bB / r.jB) * 100:F0}% smaller)");
}

// Section 4: Single struct roundtrip
Console.WriteLine();
Console.WriteLine("┌──────────────────────────────────────────────┐");
Console.WriteLine("│  Section 4: Single Struct Roundtrip (10000x) │");
Console.WriteLine("└──────────────────────────────────────────────┘");
{
    var user = new BUser(1, "Alice", "alice@example.com", 30, 95.5, true, "engineer", "NYC");
    var asonStr = Encoder.Encode(user);
    var jsonStr = JsonSerializer.Serialize(user);

    for (int w = 0; w < 200; w++)
    {
        Encoder.Encode(user); Decoder.DecodeWith(asonStr, BUser.FromMap);
        JsonSerializer.Serialize(user); JsonSerializer.Deserialize<BUser>(jsonStr);
    }

    // Encode only
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < 10000; i++) Encoder.Encode(user);
    var asonEncMs = sw.Elapsed.TotalMilliseconds;

    sw.Restart();
    for (int i = 0; i < 10000; i++) JsonSerializer.Serialize(user);
    var jsonEncMs = sw.Elapsed.TotalMilliseconds;

    // Decode only
    sw.Restart();
    for (int i = 0; i < 10000; i++) Decoder.DecodeWith(asonStr, BUser.FromMap);
    var asonDecMs = sw.Elapsed.TotalMilliseconds;

    sw.Restart();
    for (int i = 0; i < 10000; i++) JsonSerializer.Deserialize<BUser>(jsonStr);
    var jsonDecMs = sw.Elapsed.TotalMilliseconds;

    // Roundtrip combined
    sw.Restart();
    for (int i = 0; i < 10000; i++) { var s = Encoder.Encode(user); Decoder.DecodeWith(s, BUser.FromMap); }
    var asonMs = sw.Elapsed.TotalMilliseconds;

    sw.Restart();
    for (int i = 0; i < 10000; i++) { var s = JsonSerializer.Serialize(user); JsonSerializer.Deserialize<BUser>(s); }
    var jsonMs = sw.Elapsed.TotalMilliseconds;

    sw.Restart();
    for (int i = 0; i < 10000; i++) BinaryCodec.EncodeBinary(user);
    var binMs = sw.Elapsed.TotalMilliseconds;

    Console.WriteLine($"  Encode:    ASON {asonEncMs,8:F2}ms | JSON {jsonEncMs,8:F2}ms | ratio {jsonEncMs / asonEncMs:F2}x");
    Console.WriteLine($"  Decode:    ASON {asonDecMs,8:F2}ms | JSON {jsonDecMs,8:F2}ms | ratio {jsonDecMs / asonDecMs:F2}x");
    Console.WriteLine($"  Roundtrip: ASON {asonMs,8:F2}ms | JSON {jsonMs,8:F2}ms | ratio {jsonMs / asonMs:F2}x");
    Console.WriteLine($"  BIN enc:   ASON {binMs,8:F2}ms | ratio {jsonMs / binMs:F2}x vs JSON roundtrip");
}

// Section 5: Size comparison
Console.WriteLine();
Console.WriteLine("┌──────────────────────────────────────────────┐");
Console.WriteLine("│  Section 5: Size Comparison Summary          │");
Console.WriteLine("└──────────────────────────────────────────────┘");
{
    var users = DataGen.GenerateUsers(1000);
    var json = JsonSerializer.Serialize(users);
    var ason = Encoder.Encode<BUser>(users);
    var bin = BinaryCodec.EncodeBinary<BUser>(users);
    Console.WriteLine("  1000 flat structs:");
    Console.WriteLine($"    JSON:      {FormatBytes(json.Length)}");
    Console.WriteLine($"    ASON text: {FormatBytes(ason.Length)} ({(1.0 - (double)ason.Length / json.Length) * 100:F0}% smaller)");
    Console.WriteLine($"    ASON bin:  {FormatBytes(bin.Length)} ({(1.0 - (double)bin.Length / json.Length) * 100:F0}% smaller)");
}

Console.WriteLine();
Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║                    Benchmark Complete                        ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");

// ===========================================================================
// Helper functions
// ===========================================================================

static string FormatBytes(long b) => b >= 1_048_576 ? $"{b / 1_048_576.0:F1} MB"
    : b >= 1024 ? $"{b / 1024.0:F1} KB" : $"{b} B";

static (double jSer, double aSer, double bSer, double jDe, double aDe, int jB, int aB, int bB) BenchFlat(int count, int iterations)
{
    var users = DataGen.GenerateUsers(count);
    var json = JsonSerializer.Serialize(users);
    var ason = Encoder.Encode<BUser>(users);
    var bin = BinaryCodec.EncodeBinary<BUser>(users);

    for (int w = 0; w < 5; w++) { JsonSerializer.Serialize(users); Encoder.Encode<BUser>(users); BinaryCodec.EncodeBinary<BUser>(users); }

    var sw = Stopwatch.StartNew();
    for (int i = 0; i < iterations; i++) JsonSerializer.Serialize(users);
    var jSer = sw.Elapsed.TotalMilliseconds;

    sw.Restart();
    for (int i = 0; i < iterations; i++) Encoder.Encode<BUser>(users);
    var aSer = sw.Elapsed.TotalMilliseconds;

    sw.Restart();
    for (int i = 0; i < iterations; i++) BinaryCodec.EncodeBinary<BUser>(users);
    var bSer = sw.Elapsed.TotalMilliseconds;

    for (int w = 0; w < 5; w++) { JsonSerializer.Deserialize<List<BUser>>(json); Decoder.DecodeListWith(ason, BUser.FromMap); }

    sw.Restart();
    for (int i = 0; i < iterations; i++) JsonSerializer.Deserialize<List<BUser>>(json);
    var jDe = sw.Elapsed.TotalMilliseconds;

    sw.Restart();
    for (int i = 0; i < iterations; i++) Decoder.DecodeListWith(ason, BUser.FromMap);
    var aDe = sw.Elapsed.TotalMilliseconds;

    return (jSer, aSer, bSer, jDe, aDe, json.Length, ason.Length, bin.Length);
}

static (double jSer, double aSer, double bSer, double jDe, double aDe, int jB, int aB, int bB) BenchDeep(int count, int iterations)
{
    var companies = DataGen.GenerateCompanies(count);
    var json = JsonSerializer.Serialize(companies);
    var ason = Encoder.Encode<BCompany>(companies);
    var bin = BinaryCodec.EncodeBinary<BCompany>(companies);

    for (int w = 0; w < 5; w++) { JsonSerializer.Serialize(companies); Encoder.Encode<BCompany>(companies); BinaryCodec.EncodeBinary<BCompany>(companies); }

    var sw = Stopwatch.StartNew();
    for (int i = 0; i < iterations; i++) JsonSerializer.Serialize(companies);
    var jSer = sw.Elapsed.TotalMilliseconds;

    sw.Restart();
    for (int i = 0; i < iterations; i++) Encoder.Encode<BCompany>(companies);
    var aSer = sw.Elapsed.TotalMilliseconds;

    sw.Restart();
    for (int i = 0; i < iterations; i++) BinaryCodec.EncodeBinary<BCompany>(companies);
    var bSer = sw.Elapsed.TotalMilliseconds;

    for (int w = 0; w < 5; w++) JsonSerializer.Deserialize<List<BCompany>>(json);
    sw.Restart();
    for (int i = 0; i < iterations; i++) JsonSerializer.Deserialize<List<BCompany>>(json);
    var jDe = sw.Elapsed.TotalMilliseconds;

    for (int w = 0; w < 5; w++) Decoder.DecodeList(ason);
    sw.Restart();
    for (int i = 0; i < iterations; i++) Decoder.DecodeList(ason);
    var aDe = sw.Elapsed.TotalMilliseconds;

    return (jSer, aSer, bSer, jDe, aDe, json.Length, ason.Length, bin.Length);
}

// ===========================================================================
// Data types — explicit interface impl + zero-boxing WriteValues overrides
// ===========================================================================

record BUser(long Id, string Name, string Email, long Age, double Score, bool Active, string Role, string City) : IAsonSchema
{
    static readonly string[] _n = ["id", "name", "email", "age", "score", "active", "role", "city"];
    static readonly string?[] _t = ["int", "str", "str", "int", "float", "bool", "str", "str"];
    ReadOnlySpan<string> IAsonSchema.FieldNames => _n;
    ReadOnlySpan<string?> IAsonSchema.FieldTypes => _t;
    object?[] IAsonSchema.FieldValues => [Id, Name, Email, Age, Score, Active, Role, City];

    void IAsonSchema.WriteValues(ref AsonWriter w)
    {
        w.WriteInt(Id); w.WriteChar(',');
        w.WriteString(Name); w.WriteChar(',');
        w.WriteString(Email); w.WriteChar(',');
        w.WriteInt(Age); w.WriteChar(',');
        w.WriteDouble(Score); w.WriteChar(',');
        w.WriteBool(Active); w.WriteChar(',');
        w.WriteString(Role); w.WriteChar(',');
        w.WriteString(City);
    }

    void IAsonSchema.WriteBinaryValues(ref BinWriter bw)
    {
        bw.WriteI64(Id); bw.WriteString(Name); bw.WriteString(Email);
        bw.WriteI64(Age); bw.WriteF64(Score); bw.WriteBool(Active);
        bw.WriteString(Role); bw.WriteString(City);
    }

    public static BUser FromMap(Dictionary<string, object?> m) =>
        new(Convert.ToInt64(m["id"]), (string)m["name"]!, (string)m["email"]!,
            Convert.ToInt64(m["age"]), Convert.ToDouble(m["score"]),
            Convert.ToBoolean(m["active"]), (string)m["role"]!, (string)m["city"]!);
}

record BTask(long Id, string Title, long Priority, bool Done, double Hours) : IAsonSchema
{
    static readonly string[] _n = ["id", "title", "priority", "done", "hours"];
    static readonly string?[] _t = ["int", "str", "int", "bool", "float"];
    ReadOnlySpan<string> IAsonSchema.FieldNames => _n;
    ReadOnlySpan<string?> IAsonSchema.FieldTypes => _t;
    object?[] IAsonSchema.FieldValues => [Id, Title, Priority, Done, Hours];

    void IAsonSchema.WriteValues(ref AsonWriter w)
    {
        w.WriteInt(Id); w.WriteChar(',');
        w.WriteString(Title); w.WriteChar(',');
        w.WriteInt(Priority); w.WriteChar(',');
        w.WriteBool(Done); w.WriteChar(',');
        w.WriteDouble(Hours);
    }

    void IAsonSchema.WriteBinaryValues(ref BinWriter bw)
    {
        bw.WriteI64(Id); bw.WriteString(Title); bw.WriteI64(Priority);
        bw.WriteBool(Done); bw.WriteF64(Hours);
    }
}

record BProject(string Name, double Budget, bool Active, List<BTask> Tasks) : IAsonSchema
{
    static readonly string[] _n = ["name", "budget", "active", "tasks"];
    static readonly string?[] _t = ["str", "float", "bool", null];
    ReadOnlySpan<string> IAsonSchema.FieldNames => _n;
    ReadOnlySpan<string?> IAsonSchema.FieldTypes => _t;
    object?[] IAsonSchema.FieldValues => [Name, Budget, Active, Tasks];

    void IAsonSchema.WriteValues(ref AsonWriter w)
    {
        w.WriteString(Name); w.WriteChar(',');
        w.WriteDouble(Budget); w.WriteChar(',');
        w.WriteBool(Active); w.WriteChar(',');
        w.WriteValue(Tasks);
    }

    void IAsonSchema.WriteBinaryValues(ref BinWriter bw)
    {
        bw.WriteString(Name); bw.WriteF64(Budget); bw.WriteBool(Active);
        bw.WriteU32((uint)Tasks.Count);
        for (int i = 0; i < Tasks.Count; i++) ((IAsonSchema)Tasks[i]).WriteBinaryValues(ref bw);
    }
}

record BTeam(string Name, string Lead, long Size, List<BProject> Projects) : IAsonSchema
{
    static readonly string[] _n = ["name", "lead", "size", "projects"];
    static readonly string?[] _t = ["str", "str", "int", null];
    ReadOnlySpan<string> IAsonSchema.FieldNames => _n;
    ReadOnlySpan<string?> IAsonSchema.FieldTypes => _t;
    object?[] IAsonSchema.FieldValues => [Name, Lead, Size, Projects];

    void IAsonSchema.WriteValues(ref AsonWriter w)
    {
        w.WriteString(Name); w.WriteChar(',');
        w.WriteString(Lead); w.WriteChar(',');
        w.WriteInt(Size); w.WriteChar(',');
        w.WriteValue(Projects);
    }

    void IAsonSchema.WriteBinaryValues(ref BinWriter bw)
    {
        bw.WriteString(Name); bw.WriteString(Lead); bw.WriteI64(Size);
        bw.WriteU32((uint)Projects.Count);
        for (int i = 0; i < Projects.Count; i++) ((IAsonSchema)Projects[i]).WriteBinaryValues(ref bw);
    }
}

record BDivision(string Name, string Location, long Headcount, List<BTeam> Teams) : IAsonSchema
{
    static readonly string[] _n = ["name", "location", "headcount", "teams"];
    static readonly string?[] _t = ["str", "str", "int", null];
    ReadOnlySpan<string> IAsonSchema.FieldNames => _n;
    ReadOnlySpan<string?> IAsonSchema.FieldTypes => _t;
    object?[] IAsonSchema.FieldValues => [Name, Location, Headcount, Teams];

    void IAsonSchema.WriteValues(ref AsonWriter w)
    {
        w.WriteString(Name); w.WriteChar(',');
        w.WriteString(Location); w.WriteChar(',');
        w.WriteInt(Headcount); w.WriteChar(',');
        w.WriteValue(Teams);
    }

    void IAsonSchema.WriteBinaryValues(ref BinWriter bw)
    {
        bw.WriteString(Name); bw.WriteString(Location); bw.WriteI64(Headcount);
        bw.WriteU32((uint)Teams.Count);
        for (int i = 0; i < Teams.Count; i++) ((IAsonSchema)Teams[i]).WriteBinaryValues(ref bw);
    }
}

record BCompany(string Name, long Founded, double RevenueM, bool Public, List<BDivision> Divisions, List<string> Tags) : IAsonSchema
{
    static readonly string[] _n = ["name", "founded", "revenue_m", "public", "divisions", "tags"];
    static readonly string?[] _t = ["str", "int", "float", "bool", null, null];
    ReadOnlySpan<string> IAsonSchema.FieldNames => _n;
    ReadOnlySpan<string?> IAsonSchema.FieldTypes => _t;
    object?[] IAsonSchema.FieldValues => [Name, Founded, RevenueM, Public, Divisions, Tags];

    void IAsonSchema.WriteValues(ref AsonWriter w)
    {
        w.WriteString(Name); w.WriteChar(',');
        w.WriteInt(Founded); w.WriteChar(',');
        w.WriteDouble(RevenueM); w.WriteChar(',');
        w.WriteBool(Public); w.WriteChar(',');
        w.WriteValue(Divisions); w.WriteChar(',');
        w.WriteValue(Tags);
    }

    void IAsonSchema.WriteBinaryValues(ref BinWriter bw)
    {
        bw.WriteString(Name); bw.WriteI64(Founded); bw.WriteF64(RevenueM);
        bw.WriteBool(Public);
        bw.WriteU32((uint)Divisions.Count);
        for (int i = 0; i < Divisions.Count; i++) ((IAsonSchema)Divisions[i]).WriteBinaryValues(ref bw);
        bw.WriteU32((uint)Tags.Count);
        for (int i = 0; i < Tags.Count; i++) bw.WriteString(Tags[i]);
    }
}

static class DataGen
{
    static readonly string[] Names = ["Alice", "Bob", "Carol", "David", "Eve", "Frank", "Grace", "Hank"];
    static readonly string[] Roles = ["engineer", "designer", "manager", "analyst"];
    static readonly string[] Cities = ["NYC", "LA", "Chicago", "Houston", "Phoenix"];
    static readonly string[] Locs = ["NYC", "London", "Tokyo", "Berlin"];

    public static List<BUser> GenerateUsers(int n)
    {
        var list = new List<BUser>(n);
        for (int i = 0; i < n; i++)
            list.Add(new BUser(i, Names[i % Names.Length],
                $"{Names[i % Names.Length].ToLower()}@example.com",
                25 + i % 40, 50.0 + i % 50 + 0.5,
                i % 3 != 0, Roles[i % Roles.Length], Cities[i % Cities.Length]));
        return list;
    }

    public static List<BCompany> GenerateCompanies(int n)
    {
        var list = new List<BCompany>(n);
        for (int i = 0; i < n; i++)
        {
            var divisions = new List<BDivision>(2);
            for (int d = 0; d < 2; d++)
            {
                var teams = new List<BTeam>(2);
                for (int t = 0; t < 2; t++)
                {
                    var projects = new List<BProject>(3);
                    for (int p = 0; p < 3; p++)
                    {
                        var tasks = new List<BTask>(4);
                        for (int tk = 0; tk < 4; tk++)
                            tasks.Add(new BTask(i * 100 + d * 10 + t * 5 + tk, $"Task_{tk}",
                                tk % 3 + 1, tk % 2 == 0, 2.0 + tk * 1.5));
                        projects.Add(new BProject($"Proj_{t}_{p}", 100.0 + p * 50.5, p % 2 == 0, tasks));
                    }
                    teams.Add(new BTeam($"Team_{i}_{d}_{t}", Names[t % 4], 5 + t * 2, projects));
                }
                divisions.Add(new BDivision($"Div_{i}_{d}", Locs[d % 4], 50 + d * 20, teams));
            }
            list.Add(new BCompany($"Corp_{i}", 1990 + i % 35, 10.0 + i * 5.5, i % 2 == 0,
                divisions, new List<string> { "enterprise", "tech", $"sector_{i % 5}" }));
        }
        return list;
    }
}
