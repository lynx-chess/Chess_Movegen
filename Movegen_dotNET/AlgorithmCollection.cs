﻿using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Movegen;

public record class Algorithm(bool Imported, string Name, string Description, Func<int, ulong, ulong> Queen);

public class AlgorithmCollection
{
    const int perf_poscount = 5000000;
    List<Algorithm> algorithms = [];

    //Algos that need special setup or are incremental
    static readonly HashSet<string> forbiddenImports =
        [
            "QBB","GeneticQBB", "Sissy", "Hyper", "Rotate"
        ];

    public AlgorithmCollection()
    {
        //Convert PINVOKE and Native code to delegates
        var importedClasses = typeof(Interop).GetMethods(BindingFlags.Public | BindingFlags.Static).ToArray();
        var importedAlgos = importedClasses.Select(m => m.Name.Substring(0, m.Name.Length - 8)).ToArray();
        var importedFunctions = importedClasses.Select(x => (Func<int, ulong, ulong>)Delegate.CreateDelegate(typeof(Func<int, ulong, ulong>), x)).ToArray();

        var compiledClasses = AppDomain.CurrentDomain.GetAssemblies().SelectMany(t => t.GetTypes())
                               .Where(t => t.IsClass && t.Namespace == "Movegen.Implementation" && t.GetMethod("Queen") != null).ToArray();
        var compiledAlgos = compiledClasses.Select(x => x.Name).ToArray();
        var compiledFunctions = compiledClasses.Select(x => x.GetMethod("Queen")).Select(x => (Func<int, ulong, ulong>)Delegate.CreateDelegate(typeof(Func<int, ulong, ulong>), x)).ToArray();

        for (int i = 0; i < compiledClasses.Length; i++)
        {
            algorithms.Add(new
            (
                Name: compiledAlgos[i],
                Description: "na",
                Queen: compiledFunctions[i],
                Imported: false
            ));
        }

        //Enable this for imported code
        // for (int i = 0; i < importedClasses.Length; i++)
        // {
        //     if (forbiddenImports.Contains(importedAlgos[i])) continue;
        //     algorithms.Add(new
        //     (
        //         Name: "Imported: " + importedAlgos[i],
        //         Description: "na",
        //         Queen: importedFunctions[i],
        //         Imported: true
        //     ));
        // }
    }

    void TestSqOcc(int sq, ulong occ)
    {
        ulong reference = Implementation.Switch.Queen(sq, occ);
        foreach (var algo in algorithms)
        {
            ulong result = algo.Queen(sq, occ);
            if (result != reference) throw new InvalidOperationException(algo.Name);
        }
    }

    internal void Verify()
    {
        Console.Write("Verify Engines...");
        Random r = new();
        for (int i = 0; i < 64; i++) TestSqOcc(i, 0);
        for (int i = 0; i < 64; i++) TestSqOcc(i, 510);
        for (int i = 0; i < 64; i++) TestSqOcc(i, ulong.MaxValue);
        for (int i = 0; i < 10000; i++)
        {
            int sq = r.Next() & 63;
            ulong occ = (ulong)r.NextInt64();
            TestSqOcc(sq, occ);
        }
        Console.WriteLine("OK");
    }

