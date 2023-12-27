using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

static class Process_fftsg
{
    private static readonly Regex regFunctionDeclaration = new Regex(@"\s*(int|void)\s.+\(.*\)");
    private static readonly Regex regIfdefBegin = new Regex(@"#ifdef\s.+");
    private static readonly Regex regIfdefEnd = new Regex(@"#endif \/\*.+\*\/");

    public static void Run()
    {
        var lines = File.ReadAllLines("fftsg.c");
        var infos = GetLineInfos(lines).ToArray();
        using (var writer = new StreamWriter("fftsg.cs"))
        {
            writer.WriteLine("using System;");
            writer.WriteLine("");
            writer.WriteLine("namespace FftFlat");
            writer.WriteLine("{");
            writer.WriteLine("    internal static unsafe class fftsg");
            writer.WriteLine("    {");

            var previousLineWasFunctionDeclaration = false;
            var ifdefInFunctionBody = false;

            foreach (var (line, info) in lines.Zip(infos).SkipWhile(p => p.Second == LineInfo.None))
            {
                if (info == LineInfo.Include)
                {
                    continue;
                }

                if (info == LineInfo.Ifdef)
                {
                    continue;
                }

                var result = line;

                if (info == LineInfo.FunctionDeclaration)
                {
                    result = "internal static " + result;
                }

                if (info == LineInfo.FunctionBody)
                {
                    if (regFunctionDeclaration.IsMatch(line))
                    {
                        previousLineWasFunctionDeclaration = true;
                        continue;
                    }

                    if (previousLineWasFunctionDeclaration && line.Trim().Length == 0)
                    {
                        continue;
                    }

                    if (regIfdefBegin.IsMatch(line))
                    {
                        ifdefInFunctionBody = true;
                        continue;
                    }

                    if (ifdefInFunctionBody)
                    {
                        if (regIfdefEnd.IsMatch(line))
                        {
                            ifdefInFunctionBody = false;
                        }
                        continue;
                    }

                    result = result.Replace(" sin(", " Math.Sin(");
                    result = result.Replace(" -sin(", " -Math.Sin(");
                    result = result.Replace(" cos(", " Math.Cos(");
                    result = result.Replace(" atan(1.0) ", " (Math.PI / 4) ");
                }

                writer.WriteLine("        " + result);

                previousLineWasFunctionDeclaration = false;
            }

            writer.WriteLine("    }");
            writer.WriteLine("}");
        }
    }

    static IEnumerable<LineInfo> GetLineInfos(IEnumerable<string> lines)
    {
        var current = State.None;

        foreach (var line in lines)
        {
            switch (current)
            {
                case State.None:
                    if (line.Contains("/*") && line.Contains("*/"))
                    {
                        yield return LineInfo.Comment;
                        current = State.None;
                    }
                    else if (line.Contains("/*"))
                    {
                        yield return LineInfo.None;
                        current = State.LongComment;
                    }
                    else if (regFunctionDeclaration.IsMatch(line))
                    {
                        yield return LineInfo.FunctionDeclaration;
                        current = State.Function;
                    }
                    else if (regIfdefBegin.IsMatch(line))
                    {
                        yield return LineInfo.Ifdef;
                        current = State.Ifdef;
                    }
                    else if (line.StartsWith("#include"))
                    {
                        yield return LineInfo.Include;
                        current = State.None;
                    }
                    else if (line.Length == 0)
                    {
                        yield return LineInfo.None;
                        current = State.None;
                    }
                    else
                    {
                        throw new Exception();
                    }
                    break;

                case State.LongComment:
                    if (line.Contains("*/"))
                    {
                        yield return LineInfo.None;
                        current = State.None;
                    }
                    else
                    {
                        yield return LineInfo.None;
                        current = State.LongComment;
                    }
                    break;

                case State.Function:
                    if (line == "{")
                    {
                        yield return LineInfo.FunctionBegin;
                        current = State.Function;
                    }
                    else if (line == "}")
                    {
                        yield return LineInfo.FunctionEnd;
                        current = State.None;
                    }
                    else
                    {
                        yield return LineInfo.FunctionBody;
                        current = State.Function;
                    }
                    break;

                case State.Ifdef:
                    if (regIfdefEnd.IsMatch(line))
                    {
                        yield return LineInfo.Ifdef;
                        current = State.None;
                    }
                    else
                    {
                        yield return LineInfo.Ifdef;
                        current = State.Ifdef;
                    }
                    break;

                default:
                    throw new Exception();
            }
        }
    }



    enum State
    {
        None,
        LongComment,
        Function,
        Ifdef,
    }

    enum LineInfo
    {
        None,
        Comment,
        FunctionDeclaration,
        FunctionBegin,
        FunctionEnd,
        FunctionBody,
        Include,
        Ifdef,
    }
}
