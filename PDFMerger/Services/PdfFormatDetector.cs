// PdfFormatDetector.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PDFMerger.Models;
using PdfSharp.Pdf.IO;

public sealed class PdfFormatDetector
{
    public FileInspectionResult Detect(string filePath)
    {
        try
        {
            using var doc = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
            return new FileInspectionResult
            {
                FileName = Path.GetFileName(filePath),
                FileSize = new FileInfo(filePath).Length,
                IsEncrypted = false,
                IsSupported = true,
                PageCount = doc.PageCount,
                Author = doc.Info.Author ?? "",
            };
        }
        catch (PdfReaderException ex)
        {
            if (ex.Message.Contains("password") || ex.Message.Contains("encrypted"))
            {
                return new FileInspectionResult
                {
                    FileName = Path.GetFileName(filePath),
                    FileSize = new FileInfo(filePath).Length,
                    IsSupported = true,
                    IsEncrypted = true
                };
            }
            else
            {
                return new FileInspectionResult
                {
                    IsSupported = false
                };
            }
        }
        catch (Exception)
        {
            return new FileInspectionResult
            {
                IsSupported = false
            };

        }
    }
    public FileInspectionResult Detect(string filePath, string pdfPassword)
    {
        try
        {
            using var doc = PdfReader.Open(filePath, pdfPassword, PdfDocumentOpenMode.Import);
            return new FileInspectionResult
            {
                FileName = Path.GetFileName(filePath),
                FileSize = new FileInfo(filePath).Length,
                IsEncrypted = true,
                IsSupported = true,
                PageCount = doc.PageCount,
                Author = doc.Info.Author ?? "",
                Password = pdfPassword
            };
        }
        catch (Exception)
        {
            return new FileInspectionResult
            {
                IsSupported = false
            };
        }
    }

    public FileInspectionResult LiteDetect(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new FileInspectionResult
            {
                IsSupported = false,
                ErrorCode = "FileNotFound"
            };
        }

        var result = new FileInspectionResult
        {
            FileName = Path.GetFileName(filePath),
            FileSize = new FileInfo(filePath).Length
        };

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        var header = new byte[5];
        fs.Read(header, 0, 5);
        if (!Encoding.ASCII.GetString(header).StartsWith("%PDF-"))
        {
            result.IsSupported = false;
            return result;
        }
        result.IsSupported = true;

        int tailSize = (int)Math.Min(32768, fs.Length);
        var tailBuffer = new byte[tailSize];
        fs.Seek(fs.Length - tailSize, SeekOrigin.Begin);
        fs.Read(tailBuffer, 0, tailSize);
        string tail = Encoding.ASCII.GetString(tailBuffer);

        result.IsEncrypted = tail.Contains("/Encrypt");

        var startxrefMatch = Regex.Match(tail, @"startxref\s+(\d+)");
        var rootMatch = Regex.Match(tail, @"/Root\s+(\d+)\s+0\s+R");
        var infoMatch = Regex.Match(tail, @"/Info\s+(\d+)\s+0\s+R");

        if (!startxrefMatch.Success || !rootMatch.Success)
            return result; 

        long xrefOffset = long.Parse(startxrefMatch.Groups[1].Value);
        int rootObjNum = int.Parse(rootMatch.Groups[1].Value);

        // read  xref table
        // about 23000 objects,size is 470KB, read 2MB is enough
        long xrefReadLen = Math.Min(2 * 1024 * 1024, fs.Length - xrefOffset);
        var xrefBuffer = new byte[xrefReadLen];
        fs.Seek(xrefOffset, SeekOrigin.Begin);
        int bytesRead = fs.Read(xrefBuffer, 0, (int)xrefReadLen);
        string xrefContent = Encoding.ASCII.GetString(xrefBuffer, 0, bytesRead);

        var xref = ParseXref(fs, xrefContent, xrefOffset);
        if (xref.Count == 0) return result;

        // get the root object and find the /Pages reference
        string rootObj = ReadObject(fs, xref, rootObjNum);
        var pagesMatch = Regex.Match(rootObj, @"/Pages\s+(\d+)\s+0\s+R");
        if (!pagesMatch.Success) return result;
        int pagesObjNum = int.Parse(pagesMatch.Groups[1].Value);

        // get the /Count from the /Pages object
        string pagesObj = ReadObject(fs, xref, pagesObjNum);
        var countMatch = Regex.Match(pagesObj, @"/Count\s+(\d+)");
        if (countMatch.Success)
            result.PageCount = int.Parse(countMatch.Groups[1].Value);

