using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

static class Program
{
    static void Main(string[] args)
    {
        var lines = File.ReadAllLines("fft4g.c");
        var infos = GetLineInfos(lines).ToArray();
        using (var writer = new StreamWriter("fft4g.cs"))
        {
            writer.WriteLine("using System;");
            writer.WriteLine("");
            writer.WriteLine("namespace FftFlat");
            writer.WriteLine("{");
            writer.WriteLine("    internal static unsafe class fft4g");
            writer.WriteLine("    {");

            foreach (var (line, info) in lines.Zip(infos).SkipWhile(p => p.Second == LineInfo.None))
            {
                if (info == LineInfo.Include)
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
                    if (line.Trim().StartsWith("void"))
                    {
                        continue;
                    }

                    result = result.Replace(" sin(", " Math.Sin(");
                    result = result.Replace(" cos(", " Math.Cos(");
                    result = result.Replace(" atan(", " Math.Atan(");
                }

                writer.WriteLine("        " + result);
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
                    else if (line.StartsWith("void"))
                    {
                        yield return LineInfo.FunctionDeclaration;
                        current = State.Function;
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
    }
}
