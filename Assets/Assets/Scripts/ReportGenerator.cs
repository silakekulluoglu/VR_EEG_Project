using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ReportGenerator
{
    static readonly string[] BandNames  = { "Delta", "Theta", "Alpha", "Beta", "Gamma" };
    static readonly string[] BandRanges = { "0.5–4 Hz", "4–8 Hz", "8–13 Hz", "13–30 Hz", "30–100 Hz" };
    static readonly string[] BandDesc   =
    {
        "Delta waves dominate in deep sleep. Elevated delta during a task may indicate fatigue or very low arousal.",
        "Theta waves are linked to relaxed focus, memory encoding, and creative thinking. High theta is often seen during meditation or light drowsiness.",
        "Alpha waves reflect calm, idle wakefulness. They decrease (alpha suppression) when the brain actively processes information.",
        "Beta waves are associated with active thinking, alertness, and concentration. Higher beta means more mental engagement.",
        "Gamma waves are tied to high-level information processing, perception, and cognitive integration. They are less stable and can be noisy."
    };

    // Channel names in enum order (AF3=0, AF4=1, Fp1=2, Fp2=3, AF7=4, AF8=5)
    static readonly string[] ChannelNames = { "AF3", "AF4", "Fp1", "Fp2", "AF7", "AF8" };

    static bool IsBreakScene(string name) =>
        name.StartsWith("break", StringComparison.OrdinalIgnoreCase);

    public string Generate(SessionAnalyzer analyzer, string outputDir, string sessionTimestamp)
    {
        string reportPath = Path.Combine(outputDir, "report_" + sessionTimestamp + ".html");

        var sb = new System.Text.StringBuilder();

        // ── HTML head ──────────────────────────────────────────────────────────
        sb.AppendLine(@"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<title>EEG Session Report</title>
<style>
  :root {
    --purple: #6a0dad;
    --purple-light: #f4f0ff;
    --purple-mid: #d4b8f0;
    --green: #1a7a3a;
    --red: #b00;
    --text: #1a1a2e;
    --border: #ddd;
    --row-alt: #f9f4ff;
  }
  * { box-sizing: border-box; }
  body { font-family: 'Segoe UI', Arial, sans-serif; max-width: 1100px;
         margin: 40px auto; padding: 0 20px; color: var(--text); line-height: 1.6; }
  h1 { color: var(--purple); border-bottom: 3px solid var(--purple); padding-bottom: 10px; }
  h2 { color: var(--purple); margin-top: 40px; border-left: 4px solid var(--purple);
       padding-left: 12px; }
  h3 { color: #444; margin-top: 24px; }
  table { width: 100%; border-collapse: collapse; margin-top: 14px; font-size: 0.92em; }
  th { background: var(--purple); color: white; padding: 10px 12px; text-align: left; }
  td { padding: 8px 12px; border-bottom: 1px solid var(--border); }
  tr:nth-child(even) td { background: var(--row-alt); }
  .baseline-row td { background: #e8f4e8 !important; font-weight: 600; }
  .diff-pos { color: var(--red); font-weight: 600; }
  .diff-neg { color: var(--green); font-weight: 600; }
  .info-box { background: var(--purple-light); border-left: 4px solid var(--purple);
              padding: 14px 18px; margin: 16px 0; border-radius: 6px; }
  .info-box p { margin: 6px 0; }
  .info-box strong { color: var(--purple); }
  .band-grid { display: flex; flex-wrap: wrap; gap: 12px; margin-top: 12px; }
  .band-card { background: white; border: 1px solid var(--purple-mid); border-radius: 8px;
               padding: 12px 16px; flex: 1 1 180px; }
  .band-card h4 { margin: 0 0 6px; color: var(--purple); }
  .band-card .range { font-size: 0.8em; color: #888; }
  .scene-block { border: 1px solid var(--purple-mid); border-radius: 10px;
                 padding: 20px; margin: 24px 0; background: white; }
  .scene-block h3 { margin-top: 0; }
  .metric-row { display: flex; flex-wrap: wrap; gap: 12px; margin: 12px 0; }
  .metric-card { background: var(--purple-light); border-radius: 8px; padding: 10px 16px;
                 flex: 1 1 140px; text-align: center; }
  .metric-card .val { font-size: 1.5em; font-weight: 700; color: var(--purple); }
  .metric-card .name { font-size: 0.8em; color: #666; }
  .hemisphere { display: flex; gap: 20px; margin: 12px 0; }
  .hemi-box { flex: 1; text-align: center; padding: 12px; border-radius: 8px; }
  .hemi-left { background: #e8f0ff; }
  .hemi-right { background: #fff0e8; }
  .legend-note { font-size: 0.85em; color: #666; font-style: italic; }
  details.accordion { border: 1px solid var(--purple-mid); border-radius: 8px;
                      margin: 20px 0; background: white; }
  details.accordion summary { cursor: pointer; padding: 14px 18px; font-weight: 600;
                               color: var(--purple); list-style: none; display: flex;
                               align-items: center; gap: 8px; user-select: none; }
  details.accordion summary::-webkit-details-marker { display: none; }
  details.accordion summary::before { content: '▶'; font-size: 0.75em; transition: transform 0.2s; }
  details.accordion[open] summary::before { transform: rotate(90deg); }
  details.accordion .accordion-body { padding: 0 18px 18px; }
  @media print { body { margin: 10px; } .scene-block { page-break-inside: avoid; } }
</style>
</head>
<body>");

        // ── Title & meta ───────────────────────────────────────────────────────
        sb.AppendLine($"<h1>EEG Session Report</h1>");
        sb.AppendLine($"<p><strong>Session:</strong> {sessionTimestamp} &nbsp;|&nbsp; " +
                      $"<strong>Generated:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");

        // ── What does this report show? (accordion) ────────────────────────────
        sb.AppendLine(@"<details class='accordion'>
<summary>What Is This Report? (click to expand)</summary>
<div class='accordion-body'>
<div class='info-box'>
<p>This report summarises EEG (electroencephalography) data recorded during a VR experiment.
The Looxid Link headset measures electrical brain activity from <strong>6 frontal sensors</strong>:
<strong>AF3, AF4, Fp1, Fp2, AF7, AF8</strong>.</p>
<p>The recorded signals are processed into the following metrics:</p>
<ul>
<li><strong>Attention</strong> – Degree of focused, directed mental effort (0–1 scale from the Looxid SDK).</li>
<li><strong>Relaxation</strong> – Level of calm, stress-free state (0–1 scale).</li>
<li><strong>Cognitive Load</strong> – Derived as Theta/Alpha power ratio. Higher values indicate greater mental effort or task difficulty.</li>
<li><strong>Left / Right Activity</strong> – Relative power in the left vs right hemisphere frontal channels.</li>
<li><strong>Asymmetry</strong> – Difference between hemispheres. Positive values lean left-dominant; negative values lean right-dominant.</li>
<li><strong>Frequency Bands</strong> – The EEG signal is decomposed into five frequency ranges (see below).</li>
</ul>
</div>");

        // ── Band explanation cards (inside accordion) ──────────────────────────
        sb.AppendLine("<div class='band-grid'>");
        for (int b = 0; b < 5; b++)
        {
            sb.AppendLine($@"<div class='band-card'>
<h4>{BandNames[b]}</h4>
<div class='range'>{BandRanges[b]}</div>
<p style='font-size:0.85em;margin-top:6px'>{BandDesc[b]}</p>
</div>");
        }
        sb.AppendLine("</div>");
        sb.AppendLine("</div></details>"); // close accordion-body and details

        // ── Per-scene overview table ───────────────────────────────────────────
        sb.AppendLine("<h2>Per-Scene Overview</h2>");
        sb.AppendLine(@"<table>
<tr>
  <th>Scene</th>
  <th>Attention</th>
  <th>Relaxation</th>
  <th>Cognitive Load<br><span style='font-weight:normal;font-size:0.85em'>(Theta/Alpha)</span></th>
  <th>Left Activity</th>
  <th>Right Activity</th>
  <th>Asymmetry</th>
  <th>Samples</th>
</tr>");

        foreach (var stats in analyzer.sceneStatsList)
        {
            if (IsBreakScene(stats.sceneName)) continue;
            string rowClass = stats.sceneName == "BASELINE" ? "class='baseline-row'" : "";
            sb.AppendLine($@"<tr {rowClass}>
  <td><strong>{stats.sceneName}</strong></td>
  <td>{stats.AvgAttention:F3}</td>
  <td>{stats.AvgRelaxation:F3}</td>
  <td>{stats.AvgCognitiveLoad:F3}</td>
  <td>{stats.AvgLeftActivity:F3}</td>
  <td>{stats.AvgRightActivity:F3}</td>
  <td>{stats.AvgAsymmetry:F3}</td>
  <td>{stats.attention.Count}</td>
</tr>");
        }
        sb.AppendLine("</table>");
        sb.AppendLine(@"<p class='legend-note'>BASELINE row (green) is the resting-state reference.
Attention and Relaxation are Looxid SDK indexes (0–1). Cognitive Load is Theta/Alpha (higher = more demand).</p>");

        // ── Baseline comparison ────────────────────────────────────────────────
        if (analyzer.baselineStats != null)
        {
            sb.AppendLine("<h2>Baseline Comparison</h2>");
            sb.AppendLine("<div class='info-box'><p>Values show % difference from the BASELINE condition. " +
                          "<span class='diff-pos'>Red = higher than baseline</span>, " +
                          "<span class='diff-neg'>Green = lower than baseline</span>.<br>" +
                          "For <em>Attention</em> and <em>Relaxation</em>: higher is generally better for their respective states. " +
                          "For <em>Cognitive Load</em>: higher means more mental effort was required.</p></div>");

            sb.AppendLine(@"<table>
<tr>
  <th>Scene</th>
  <th>Attention Δ%</th>
  <th>Relaxation Δ%</th>
  <th>Cognitive Load Δ%</th>
  <th>Left Activity Δ%</th>
  <th>Right Activity Δ%</th>
</tr>");

            var baseline = analyzer.baselineStats;
            foreach (var stats in analyzer.sceneStatsList)
            {
                if (stats.sceneName == "BASELINE") continue;
                if (IsBreakScene(stats.sceneName)) continue;
                sb.AppendLine($@"<tr>
  <td><strong>{stats.sceneName}</strong></td>
  <td>{FormatDiff(stats.AvgAttention,     baseline.AvgAttention)}</td>
  <td>{FormatDiff(stats.AvgRelaxation,    baseline.AvgRelaxation)}</td>
  <td>{FormatDiff(stats.AvgCognitiveLoad, baseline.AvgCognitiveLoad)}</td>
  <td>{FormatDiff(stats.AvgLeftActivity,  baseline.AvgLeftActivity)}</td>
  <td>{FormatDiff(stats.AvgRightActivity, baseline.AvgRightActivity)}</td>
</tr>");
            }
            sb.AppendLine("</table>");
        }

        // ── Per-scene detailed blocks ──────────────────────────────────────────
        sb.AppendLine("<h2>Scene-by-Scene Detail</h2>");
        sb.AppendLine("<div class='info-box'><p>Each section below shows the averaged EEG metrics for one scene, " +
                      "including hemisphere activity and (when available) per-channel frequency band power.</p></div>");

        foreach (var stats in analyzer.sceneStatsList)
        {
            if (IsBreakScene(stats.sceneName)) continue;

            sb.AppendLine($"<div class='scene-block'>");
            sb.AppendLine($"<h3>{stats.sceneName}</h3>");

            // Metric cards
            sb.AppendLine("<div class='metric-row'>");
            AppendMetricCard(sb, stats.AvgAttention.ToString("F3"), "Attention");
            AppendMetricCard(sb, stats.AvgRelaxation.ToString("F3"), "Relaxation");
            AppendMetricCard(sb, stats.AvgCognitiveLoad.ToString("F3"), "Cognitive Load");
            AppendMetricCard(sb, stats.attention.Count.ToString(), "Samples");
            sb.AppendLine("</div>");

            // Hemisphere row
            sb.AppendLine($@"<div class='hemisphere'>
<div class='hemi-box hemi-left'>
  <div style='font-size:1.3em;font-weight:700'>{stats.AvgLeftActivity:F3}</div>
  <div style='font-size:0.85em;color:#555'>Left Hemisphere (AF3, Fp1, AF7)</div>
</div>
<div class='hemi-box hemi-right'>
  <div style='font-size:1.3em;font-weight:700'>{stats.AvgRightActivity:F3}</div>
  <div style='font-size:0.85em;color:#555'>Right Hemisphere (AF4, Fp2, AF8)</div>
</div>
</div>
<p style='font-size:0.85em;color:#666'>Asymmetry index: <strong>{stats.AvgAsymmetry:F3}</strong>
(positive = left-dominant, negative = right-dominant)</p>");

            // Per-channel band table (if data exists)
            if (stats.bandChannelData != null)
            {
                sb.AppendLine("<h4 style='margin-top:16px'>Frequency Band Power per Channel (averages)</h4>");
                sb.AppendLine(@"<table>
<tr>
  <th>Channel</th>
  <th>Delta (0.5–4 Hz)</th>
  <th>Theta (4–8 Hz)</th>
  <th>Alpha (8–13 Hz)</th>
  <th>Beta (13–30 Hz)</th>
  <th>Gamma (30–100 Hz)</th>
</tr>");
                for (int c = 0; c < 6; c++)
                {
                    sb.Append($"<tr><td><strong>{ChannelNames[c]}</strong></td>");
                    for (int b = 0; b < 5; b++)
                    {
                        float[] avgs = stats.AvgBandPerChannel(b);
                        float val = (avgs != null && c < avgs.Length) ? avgs[c] : 0f;
                        sb.Append($"<td>{val:F4}</td>");
                    }
                    sb.AppendLine("</tr>");
                }
                sb.AppendLine("</table>");
                sb.AppendLine(@"<p class='legend-note'>Values are relative band power (μV²).
Channels: AF3/AF4 are medial-frontal, Fp1/Fp2 are frontal-polar (center), AF7/AF8 are lateral-frontal.</p>");
            }

            sb.AppendLine("</div>"); // scene-block
        }

        sb.AppendLine("</body></html>");

        File.WriteAllText(reportPath, sb.ToString());
        return reportPath;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static void AppendMetricCard(System.Text.StringBuilder sb, string val, string name)
    {
        sb.AppendLine($"<div class='metric-card'><div class='val'>{val}</div><div class='name'>{name}</div></div>");
    }

    string FormatDiff(float value, float baseline)
    {
        if (baseline == 0f) return "<span>N/A</span>";
        float diff = ((value - baseline) / Mathf.Abs(baseline)) * 100f;
        string cssClass = diff > 0 ? "diff-pos" : "diff-neg";
        string sign = diff > 0 ? "+" : "";
        return $"<span class='{cssClass}'>{sign}{diff:F1}%</span>";
    }
}