        // get the /Author from the /Info object
        if (infoMatch.Success)
        {
            int infoObjNum = int.Parse(infoMatch.Groups[1].Value);
            string infoObj = ReadObject(fs, xref, infoObjNum);
            var authorMatch = Regex.Match(infoObj, @"/Author\s*\((.*?)\)", RegexOptions.Singleline);
            if (authorMatch.Success)
                result.Author = DecodePdfString(authorMatch.Groups[1].Value);
        }
        return result;
    }

    private static Dictionary<int, long> ParseXrefTable(string content)
    {
        var map = new Dictionary<int, long>();
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        bool inXref = false;
        int currentObjNum = 0;

        foreach (var raw in lines)
        {
            string line = raw.Trim();
            if (line == "xref") { inXref = true; continue; }
            if (!inXref) continue;
            if (line.StartsWith("trailer")) break;

            // subsection header: "start count"
            var sub = Regex.Match(line, @"^(\d+)\s+(\d+)$");
            if (sub.Success)
            {
                currentObjNum = int.Parse(sub.Groups[1].Value);
                continue;
            }

            // 条目: "0000000016 00000 n"
            var entry = Regex.Match(line, @"^(\d{10})\s+(\d{5})\s+([nf])");
            if (entry.Success)
            {
                if (entry.Groups[3].Value == "n")
                    map[currentObjNum] = long.Parse(entry.Groups[1].Value);
                currentObjNum++;
            }
        }
        return map;
    }
    private static Dictionary<int, long> ParseXref(FileStream fs, string xrefContent, long xrefOffset)
    {
        // first try xref table
        var table = ParseXrefTable(xrefContent);
        if (table.Count > 0) return table;

        // than try xref stream
        return ParseXrefStream(fs, xrefContent, xrefOffset);
    }

    private static Dictionary<int, long> ParseXrefStream(FileStream fs, string content, long xrefOffset)
    {
        var map = new Dictionary<int, long>();

        var wMatch = Regex.Match(content, @"/W\s*\[\s*(\d+)\s+(\d+)\s+(\d+)\s*\]");
        if (!wMatch.Success) return map;
        int w1 = int.Parse(wMatch.Groups[1].Value);
        int w2 = int.Parse(wMatch.Groups[2].Value);
        int w3 = int.Parse(wMatch.Groups[3].Value);
        int entrySize = w1 + w2 + w3;

        var sizeMatch = Regex.Match(content, @"/Size\s+(\d+)");
        int totalSize = sizeMatch.Success ? int.Parse(sizeMatch.Groups[1].Value) : 0;

        var indexMatches = Regex.Matches(content, @"/Index\s*\[([^\]]+)\]");
        var indexRanges = new List<(int start, int count)>();
        if (indexMatches.Count > 0)
        {
            var nums = indexMatches[0].Groups[1].Value
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse).ToArray();
            for (int i = 0; i + 1 < nums.Length; i += 2)
                indexRanges.Add((nums[i], nums[i + 1]));
        }
        else
        {
            indexRanges.Add((0, totalSize));
        }

        int streamIdx = content.IndexOf("stream", StringComparison.Ordinal);
        if (streamIdx < 0) return map;
        int dataStart = streamIdx + 6;

        // skip newline after stream (\r\n or \n)
        if (dataStart < content.Length && content[dataStart] == '\r') dataStart++;
        if (dataStart < content.Length && content[dataStart] == '\n') dataStart++;

        int endStreamIdx = content.IndexOf("endstream", dataStart, StringComparison.Ordinal);
        if (endStreamIdx < 0) return map;

        // extract compressed data (read precisely from file stream to avoid string encoding corrupting binary)
        int compressedLen = endStreamIdx - dataStart;
        var compressed = new byte[compressedLen];
        // note: content is read from xrefOffset, dataStart is relative to content
        long compressedFileOffset = xrefOffset + dataStart;
        fs.Seek(compressedFileOffset, SeekOrigin.Begin);
        fs.Read(compressed, 0, compressedLen);

        byte[] decompressed;
        try
        {
            using var input = new MemoryStream(compressed);
            using var zlib = new System.IO.Compression.ZLibStream(input, System.IO.Compression.CompressionMode.Decompress);
            using var output = new MemoryStream();
            zlib.CopyTo(output);
            decompressed = output.ToArray();
        }
        catch
        {
            return map;
        }

        // parse entries according to /W and /Index
        int pos = 0;
        foreach (var (start, count) in indexRanges)
        {
            for (int i = 0; i < count && pos + entrySize <= decompressed.Length; i++)
            {
                int objNum = start + i;
                long f1 = ReadBigEndian(decompressed, pos, w1); pos += w1;
                long f2 = ReadBigEndian(decompressed, pos, w2); pos += w2;
                long f3 = ReadBigEndian(decompressed, pos, w3); pos += w3;

                // type 1: normal object, f2 is file offset
                // type 2: compressed object, skip (in object stream, requires extra decompression)
                if (f1 == 1)
                    map[objNum] = f2;
                // type 0 is free object, ignore
            }
        }

        return map;
    }
    private static long ReadBigEndian(byte[] data, int offset, int width)
    {
        long value = 0;
        for (int i = 0; i < width; i++)
            value = (value << 8) | data[offset + i];
        return value;
    }
    private static string ReadObject(FileStream fs, Dictionary<int, long> xref, int objNum)
    {
        if (!xref.TryGetValue(objNum, out long offset)) return "";

        int size = (int)Math.Min(32768, fs.Length - offset);
        var buf = new byte[size];
        fs.Seek(offset, SeekOrigin.Begin);
        int n = fs.Read(buf, 0, size);
        return Encoding.ASCII.GetString(buf, 0, n);
    }

    private static string DecodePdfString(string raw) =>
        raw.Replace(@"\)", ")").Replace(@"\(", "(").Replace(@"\\", @"\");
}

