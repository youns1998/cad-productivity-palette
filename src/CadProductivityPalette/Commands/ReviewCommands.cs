using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CadProductivityPalette.Core;
using CadProductivityPalette.Services;
using AcApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CadProductivityPalette.Commands;

public sealed class ReviewCommands
{
    [CommandMethod("CADREVIEWEXPORT", CommandFlags.Modal)]
    public void ExportInteractive()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "도면 점검 보고서 저장",
            Filter = "CSV 보고서|*.csv|JSON 보고서|*.json",
            DefaultExt = ".csv",
            FileName = "drawing-review.csv",
            OverwritePrompt = true,
        };
        if (dialog.ShowDialog() == true)
            Export(dialog.FileName);
    }

    [CommandMethod("CADREVIEWBATCH", CommandFlags.Modal)]
    public void ExportBatch()
    {
        var doc = AcApplication.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;
        var result = doc.Editor.GetString(
            new PromptStringOptions("\n보고서 저장 경로(.csv 또는 .json): ") { AllowSpaces = true }
        );
        if (result.Status == PromptStatus.OK)
            Export(result.StringResult.Trim('"'));
    }

    private static void Export(string path)
    {
        var doc = AcApplication.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;
        try
        {
            var report = new DrawingStatusService().Analyze(doc).Report;
            if (report is null)
            {
                doc.Editor.WriteMessage(
                    "\n[CAD 생산성 도구] 점검에 실패해 보고서를 저장하지 않았습니다."
                );
                return;
            }
            DrawingReviewWriter.Write(report, path);
            doc.Editor.WriteMessage(
                $"\n[CAD 생산성 도구] 보고서 저장: {path} / 객체 {report.EntityCount}개 / 검토 후보 {report.CandidateCount}건"
            );
        }
        catch (System.Exception error)
        {
            doc.Editor.WriteMessage($"\n[CAD 생산성 도구] 보고서 저장 실패: {error.Message}");
        }
    }
}
