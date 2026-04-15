using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using UnityEngine;

public class ReportGenerator
{
    public string Generate(SessionAnalyzer analyzer, string outputDir, string sessionTimestamp)
    {
        string reportPath = Path.Combine(outputDir, "report_" + sessionTimestamp + ".html");

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(@"<!DOCTYPE html>
<html lang='tr'>
<head>
<meta charset='UTF-8'>
<title>EEG Session Report</title>
<style>
  body { font-family: Arial, sans-serif; max-width: 900px; margin: 40px auto; color: #222; }
  h1 { color: #6a0dad; border-bottom: 2px solid #6a0dad; padding-bottom: 8px; }
  h2 { color: #444; margin-top: 32px; }
  table { width: 100%; border-collapse: collapse; margin-top: 12px; }
  th { background: #6a0dad; color: white; padding: 10px; text-align: left; }
  td { padding: 9px 10px; border-bottom: 1px solid #ddd; }
  tr:nth-child(even) { background: #f9f4ff; }
  .baseline-row { background: #e8f4e8 !important; font-weight: bold; }
  .diff-pos { color: #c00; }
  .diff-neg { color: #080; }
  .summary-box { background: #f4f0ff; border-left: 4px solid #6a0dad;
                 padding: 12px 16px; margin: 16px 0; border-radius: 4px; }
  @media print { body { margin: 20px; } }
</style>
</head>
<body>");

        sb.AppendLine($"<h1>EEG Session Report</h1>");
        sb.AppendLine($"<p><strong>Session:</strong> {sessionTimestamp}</p>");
        sb.AppendLine($"<p><strong>Generated:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");

        // ─── Per-scene averages ───
        sb.AppendLine("<h2>Per-Scene Averages</h2>");
        sb.AppendLine(@"<table>
<tr>
  <th>Scene</th>
  <th>Avg Attention</th>
  <th>Avg Relaxation</th>
  <th>Avg Cognitive Load</th>
  <th>Sample Count</th>
</tr>");

        foreach (var stats in analyzer.sceneStatsList)
        {
            string rowClass = stats.sceneName == "BASELINE" ? "class='baseline-row'" : "";
            sb.AppendLine($@"<tr {rowClass}>
  <td>{stats.sceneName}</td>
  <td>{stats.AvgAttention:F3}</td>
  <td>{stats.AvgRelaxation:F3}</td>
  <td>{stats.AvgCognitiveLoad:F3}</td>
  <td>{stats.attention.Count}</td>
</tr>");
        }
        sb.AppendLine("</table>");

        // ─── Baseline comparison ───
        if (analyzer.baselineStats != null)
        {
            sb.AppendLine("<h2>Baseline Comparison</h2>");
            sb.AppendLine("<div class='summary-box'>Values show % difference from baseline. " +
                          "<span class='diff-pos'>Red = higher than baseline</span>, " +
                          "<span class='diff-neg'>Green = lower than baseline</span>.</div>");

            sb.AppendLine(@"<table>
<tr>
  <th>Scene</th>
  <th>Attention Δ%</th>
  <th>Relaxation Δ%</th>
  <th>Cognitive Load Δ%</th>
</tr>");

            var baseline = analyzer.baselineStats;

            foreach (var stats in analyzer.sceneStatsList)
            {
                if (stats.sceneName == "BASELINE") continue;

                string attDiff  = FormatDiff(stats.AvgAttention,     baseline.AvgAttention);
                string relDiff  = FormatDiff(stats.AvgRelaxation,    baseline.AvgRelaxation);
                string cogDiff  = FormatDiff(stats.AvgCognitiveLoad, baseline.AvgCognitiveLoad);

                sb.AppendLine($@"<tr>
  <td>{stats.sceneName}</td>
  <td>{attDiff}</td>
  <td>{relDiff}</td>
  <td>{cogDiff}</td>
</tr>");
            }
            sb.AppendLine("</table>");
        }

        sb.AppendLine("</body></html>");

        File.WriteAllText(reportPath, sb.ToString());
        return reportPath;
    }

    string FormatDiff(float value, float baseline)
    {
        if (baseline == 0f) return "N/A";
        float diff = ((value - baseline) / Mathf.Abs(baseline)) * 100f;
        string cssClass = diff > 0 ? "diff-pos" : "diff-neg";
        string sign = diff > 0 ? "+" : "";
        return $"<span class='{cssClass}'>{sign}{diff:F1}%</span>";
    }
}