    void Native_Code(ulong[] occs, int[] squares)
    {
        Console.WriteLine("\nCSharp Native Code");
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            ulong bulk = 0;
            for (int i = 0; i < perf_poscount; i++)
            {
                ulong occ = occs[i]; int offset = 12 * i;
                for (int r = 0; r < 12; r++)
                {
                    bulk ^= Implementation.Switch.Queen(squares[offset + r], occ);
                }
            }
            double result = perf_poscount * 12000.0 / (stopwatch.Elapsed.TotalSeconds * 1000000000.0);
            Console.WriteLine($"{"Switch",-40} {result,-10:0.00}");
        }
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            ulong bulk = 0;
            for (int i = 0; i < perf_poscount; i++)
            {
                ulong occ = occs[i]; int offset = 12 * i;
                for (int r = 0; r < 12; r++)
                {
                    bulk ^= Implementation.Switch_JumpTable.Queen(squares[offset + r], occ);
                }
            }
            double result = perf_poscount * 12000.0 / (stopwatch.Elapsed.TotalSeconds * 1000000000.0);
            Console.WriteLine($"{"Switch JumpTable",-40} {result,-10:0.00}");
        }
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            ulong bulk = 0;
            for (int i = 0; i < perf_poscount; i++)
            {
                ulong occ = occs[i]; int offset = 12 * i;
                for (int r = 0; r < 12; r++)
                {
                    bulk ^= Implementation.ObstructionDiff.Queen(squares[offset + r], occ);
                }
            }
            double result = perf_poscount * 12000.0 / (stopwatch.Elapsed.TotalSeconds * 1000000000.0);
            Console.WriteLine($"{"ObstructionDiff",-40} {result,-10:0.00}");
        }
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            ulong bulk = 0;
            for (int i = 0; i < perf_poscount; i++)
            {
                ulong occ = occs[i]; int offset = 12 * i;
                for (int r = 0; r < 12; r++)
                {
                    bulk ^= Implementation.Leorik.Queen(squares[offset + r], occ);
                }
            }
            double result = perf_poscount * 12000.0 / (stopwatch.Elapsed.TotalSeconds * 1000000000.0);
            Console.WriteLine($"{"Leorik",-40} {result,-10:0.00}");
        }
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            ulong bulk = 0;
            for (int i = 0; i < perf_poscount; i++)
            {
                ulong occ = occs[i]; int offset = 12 * i;
                for (int r = 0; r < 12; r++)
                {
                    bulk ^= Implementation.HyperbolaQsc.Queen(squares[offset + r], occ);
                }
            }
            double result = perf_poscount * 12000.0 / (stopwatch.Elapsed.TotalSeconds * 1000000000.0);
            Console.WriteLine($"{"HyperbolaQsc",-40} {result,-10:0.00}");
        }
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            ulong bulk = 0;
            for (int i = 0; i < perf_poscount; i++)
            {
                ulong occ = occs[i]; int offset = 12 * i;
                for (int r = 0; r < 12; r++)
                {
                    bulk ^= Implementation.Lynx.Attacks.QueenAttacks(squares[offset + r], occ);
                }
            }
            double result = perf_poscount * 12000.0 / (stopwatch.Elapsed.TotalSeconds * 1000000000.0);
            Console.WriteLine($"{"Lynx",-40} {result.ToString("0.00"),-10}");
        }
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            ulong bulk = 0;
            for (int i = 0; i < perf_poscount; i++)
            {
                ulong occ = occs[i]; int offset = 12 * i;
                for (int r = 0; r < 12; r++)
                {
                    bulk ^= Implementation.FancyMagic.Queen(squares[offset + r], occ);
                }
            }
            double result = perf_poscount * 12000.0 / (stopwatch.Elapsed.TotalSeconds * 1000000000.0);
            Console.WriteLine($"{"FancyMagic",-40} {result,-10:0.00}");
        }
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            ulong bulk = 0;
            for (int i = 0; i < perf_poscount; i++)
            {
                ulong occ = occs[i]; int offset = 12 * i;
                for (int r = 0; r < 12; r++)
                {
                    bulk ^= Implementation.FancyMagic_Unsafe.Queen(squares[offset + r], occ);
                }
            }
            double result = perf_poscount * 12000.0 / (stopwatch.Elapsed.TotalSeconds * 1000000000.0);
            Console.WriteLine($"{"FancyMagic Unsafe",-40} {result,-10:0.00}");
        }
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            ulong bulk = 0;
            for (int i = 0; i < perf_poscount; i++)
            {
                ulong occ = occs[i]; int offset = 12 * i;
                for (int r = 0; r < 12; r++)
                {
                    bulk ^= Implementation.Pext.Queen(squares[offset + r], occ);
                }
            }
            double result = perf_poscount * 12000.0 / (stopwatch.Elapsed.TotalSeconds * 1000000000.0);
            Console.WriteLine($"{"Pext",-40} {result,-10:0.00}");
        }
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            ulong bulk = 0;
            for (int i = 0; i < perf_poscount; i++)
            {
                ulong occ = occs[i]; int offset = 12 * i;
                for (int r = 0; r < 12; r++)
                {
                    bulk ^= Implementation.Pext_Unsafe.Queen(squares[offset + r], occ);
                }
            }
            double result = perf_poscount * 12000.0 / (stopwatch.Elapsed.TotalSeconds * 1000000000.0);
            Console.WriteLine($"{"Pext Unsafe",-40} {result,-10:0.00}");
        }
    }

    public interface ISliderAlgorithm;
    public struct Pext : ISliderAlgorithm;
    public struct PextUnsafe : ISliderAlgorithm;
    public struct Switch : ISliderAlgorithm;
    public struct Switch_Jumptable : ISliderAlgorithm;
    public struct ObstructionDiff : ISliderAlgorithm;
    public struct Leorik : ISliderAlgorithm;
    public struct Lynx : ISliderAlgorithm;
    public struct HyperbolaQsc : ISliderAlgorithm;
    public struct FancyMagic : ISliderAlgorithm;
    public struct FancyMagicUnsafe : ISliderAlgorithm;

    static unsafe void Native_Code2(ulong[] o, int[] s)
    {
        Console.WriteLine("\nCSharp Optimized Test Loop");

        fixed (int* squares = s)
        fixed (ulong* occs = o)
        {
            ulong* occs_end = occs + o.Length;

            TestSlider<Switch>(occs, occs_end, squares);
            TestSlider<Switch_Jumptable>(occs, occs_end, squares);
            TestSlider<ObstructionDiff>(occs, occs_end, squares);
            TestSlider<Leorik>(occs, occs_end, squares);
            TestSlider<HyperbolaQsc>(occs, occs_end, squares);
            TestSlider<Lynx>(occs, occs_end, squares);
            TestSlider<FancyMagic>(occs, occs_end, squares);
            TestSlider<FancyMagicUnsafe>(occs, occs_end, squares);
            TestSlider<Pext>(occs, occs_end, squares);
            TestSlider<PextUnsafe>(occs, occs_end, squares);
        }
    }

    public class Slider<T> where T : struct, ISliderAlgorithm
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Queen(int square, ulong occ)
        {
            if (typeof(T) == typeof(Pext))
            {
                return Implementation.Pext.Queen(square, occ);
            }
            else if (typeof(T) == typeof(PextUnsafe))
            {
                return Implementation.Pext_Unsafe.Queen(square, occ);
            }
            else if (typeof(T) == typeof(FancyMagic))
            {
                return Implementation.FancyMagic.Queen(square, occ);
            }
            else if (typeof(T) == typeof(FancyMagicUnsafe))
            {
                return Implementation.FancyMagic_Unsafe.Queen(square, occ);
            }
            else if (typeof(T) == typeof(Switch))
            {
                return Implementation.Switch.Queen(square, occ);
            }
            else if (typeof(T) == typeof(Switch_Jumptable))
            {
                return Implementation.Switch_JumpTable.Queen(square, occ);
            }
            else if (typeof(T) == typeof(ObstructionDiff))
            {
                return Implementation.ObstructionDiff.Queen(square, occ);
            }
            else if (typeof(T) == typeof(Leorik))
            {
                return Implementation.Leorik.Queen(square, occ);
            }
            else if (typeof(T) == typeof(Lynx))
            {
                return Implementation.Lynx.Attacks.QueenAttacks(square, occ);
            }
            else if (typeof(T) == typeof(HyperbolaQsc))
            {
                return Implementation.HyperbolaQsc.Queen(square, occ);
            }
            else
            {
                return 0;
            }
        }
    }

    static unsafe void TestSlider<T>(ulong* occs, ulong* occs_end, int* squares) where T : struct, ISliderAlgorithm
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        ulong bulk = 0;
        while (occs != occs_end)
        {
            ulong occ = *occs++;
            bulk ^= Slider<T>.Queen(*squares++, occ);
            bulk ^= Slider<T>.Queen(*squares++, occ);
            bulk ^= Slider<T>.Queen(*squares++, occ);
            bulk ^= Slider<T>.Queen(*squares++, occ);
            bulk ^= Slider<T>.Queen(*squares++, occ);
            bulk ^= Slider<T>.Queen(*squares++, occ);
            bulk ^= Slider<T>.Queen(*squares++, occ);
            bulk ^= Slider<T>.Queen(*squares++, occ);
            bulk ^= Slider<T>.Queen(*squares++, occ);
            bulk ^= Slider<T>.Queen(*squares++, occ);
            bulk ^= Slider<T>.Queen(*squares++, occ);
            bulk ^= Slider<T>.Queen(*squares++, occ);
        }
        double result = perf_poscount * 12000.0 / (stopwatch.Elapsed.TotalSeconds * 1000000000.0);
        Console.WriteLine($"{$"*{typeof(T).Name}",-40} {result.ToString("0.00"),-10}");
    }

    void Implement_Code(ulong[] occs, int[] squares)
    {
        Console.WriteLine("\nImplementation Comparison");
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            ulong bulk = 0;
            for (int i = 0; i < perf_poscount; i++)
            {
                ulong occ = occs[i]; int offset = 12 * i;
                for (int r = 0; r < 12; r++)
                {
                    bulk ^= Implementation.Pext.Queen_NoInline(squares[offset + r], occ);
                }
            }
            double result = perf_poscount * 12000.0 / (stopwatch.Elapsed.TotalSeconds * 1000000000.0);
            Console.WriteLine($"{"Pext NoInline",-40} {result,-10:0.00}");
        }

        {
            var Queen = algorithms.First(x => x.Name == "Pext").Queen;
            Stopwatch stopwatch = Stopwatch.StartNew();
            ulong bulk = 0;
            for (int i = 0; i < perf_poscount; i++)
            {
                ulong occ = occs[i]; int offset = 12 * i;
                for (int r = 0; r < 12; r++)
                {
                    bulk ^= Queen(squares[offset + r], occ);
                }
            }
            double result = perf_poscount * 12000.0 / (stopwatch.Elapsed.TotalSeconds * 1000000000.0);
            Console.WriteLine($"{"Pext Delegate",-40} {result,-10:0.00}");
        }

        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            ulong bulk = 0;
            for (int i = 0; i < perf_poscount; i++)
            {
                ulong occ = occs[i]; int offset = 12 * i;
                for (int r = 0; r < 12; r++)
                {
                    bulk ^= Implementation.Pext.Queen(squares[offset + r], occ);
                }
            }
            double result = perf_poscount * 12000.0 / (stopwatch.Elapsed.TotalSeconds * 1000000000.0);
            Console.WriteLine($"{"Pext Inlined",-40} {result,-10:0.00}");
        }

        {
            var Attacks = Implementation.Pext.Attacks;
            var Entries = Implementation.Pext.Entries;
            Stopwatch stopwatch = Stopwatch.StartNew();
            ulong bulk = 0;
            for (int i = 0; i < perf_poscount; i++)
            {
                ulong occ = occs[i]; int offset = 12 * i;
                for (int r = 0; r < 12; r++)
                {
                    int sq = squares[offset + r];
                    bulk ^= (Attacks[Entries[sq].offset_rook + Bmi2.X64.ParallelBitExtract(occ, Entries[sq].mask_rook)] |
                        Attacks[Entries[sq].offset_bish + Bmi2.X64.ParallelBitExtract(occ, Entries[sq].mask_bish)]);
                }
            }
            double result = perf_poscount * 12000.0 / (stopwatch.Elapsed.TotalSeconds * 1000000000.0);
            Console.WriteLine($"{"Pext Manual Inlining",-40} {result,-10:0.00}");
        }

        {
            Movegen_cs.Runtime.Pext_DynamicCompilation compiler = new Movegen_cs.Runtime.Pext_DynamicCompilation();
            var Queen = compiler.Compile();
            Queen(0, 0); //Static constructor is called, asm fully loaded etc. 

            Stopwatch stopwatch = Stopwatch.StartNew();
            ulong bulk = 0;
            for (int i = 0; i < perf_poscount; i++)
            {
                ulong occ = occs[i]; int offset = 12 * i;
                for (int r = 0; r < 12; r++)
                {
                    bulk ^= Queen(squares[offset + r], occ);
                }
            }
            double result = perf_poscount * 12000.0 / (stopwatch.Elapsed.TotalSeconds * 1000000000.0);
            Console.WriteLine($"{"Pext Dynamic Compilation",-40} {result,-10:0.00}");

            compiler.Unload();
        }

        //{
        //    Stopwatch stopwatch = Stopwatch.StartNew();
        //    ulong bulk = 0;
        //    for (int i = 0; i < perf_poscount; i++)
        //    {
        //        ulong occ = occs[i]; int offset = 12 * i;
        //        for (int r = 0; r < 12; r++)
        //        {
        //            bulk ^= Interop.Pext_t_Queen(squares[offset + r], occ);
        //        }
        //    }
        //    double result = perf_poscount * 12000.0 / (stopwatch.Elapsed.TotalSeconds * 1000000000.0);
        //    Console.WriteLine($"{"Pext Pinvoke C++",-40} {result.ToString("0.00"),-10}");
        //}
    }

    void Imported_Code(ulong[] occs, int[] squares)
    {
        Console.WriteLine("\nImported Algorithmic Comparison");
        foreach (var algo in algorithms.Where(x => x.Imported == true))
        {
            var Queen = algo.Queen;
            Stopwatch stopwatch = Stopwatch.StartNew();
            ulong bulk = 0;
            for (int i = 0; i < perf_poscount; i++)
            {
                ulong occ = occs[i]; int offset = 12 * i;
                for (int r = 0; r < 12; r++)
                {
                    bulk ^= Queen(squares[offset + r], occ);
                }
            }
            double result = perf_poscount * 12000.0 / (stopwatch.Elapsed.TotalSeconds * 1000000000.0);
            Console.WriteLine($"{algo.Name,-40} {result,-10:0.00}");
        }
    }

    public static void Run()
    {
        //Prepare random 
        Random random = new(362436069);
        List<ulong> occs = [];
        List<int> squares = [];
        for (int i = 0; i < perf_poscount; i++)
        {
            occs.Add((ulong)random.NextInt64());

            for (int r = 0; r < 12; r++)
            {
                squares.Add(random.Next() % 64);
            }
        }
        ulong[] O = [.. occs];
        int[] sq = [.. squares];

        for (int i = 0; i < 5; i++)
        {
            //Native_Code(O, sq);
            //Implement_Code(O, sq);
            Native_Code2(O, sq);
            // To run this make sure to have movegen_compare.exe besides movegen_cs.exe
            // Imported_Code(O, sq);
        }
    }

    internal static void PrintHeader()
    {
        Console.WriteLine($"{"Algorithm",-40} {"Million Queen/s",-10}");
    }
}
